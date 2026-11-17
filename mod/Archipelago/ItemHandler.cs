using System;
using System.Collections.Generic;
using UnityEngine;

namespace APKingdom2Crown;

// Turns received items into game effects.
//  - Unlock items (castle tiers, tech, islands, statues, hermits, mounts) are never "applied": the gating
//    code just asks Has()/Count() against everything received so far.
//  - Coins, gems, vagrants and traps are applied once each, in server order. ModState.ItemsApplied
//    remembers how far we got so reconnecting doesn't hand them out again.
internal static class ItemHandler
{
    private const float TrapSpacingSeconds = 3f;

    // Items granted from the Debug tab; they count towards unlocks exactly like received items.
    public static readonly Dictionary<string, int> DebugGrants = new();

    // Debug tab switch: apply the AP gating rules even while not connected.
    public static bool SimulateGating;

    private static readonly Queue<(string Item, string Source)> Effects = new();
    private static float _nextTrapTime;

    private static readonly Dictionary<string, int> CountCache = new();
    private static int _cachedReceived = -1;
    private static int _cachedGrants = -1;
    private static int _grantVersion;

    // Gating applies while connected and keeps applying after a disconnect (we still know what was
    // received), but never in an unconnected vanilla game unless the debug switch is on.
    public static bool GatingActive => SimulateGating || AP.Connected || !AP.State.IsOffline;

    public static int Count(string itemName)
    {
        if (_cachedReceived != AP.Received.Count || _cachedGrants != _grantVersion) RebuildCounts();
        return CountCache.TryGetValue(itemName, out var n) ? n : 0;
    }

    public static bool Has(string itemName) => Count(itemName) > 0;

    private static void RebuildCounts()
    {
        CountCache.Clear();
        foreach (var item in AP.Received)
            CountCache[item.Name] = (CountCache.TryGetValue(item.Name, out var n) ? n : 0) + 1;
        foreach (var grant in DebugGrants)
            CountCache[grant.Key] = (CountCache.TryGetValue(grant.Key, out var n) ? n : 0) + grant.Value;
        _cachedReceived = AP.Received.Count;
        _cachedGrants = _grantVersion;
    }

    public static void DebugGive(string itemName)
    {
        if (GameData.KindOf(itemName) == GameData.ItemKind.Unlock)
        {
            DebugGrants[itemName] = (DebugGrants.TryGetValue(itemName, out var n) ? n : 0) + 1;
            _grantVersion++;
            Feed.Add($"<color=#C9A0FF>[Debug] Granted {AP.Escape(itemName)}</color>");
            return;
        }
        Effects.Enqueue((itemName, "Debug"));
        _nextTrapTime = 0f; // debug traps fire straight away
    }

    public static void ClearDebugGrants()
    {
        DebugGrants.Clear();
        _grantVersion++;
    }

    public static void QueueDeathLink(string source, string cause)
    {
        var who = string.IsNullOrEmpty(source) ? "Someone" : source;
        Feed.Add($"<color=#FA8072>DeathLink: {AP.Escape(who)} died{(string.IsNullOrEmpty(cause) ? "" : $" ({AP.Escape(cause)})")}</color>");
        Effects.Enqueue((AP.Slot.DeathLinkEffect switch
        {
            0 => DeathLinkCrown,
            1 => DeathLinkWallet,
            _ => DeathLinkBoth, // 2, or 3 from seeds made when "harsh" was its own choice
        }, "DeathLink"));
    }

    // Internal effect names for DeathLinks (not items).
    private const string DeathLinkCrown = "DeathLink: Crown";
    private const string DeathLinkWallet = "DeathLink: Wallet";
    private const string DeathLinkBoth = "DeathLink: Both";

    private static bool IsDeathLink(string effect) => effect is DeathLinkCrown or DeathLinkWallet or DeathLinkBoth;

    public static void DebugDeathLink()
    {
        Effects.Enqueue((DeathLinkBoth, "Debug"));
        _nextTrapTime = 0f;
    }

    public static int PendingEffects => Effects.Count;

    public static void Update()
    {
        // Hold everything while a different campaign from the seed's is loaded, so it isn't spent there.
        if (!GameAccess.IsPlaying || !GameAccess.CampaignMatches) return;

        if (AP.Connected)
        {
            var state = AP.State;
            while (state.ItemsApplied < AP.Received.Count)
            {
                var item = AP.Received[state.ItemsApplied];
                if (GameData.KindOf(item.Name) != GameData.ItemKind.Unlock)
                    Effects.Enqueue((item.Name, item.Sender));
                state.ItemsApplied++;
                state.Dirty = true;
            }
        }

        while (Effects.Count > 0)
        {
            var (itemName, source) = Effects.Peek();
            bool isTrap = GameData.KindOf(itemName) == GameData.ItemKind.Trap || IsDeathLink(itemName);
            if (isTrap && Time.unscaledTime < _nextTrapTime) break;
            Effects.Dequeue();
            try
            {
                Apply(itemName, source);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[AP] Applying {itemName} failed: {e}");
            }
            if (isTrap)
            {
                _nextTrapTime = Time.unscaledTime + TrapSpacingSeconds;
                break;
            }
        }
    }

    private static void Apply(string itemName, string source)
    {
        switch (itemName)
        {
            case GameData.GemPouch:
                ItemEffects.GiveCurrency(CurrencyType.Gems, GameData.GemsPerPouch);
                return;
            case GameData.Coins:
                ItemEffects.GiveCurrency(CurrencyType.Coins, UnityEngine.Random.Range(3, 8));
                return;
            case GameData.Gem:
                ItemEffects.GiveCurrency(CurrencyType.Gems, 1);
                return;
            case GameData.Vagrants:
                ItemEffects.SpawnVagrants(AP.Slot.VagrantsPerItem);
                return;
            case GameData.TuftOfGrass:
                ItemEffects.FeedSteed();
                return;
            case GameData.SunkenGold:
                ItemEffects.SunkenGold();
                return;
            case GameData.GreedMaskShard:
                ItemEffects.GreedMaskShard();
                return;
            case GameData.TripTrap:
                ItemEffects.Trip(source);
                return;
            case GameData.SlipperyPouchTrap:
                ItemEffects.SlipperyPouch(source);
                return;
            case GameData.WavesGoneByTrap:
                ItemEffects.WavesGoneBy(source, AP.Slot.WavesGoneByDays);
                return;
            case GameData.DamnedNightTrap:
                ItemEffects.DamnedNight(source);
                return;
            case GameData.DecayTrap:
                ItemEffects.Decay(source);
                return;
            case GameData.StamDamTrap:
                ItemEffects.StamDam(source);
                return;
            case GameData.DesertionTrap:
                ItemEffects.Desertion(source, AP.Slot.BadKingAmount);
                return;
            case GameData.ToolTheftTrap:
                ItemEffects.ToolTheft(source, AP.Slot.BadKingAmount);
                return;
            case GameData.RedMoonTrap:
                ItemEffects.RedMoon(source);
                return;
            case DeathLinkCrown:
                ItemEffects.DeathLink(source, crown: true, wallet: false);
                return;
            case DeathLinkWallet:
                ItemEffects.DeathLink(source, crown: false, wallet: true);
                return;
            case DeathLinkBoth:
                ItemEffects.DeathLink(source, crown: true, wallet: true);
                return;
        }
        if (Array.IndexOf(GameData.JunkItems, itemName) >= 0)
        {
            ItemEffects.Junk(itemName);
            return;
        }
        Plugin.Log.LogInfo($"[AP] No effect for item '{itemName}'");
    }
}
