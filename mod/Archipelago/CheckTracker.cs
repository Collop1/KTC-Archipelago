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
//  - Milestones (kills, nights, trees, ...) are counters saved in ModState. Everyday ones are counted per island
//    ("Island 3: Slay Greed 100" only counts what happens on island 3), the rarer ones over the whole campaign.
//  - Castle level, arrival, liberation, Norselands puzzles, Olympus quests and the goal are polled.
//  Call of Olympus scatters its content randomly, so there the same things are counted campaign-wide.
internal static class CheckTracker
{
    private const float PollInterval = 1f;
    private const float SamePositionTolerance = 1.5f;

    private static float _nextPoll;
    private static bool? _wasDaytime;
    private static int _dayIsland;
    // Trees a builder has worked on, by address and when. Trees also collapse for reasons that aren't the
    // player's doing (restored from a save, cleared by the level), so only these count when they fall.
    private static readonly Dictionary<IntPtr, float> WorkedTrees = new();
    private const float WorkedTreeMemory = 120f;

    // Never while playing a different campaign from the seed's: its checks would be the wrong ones.
    public static bool Tracking => (AP.Connected || !AP.State.IsOffline || ItemHandler.SimulateGating) && GameAccess.CampaignMatches;

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
        var campaign = AP.Slot.Campaign;
        // Campaign-wide counting (Olympus) still remembers positions per island, since x is per island.
        var key = campaign.CampaignWide ? $"{thing}@L{GameAccess.CurrentLand}" : $"{thing}@{island}";
        if (!state.CountedPositions.TryGetValue(key, out var positions))
            state.CountedPositions[key] = positions = new List<float>();
        if (positions.Any(p => Mathf.Abs(p - x) < SamePositionTolerance)) return;
        positions.Add(x);
        state.Dirty = true;

