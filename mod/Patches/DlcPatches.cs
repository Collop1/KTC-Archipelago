using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace APKingdom2Crown;

// Items of Power (Norselands' god items, Call of Olympus' god gifts) can't be used until the matching
// "Power of ..." item has been received. Every power class overrides CanActivate, so each one is patched.
[HarmonyPatch]
internal static class PowerCanActivatePatch
{
    private static readonly Dictionary<string, float> LastWarning = new();

    // Several of these overrides compile to identical machine code, which the game's build merges into one function
    // (Thor's, Hel's and the four Olympus gods' all share one). Hooking that function once per class chained the
    // hooks into each other, and using any of those powers crashed the game with a stack overflow. So each piece of
    // game code is hooked once; the postfix works out which power it is from the instance either way.
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var hooked = new HashSet<IntPtr>();
        foreach (var type in new[]
                 {
                     typeof(ThorItem), typeof(HeimdalItem), typeof(HelsHead), typeof(LokiStaff),
                     typeof(ArtemisBow), typeof(MedusaShield), typeof(HephaestusHammer), typeof(HermesStaff),
                 })
        {
            var method = AccessTools.DeclaredMethod(type, nameof(ItemOfPower.CanActivate));
            if (method == null) continue;
            var code = NativeCode(method);
            if (code == IntPtr.Zero)
            {
                // Can't tell whether it's shared, and hooking shared code twice crashes; better ungated than that.
                Plugin.Log.LogWarning($"[AP] Not gating {type.Name}: couldn't find its game code");
                continue;
            }
            if (!hooked.Add(code)) continue;
            yield return method;
        }
        Plugin.Log.LogInfo($"[AP] Items of Power: {hooked.Count} distinct CanActivate functions hooked");
    }

    private static IntPtr NativeCode(MethodBase method)
    {
        try
        {
            var field = Il2CppInterop.Common.Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
            var info = (IntPtr)field.GetValue(null);
            return info == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(info); // MethodInfo starts with its code pointer
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Power code lookup for {method.DeclaringType?.Name}: {e.Message}");
            return IntPtr.Zero;
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
