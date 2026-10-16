using System.Collections.Generic;

namespace APKingdom2Crown;

// Item and location names, mirroring apworld/kingdom_two_crowns/items.py and locations.py.
// The mod never hard-codes ids: they come from the server's data package, so only the names
// here need to match the apworld.
internal static class GameData
{
    public const string GameName = "Kingdom Two Crowns";

    // --- Items ---
    public const string ProgressiveCastle = "Progressive Castle";
    public const string StoneTech = "Stone Technology";
    public const string IronTech = "Iron Technology";
    public const int MaxCastleUpgrades = 6;

    public static string IslandUnlock(int island) => $"Island {island} Unlock";

    public static readonly Dictionary<Statue.Deity, string> StatueItems = new()
    {
        [Statue.Deity.Archer] = "Statue of Archery",
        [Statue.Deity.Worker] = "Statue of Building",
        [Statue.Deity.Knight] = "Statue of Knights",
        [Statue.Deity.Farmer] = "Statue of Farming",
    };

    public static readonly Dictionary<Hermit.HermitType, string> HermitItems = new()
    {
        [Hermit.HermitType.Ballista] = "Ballista Hermit",
        [Hermit.HermitType.Baker] = "Bakery Hermit",
        [Hermit.HermitType.Horn] = "Horn Hermit",
        [Hermit.HermitType.Horse] = "Stable Hermit",
        [Hermit.HermitType.Knight] = "Warrior Hermit",
    };

    public static readonly Dictionary<SteedType, string> MountItems = new()
    {
        [SteedType.P1Griffin] = "Griffin Mount",
        [SteedType.P2Griffin] = "Griffin Mount",
        [SteedType.Stag] = "Stag Mount",
        [SteedType.P2Stag] = "Stag Mount",
        [SteedType.P1Warhorse] = "Warhorse Mount",
        [SteedType.P2Warhorse] = "Warhorse Mount",
        [SteedType.Bear] = "Bear Mount",
        [SteedType.Lizard] = "Lizard Mount",
        [SteedType.Unicorn] = "Unicorn Mount",
        [SteedType.HorseStamina] = "Stamina Horse",
        [SteedType.HorseBurst] = "Burst Horse",
        [SteedType.HorseFast] = "Fast Horse",
    };

    public const string CoinPouch = "Coin Pouch";
    public const string CoinSack = "Coin Sack";
    public const string Gem = "Gem";
    public const string Vagrants = "Vagrants";

    public static readonly string[] JunkItems =
    {
        "Sunken Gold", "Broken Crown", "Tuft of Grass", "Greed Mask Shard", "Soggy Bread", "Bent Arrow", "Lost Banner",
    };

    public const string TripTrap = "Trip Trap";
    public const string SlipperyPouchTrap = "Slippery Pouch Trap";
    public const string WaveBackTrap = "Wave Back Trap";
    public const string WavesGoneByTrap = "Waves Gone By Trap";
    public const string DamnedNightTrap = "Damned Night Trap";
    public const string DecayTrap = "Decay Trap";

    public static readonly string[] Traps =
    {
        TripTrap, SlipperyPouchTrap, WaveBackTrap, WavesGoneByTrap, DamnedNightTrap, DecayTrap,
    };

    public enum ItemKind { Unlock, Filler, Junk, Trap }

    public sealed class ItemGroup
    {
        public string Title;
        public ItemKind Kind;
        public string[] Names;
    }

    // Every item, grouped for the menu (Items and Debug tabs).
    public static readonly ItemGroup[] ItemGroups =
    {
        new() { Title = "Buildings", Kind = ItemKind.Unlock, Names = new[] { ProgressiveCastle, StoneTech, IronTech } },
        new() { Title = "Islands", Kind = ItemKind.Unlock, Names = new[] { IslandUnlock(2), IslandUnlock(3), IslandUnlock(4), IslandUnlock(5) } },
        new() { Title = "Statues", Kind = ItemKind.Unlock, Names = new[] { "Statue of Archery", "Statue of Building", "Statue of Knights", "Statue of Farming" } },
        new() { Title = "Hermits", Kind = ItemKind.Unlock, Names = new[] { "Ballista Hermit", "Bakery Hermit", "Horn Hermit", "Stable Hermit", "Warrior Hermit" } },
        new()
        {
            Title = "Mounts", Kind = ItemKind.Unlock,
            Names = new[] { "Griffin Mount", "Stag Mount", "Warhorse Mount", "Bear Mount", "Lizard Mount", "Unicorn Mount", "Stamina Horse", "Burst Horse", "Fast Horse" },
        },
        new() { Title = "Filler", Kind = ItemKind.Filler, Names = new[] { CoinPouch, CoinSack, Gem, Vagrants } },
        new() { Title = "Junk", Kind = ItemKind.Junk, Names = JunkItems },
        new() { Title = "Traps", Kind = ItemKind.Trap, Names = Traps },
    };

