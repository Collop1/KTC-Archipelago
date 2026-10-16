using System;
using System.Collections.Generic;
using System.Text.Json;

namespace APKingdom2Crown;

// Options the apworld sends in slot_data (see apworld world.fill_slot_data). Defaults match the apworld
// defaults so the debug tab behaves sensibly before connecting.
internal sealed class SlotSettings
{
    public int SlotDataVersion;
    public int IslandsToLiberate = 5;
    public bool IslandUnlocks = true;
    public int ForestChecks = 1;
    public int WavesGoneByDays = 10;
    public int VagrantsPerItem = 2;
    public bool DeathLink;
    public int DeathLinkEffect; // 0 crown, 1 wallet, 2 both

    public readonly Dictionary<GameData.Milestone, (int Interval, int Count)> Milestones = new()
    {
        [GameData.Milestone.Greed] = (20, 10),
        [GameData.Milestone.Greedlings] = (25, 6),
        [GameData.Milestone.Floaters] = (5, 4),
        [GameData.Milestone.Breeders] = (2, 3),
        [GameData.Milestone.CrownStealers] = (2, 3),
        [GameData.Milestone.Nights] = (5, 8),
        [GameData.Milestone.Trees] = (10, 10),
    };

    public static SlotSettings Default => new();

    public static SlotSettings FromSlotData(Dictionary<string, object> data)
    {
        var s = new SlotSettings();
        if (data == null) return s;
        s.SlotDataVersion = Int(data, "slot_data_version", 0);
        s.IslandsToLiberate = Int(data, "islands_to_liberate", s.IslandsToLiberate);
        s.IslandUnlocks = Int(data, "island_unlocks", 1) != 0;
        s.ForestChecks = Int(data, "forest_checks", s.ForestChecks);
        s.WavesGoneByDays = Int(data, "waves_gone_by_days", s.WavesGoneByDays);
        s.VagrantsPerItem = Int(data, "vagrants_per_item", s.VagrantsPerItem);
        s.DeathLink = Int(data, "death_link", 0) != 0;
        s.DeathLinkEffect = Int(data, "death_link_effect", 0);

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
