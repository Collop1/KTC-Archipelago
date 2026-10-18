using System;
using System.Collections.Generic;
using System.Text.Json;

namespace APKingdom2Crown;

// Options the apworld sends in slot_data (see apworld world.fill_slot_data). Defaults match the apworld
// defaults so the debug tab behaves sensibly before connecting.
internal sealed class SlotSettings
{
    public int SlotDataVersion;
    public GameData.Campaign Campaign = GameData.Campaigns[0];
    public int IslandsToLiberate = 6;
    public int PlayIslands; // islands 1..N are in the seed; 0 = every island (Olympus, or older seeds)
    public bool IslandUnlocks = true;
    public int WavesGoneByDays = 10;
    public int VagrantsPerItem = 2;
    public bool DeathLink;
    public int DeathLinkEffect = 2; // 0 crown, 1 wallet, 2 both (3 = "harsh" from older seeds, same as both)
    public int StolenGemsReturnNights = 10;
    public int BadKingAmount = 3;

    public readonly Dictionary<GameData.Milestone, (int Interval, int Count)> Milestones = new()
    {
        // The apworld defaults for five islands to liberate.
        [GameData.Milestone.Greed] = (100, 15),
        [GameData.Milestone.Greedlings] = (100, 10),
        [GameData.Milestone.Floaters] = (10, 5),
        [GameData.Milestone.Breeders] = (3, 5),
        [GameData.Milestone.CrownStealers] = (3, 5),
        [GameData.Milestone.Nights] = (5, 10),
        [GameData.Milestone.Trees] = (10, 10),
        [GameData.Milestone.Walls] = (1, 33),
        [GameData.Milestone.Towers] = (1, 25),
        [GameData.Milestone.Farms] = (1, 8),
        [GameData.Milestone.CitizenHouses] = (1, 8),
        [GameData.Milestone.Villagers] = (10, 15),
        [GameData.Milestone.Archers] = (5, 10),
        [GameData.Milestone.Builders] = (5, 5),
        [GameData.Milestone.Farmers] = (3, 5),
        [GameData.Milestone.Knights] = (2, 5),
        [GameData.Milestone.Animals] = (100, 15),
        [GameData.Milestone.Deer] = (10, 10),
        [GameData.Milestone.Rabbits] = (50, 0),
        [GameData.Milestone.Boars] = (5, 0),
        [GameData.Milestone.Bribes] = (10, 10),
        [GameData.Milestone.CoinsSpent] = (100, 15),
    };

    public static SlotSettings Default => new();

    // Islands that must be liberated for the goal, capped at what the campaign has.
    public int GoalIslands => System.Math.Min(IslandsToLiberate, Campaign.IslandCount);

    // Island unlock items only exist for campaigns with numbered islands.
    public bool UsesIslandUnlocks => IslandUnlocks && !Campaign.CampaignWide;

    // How many numbered islands this seed uses (Only Goal Islands): the boat never goes past the last of them.
    public int IslandsInPlay => PlayIslands > 0 ? Math.Min(PlayIslands, Campaign.IslandCount) : Campaign.IslandCount;
    public bool IslandsLimited => !Campaign.CampaignWide && IslandsInPlay < Campaign.IslandCount;

    public static SlotSettings FromSlotData(Dictionary<string, object> data)
    {
        var s = new SlotSettings();
        if (data == null) return s;
        s.SlotDataVersion = Int(data, "slot_data_version", 0);
        if (data.TryGetValue("campaign", out var campaign) && campaign != null)
            s.Campaign = GameData.CampaignByKey(campaign.ToString());
        s.IslandsToLiberate = Int(data, "islands_to_liberate", s.IslandsToLiberate);
        s.PlayIslands = Int(data, "play_islands", 0);
        s.IslandUnlocks = Int(data, "island_unlocks", 1) != 0;
        s.WavesGoneByDays = Int(data, "waves_gone_by_days", s.WavesGoneByDays);
        s.VagrantsPerItem = Int(data, "vagrants_per_item", s.VagrantsPerItem);
        s.DeathLink = Int(data, "death_link", 0) != 0;
        s.DeathLinkEffect = Int(data, "death_link_effect", s.DeathLinkEffect);
        s.StolenGemsReturnNights = Int(data, "stolen_gems_return_nights", s.StolenGemsReturnNights);
        s.BadKingAmount = Int(data, "bad_king_amount", s.BadKingAmount);

        if (data.TryGetValue("milestones", out var raw) && raw != null)
        {
            try
            {
                // The value is a Newtonsoft JObject; its ToString() is plain JSON, so read it with System.Text.Json
                // rather than taking a compile-time dependency on Newtonsoft.
                using var doc = JsonDocument.Parse(raw.ToString());
                foreach (var milestone in s.Milestones.Keys)
                {
                    if (!doc.RootElement.TryGetProperty(GameData.Milestones[milestone].SlotKey, out var entry)) continue;
                    int interval = entry.GetProperty("interval").GetInt32();
                    int count = entry.GetProperty("count").GetInt32();
                    s.Milestones[milestone] = (Math.Max(1, interval), Math.Max(0, count));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[AP] Could not read milestone settings from slot data: {e.Message}");
            }
        }
        return s;
    }

    private static int Int(Dictionary<string, object> data, string key, int fallback)
    {
        if (!data.TryGetValue(key, out var value) || value == null) return fallback;
        try
        {
            return value is bool b ? (b ? 1 : 0) : Convert.ToInt32(value);
        }
        catch
        {
            return bool.TryParse(value.ToString(), out var parsed) ? (parsed ? 1 : 0) : fallback;
        }
    }
}