    public static ItemKind KindOf(string itemName)
    {
        foreach (var group in ItemGroups)
            foreach (var name in group.Names)
                if (name == itemName) return group.Kind;
        return ItemKind.Junk;
    }

    // --- Locations ---
    public const int IslandCount = 5;

    public enum IslandThing { Portal, CoinChest, GemChest, Statue, MountShrine, HermitCabin }

    private static readonly Dictionary<IslandThing, string> IslandThingNames = new()
    {
        [IslandThing.Portal] = "Destroy Portal",
        [IslandThing.CoinChest] = "Coin Chest",
        [IslandThing.GemChest] = "Gem Chest",
        [IslandThing.Statue] = "Statue",
        [IslandThing.MountShrine] = "Mount Shrine",
        [IslandThing.HermitCabin] = "Hermit Cabin",
    };

    // island -> (portals, coin chests, gem chests, statues, mount shrines, hermit cabins); see locations.py ISLAND_CONTENT
    private static readonly Dictionary<int, Dictionary<IslandThing, int>> IslandContent = new()
    {
        [1] = Content(portals: 1, coin: 3, gem: 0, statues: 1, shrines: 1, hermits: 1),
        [2] = Content(portals: 3, coin: 2, gem: 3, statues: 1, shrines: 1, hermits: 1),
        [3] = Content(portals: 5, coin: 3, gem: 3, statues: 1, shrines: 2, hermits: 1),
        [4] = Content(portals: 7, coin: 1, gem: 3, statues: 0, shrines: 2, hermits: 1),
        [5] = Content(portals: 7, coin: 0, gem: 3, statues: 1, shrines: 1, hermits: 1),
    };

    private static Dictionary<IslandThing, int> Content(int portals, int coin, int gem, int statues, int shrines, int hermits) => new()
    {
        [IslandThing.Portal] = portals,
        [IslandThing.CoinChest] = coin,
        [IslandThing.GemChest] = gem,
        [IslandThing.Statue] = statues,
        [IslandThing.MountShrine] = shrines,
        [IslandThing.HermitCabin] = hermits,
    };

    public static int ExpectedCount(int island, IslandThing thing) =>
        IslandContent.TryGetValue(island, out var content) ? content[thing] : 0;

    public static string IslandThingLocation(int island, IslandThing thing, int k) => $"Island {island}: {IslandThingNames[thing]} {k}";
    public static string IslandThingLabel(IslandThing thing) => IslandThingNames[thing];

    public static string BoatLocation(int island) => $"Island {island}: Build the Boat";
    public static string ArriveLocation(int island) => $"Island {island}: Arrive";
    public static string LiberateLocation(int island) => $"Island {island}: Liberate";
    public const string DogLocation = "Island 2: Free the Dog";
    public const string FullUpgradeLocation = "Fully Upgrade an Island";
    public static string CastleLevelLocation(int level) => $"Castle Level {level}";
    public static string ForestLocation(int k) => $"Clear Forest {k}";

    public enum Milestone { Greed, Greedlings, Floaters, Breeders, CrownStealers, Nights, Trees }

    // slot_data "milestones" key, location template, label for the menu
    public static readonly Dictionary<Milestone, (string SlotKey, string Template, string Label)> Milestones = new()
    {
        [Milestone.Greed] = ("greed", "Slay {0} Greed", "Greed slain"),
        [Milestone.Greedlings] = ("greedlings", "Slay {0} Greedlings", "Greedlings slain"),
        [Milestone.Floaters] = ("floaters", "Slay {0} Floaters", "Floaters slain"),
        [Milestone.Breeders] = ("breeders", "Slay {0} Breeders", "Breeders slain"),
        [Milestone.CrownStealers] = ("crown_stealers", "Slay {0} Crown Stealers", "Crown Stealers slain"),
        [Milestone.Nights] = ("nights", "Survive {0} Nights", "Nights survived"),
        [Milestone.Trees] = ("trees", "Fell {0} Trees", "Trees felled"),
    };

    public static string MilestoneLocation(Milestone milestone, int threshold) => string.Format(Milestones[milestone].Template, threshold);

    // Which kill milestone (besides the "Greed" total) an enemy type counts towards.
    public static Milestone? KillCategory(EnemyType type) => type switch
    {
        EnemyType.TrollWeak or EnemyType.TrollMedium or EnemyType.ToughTroll => Milestone.Greedlings,
        EnemyType.Squid => Milestone.Floaters,
        EnemyType.Boss or EnemyType.KillerBoss or EnemyType.BossWithStealer or EnemyType.GauntletBoss or EnemyType.Ogre => Milestone.Breeders,
        EnemyType.Stealer => Milestone.CrownStealers,
        _ => null,
    };
}
