using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace APKingdom2Crown;

// Decides which location to send when something happens in the game.
//  - Per-island things (portals, chests, statues, shrines, hermit cabins) are progressive: the next
//    unchecked "Island N: <thing> k" is sent. Each physical object is remembered by its x position so
//    the same chest/portal can never count twice.
//  - Milestones (kills, nights, trees, forests) are counters saved in ModState.
//  - Castle level, arrival, liberation and the goal are polled.
internal static class CheckTracker
{
    private const float PollInterval = 1f;
    private const float SamePositionTolerance = 1.5f;

    private static float _nextPoll;
    private static float _nextFullUpgradePoll;
    private static bool? _wasDaytime;
    private static int _dayIsland;
    // A tree or forest can report its end twice (local call + network echo). Objects are reused and
    // addresses recycled, so only ignore repeats for a short while.
    private static readonly Dictionary<IntPtr, float> CountedTrees = new();
    private static readonly Dictionary<IntPtr, float> CountedForests = new();
    private const float RepeatWindow = 10f;

    private static bool FirstReport(Dictionary<IntPtr, float> seen, IntPtr ptr)
    {
        float now = Time.unscaledTime;
        if (seen.TryGetValue(ptr, out var when) && now - when < RepeatWindow) return false;
        seen[ptr] = now;
        if (seen.Count > 512) seen.Clear();
        return true;
    }

    public static bool Tracking => AP.Connected || !AP.State.IsOffline || ItemHandler.SimulateGating;

    // --- Per-island progressive checks ---

    public static void OnIslandThing(GameData.IslandThing thing, float x)
    {
        if (!Tracking) return;
        int island = GameAccess.CurrentIsland;
        if (island == 0)
        {
            Plugin.Log.LogWarning($"[AP] {thing} happened but the current island is unknown");
            return;
        }

        var state = AP.State;
        var key = $"{thing}@{island}";
        if (!state.CountedPositions.TryGetValue(key, out var positions))
            state.CountedPositions[key] = positions = new List<float>();
        if (positions.Any(p => Mathf.Abs(p - x) < SamePositionTolerance)) return;
        positions.Add(x);
        state.Dirty = true;

        int expected = GameData.ExpectedCount(island, thing);
        // Online the server knows exactly what's checked, so fill the first gap. Offline we only know
        // how many of these we've seen on this island.
        int start = AP.Connected ? 1 : positions.Count;
        for (int k = start; k <= expected; k++)
        {
            var name = GameData.IslandThingLocation(island, thing, k);
            if (AP.IsChecked(name)) continue;
            AP.SendLocation(name);
            return;
        }
        Plugin.Log.LogInfo($"[AP] Extra {thing} on island {island} at x={x:F0} (all {expected} checks already done)");
        Feed.Add($"<color=#AAAAAA>No more {GameData.IslandThingLabel(thing)} checks on Island {island}.</color>");
    }

    public static void OnDogFreed()
    {
        if (Tracking) AP.SendLocation(GameData.DogLocation);
    }

    public static void OnBoatLaunched()
    {
        if (!Tracking) return;
        int island = GameAccess.CurrentIsland;
        if (island > 0) AP.SendLocation(GameData.BoatLocation(island));
    }

    // --- Milestones ---

    public static int MilestoneCount(GameData.Milestone milestone) => AP.State.Counter(milestone.ToString());

    public static void AddMilestone(GameData.Milestone milestone, int amount = 1)
    {
        if (!Tracking) return;
        int value = AP.State.AddCounter(milestone.ToString(), amount);
        SendReachedMilestones(milestone, value);
    }

    private static void SendReachedMilestones(GameData.Milestone milestone, int value)
    {
        var (interval, count) = AP.Slot.Milestones[milestone];
        for (int i = 1; i <= count && interval * i <= value; i++)
        {
            var name = GameData.MilestoneLocation(milestone, interval * i);
            if (!AP.IsChecked(name)) AP.SendLocation(name);
        }
    }

    // Next threshold to reach, or null once every milestone check of this kind is done.
    public static int? NextMilestone(GameData.Milestone milestone)
    {
        var (interval, count) = AP.Slot.Milestones[milestone];
        int value = MilestoneCount(milestone);
        for (int i = 1; i <= count; i++)
            if (interval * i > value) return interval * i;
        return null;
    }

    public static void OnEnemyKilled(EnemyType type)
    {
        if (!Tracking || type == EnemyType.Object) return;
        AddMilestone(GameData.Milestone.Greed);
        var category = GameData.KillCategory(type);
        if (category.HasValue) AddMilestone(category.Value);
        Plugin.Log.LogDebug($"[AP] Killed {type}");
    }

    public static void OnTreeFelled(IntPtr tree)
    {
        if (!Tracking || !FirstReport(CountedTrees, tree)) return;
        AddMilestone(GameData.Milestone.Trees);
    }

    public const string ForestCounter = "Forests";