        int expected = GameData.ExpectedCount(campaign, island, thing);
        // Online the server knows exactly what's checked, so fill the first gap. Offline we only know
        // how many of these we've seen.
        int seen = campaign.CampaignWide
            ? state.CountedPositions.Where(e => e.Key.StartsWith($"{thing}@L")).Sum(e => e.Value.Count)
            : positions.Count;
        int start = AP.Connected ? 1 : seen;
        for (int k = start; k <= expected; k++)
        {
            var name = GameData.IslandThingLocation(campaign, island, thing, k);
            if (AP.IsChecked(name)) continue;
            AP.SendLocation(name);
            return;
        }
        var where = campaign.CampaignWide ? "this campaign" : $"Island {island}";
        Plugin.Log.LogInfo($"[AP] Extra {thing} on {where} at x={x:F0} (all {expected} checks already done)");
        Feed.Add($"<color=#AAAAAA>No more {GameData.IslandThingLabel(thing)} checks on {where}.</color>");
    }

    public static void OnDogFreed()
    {
        if (Tracking) AP.SendLocation(GameData.DogLocation(AP.Slot.Campaign));
    }

    public static void OnBoatLaunched()
    {
        if (!Tracking || AP.Slot.Campaign.CampaignWide) return;
        int island = GameAccess.CurrentIsland;
        if (island > 0) AP.SendLocation(GameData.BoatLocation(island));
    }

    // --- Milestones ---

    // The island a milestone counts on right now: the current island for island-counted kinds, 0 for campaign-wide
    // ones, -1 when it's island-counted but the island isn't known.
    public static int MilestoneIsland(GameData.Milestone milestone)
    {
        if (!AP.Slot.IsPerIsland(milestone)) return 0;
        int island = GameAccess.CurrentIsland;
        return island > 0 ? island : -1;
    }

    private static string CounterKey(GameData.Milestone milestone, int island) =>
        island > 0 ? $"{milestone}@{island}" : milestone.ToString();

    public static int MilestoneCount(GameData.Milestone milestone, int island) => AP.State.Counter(CounterKey(milestone, island));

    public static int MilestoneCount(GameData.Milestone milestone) => MilestoneCount(milestone, Math.Max(0, MilestoneIsland(milestone)));

    public static void AddMilestone(GameData.Milestone milestone, int amount = 1)
    {
        if (!Tracking) return;
        int island = MilestoneIsland(milestone);
        if (island < 0) return;
        int value = AP.State.AddCounter(CounterKey(milestone, island), amount);
        SendReachedMilestones(milestone, value, island);
    }

    private static void SendReachedMilestones(GameData.Milestone milestone, int value, int island)
    {
        var (interval, count) = AP.Slot.Milestones[milestone];
        for (int i = 1; i <= count && interval * i <= value; i++)
        {
            var name = GameData.MilestoneLocation(milestone, interval * i, island);
            if (!AP.IsChecked(name)) AP.SendLocation(name);
        }
    }

    // Next threshold to reach (on the current island for island-counted kinds), or null once they're all done.
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

    public static void OnTreeWorked(IntPtr tree)
    {
        WorkedTrees[tree] = Time.unscaledTime;
        if (WorkedTrees.Count > 256)
        {
            float cutoff = Time.unscaledTime - WorkedTreeMemory;
            foreach (var stale in WorkedTrees.Where(t => t.Value < cutoff).Select(t => t.Key).ToList())
                WorkedTrees.Remove(stale);
        }
    }

    public static void OnTreeCollapsed(IntPtr tree, float x)
    {
        if (!WorkedTrees.TryGetValue(tree, out var when)) return;
        WorkedTrees.Remove(tree); // also stops a second collapse call for the same tree counting
        if (!Tracking || Time.unscaledTime - when > WorkedTreeMemory) return;
        AddMilestone(GameData.Milestone.Trees);
        Plugin.Log.LogInfo($"[AP] Tree felled at x={x:F0} (total {MilestoneCount(GameData.Milestone.Trees)})");
    }

    // After (re)connecting, resend anything the counters already earned in case it was missed.
    public static void ResendEarned()
    {
        foreach (var milestone in AP.Slot.Milestones.Keys)
        {
            if (!AP.Slot.IsPerIsland(milestone))
            {
                SendReachedMilestones(milestone, MilestoneCount(milestone, 0), 0);
                continue;
            }
            for (int island = 1; island <= AP.Slot.IslandsInPlay; island++)
                SendReachedMilestones(milestone, MilestoneCount(milestone, island), island);
        }
        SendCounterCheck(GrassCounter, GameData.GrassTarget, GameData.GrassLocation);
    }

    // --- One-off checks ---

    public const string GrassCounter = "Grass";

    private static void SendCounterCheck(string counter, int target, string location)
    {
        if (AP.State.Counter(counter) >= target && !AP.IsChecked(location)) AP.SendLocation(location);
    }

    public static void OnGrassEaten()
    {
        if (!Tracking) return;
        AP.State.AddCounter(GrassCounter, 1);
        SendCounterCheck(GrassCounter, GameData.GrassTarget, GameData.GrassLocation);
    }

    public static void OnCoinSunk() => AddMilestone(GameData.Milestone.SunkCoins);

    public static void OnTechBuilding(TechnologyAge age)
    {
        if (!Tracking) return;
        var campaign = AP.Slot.Campaign;
        var name = age switch
        {
            TechnologyAge.Stone => GameData.QuarryLocation(campaign),
            TechnologyAge.Iron => GameData.MineLocation(campaign),
            _ => null,
        };
        Plugin.Log.LogInfo($"[AP] Tech building built: {age}");
        if (name != null && !AP.IsChecked(name)) AP.SendLocation(name);
    }

    public static void OnHermitBuilding(Hermit.HermitType type)
    {
        if (!Tracking) return;
        if (!GameData.HermitBuildingLocations.TryGetValue(type, out var name)) return;
        if (!AP.IsChecked(name)) AP.SendLocation(name);
    }

    public static void OnAnimalKilled(GameData.Milestone? kind)
    {
        if (!Tracking) return;
        AddMilestone(GameData.Milestone.Animals);
        if (kind.HasValue) AddMilestone(kind.Value);
    }

    // --- Building sanity ---
    // Buildings are noticed by polling: a *built* wall, tower, farm or citizen house at a new x position on this
    // island counts once. Whatever already stood the first time an island was looked at doesn't count, and nor do
    // upgrades or rebuilding where one stood before.
    //  - Walls and citizen houses only exist once built.
    //  - Tower spots always exist (level 0 = unbuilt, 3+ = built), so a tower counts when its spot gets a level.
    //  - Farms are the Farmhouse building (the stable hermit's stable is one too, so it's left out), not the field
    //    plots around it, which multiply as the farm grows.

    private static float _nextBuildingPoll;

    private static void PollBuildings()
    {
        if (Time.unscaledTime < _nextBuildingPoll) return;
        _nextBuildingPoll = Time.unscaledTime + 2f;
        int land = GameAccess.CurrentLand;
        if (land < 0) return;
        CountBuildings(GameData.Milestone.Walls, land, Object.FindObjectsByType<Wall>(FindObjectsSortMode.None)
            .Where(w => w != null && w.gameObject.activeInHierarchy).Select(w => w.transform.position.x));
        CountBuildings(GameData.Milestone.Towers, land, Object.FindObjectsByType<Tower>(FindObjectsSortMode.None)
            .Where(t => t != null && t.gameObject.activeInHierarchy && t.level > 0).Select(t => t.transform.position.x));
        CountBuildings(GameData.Milestone.Farms, land, Object.FindObjectsByType<Farmhouse>(FindObjectsSortMode.None)
            .Where(f => f != null && f.gameObject.activeInHierarchy && !f.isStable && f.level > 0).Select(f => f.transform.position.x));
        CountBuildings(GameData.Milestone.CitizenHouses, land, Object.FindObjectsByType<CitizenHousePayable>(FindObjectsSortMode.None)
            .Where(c => c != null && c.gameObject.activeInHierarchy).Select(c => c.transform.position.x));
    }

    private static void CountBuildings(GameData.Milestone kind, int land, IEnumerable<float> positions)
    {
        var state = AP.State;
        var key = $"Built:{kind}@L{land}";
        bool firstLook = !state.CountedPositions.TryGetValue(key, out var known);
        if (firstLook) state.CountedPositions[key] = known = new List<float>();
        foreach (var x in positions)
        {
            if (known.Any(p => Mathf.Abs(p - x) < SamePositionTolerance)) continue;
            known.Add(x);
            state.Dirty = true;
            if (firstLook) continue;
            AddMilestone(kind);
            Plugin.Log.LogInfo($"[AP] New {kind} at x={x:F0} (total {MilestoneCount(kind)})");
        }
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
        try { PollPuzzles(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollPuzzles: {e.Message}"); }
        try { PollQuests(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollQuests: {e.Message}"); }
        try { PollBuildings(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] PollBuildings: {e.Message}"); }
        try { StolenGems.Poll(); } catch (Exception e) { Plugin.Log.LogDebug($"[AP] StolenGems: {e.Message}"); }
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
            var where = AP.Slot.IsPerIsland(GameData.Milestone.Nights) && island > 0 ? $" on Island {island}" : "";
            Feed.Add($"<color=#FFD27F>Dawn breaks. Nights survived{where}: {nights}</color>");
            StolenGems.OnNightSurvived();
        }
        _wasDaytime = day;
        _dayIsland = island;
    }

    private static void PollIsland()
    {
        if (AP.Slot.Campaign.CampaignWide)
        {
            if (GameData.OlympusIslands.TryGetValue(GameAccess.CurrentLevelConfigName, out var olympusIsland))
            {
                var reach = GameData.OlympusReachLocation(olympusIsland);
                if (!AP.IsChecked(reach)) AP.SendLocation(reach);
            }
            return;
        }
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

    public static int LiberatedIslands()
    {
        var campaign = AP.Slot.Campaign;
        if (campaign.CampaignWide) return 0;
        return Enumerable.Range(1, campaign.IslandCount).Count(GameAccess.IsIslandSecured);
    }

    private static void PollLiberation()
    {
        var campaign = AP.Slot.Campaign;
        if (campaign.CampaignWide) return;
        int liberated = 0;
        for (int island = 1; island <= campaign.IslandCount; island++)
        {
            if (!GameAccess.IsIslandSecured(island)) continue;
            liberated++;
            var name = GameData.LiberateLocation(island);
            if (!AP.IsChecked(name)) AP.SendLocation(name);
        }
        if (liberated >= AP.Slot.GoalIslands && AP.Connected) AP.ReportGoal();
    }

    // Something that was already done the first time we looked at it (an old save, say) never sends a
    // check by itself; only finishing it while tracking does. The first look is remembered per seed.
    private static bool BecameDone(string key, bool done)
    {
        var baselineKey = "baseline:" + key;
        if (!AP.State.Counters.ContainsKey(baselineKey))
        {
            AP.State.Counters[baselineKey] = done ? 1 : 0;
            AP.State.Dirty = true;
            if (done) Plugin.Log.LogInfo($"[AP] {key} was already done when tracking started; not sending it");
            return false;
        }
        return done && AP.State.Counters[baselineKey] == 0;
    }

    // Norselands: solving a god's puzzle unlocks its Item of Power in the campaign save.
    private static void PollPuzzles()
    {
        var campaign = AP.Slot.Campaign;
        var save = CampaignSaveData.current;
        if (save == null) return;
        foreach (var (island, content) in campaign.Islands)
        {
            if (content.Puzzle == null) continue;
            var power = GameData.PuzzlePowers[content.Puzzle];
            bool unlocked = save.GetRulerItemStatus(power).lockState == Player.LockState.Unlocked;
            var name = GameData.PuzzleLocation(island, content.Puzzle);
            if (BecameDone("power:" + power, unlocked) && !AP.IsChecked(name))
            {
                Feed.Add($"<color=#00FF7F>{content.Puzzle}'s puzzle solved!</color>");
                AP.SendLocation(name);
            }
        }
    }

    // Call of Olympus: quests, including the final battle against the World Serpent (the goal).
    private static void PollQuests()
    {
        if (!AP.Slot.Campaign.CampaignWide) return;
        foreach (var (quest, name) in GameData.OlympusQuests)
        {
            bool done;
            try { done = QuestManager.GetQuestCompleted(quest); }
            catch { continue; }
            if (!BecameDone("quest:" + quest, done) || AP.IsChecked(name)) continue;
            AP.SendLocation(name);
            if (name == GameData.OlympusGoalLocation && AP.Connected) AP.ReportGoal();
        }
    }
}
