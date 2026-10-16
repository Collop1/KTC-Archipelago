using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace APKingdom2Crown;

// Items of Power (Norselands' god items, Call of Olympus' god gifts) can't be used until the matching
// "Power of ..." item has been received. Every power class overrides CanActivate, so each one is patched.
[HarmonyPatch]
internal static class PowerCanActivatePatch
{
    private static readonly Dictionary<string, float> LastWarning = new();

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var type in new[]
                 {
                     typeof(ThorItem), typeof(HeimdalItem), typeof(HelsHead), typeof(LokiStaff),
                     typeof(ArtemisBow), typeof(MedusaShield), typeof(HephaestusHammer), typeof(HermesStaff),
                 })
        {
            var method = AccessTools.DeclaredMethod(type, nameof(ItemOfPower.CanActivate));
            if (method != null) yield return method;
        }
    }

    private static void Postfix(ItemOfPower __instance, ref bool __result)
    {
        try
        {
            if (!__result || !Gating.Active) return;
            var item = GameData.PowerItem(AP.Slot.Campaign, __instance.itemType);
            if (item == null || ItemHandler.Has(item)) return;
            __result = false;

            float now = Time.unscaledTime;
            if (LastWarning.TryGetValue(item, out var when) && now - when < 20f) return;
            LastWarning[item] = now;
            Feed.Add($"<color=#AAAAAA>{AP.Escape(item)} hasn't been found yet, so this power can't be used.</color>");
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Power patch: {e}"); }
    }
}

// Dead Lands: freeing a caged ruler at a ruler statue is a check.
[HarmonyPatch(typeof(UnlockNewRulerStatue), nameof(UnlockNewRulerStatue.Pay))]
internal static class RulerStatuePayPatch
{
    private static void Postfix(UnlockNewRulerStatue __instance)
    {
        try { CheckTracker.OnIslandThing(GameData.IslandThing.RulerStatue, __instance.transform.position.x); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Ruler statue patch: {e}"); }
    }
}
