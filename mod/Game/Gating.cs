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
//  - buying a bomb at the castle's banners on an island without its "Island N Bombs" (bomb unlocks)
//  - buying Hermes's sack in Call of Olympus: never with Progressive Coin Sacks, otherwise not without "Hermes Sack"
// The first payment on statues/shrines/cabins (the gem cost) is always allowed, since that's the check.
// Also keeps the kingdom's stone/iron tech in line with the Stone/Iron Technology items, and the purse's size in line
// with the Progressive Coin Sacks received.
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
            RestorePurse();
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
        try { ApplySailingMap(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating map: {e.Message}"); }
        try { ApplyBombs(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating bombs: {e.Message}"); }
        try { ApplyHermesSack(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating Hermes sack: {e.Message}"); }
        try { ApplyPurse(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] Gating purse: {e.Message}"); }
    }

    // Seeds from before the sack items (slot data 1-7) have no Hermes Sack to receive, so it's left alone there.
    private static bool SackItemsInSeed => AP.Slot.SlotDataVersion == 0 || AP.Slot.SlotDataVersion >= 8;

    public static bool HermesSackAllowed() =>
        !Active || !SackItemsInSeed || (AP.Slot.ProgressiveSacks == 0 && ItemHandler.Has(GameData.HermesSack));

    // The sack shrine pays through a PayableComponent beside it rather than being a Payable itself.
    private static void ApplyHermesSack()
    {
        bool block = !HermesSackAllowed();
        foreach (var shrine in Object.FindObjectsByType<CurrencyBagPayable>(FindObjectsSortMode.None))
        {
            if (shrine == null || shrine.UnlocksCurrencyBagType != CurrencyBagType.Hermes) continue;
            var payable = shrine.GetComponent<PayableComponent>() ?? shrine.GetComponentInChildren<PayableComponent>();
            if (payable != null) SetBlocked(payable, block);
        }
    }

    // --- Progressive Coin Sacks ---
    // The sack has no number for its capacity: every coin and gem in it is a little physics body, and whatever doesn't
    // fit falls out. So the sack is made smaller or bigger by resizing what's inside it: bigger coins fill it sooner.
    // It holds 0.5x its usual amount with no sacks, rising in equal steps to 3x with all of them. Measured in game (the
    // usual sack settles at about 50 coins and gems): coins 1.6x as big settled at 20, 0.47x as big at 205, so
    // capacity goes with size^-1.9, close to the coins' area.

    private const float SizePerCapacity = -1f / 1.9f;
    private static float _coinSize = 1f; // what the coins in the sack are currently resized to
    private static int _resizedCount = -1;

    public static int SacksReceived => Math.Min(ItemHandler.Count(GameData.ProgressiveCoinSack), AP.Slot.ProgressiveSacks);

    // How much the sack holds compared with normal.
    public static float PurseScale => AP.Slot.ProgressiveSacks <= 0 ? 1f : 0.5f + 2.5f * SacksReceived / AP.Slot.ProgressiveSacks;

    public static float CoinSize => _coinSize;

    private static CurrencyBag PlayerSack => GameAccess.Player?.wallet?._currencyBag;

    private static void ApplyPurse()
    {
        float size = AP.Slot.ProgressiveSacks > 0 ? Mathf.Pow(PurseScale, SizePerCapacity) : 1f;
        if (size == 1f && _coinSize == 1f) return;
        var sack = PlayerSack;
        if (sack == null) return;
        // Coins come and go all the time; only walk the sack when its contents or the size changed.
        int count = sack._currencyObjects.Count;
        if (size == _coinSize && count == _resizedCount) return;
        ResizeCoins(sack, size, 0);
        _coinSize = size;
        _resizedCount = count;
    }

    // CurrencyBag.SpawnCurrency postfix: a coin entering the sack takes the sack's size straight away, rather than
    // up to half a second later, when growing it could knock other coins out.
    public static void OnCoinEnteredSack(CurrencyBag sack)
    {
        if (_coinSize == 1f) return;
        int count = sack._currencyObjects.Count;
        ResizeCoins(sack, _coinSize, Math.Max(0, count - 4)); // the newest are at the end
    }

    private static void ResizeCoins(CurrencyBag sack, float size, int from)
    {
        var coins = sack._currencyObjects;
        var want = new Vector3(size, size, 1f);
        for (int i = from; i < coins.Count; i++)
        {
            var coin = coins[i];
            if (coin == null) continue;
            var t = coin.transform;
            if (t.localScale != want) t.localScale = want;
        }
    }

    private static void RestorePurse()
    {
        if (_coinSize == 1f) return;
        try
        {
            var sack = PlayerSack;
            if (sack != null) ResizeCoins(sack, 1f, 0);
        }
        catch { /* gone with its scene */ }
        _coinSize = 1f;
        _resizedCount = -1;
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

    // Islands unlocked by items have to be on the sailing map even if the base game wouldn't have opened them yet
    // (the map only offers islands up to the furthest one reached). Being further along than the current island is
    // the normal state after sailing back to an earlier island, so this is safe to do at any time.
    private static void ApplySailingMap()
    {
        if (!IslandGatingActive || !AP.Slot.UsesIslandUnlocks) return;
        var campaign = CampaignSaveData.current;
        if (campaign == null) return;
        int wanted = GameAccess.IslandToLand(HighestUnlockedIsland());
        if (campaign.furthestUnlockedLand >= wanted) return;
        Plugin.Log.LogInfo($"[AP] furthestUnlockedLand {campaign.furthestUnlockedLand} -> {wanted}");
        campaign.furthestUnlockedLand = wanted;
    }

    public static bool BombsAllowedHere()
    {
        if (!Active || !AP.Slot.BombUnlocks || AP.Slot.Campaign.CampaignWide) return true;
        int island = GameAccess.CurrentIsland;
        return island <= 0 || ItemHandler.Has(GameData.IslandBombs(island));
    }

    private static void ApplyBombs()
    {
        bool block = !BombsAllowedHere();
        foreach (var banner in Object.FindObjectsByType<PayableBombPurchase>(FindObjectsSortMode.None))
            if (banner != null) SetBlocked(banner, block);
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