    public static void OnForestCleared(IntPtr forest)
    {
        if (!Tracking || !FirstReport(CountedForests, forest)) return;
        int cleared = AP.State.AddCounter(ForestCounter, 1);
        Feed.Add("<color=#00FF7F>A whole forest has been cleared!</color>");
        SendForests(cleared);
    }

    private static void SendForests(int cleared)
    {
        for (int k = 1; k <= Math.Min(cleared, AP.Slot.ForestChecks); k++)
        {
            var name = GameData.ForestLocation(k);
            if (!AP.IsChecked(name)) AP.SendLocation(name);
        }
    }

    // After (re)connecting, resend anything the counters already earned in case it was missed.
    public static void ResendEarned()
    {
        foreach (var milestone in AP.Slot.Milestones.Keys)
            SendReachedMilestones(milestone, MilestoneCount(milestone));
        SendForests(AP.State.Counter(ForestCounter));
    }

    // --- Polling ---

    public static void Update()
    {
        if (!Tracking) return;
        if (!GameAccess.IsPlaying)
        {
            _wasDaytime = null; // don't count a dawn that happened across a load or island change
            return;
        }
        if (Time.unscaledTime < _nextPoll) return;
        _nextPoll = Time.unscaledTime + PollInterval;

        try { PollNights(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollNights: {e.Message}"); }
        try { PollIsland(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollIsland: {e.Message}"); }
        try { PollCastle(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollCastle: {e.Message}"); }
        try { PollLiberation(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollLiberation: {e.Message}"); }

        if (Time.unscaledTime >= _nextFullUpgradePoll)
        {
            _nextFullUpgradePoll = Time.unscaledTime + 5f;
            try { PollFullUpgrade(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollFullUpgrade: {e.Message}"); }
        }
    }

    private static void PollNights()
    {
        var director = GameAccess.Director;
        if (director == null) return;
        bool day = director.IsDaytime;
        int island = GameAccess.CurrentIsland;
        if (_wasDaytime.HasValue && _dayIsland == island && !_wasDaytime.Value && day)
        {
            AddMilestone(GameData.Milestone.Nights);
            int nights = MilestoneCount(GameData.Milestone.Nights);
            Feed.Add($"<color=#FFD27F>Dawn breaks. Nights survived: {nights}</color>");
        }
        _wasDaytime = day;
        _dayIsland = island;
    }

    private static void PollIsland()
    {
        int island = GameAccess.CurrentIsland;
        if (island >= 2 && !AP.IsChecked(GameData.ArriveLocation(island))) AP.SendLocation(GameData.ArriveLocation(island));
    }

    private static void PollCastle()
    {
        int level = GameAccess.CastleLevel;
        for (int l = 2; l <= Math.Min(level, 7); l++)
        {
            var name = GameData.CastleLevelLocation(l);
            if (!AP.IsChecked(name)) AP.SendLocation(name);
        }
    }

    private static void PollLiberation()
    {
        int liberated = 0;
        for (int island = 1; island <= GameData.IslandCount; island++)
        {
            if (!GameAccess.IsIslandSecured(island)) continue;
            liberated++;
            var name = GameData.LiberateLocation(island);
            if (!AP.IsChecked(name)) AP.SendLocation(name);
        }
        if (liberated >= AP.Slot.IslandsToLiberate && AP.Connected) AP.ReportGoal();
    }

    // An island is fully upgraded once the castle is at its last level and no built wall or tower can be
    // upgraded any further.
    public static (bool Done, string Detail) FullUpgradeStatus()
    {
        var kingdom = GameAccess.Kingdom;
        if (kingdom == null) return (false, "no kingdom");
        int castle = GameAccess.CastleLevel;
        int walls = 0, wallsLeft = 0, towers = 0, towersLeft = 0;
        foreach (var wall in Object.FindObjectsByType<Wall>(FindObjectsSortMode.None))
        {
            if (wall == null || !wall.gameObject.activeInHierarchy) continue;
            walls++;
            var upgrade = wall.GetComponent<PayableUpgrade>();
            if (upgrade != null && upgrade.HasUpgrade()) wallsLeft++;
        }
        foreach (var tower in Object.FindObjectsByType<Tower>(FindObjectsSortMode.None))
        {
            if (tower == null || !tower.gameObject.activeInHierarchy) continue;
            towers++;
            var upgrade = tower.GetComponent<PayableUpgrade>();
            if (upgrade != null && upgrade.HasUpgrade()) towersLeft++;
        }
        bool done = castle >= 7 && walls >= 2 && wallsLeft == 0 && towersLeft == 0;
        return (done, $"castle {castle}/7, walls upgradeable {wallsLeft}/{walls}, towers upgradeable {towersLeft}/{towers}");
    }

    private static void PollFullUpgrade()
    {
        if (AP.IsChecked(GameData.FullUpgradeLocation)) return;
        if (GameAccess.CastleLevel < 7) return;
        var (done, detail) = FullUpgradeStatus();
        if (!done) return;
        Plugin.Log.LogInfo($"[AP] Island fully upgraded: {detail}");
        AP.SendLocation(GameData.FullUpgradeLocation);
    }
}
