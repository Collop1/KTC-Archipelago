using System.Collections.Generic;
using System.Linq;

namespace APKingdom2Crown;

// Item and location names plus what each campaign contains, mirroring apworld/kingdom_two_crowns
// (items.py, locations.py, campaigns.py). The mod never hard-codes ids: they come from the server's data
// package, so only the names and counts here need to match the apworld.
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
        [Statue.Deity.Pike] = "Statue of Pikemen",
    };

    public static readonly Dictionary<Hermit.HermitType, string> HermitItems = new()
    {
        [Hermit.HermitType.Ballista] = "Ballista Hermit",
        [Hermit.HermitType.Baker] = "Bakery Hermit",
        [Hermit.HermitType.Horn] = "Horn Hermit",
        [Hermit.HermitType.Horse] = "Stable Hermit",
        [Hermit.HermitType.Knight] = "Warrior Hermit",
        [Hermit.HermitType.Fire] = "Fire Hermit",
    };

    // Steed type -> mount item. A campaign only gates a steed whose item is in its own pool.
    private static readonly Dictionary<SteedType, string> MountItems = new()
    {
        [SteedType.P1Griffin] = "Griffin Mount", [SteedType.P2Griffin] = "Griffin Mount",
        [SteedType.Stag] = "Stag Mount", [SteedType.P2Stag] = "Stag Mount",
        [SteedType.HorseStamina] = "Stamina Horse",
        [SteedType.P1Warhorse] = "Warhorse Mount", [SteedType.P2Warhorse] = "Warhorse Mount",
        [SteedType.Bear] = "Bear Mount",
        [SteedType.Lizard] = "Lizard Mount",
        [SteedType.Unicorn] = "Unicorn Mount",
        [SteedType.Bloodstained] = "Gamigin Mount",
        [SteedType.Barrier] = "Golem Mount",
        [SteedType.Trap] = "Beetle Mount",
        [SteedType.DayNight] = "Hrimfaxe Mount",
        [SteedType.CatCart] = "Cat Cart Mount",
        [SteedType.Reindeer_Norselands] = "Reindeer Mount", [SteedType.P2Reindeer_Norselands] = "Reindeer Mount",
        [SteedType.Sleipnir] = "Sleipnir Mount",
        [SteedType.Kelpie] = "Kelpie Mount", [SteedType.P2Kelpie] = "Kelpie Mount",
        [SteedType.Gullinbursti] = "Gullinbursti Mount",
        [SteedType.P1Wolf] = "Wolf Mount", [SteedType.P2Wolf] = "Wolf Mount",
        [SteedType.MolossianHound] = "Molossian Hound Mount",
        [SteedType.Chimera] = "Chimera Mount",
        [SteedType.Spider] = "Spider Mount",
        [SteedType.Donkey] = "Donkey Mount",
        [SteedType.Cerberus] = "Cerberus Mount",
        [SteedType.Hippocampus] = "Hippocampus Mount",
        [SteedType.TheChariotDay] = "Chariot Mount", [SteedType.TheChariotNight] = "Chariot Mount",
        [SteedType.Pegasus] = "Pegasus Mount",
    };

    public static readonly Dictionary<ItemOfPower.ItemType, string> PowerItems = new()
    {
        [ItemOfPower.ItemType.ThorItem] = "Power of Thor",
        [ItemOfPower.ItemType.HeimdalItem] = "Power of Heimdall",
        [ItemOfPower.ItemType.HelItem] = "Power of Hel",
        [ItemOfPower.ItemType.LokiItem] = "Power of Loki",
        [ItemOfPower.ItemType.ArtemisBow] = "Power of Artemis",
        [ItemOfPower.ItemType.MedusaShield] = "Power of Athena",
        [ItemOfPower.ItemType.HephaestusHammer] = "Power of Hephaestus",
        [ItemOfPower.ItemType.HermesStaff] = "Power of Hermes",
    };

    // Norselands god -> the Item of Power its puzzle grants.
    public static readonly Dictionary<string, ItemOfPower.ItemType> PuzzlePowers = new()
    {
        ["Heimdall"] = ItemOfPower.ItemType.HeimdalItem,
        ["Thor"] = ItemOfPower.ItemType.ThorItem,
        ["Hel"] = ItemOfPower.ItemType.HelItem,
        ["Loki"] = ItemOfPower.ItemType.LokiItem,
    };

    public const string GemPouch = "Gem Pouch";
    public const int GemsPerPouch = 3;
    public const string CoinPouch = "Coin Pouch";
    public const string CoinSack = "Coin Sack";
    public const string Gem = "Gem";
    public const string Vagrants = "Vagrants";

    public static readonly string[] JunkItems =
    {
        "Sunken Gold (Junk)", "Broken Crown (Junk)", "Tuft of Grass (Junk)", "Greed Mask Shard (Junk)",
        "Soggy Bread (Junk)", "Bent Arrow (Junk)", "Lost Banner (Junk)",
    };

    public const string TripTrap = "Trip Trap";
    public const string SlipperyPouchTrap = "Slippery Pouch Trap";
    public const string WaveBackTrap = "Wave Back Trap";
    public const string WavesGoneByTrap = "Waves Gone By Trap";
    public const string DamnedNightTrap = "Damned Night Trap";
    public const string DecayTrap = "Decay Trap";
    public const string StamDamTrap = "Stam Dam Trap";
    public const string DesertionTrap = "Desertion Trap";
    public const string ToolTheftTrap = "Tool Theft Trap";

    public static readonly string[] Traps =
    {
        TripTrap, SlipperyPouchTrap, WaveBackTrap, WavesGoneByTrap, DamnedNightTrap, DecayTrap, StamDamTrap,
        DesertionTrap, ToolTheftTrap,
    };

    public enum ItemKind { Unlock, Filler, Junk, Trap }

    public sealed class ItemGroup
    {
        public string Title;
        public ItemKind Kind;
        public string[] Names;
    }

    // The items a campaign can receive, grouped for the menu (Items and Debug tabs).
    public static ItemGroup[] ItemGroupsFor(Campaign campaign)
    {
        var groups = new List<ItemGroup>
        {
            new() { Title = "Buildings", Kind = ItemKind.Unlock, Names = new[] { ProgressiveCastle, StoneTech, IronTech } },
        };
        if (!campaign.CampaignWide)
            groups.Add(new() { Title = "Islands", Kind = ItemKind.Unlock, Names = Enumerable.Range(2, campaign.IslandCount - 1).Select(IslandUnlock).ToArray() });
        groups.Add(new() { Title = "Statues", Kind = ItemKind.Unlock, Names = campaign.Statues });
        groups.Add(new() { Title = "Hermits", Kind = ItemKind.Unlock, Names = campaign.Hermits });
        groups.Add(new() { Title = "Mounts", Kind = ItemKind.Unlock, Names = campaign.Mounts });
        if (campaign.Powers.Length > 0)
            groups.Add(new() { Title = "Powers", Kind = ItemKind.Unlock, Names = campaign.Powers });
        groups.Add(new() { Title = "Filler", Kind = ItemKind.Filler, Names = new[] { GemPouch, CoinPouch, CoinSack, Gem, Vagrants } });
        groups.Add(new() { Title = "Junk", Kind = ItemKind.Junk, Names = JunkItems });
        groups.Add(new() { Title = "Traps", Kind = ItemKind.Trap, Names = Traps });
        return groups.ToArray();
    }

    public static ItemKind KindOf(string itemName)
    {
        // Gem Pouches are progression in the apworld, but in the game they're spent like any filler.
        if (itemName is GemPouch or CoinPouch or CoinSack or Gem or Vagrants) return ItemKind.Filler;
        if (System.Array.IndexOf(Traps, itemName) >= 0) return ItemKind.Trap;
        if (System.Array.IndexOf(JunkItems, itemName) >= 0) return ItemKind.Junk;
        return ItemKind.Unlock;
    }

    // --- Campaigns (apworld campaigns.py) ---

    public enum IslandThing { Portal, CoinChest, GemChest, Statue, MountShrine, HermitCabin, RulerStatue }

    private static readonly Dictionary<IslandThing, string> IslandThingNames = new()
    {
        [IslandThing.Portal] = "Destroy Portal",
        [IslandThing.CoinChest] = "Coin Chest",
        [IslandThing.GemChest] = "Gem Chest",
        [IslandThing.Statue] = "Statue",
        [IslandThing.MountShrine] = "Mount Shrine",
        [IslandThing.HermitCabin] = "Hermit Cabin",
        [IslandThing.RulerStatue] = "Ruler Statue",
    };

    public sealed class Content
    {
        public readonly Dictionary<IslandThing, int> Counts;
        public readonly string Puzzle;

        public Content(int coin, int gem, int portals, int statues, int shrines, int hermits, int rulers = 0, string puzzle = null)
        {
            Counts = new Dictionary<IslandThing, int>
            {
                [IslandThing.CoinChest] = coin, [IslandThing.GemChest] = gem, [IslandThing.Portal] = portals,
                [IslandThing.Statue] = statues, [IslandThing.MountShrine] = shrines, [IslandThing.HermitCabin] = hermits,
                [IslandThing.RulerStatue] = rulers,
            };
            Puzzle = puzzle;
        }
    }

    public sealed class Campaign
    {
        public string Key, Display;
        public int BiomeIndex;
        public Dictionary<int, Content> Islands = new();
        public Content Totals; // campaign-wide content (Olympus)
        public string[] Statues, Hermits, Mounts, Powers = System.Array.Empty<string>();
        public int DogIsland = 2;
        public int QuarryIsland, MineIsland; // 0 = anywhere (Olympus)

        public bool CampaignWide => Totals != null;
        public int IslandCount => Islands.Count;
    }

    private static readonly string[] StatuesBase = { "Statue of Archery", "Statue of Farming", "Statue of Building", "Statue of Knights" };
    private static readonly string[] HermitsBase = { "Ballista Hermit", "Stable Hermit", "Bakery Hermit", "Warrior Hermit", "Horn Hermit" };

    private static Dictionary<int, Content> BaseIslands() => new()
    {
        [1] = new Content(coin: 3, gem: 0, portals: 0, statues: 1, shrines: 1, hermits: 1),
        [2] = new Content(coin: 2, gem: 3, portals: 2, statues: 1, shrines: 1, hermits: 1),
        [3] = new Content(coin: 3, gem: 3, portals: 4, statues: 1, shrines: 2, hermits: 1),
        [4] = new Content(coin: 1, gem: 3, portals: 6, statues: 0, shrines: 2, hermits: 1),
        [5] = new Content(coin: 0, gem: 3, portals: 6, statues: 1, shrines: 1, hermits: 1),
    };

    public static readonly Campaign[] Campaigns =
    {
        new()
        {
            Key = "two_crowns", Display = "Two Crowns", BiomeIndex = 0, Islands = BaseIslands(), QuarryIsland = 2, MineIsland = 4,
            Statues = StatuesBase, Hermits = HermitsBase,
            Mounts = new[] { "Griffin Mount", "Stag Mount", "Stamina Horse", "Warhorse Mount", "Bear Mount", "Lizard Mount", "Unicorn Mount" },
        },
        new()
        {
            Key = "shogun", Display = "Shogun", BiomeIndex = 1, Islands = BaseIslands(), QuarryIsland = 2, MineIsland = 4,
            Statues = StatuesBase, Hermits = HermitsBase,
            Mounts = new[] { "Griffin Mount", "Stag Mount", "Stamina Horse", "Warhorse Mount", "Bear Mount", "Lizard Mount", "Kirin Mount" },
        },
        new()
        {
            Key = "dead_lands", Display = "Dead Lands", BiomeIndex = 2, QuarryIsland = 2, MineIsland = 4,
            Islands = new Dictionary<int, Content>
            {
                [1] = new Content(coin: 3, gem: 0, portals: 0, statues: 1, shrines: 1, hermits: 1),
                [2] = new Content(coin: 2, gem: 3, portals: 2, statues: 1, shrines: 1, hermits: 1, rulers: 1),
                [3] = new Content(coin: 3, gem: 3, portals: 4, statues: 1, shrines: 2, hermits: 1, rulers: 1),
                [4] = new Content(coin: 1, gem: 3, portals: 6, statues: 0, shrines: 2, hermits: 1, rulers: 1),
                [5] = new Content(coin: 0, gem: 3, portals: 6, statues: 1, shrines: 1, hermits: 1),
            },
            Statues = StatuesBase, Hermits = HermitsBase,
            Mounts = new[] { "Gamigin Mount", "Stag Mount", "Bear Mount", "Golem Mount", "Lizard Mount", "Beetle Mount", "Unicorn Mount" },
        },
        new()
        {
            Key = "norselands", Display = "Norselands", BiomeIndex = 3, QuarryIsland = 2, MineIsland = 5,
            Islands = new Dictionary<int, Content>
            {
                [1] = new Content(coin: 3, gem: 0, portals: 0, statues: 1, shrines: 1, hermits: 1, puzzle: "Heimdall"),
                [2] = new Content(coin: 3, gem: 2, portals: 2, statues: 0, shrines: 2, hermits: 1),
                [3] = new Content(coin: 2, gem: 2, portals: 4, statues: 1, shrines: 1, hermits: 1, puzzle: "Thor"),
                [4] = new Content(coin: 1, gem: 2, portals: 4, statues: 1, shrines: 1, hermits: 1, puzzle: "Hel"),
                [5] = new Content(coin: 1, gem: 2, portals: 6, statues: 1, shrines: 1, hermits: 0),
                [6] = new Content(coin: 1, gem: 3, portals: 5, statues: 0, shrines: 1, hermits: 1, puzzle: "Loki"),
            },
            Statues = StatuesBase, Hermits = HermitsBase,
            Mounts = new[] { "Hrimfaxe Mount", "Cat Cart Mount", "Reindeer Mount", "Sleipnir Mount", "Kelpie Mount", "Gullinbursti Mount", "Wolf Mount" },
            Powers = new[] { "Power of Heimdall", "Power of Thor", "Power of Hel", "Power of Loki" },
        },
        new()
        {
            Key = "call_of_olympus", Display = "Call of Olympus", BiomeIndex = 5,
            Totals = new Content(coin: 23, gem: 13, portals: 16, statues: 5, shrines: 12, hermits: 6),
            Statues = StatuesBase.Append("Statue of Pikemen").ToArray(),
            Hermits = HermitsBase.Append("Fire Hermit").ToArray(),
            Mounts = new[]
            {
                "Stamina Horse", "Bear Mount", "Stag Mount", "Griffin Mount", "Molossian Hound Mount", "Chimera Mount",
                "Spider Mount", "Donkey Mount", "Cerberus Mount", "Hippocampus Mount", "Chariot Mount", "Pegasus Mount",
            },
            Powers = new[] { "Power of Artemis", "Power of Athena", "Power of Hephaestus", "Power of Hermes" },
            DogIsland = 0,
        },
    };

    public static Campaign CampaignByKey(string key) => Campaigns.FirstOrDefault(c => c.Key == key) ?? Campaigns[0];

    public static string MountItem(Campaign campaign, SteedType type)
    {
        if (!MountItems.TryGetValue(type, out var item)) return null;
        if (campaign.Key == "shogun" && item == "Unicorn Mount") item = "Kirin Mount";
        return System.Array.IndexOf(campaign.Mounts, item) >= 0 ? item : null;
    }

    public static string PowerItem(Campaign campaign, ItemOfPower.ItemType type) =>
        PowerItems.TryGetValue(type, out var item) && System.Array.IndexOf(campaign.Powers, item) >= 0 ? item : null;

    public static string StatueItem(Campaign campaign, Statue.Deity deity) =>
        StatueItems.TryGetValue(deity, out var item) && System.Array.IndexOf(campaign.Statues, item) >= 0 ? item : null;

    public static string HermitItem(Campaign campaign, Hermit.HermitType type) =>
        HermitItems.TryGetValue(type, out var item) && System.Array.IndexOf(campaign.Hermits, item) >= 0 ? item : null;

    public static int ExpectedCount(Campaign campaign, int island, IslandThing thing)
    {
        if (campaign.CampaignWide) return campaign.Totals.Counts[thing];
        return campaign.Islands.TryGetValue(island, out var content) ? content.Counts[thing] : 0;
    }

    // --- Location names (apworld locations.py) ---

    public static string IslandThingLocation(Campaign campaign, int island, IslandThing thing, int k) =>
        campaign.CampaignWide ? $"{IslandThingNames[thing]} {k}" : $"Island {island}: {IslandThingNames[thing]} {k}";

    public static string IslandThingLabel(IslandThing thing) => IslandThingNames[thing];

    public static string BoatLocation(int island) => $"Island {island}: Build the Boat";
    public static string ArriveLocation(int island) => $"Island {island}: Arrive";
    public static string LiberateLocation(int island) => $"Island {island}: Liberate";
    public static string PuzzleLocation(int island, string god) => $"Island {island}: {god}'s Puzzle";
    public static string DogLocation(Campaign campaign) => campaign.CampaignWide ? "Free the Dog" : "Island 2: Free the Dog";
    public const string FullUpgradeLocation = "Fully Upgrade an Island";
    public static string QuarryLocation(Campaign campaign) => campaign.CampaignWide ? "Build the Quarry" : $"Island {campaign.QuarryIsland}: Build the Quarry";
    public static string MineLocation(Campaign campaign) => campaign.CampaignWide ? "Build the Mine" : $"Island {campaign.MineIsland}: Build the Mine";
    public const string GrassLocation = "Eat Grass 100 Times";
    public const int GrassTarget = 100;
    public const string SunkCoinsLocation = "Sink 25 Coins";
    public const int SunkCoinsTarget = 25;

    public static readonly Dictionary<Hermit.HermitType, string> HermitBuildingLocations = new()
    {
        [Hermit.HermitType.Ballista] = "Hermit Building: Ballista",
        [Hermit.HermitType.Baker] = "Hermit Building: Bakery",
        [Hermit.HermitType.Horse] = "Hermit Building: Stable",
        [Hermit.HermitType.Knight] = "Hermit Building: Warrior",
        [Hermit.HermitType.Horn] = "Hermit Building: Horn",
        [Hermit.HermitType.Fire] = "Hermit Building: Fire",
    };
    public static string CastleLevelLocation(int level) => $"Castle Level {level}";

    // Call of Olympus: level config name -> island name used in "Reach ..." checks.
    public static readonly Dictionary<string, string> OlympusIslands = new()
    {
        ["Greece_Land_God_Artemis"] = "Artemis's Island",
        ["Greece_Land_God_Athena"] = "Athena's Island",
        ["Greece_Land_God_Hephaestus"] = "Hephaestus's Island",
        ["Greece_Land_God_Hermes"] = "Hermes's Island",
        ["Greece_Land_Quest_Artemis"] = "Artemis's Quest Island",
        ["Greece_Land_Quest_Athena"] = "Athena's Quest Island",
        ["Greece_Land_Quest_Hephaestus"] = "Hephaestus's Quest Island",
        ["Greece_Land_Quest_Hermes"] = "Hermes's Quest Island",
        ["Greece_Land_MtOlympus_Serpent"] = "Mount Olympus",
    };

    public static string OlympusReachLocation(string island) => $"Reach {island}";

    // Call of Olympus quests that send a check when completed.
    public static readonly Dictionary<QuestType, string> OlympusQuests = new()
    {
        [QuestType.OracleIsland] = "Complete the Oracle's Quest",
        [QuestType.GodIslandArtemis] = "Complete Artemis's Island Quest",
        [QuestType.GodIslandAthena] = "Complete Athena's Island Quest",
        [QuestType.GodIslandHephaestus] = "Complete Hephaestus's Island Quest",
        [QuestType.GodIslandHermes] = "Complete Hermes's Island Quest",
        [QuestType.Artemis] = "Complete Artemis's Quest",
        [QuestType.Athena] = "Complete Athena's Quest",
        [QuestType.Hephaestus] = "Complete Hephaestus's Quest",
        [QuestType.Hermes] = "Complete Hermes's Quest",
        [QuestType.MtOlympusSerpent] = "Defeat the World Serpent",
    };

    public const string OlympusGoalLocation = "Defeat the World Serpent";

    public enum Milestone
    {
        Greed, Greedlings, Floaters, Breeders, CrownStealers, Nights, Trees,
        Walls, Towers, Farms, CitizenHouses,
        Villagers, Archers, Builders, Farmers, Knights,
        Animals, Deer, Rabbits, Boars,
        Bribes, CoinsSpent,
    }

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
        // Building sanity: one check per building, so the "threshold" is the building's number.
        [Milestone.Walls] = ("walls", "Build Wall {0}", "Walls built"),
        [Milestone.Towers] = ("towers", "Build Archer Tower {0}", "Archer towers built"),
        [Milestone.Farms] = ("farms", "Build Farm {0}", "Farms built"),
        [Milestone.CitizenHouses] = ("citizen_houses", "Build Citizen House {0}", "Citizen houses built"),
        [Milestone.Villagers] = ("villagers", "Recruit {0} Villagers", "Villagers recruited"),
        [Milestone.Archers] = ("archers", "Recruit {0} Archers", "Archers recruited"),
        [Milestone.Builders] = ("builders", "Recruit {0} Builders", "Builders recruited"),
        [Milestone.Farmers] = ("farmers", "Recruit {0} Farmers", "Farmers recruited"),
        [Milestone.Knights] = ("knights", "Recruit {0} Knights", "Knights recruited"),
        [Milestone.Animals] = ("animals", "Hunt {0} Animals", "Animals hunted"),
        [Milestone.Deer] = ("deer", "Hunt {0} Deer", "Deer hunted"),
        [Milestone.Rabbits] = ("rabbits", "Hunt {0} Rabbits", "Rabbits hunted"),
        [Milestone.Boars] = ("boars", "Hunt {0} Boars", "Boars hunted"),
        [Milestone.Bribes] = ("bribes", "Bribe the Greed with {0} Coins", "Coins the Greed took"),
        [Milestone.CoinsSpent] = ("coins_spent", "Spend {0} Coins", "Coins spent"),
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
