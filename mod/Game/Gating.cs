using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace APKingdom2Crown;

// Blocks payments the player hasn't earned yet by toggling the game's own Payable.forceBlockPayment:
//  - castle upgrades beyond the Progressive Castle count
//  - activating a statue's blessing, paying a freed hermit, or riding a mount without its item
//  - using an Item of Power without its "Power of ..." item (patched in PowerPatches)
//  - setting sail when no other island is unlocked, and sailing past the seed's last island (Only Goal Islands)
// The first payment on statues/shrines/cabins (the gem cost) is always allowed, since that's the check.
// Also keeps the kingdom's stone/iron tech in line with the Stone/Iron Technology items.
internal static class Gating
{
    private const float Interval = 0.5f;

    private static float _next;
    private static readonly Dictionary<IntPtr, Payable> Blocked = new();

    // Off while playing a different campaign from the seed's, so a wrong campaign isn't locked up.
    public static bool Active => ItemHandler.GatingActive && GameAccess.CampaignMatches;

    public static bool IslandGatingActive => Active && (AP.Slot.UsesIslandUnlocks || AP.Slot.IslandsLimited);

    // Whether the boat may take you to this island. Without island unlocks the game still opens islands in order;
    // this only ever forbids, so islands past the seed's last one stay shut.
    public static bool IsIslandUnlocked(int island)
    {
        if (island <= 1 || !IslandGatingActive) return true;
        if (island > AP.Slot.IslandsInPlay) return false;
        return !AP.Slot.UsesIslandUnlocks || ItemHandler.Has(GameData.IslandUnlock(island));
    }

    public static int HighestUnlockedIsland()
    {
        for (int island = AP.Slot.IslandsInPlay; island >= 1; island--)
            if (IsIslandUnlocked(island)) return island;
        return 1;
    }

    public static bool CanSailSomewhere()
    {
        int current = GameAccess.CurrentIsland;
        for (int island = 1; island <= AP.Slot.IslandsInPlay; island++)
            if (island != current && IsIslandUnlocked(island)) return true;
        return false;
    }

    public static void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Interval;

        if (!Active)
        {
            ReleaseAll();
            return;
        }
        if (!GameAccess.IsPlaying) return;
        PurgeDestroyed();

        try { ApplyTech(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating tech: {e.Message}"); }
        try { ApplyCastle(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating castle: {e.Message}"); }
        try { ApplyStatues(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating statues: {e.Message}"); }
        try { ApplyHermits(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating hermits: {e.Message}"); }
        try { ApplySteeds(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating steeds: {e.Message}"); }
        try { ApplyBoat(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating boat: {e.Message}"); }
    }

    private static void ApplyTech()
    {
        var kingdom = GameAccess.Kingdom;
        if (kingdom == null) return;
        bool stone = ItemHandler.Has(GameData.StoneTech);
        bool iron = ItemHandler.Has(GameData.IronTech);
        if (kingdom.StoneBuildingUnlocked != stone) kingdom.StoneBuildingUnlocked = stone;
        if (kingdom.IronBuildingUnlocked != iron) kingdom.IronBuildingUnlocked = iron;
    }

    // Castle1 -> Castle2 needs one Progressive Castle, ..., Castle6 -> Castle7 needs six.
    public static bool CastleUpgradeAllowed(int castleLevel) => ItemHandler.Count(GameData.ProgressiveCastle) >= castleLevel;

    private static void ApplyCastle()
    {
        var castle = GameAccess.Kingdom?.castle;
        if (castle == null) return;
        var upgrade = castle._payableUpgrade;
        if (upgrade == null) upgrade = castle.GetComponent<PayableUpgrade>();
        if (upgrade == null) return;
        SetBlocked(upgrade, !CastleUpgradeAllowed(GameAccess.CastleLevel));
    }

    private static void ApplyStatues()
    {
        foreach (var statue in Object.FindObjectsByType<Statue>(FindObjectsSortMode.None))
        {
            if (statue == null) continue;
            var item = GameData.StatueItem(AP.Slot.Campaign, statue.deity);
            bool block = statue.deityStatus != Statue.DeityStatus.GemLocked && item != null && !ItemHandler.Has(item);
            SetBlocked(statue, block);
        }
    }

    private static void ApplyHermits()
    {
        foreach (var hermit in Object.FindObjectsByType<Hermit>(FindObjectsSortMode.None))
        {
            if (hermit == null) continue;
            var item = GameData.HermitItem(AP.Slot.Campaign, hermit.Type);
            bool block = item != null && !ItemHandler.Has(item);
            SetBlocked(hermit, block);
        }
    }

    private static void ApplySteeds()
    {
        foreach (var steed in Object.FindObjectsByType<Steed>(FindObjectsSortMode.None))
        {
            if (steed == null) continue;
            var item = GameData.MountItem(AP.Slot.Campaign, steed.steedType);
            bool block = item != null && !ItemHandler.Has(item);
            SetBlocked(steed, block);
        }
    }

    private static void ApplyBoat()
    {
        if (!IslandGatingActive) return;
        bool block = !CanSailSomewhere();
        foreach (var sail in Object.FindObjectsByType<BoatSailPosition>(FindObjectsSortMode.None))
            if (sail != null) SetBlocked(sail, block);
    }

    // Only ever clears blocks we set ourselves, so the game's own uses of forceBlockPayment survive.
    private static void SetBlocked(Payable payable, bool block)
    {
        var ptr = payable.Pointer;
        if (block)
        {
            if (Blocked.ContainsKey(ptr)) return;
            if (payable.forceBlockPayment) return; // the game already blocks it
            payable.forceBlockPayment = true;
            Blocked[ptr] = payable;
        }
        else if (Blocked.Remove(ptr))
        {
            payable.forceBlockPayment = false;
        }
    }

    // Objects from an unloaded island are gone; drop them so a new object reusing the address isn't skipped.
    private static void PurgeDestroyed()
    {
        List<IntPtr> dead = null;
        foreach (var entry in Blocked)
        {
            bool alive;
            try { alive = entry.Value != null; }
            catch { alive = false; }
            if (!alive) (dead ??= new List<IntPtr>()).Add(entry.Key);
        }
        if (dead != null)
            foreach (var ptr in dead) Blocked.Remove(ptr);
    }

    private static void ReleaseAll()
    {
        if (Blocked.Count == 0) return;
        foreach (var payable in Blocked.Values)
        {
            try
            {
                if (payable != null) payable.forceBlockPayment = false;
            }
            catch { /* destroyed with its scene */ }
        }
        Blocked.Clear();
    }

    public static int BlockedCount => Blocked.Count;
}
