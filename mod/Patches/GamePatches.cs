using System;
using HarmonyLib;
using UnityEngine;

namespace APKingdom2Crown;

// Losing the game (crown carried off) is a death for DeathLink.
[HarmonyPatch(typeof(Game), nameof(Game.Lose))]
internal static class GameLosePatch
{
    private static void Prefix()
    {
        try
        {
            // Don't echo a death that a received DeathLink just caused.
            if (Time.unscaledTime - ItemEffects.LastDeathLinkEffectTime < 90f) return;
            AP.SendDeathLink($"{AP.SlotName} lost their crown to the Greed.");
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Lose patch: {e}"); }
    }
}

// While a menu text box has focus, keystrokes are for the text box, not the monarch.
[HarmonyPatch(typeof(Player), nameof(Player.IControllable_ReceiveInput))]
internal static class PlayerInputPatch
{
    private static bool Prefix() => !TextInput.AnyFocused;
}

[HarmonyPatch(typeof(Game), nameof(Game.CheckForPause))]
internal static class PauseInputPatch
{
    private static bool Prefix() => !TextInput.AnyFocused;
}

// Waves Gone By: waves on an island spawn as if extra days had passed there.
[HarmonyPatch(typeof(EnemyManager), nameof(EnemyManager.SpawnWave))]
internal static class SpawnWavePatch
{
    private static int _logged;

    private static void Prefix(ref int targetDay, ref int multiplierDay, ref int daysOnCurrentIsland)
    {
        try
        {
            int offset = AP.State.WaveOffset(GameAccess.CurrentIsland);
            if (_logged < 20)
            {
                _logged++;
                Plugin.Log.LogInfo($"[AP] SpawnWave targetDay={targetDay} multiplierDay={multiplierDay} daysOnIsland={daysOnCurrentIsland} offset={offset}");
            }
            if (offset <= 0 || !ItemHandler.GatingActive) return;
            targetDay += offset;
            multiplierDay += offset;
            daysOnCurrentIsland += offset;
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] SpawnWave patch: {e}"); }
    }
}

// --- Island unlocks: only islands you've received can be picked on the sailing map. ---

[HarmonyPatch(typeof(Map), nameof(Map.TrySelectLand))]
internal static class MapSelectLandPatch
{
    private static bool Prefix(int land, ref bool __result)
    {
        try
        {
            if (Gating.IsIslandUnlocked(GameAccess.LandToIsland(land))) return true;
            __result = false;
            return false;
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[AP] Map select patch: {e}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(MapLand), nameof(MapLand.SetUnlocked))]
internal static class MapLandUnlockedPatch
{
    private static void Prefix(MapLand __instance, ref bool unlocked)
    {
        try
        {
            if (Gating.IslandGatingActive) unlocked = Gating.IsIslandUnlocked(GameAccess.LandToIsland(__instance.land));
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Map land patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Game), nameof(Game.SailAway))]
internal static class SailAwayPatch
{
    // Make sure islands unlocked by items are reachable on the map even if the base game wouldn't have
    // unlocked them yet.
    private static void Prefix()
    {
        try
        {
            if (!Gating.IslandGatingActive) return;
            var campaign = CampaignSaveData.current;
            if (campaign == null) return;
            int wanted = GameAccess.IslandToLand(Gating.HighestUnlockedIsland());
            if (campaign.furthestUnlockedLand < wanted)
            {
                Plugin.Log.LogInfo($"[AP] furthestUnlockedLand {campaign.furthestUnlockedLand} -> {wanted}");
                campaign.furthestUnlockedLand = wanted;
            }
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Sail away patch: {e}"); }
    }
}

// Safety net: if the game still tries to sail somewhere locked, go to the nearest unlocked island instead.
[HarmonyPatch(typeof(Game), nameof(Game.SetCurrentLand))]
internal static class SetCurrentLandPatch
{
    private static void Prefix(Game __instance, ref int land)
    {
        try
        {
            if (!Gating.IslandGatingActive || __instance.state != Game.State.SailingAway) return;
            int target = GameAccess.LandToIsland(land);
            if (Gating.IsIslandUnlocked(target)) return;

            int current = GameAccess.CurrentIsland;
            int best = 0;
            for (int distance = 1; distance < GameData.IslandCount && best == 0; distance++)
            {
                foreach (var candidate in new[] { target + distance, target - distance })
                {
                    if (candidate < 1 || candidate > GameData.IslandCount || candidate == current) continue;
                    if (!Gating.IsIslandUnlocked(candidate)) continue;
                    best = candidate;
                    break;
                }
            }
            if (best == 0) best = current > 0 ? current : 1;
            Plugin.Log.LogWarning($"[AP] Island {target} is locked; sailing to island {best} instead");
            Feed.Add($"<color=#FA8072>Island {target} is still locked. Sailing to Island {best} instead.</color>");
            land = GameAccess.IslandToLand(best);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Set land patch: {e}"); }
    }
}

[HarmonyPatch(typeof(BoatSailPosition), nameof(BoatSailPosition.CanPay))]
internal static class BoatSailCanPayPatch
{
    private static void Postfix(ref bool __result)
    {
        try
        {
            if (__result && Gating.IslandGatingActive && !Gating.CanSailSomewhere()) __result = false;
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Boat sail patch: {e}"); }
    }
}
