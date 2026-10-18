"""What each Kingdom Two Crowns campaign contains.

The per-island numbers come straight from the game's own level configs (each biome's LevelConfig assets,
dumped with the mod's Debug > "Dump campaign data" button), so they're exact rather than observed:
    LevelBlockGroup.Chest -> coin chests, GemChest -> gem chests (minimum where the game picks a range),
    Portal -> regular portals (the cliff portal is liberation), Statue*/Steed*/HermitHouse* -> statues,
    mount shrines and hermit cabins, Dog, UnlockNewRulerStatue (Dead Lands), Puzzle*Norselands.

Gem costs: chests give nothing but their check in this randomizer, so gems only come from "Gem Pouch"
items, and gem-cost checks need enough of them in logic. The base campaign's costs are known per island;
the DLCs' are estimated at 3 gems per statue, shrine or cabin.

Shogun reuses the base campaign's island layouts with its own mounts. Call of Olympus is different: its
statues, shrines, hermits, gem chests and dog are scattered at random over its god and quest islands each
campaign, so its checks are counted campaign-wide instead of per island.
"""
from typing import NamedTuple, Optional


class IslandContent(NamedTuple):
    coin_chests: int
    gem_chests: int
    portals: int
    statues: int
    mount_shrines: int
    hermit_cabins: int
    ruler_statues: int = 0
    puzzle: Optional[str] = None  # Norselands god whose puzzle is on this island


class Campaign(NamedTuple):
    key: str
    display: str
    biome_index: int  # BiomeHolder.Biomes in the game
    islands: dict[int, IslandContent]  # empty for campaign-wide (Olympus) campaigns
    statues: tuple[str, ...]
    hermits: tuple[str, ...]
    mounts: tuple[str, ...]
    powers: tuple[str, ...] = ()
    dog_island: int = 2
    # Campaign-wide content (Olympus only)
    totals: Optional[IslandContent] = None
    quarry_island: int = 0  # Stone Quarry / Iron Mine buildings; 0 = somewhere random (Olympus)
    mine_island: int = 0
    hermit_islands: dict[str, int] = {}  # hermit item -> island of its cabin (numbered campaigns)
    gem_costs: dict[int, int] = {}  # island -> total gems its statues, shrines and cabins cost

    def gem_cost(self, island: int) -> int:
        if island in self.gem_costs:
            return self.gem_costs[island]
        content = self.islands[island]
        return GEM_COST_ESTIMATE * (content.statues + content.mount_shrines + content.hermit_cabins)

    def cumulative_gem_cost(self, island: int) -> int:
        """Gems needed to pay for everything on this island and every island before it."""
        return sum(self.gem_cost(i) for i in self.islands if i <= island)

    @property
    def total_gem_cost(self) -> int:
        if self.is_campaign_wide:
            return GEM_COST_ESTIMATE * (self.totals.statues + self.totals.mount_shrines + self.totals.hermit_cabins)
        return self.cumulative_gem_cost(max(self.islands))

    @property
    def island_count(self) -> int:
        return len(self.islands)

    @property
    def is_campaign_wide(self) -> bool:
        return self.totals is not None


GEM_COST_ESTIMATE = 3

STATUES_BASE = ("Statue of Archery", "Statue of Farming", "Statue of Building", "Statue of Knights")
HERMITS_BASE = ("Ballista Hermit", "Stable Hermit", "Bakery Hermit", "Warrior Hermit", "Horn Hermit")
BASE_HERMIT_ISLANDS = {"Ballista Hermit": 1, "Stable Hermit": 2, "Bakery Hermit": 3, "Warrior Hermit": 4, "Horn Hermit": 5}
# Base campaign gem costs: island 1 archery statue 3 + griffin 2 + ballista 3, island 2 farming 1 + stag 1 +
# stable 1, island 3 building 3 + warhorse 2 + draft horse 1 + bakery 4, island 4 bear 3 + lizard 3 + warrior 2,
# island 5 knights 2 + unicorn 4 + horn 3.
BASE_GEM_COSTS = {1: 8, 2: 3, 3: 10, 4: 8, 5: 9}

BASE_ISLANDS = {
    1: IslandContent(coin_chests=3, gem_chests=0, portals=0, statues=1, mount_shrines=1, hermit_cabins=1),
    2: IslandContent(coin_chests=2, gem_chests=3, portals=2, statues=1, mount_shrines=1, hermit_cabins=1),
    3: IslandContent(coin_chests=3, gem_chests=3, portals=4, statues=1, mount_shrines=2, hermit_cabins=1),
    4: IslandContent(coin_chests=1, gem_chests=3, portals=6, statues=0, mount_shrines=2, hermit_cabins=1),
    5: IslandContent(coin_chests=0, gem_chests=3, portals=6, statues=1, mount_shrines=1, hermit_cabins=1),
}

DEAD_LANDS_ISLANDS = {
    1: IslandContent(coin_chests=3, gem_chests=0, portals=0, statues=1, mount_shrines=1, hermit_cabins=1),
    2: IslandContent(coin_chests=2, gem_chests=3, portals=2, statues=1, mount_shrines=1, hermit_cabins=1, ruler_statues=1),
    3: IslandContent(coin_chests=3, gem_chests=3, portals=4, statues=1, mount_shrines=2, hermit_cabins=1, ruler_statues=1),
    4: IslandContent(coin_chests=1, gem_chests=3, portals=6, statues=0, mount_shrines=2, hermit_cabins=1, ruler_statues=1),
    5: IslandContent(coin_chests=0, gem_chests=3, portals=6, statues=1, mount_shrines=1, hermit_cabins=1),
}

