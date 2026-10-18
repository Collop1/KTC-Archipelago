using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APKingdom2Crown;

// Progress that the server doesn't track for us, saved per seed + slot: milestone counters, which
// chests/portals/etc. were already counted on each island, how many received items have been applied,
// and checks waiting to be sent while disconnected.
internal sealed class ModState
{
    public string Seed { get; set; } = "";
    public string Slot { get; set; } = "";

    // Number of received items (in server order) whose effects were already applied to the game.
    public int ItemsApplied { get; set; }

    public Dictionary<string, int> Counters { get; set; } = new();

    // "Portal@3" -> x positions of the portals already counted on island 3
    public Dictionary<string, List<float>> CountedPositions { get; set; } = new();

    // island number -> extra wave days from Waves Gone By traps
    public Dictionary<int, int> WaveOffsets { get; set; } = new();

    public List<string> PendingLocations { get; set; } = new();

    // Gems the Greed stole on a DeathLink, waiting to be won back (see StolenGems).
    public List<StolenGemsEntry> StolenGems { get; set; } = new();

    [JsonIgnore] public bool Dirty;

    [JsonIgnore] public bool IsOffline => Seed.Length == 0;

    public int Counter(string key) => Counters.TryGetValue(key, out var v) ? v : 0;

    public int AddCounter(string key, int amount)
    {
        var v = Counter(key) + amount;
        Counters[key] = v;
        Dirty = true;
        return v;
    }

    public int WaveOffset(int island) => WaveOffsets.TryGetValue(island, out var v) ? v : 0;

    private static string PathFor(string seed, string slot) =>
        System.IO.Path.Combine(Storage.Folder, "saves", Storage.SafeFileName($"{seed}_{slot}.json"));

    public static ModState LoadFor(string seed, string slot)
    {
        var state = Storage.Load<ModState>(PathFor(seed, slot));
        state.Seed = seed;
        state.Slot = slot;
        return state;
    }

    public void Save()
    {
        Dirty = false;
        if (IsOffline) return; // debug-only progress before ever connecting isn't worth keeping
        Storage.Save(PathFor(Seed, Slot), this);
    }
}

internal sealed class StolenGemsEntry
{
    public int Island { get; set; }
    public int Gems { get; set; }
    public int NightsLeft { get; set; }
}
