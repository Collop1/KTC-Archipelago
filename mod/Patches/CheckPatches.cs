using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace APKingdom2Crown;

// Hooks that report game events to CheckTracker. Every body is wrapped so a surprise in the game's
// code can never break the game itself.

[HarmonyPatch(typeof(Portal), nameof(Portal.Crumble))]
internal static class PortalCrumblePatch
{
    private static void Postfix(Portal __instance)
    {
        try
        {
            // Only "Regular" portals are the ones the player destroys; Cliff portals are liberation and
            // Dock portals vanish during the boat-arrival sequence.
            if (__instance.type != Portal.Type.Regular) return;
            CheckTracker.OnIslandThing(GameData.IslandThing.Portal, __instance.transform.position.x);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Portal patch: {e}"); }
    }
}

// Hooked on OnTriggerStay2D, not ReceiveOpenCall: ReceiveOpenCall (an RPC receiver) never fires for a
// local chest, while OnTriggerStay2D fires every physics tick the player overlaps the chest, before and
// after it's opened, hence the _wasTriggered gate and the per-instance dedupe.
[HarmonyPatch(typeof(Chest), nameof(Chest.OnTriggerStay2D))]
internal static class ChestOpenPatch
{
    // Native objects get new addresses on every island load, so remember pointer + position: a new chest
    // that happens to reuse an old address still counts.
    private static readonly Dictionary<IntPtr, float> Processed = new();

    private static void Postfix(Chest __instance)
    {
        try
        {
            if (!__instance._wasTriggered) return;
            float x = __instance.transform.position.x;
            if (Processed.TryGetValue(__instance.Pointer, out var seenX) && Mathf.Abs(seenX - x) < 0.5f) return;
            Processed[__instance.Pointer] = x;
            var thing = __instance.currencyType == CurrencyType.Gems ? GameData.IslandThing.GemChest : GameData.IslandThing.CoinChest;
            CheckTracker.OnIslandThing(thing, x);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Chest patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Statue), nameof(Statue.Pay))]
internal static class StatuePayPatch
{
    private static void Prefix(Statue __instance)
    {
        try
        {
            // Paying gems to uncover the statue is the check; later coin payments activate the blessing.
            if (__instance.deityStatus == Statue.DeityStatus.GemLocked)
                CheckTracker.OnIslandThing(GameData.IslandThing.Statue, __instance.transform.position.x);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Statue patch: {e}"); }
    }
}

[HarmonyPatch(typeof(SteedSpawn), nameof(SteedSpawn.Pay))]
internal static class SteedSpawnPayPatch
{
    private static void Postfix(SteedSpawn __instance)
    {
        try { CheckTracker.OnIslandThing(GameData.IslandThing.MountShrine, __instance.transform.position.x); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Mount shrine patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Cabin), nameof(Cabin.Pay))]
internal static class CabinPayPatch
{
    private static void Postfix(Cabin __instance)
    {
        try { CheckTracker.OnIslandThing(GameData.IslandThing.HermitCabin, __instance.transform.position.x); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Hermit cabin patch: {e}"); }
    }
}

[HarmonyPatch(typeof(DogSpawn), nameof(DogSpawn.FreeDog))]
internal static class DogSpawnFreeDogPatch
{
    private static void Postfix()
    {
        try { CheckTracker.OnDogFreed(); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Dog patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Wharf), nameof(Wharf.LaunchBoat))]
internal static class WharfLaunchBoatPatch
{
    private static void Postfix()
    {
        try { CheckTracker.OnBoatLaunched(); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Boat patch: {e}"); }
    }
}

[HarmonyPatch(typeof(WorkableTree), nameof(WorkableTree.CollapseTree))]
internal static class TreeCollapsePatch
{
    private static void Postfix(WorkableTree __instance)
    {
        try
        {
            if (GameAccess.IsPlaying) CheckTracker.OnTreeFelled(__instance.Pointer);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Tree patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Forest), nameof(Forest.RemoveAndUpdate))]
internal static class ForestRemovePatch
{
    private static void Postfix(Forest __instance)
    {
        try
        {
            // Forests also empty out while an island is being unloaded, so only count it mid-game.
            if (!GameAccess.IsPlaying) return;
            var items = __instance._forestItems;
            if (items != null && items.Count == 0) CheckTracker.OnForestCleared(__instance.Pointer);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Forest patch: {e}"); }
    }
}

// Greed kills: a Damageable that goes from alive to dead and belongs to an Enemy.
internal static class EnemyKillTracking
{
    // The damage overloads can call each other, so the same death may be seen twice in one hit.
    // Enemies are pooled and reused, so only ignore repeats for a moment rather than forever.
    private static readonly Dictionary<IntPtr, float> RecentKills = new();

    public static void Before(Damageable damageable, out bool wasDead)
    {
        wasDead = true;
        try { wasDead = damageable.isDead; }
        catch { }
    }

    public static void After(Damageable damageable, bool wasDead)
    {
        if (wasDead) return;
        try
        {
            if (!damageable.isDead) return;
            var enemy = damageable.GetComponent<Enemy>();
            if (enemy == null) return;

            float now = Time.unscaledTime;
            if (RecentKills.TryGetValue(damageable.Pointer, out var when) && now - when < 2f) return;
            RecentKills[damageable.Pointer] = now;
            if (RecentKills.Count > 256) RecentKills.Clear();

            CheckTracker.OnEnemyKilled(enemy.Type);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Kill tracking: {e}"); }
    }
}

[HarmonyPatch(typeof(Damageable), nameof(Damageable.ReceiveDamage),
    new[] { typeof(int), typeof(GameObject), typeof(DamageSource), typeof(Vector2) })]
internal static class DamageablePatch
{
    private static void Prefix(Damageable __instance, out bool __state) => EnemyKillTracking.Before(__instance, out __state);
    private static void Postfix(Damageable __instance, bool __state) => EnemyKillTracking.After(__instance, __state);
}

[HarmonyPatch(typeof(Damageable), nameof(Damageable.ReceiveFatalDamage))]
internal static class DamageableFatalPatch
{
    private static void Prefix(Damageable __instance, out bool __state) => EnemyKillTracking.Before(__instance, out __state);
    private static void Postfix(Damageable __instance, bool __state) => EnemyKillTracking.After(__instance, __state);
}