NORSELANDS_ISLANDS = {
    1: IslandContent(coin_chests=3, gem_chests=0, portals=0, statues=1, mount_shrines=1, hermit_cabins=1, puzzle="Heimdall"),
    2: IslandContent(coin_chests=3, gem_chests=2, portals=2, statues=0, mount_shrines=2, hermit_cabins=1),
    3: IslandContent(coin_chests=2, gem_chests=2, portals=4, statues=1, mount_shrines=1, hermit_cabins=1, puzzle="Thor"),
    4: IslandContent(coin_chests=1, gem_chests=2, portals=4, statues=1, mount_shrines=1, hermit_cabins=1, puzzle="Hel"),
    5: IslandContent(coin_chests=1, gem_chests=2, portals=6, statues=1, mount_shrines=1, hermit_cabins=0),
    6: IslandContent(coin_chests=1, gem_chests=3, portals=5, statues=0, mount_shrines=1, hermit_cabins=1, puzzle="Loki"),
}

# Olympus totals over the whole campaign. Gem chests use the guaranteed minimum (1 on the oracle island,
# 1-2 on each god island, 2-3 on each quest island); portals count the four quest islands only.
OLYMPUS_TOTALS = IslandContent(coin_chests=23, gem_chests=13, portals=16, statues=5, mount_shrines=12, hermit_cabins=6)

# Olympus islands you can arrive at (the oracle island is the start), in rough progression order.
OLYMPUS_ISLANDS = (
    "Artemis's Island", "Athena's Island", "Hephaestus's Island", "Hermes's Island",
    "Artemis's Quest Island", "Athena's Quest Island", "Hephaestus's Quest Island", "Hermes's Quest Island",
    "Mount Olympus",
)
# Olympus quests that give checks; finishing a god's quest is also where its power comes from in the game.
OLYMPUS_GOD_QUESTS = ("Artemis", "Athena", "Hephaestus", "Hermes")
OLYMPUS_OTHER_QUESTS = ("Oracle", "Artemis's Island", "Athena's Island", "Hephaestus's Island", "Hermes's Island")

CAMPAIGNS: dict[str, Campaign] = {
    "two_crowns": Campaign(
        key="two_crowns", display="Two Crowns", biome_index=0, islands=BASE_ISLANDS,
        quarry_island=2, mine_island=4, hermit_islands=BASE_HERMIT_ISLANDS, gem_costs=BASE_GEM_COSTS,
        statues=STATUES_BASE, hermits=HERMITS_BASE,
        mounts=("Griffin Mount", "Stag Mount", "Stamina Horse", "Warhorse Mount", "Bear Mount", "Lizard Mount",
                "Unicorn Mount"),
    ),
    "shogun": Campaign(
        key="shogun", display="Shogun", biome_index=1, islands=BASE_ISLANDS,
        quarry_island=2, mine_island=4, hermit_islands=BASE_HERMIT_ISLANDS, gem_costs=BASE_GEM_COSTS,
        statues=STATUES_BASE, hermits=HERMITS_BASE,
        mounts=("Griffin Mount", "Stag Mount", "Stamina Horse", "Warhorse Mount", "Bear Mount", "Lizard Mount",
                "Kirin Mount"),
    ),
    "dead_lands": Campaign(
        key="dead_lands", display="Dead Lands", biome_index=2, islands=DEAD_LANDS_ISLANDS,
        quarry_island=2, mine_island=4, hermit_islands=BASE_HERMIT_ISLANDS, gem_costs=BASE_GEM_COSTS,
        statues=STATUES_BASE, hermits=HERMITS_BASE,
        mounts=("Gamigin Mount", "Stag Mount", "Bear Mount", "Golem Mount", "Lizard Mount", "Beetle Mount",
                "Unicorn Mount"),
    ),
    "norselands": Campaign(
        key="norselands", display="Norselands", biome_index=3, islands=NORSELANDS_ISLANDS,
        quarry_island=2, mine_island=5,
        hermit_islands={"Horn Hermit": 1, "Bakery Hermit": 2, "Ballista Hermit": 3, "Stable Hermit": 4, "Warrior Hermit": 6},
        statues=STATUES_BASE, hermits=HERMITS_BASE,
        mounts=("Hrimfaxe Mount", "Cat Cart Mount", "Reindeer Mount", "Sleipnir Mount", "Kelpie Mount",
                "Gullinbursti Mount", "Wolf Mount"),
        powers=("Power of Heimdall", "Power of Thor", "Power of Hel", "Power of Loki"),
    ),
    "call_of_olympus": Campaign(
        key="call_of_olympus", display="Call of Olympus", biome_index=5, islands={},
        statues=STATUES_BASE + ("Statue of Pikemen",), hermits=HERMITS_BASE + ("Fire Hermit",),
        mounts=("Stamina Horse", "Bear Mount", "Stag Mount", "Griffin Mount", "Molossian Hound Mount",
                "Chimera Mount", "Spider Mount", "Donkey Mount", "Cerberus Mount", "Hippocampus Mount",
                "Chariot Mount", "Pegasus Mount"),
        powers=("Power of Artemis", "Power of Athena", "Power of Hephaestus", "Power of Hermes"),
        dog_island=0,
        totals=OLYMPUS_TOTALS,
    ),
}

# Option value (options.Campaign) -> campaign
CAMPAIGN_BY_OPTION = ("two_crowns", "shogun", "dead_lands", "norselands", "call_of_olympus")
MAX_ISLANDS = max(c.island_count for c in CAMPAIGNS.values())
