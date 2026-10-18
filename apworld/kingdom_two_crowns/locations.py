"""Location table for Kingdom Two Crowns.

The mod builds the same names (mod/Archipelago/GameData.cs) and looks their ids up in the server's data
package, so names here and there must match exactly. Ids come from fixed formulas so they never shift when
options or content tables change. Every name any campaign can use is registered; each world only creates
the ones its campaign has.

Numbered-island campaigns (Two Crowns, Shogun, Dead Lands, Norselands) get per-island progressive checks:
the first coin chest you open on island 3 sends "Island 3: Coin Chest 1", the next "Island 3: Coin Chest 2"
and so on, whichever physical chest it was. Call of Olympus scatters its content randomly, so its checks
are counted over the whole campaign ("Coin Chest 7").
"""
from fractions import Fraction
from math import floor
from typing import NamedTuple

from BaseClasses import Location

from .campaigns import (
    CAMPAIGNS, MAX_ISLANDS, OLYMPUS_GOD_QUESTS, OLYMPUS_ISLANDS, OLYMPUS_OTHER_QUESTS, IslandContent,
)
from .options import MAX_MILESTONES_PER_ISLAND

LOC_BASE = 7_560_000
OLYMPUS_BASE = 7_580_000
MILESTONE_BASE = 7_600_000


class KingdomTwoCrownsLocation(Location):
    game = "Kingdom Two Crowns"


# kind -> (label, id slot). Progressive kinds get " {k}" appended.
PROGRESSIVE_KINDS: dict[str, tuple[str, int]] = {
    "portals": ("Destroy Portal", 1),
    "coin_chests": ("Coin Chest", 2),
    "gem_chests": ("Gem Chest", 3),
    "statues": ("Statue", 4),
    "mount_shrines": ("Mount Shrine", 5),
    "hermit_cabins": ("Hermit Cabin", 6),
    "ruler_statues": ("Ruler Statue", 7),
}

# Kinds that cost gems to pay for.
GEM_KINDS = {"statues", "mount_shrines", "hermit_cabins"}

PUZZLE_GODS = ("Heimdall", "Thor", "Hel", "Loki")


def island_progressive_name(island: int, kind: str, k: int) -> str:
    return f"Island {island}: {PROGRESSIVE_KINDS[kind][0]} {k}"


def campaign_progressive_name(kind: str, k: int) -> str:
    return f"{PROGRESSIVE_KINDS[kind][0]} {k}"


def boat_name(island: int) -> str:
    return f"Island {island}: Build the Boat"


def arrive_name(island: int) -> str:
    return f"Island {island}: Arrive"


def liberate_name(island: int) -> str:
    return f"Island {island}: Liberate"


def puzzle_name(island: int, god: str) -> str:
    return f"Island {island}: {god}'s Puzzle"


def olympus_reach_name(island: str) -> str:
    return f"Reach {island}"


def olympus_god_quest_name(god: str) -> str:
    return f"Complete {god}'s Quest"


def olympus_other_quest_name(place: str) -> str:
    return "Complete the Oracle's Quest" if place == "Oracle" else f"Complete {place} Quest"


def quarry_name(island: int) -> str:
    return f"Island {island}: Build the Quarry" if island else "Build the Quarry"


def mine_name(island: int) -> str:
    return f"Island {island}: Build the Mine" if island else "Build the Mine"


# Hermit item -> what its building check is called
HERMIT_BUILDINGS: dict[str, str] = {
    "Ballista Hermit": "Hermit Building: Ballista",
    "Bakery Hermit": "Hermit Building: Bakery",
    "Stable Hermit": "Hermit Building: Stable",
    "Warrior Hermit": "Hermit Building: Warrior",
    "Horn Hermit": "Hermit Building: Horn",
    "Fire Hermit": "Hermit Building: Fire",
}

GRASS_LOCATION = "Eat Grass 100 Times"
WATER_LOCATION = "Sink 25 Coins"

DOG_LOCATION = "Island 2: Free the Dog"
OLYMPUS_DOG_LOCATION = "Free the Dog"
OLYMPUS_GOAL_LOCATION = "Defeat the World Serpent"
FULL_UPGRADE_LOCATION = "Fully Upgrade an Island"


def castle_level_name(level: int) -> str:
    return f"Castle Level {level}"


CASTLE_LEVELS = range(2, 8)


class Milestone(NamedTuple):
    template: str  # formatted with n = threshold
    interval_option: str  # "" for building sanity: one check per building
    count_option: str  # checks per island to liberate (building sanity: the on/off toggle)
    intervals: tuple[int, ...]
    id_slot: int


MILESTONES: dict[str, Milestone] = {
    "greed": Milestone("Slay {n} Greed", "greed_kill_interval", "greed_kill_checks", (25, 50, 100, 200), 0),
    "greedlings": Milestone("Slay {n} Greedlings", "greedling_kill_interval", "greedling_kill_checks",
                            (25, 50, 100, 200), 1),
    "floaters": Milestone("Slay {n} Floaters", "floater_kill_interval", "floater_kill_checks", (5, 10, 20, 50), 2),
    "breeders": Milestone("Slay {n} Breeders", "breeder_kill_interval", "breeder_kill_checks", (2, 3, 5, 10), 3),
    "crown_stealers": Milestone("Slay {n} Crown Stealers", "crown_stealer_kill_interval",
                                "crown_stealer_kill_checks", (2, 3, 5, 10), 4),
    "nights": Milestone("Survive {n} Nights", "night_interval", "night_checks", (3, 5, 10, 15), 5),
    "trees": Milestone("Fell {n} Trees", "tree_interval", "tree_checks", (5, 10, 20, 25), 6),
    # Building sanity: one check per new building (not upgrades), counted over the whole campaign
    "walls": Milestone("Build Wall {n}", "", "building_sanity", (1,), 7),
    "towers": Milestone("Build Archer Tower {n}", "", "building_sanity", (1,), 8),
    "farms": Milestone("Build Farm {n}", "", "building_sanity", (1,), 9),
    "citizen_houses": Milestone("Build Citizen House {n}", "", "building_sanity", (1,), 10),
    # Unit sanity (subjects given their tool or role)
    "villagers": Milestone("Recruit {n} Villagers", "villager_interval", "villager_checks", (5, 10, 20, 25), 11),
    "archers": Milestone("Recruit {n} Archers", "archer_interval", "archer_checks", (3, 5, 10, 20), 12),
    "builders": Milestone("Recruit {n} Builders", "builder_interval", "builder_checks", (3, 5, 10), 13),
    "farmers": Milestone("Recruit {n} Farmers", "farmer_interval", "farmer_checks", (2, 3, 5, 10), 14),
    "knights": Milestone("Recruit {n} Knights", "knight_interval", "knight_checks", (1, 2, 3, 5), 15),
    # Animal sanity (killed by anyone)
    "animals": Milestone("Hunt {n} Animals", "animal_interval", "animal_checks", (25, 50, 100, 200), 16),
    "deer": Milestone("Hunt {n} Deer", "deer_interval", "deer_checks", (5, 10, 20, 50), 17),
    "rabbits": Milestone("Hunt {n} Rabbits", "rabbit_interval", "rabbit_checks", (10, 25, 50, 100), 18),
    "boars": Milestone("Hunt {n} Boars", "boar_interval", "boar_checks", (2, 5, 10, 20), 19),
    # Coins
    "bribes": Milestone("Bribe the Greed with {n} Coins", "bribe_interval", "bribe_checks", (5, 10, 25, 50), 20),
    "coins_spent": Milestone("Spend {n} Coins", "spend_interval", "spend_checks", (50, 100, 200, 500), 21),
}

# Building sanity: buildings per island to liberate. The hardest Norselands difficulty guarantees about 48 wall
# spots, 35 tower spots, 11 farm spots and 11 vagrant camps over its six islands; these stay under that
# (40 / 30 / 10 / 10 for all six).
BUILDING_RATES: dict[str, Fraction] = {
    "walls": Fraction(20, 3),
    "towers": Fraction(5),
    "farms": Fraction(5, 3),
    "citizen_houses": Fraction(5, 3),
}

# Call of Olympus has no "islands to liberate"; its milestone and building counts treat it as five islands.
OLYMPUS_GOAL_ISLANDS = 5

# category -> (extra castle upgrades on top of the tier, minimum islands reachable). Kinds that need more islands
# than the seed has are left out entirely: the rarer Greed (and boars) may never turn up on the first island.
MILESTONE_REQUIREMENTS: dict[str, tuple[int, int]] = {
    "greed": (0, 1),
    "greedlings": (0, 1),
    "floaters": (1, 2),
    "breeders": (2, 3),
    "crown_stealers": (1, 2),
    "nights": (0, 1),
    "trees": (-99, 1),
    "walls": (0, 1),
    "towers": (1, 1),
    "farms": (1, 1),
    "citizen_houses": (0, 1),
    "villagers": (-99, 1),
    "archers": (0, 1),
    "builders": (0, 1),
    "farmers": (1, 1),
    "knights": (2, 1),
    "animals": (-99, 1),
    "deer": (-99, 1),
    "rabbits": (0, 1),
    "boars": (0, 2),
    "bribes": (0, 1),
    "coins_spent": (0, 1),
}


def milestone_name(category: str, threshold: int) -> str:
    return MILESTONES[category].template.format(n=threshold)


def goal_islands(world) -> int:
    campaign = world.campaign
    if campaign.is_campaign_wide:
        return OLYMPUS_GOAL_ISLANDS
    return min(world.options.islands_to_liberate.value, campaign.island_count)


def available_islands(world) -> int:
    """How many islands the player can get to in this seed."""
    return OLYMPUS_GOAL_ISLANDS if world.campaign.is_campaign_wide else len(world.islands_in_play)


def hermits_in_play(world) -> list[str]:
    """Hermits whose cabin is on an island in this seed (all of them for Call of Olympus)."""
    campaign = world.campaign
    if campaign.is_campaign_wide:
        return list(campaign.hermits)
    return [h for h in campaign.hermits if campaign.hermit_islands.get(h, 1) in world.islands_in_play]


def powers_in_play(world) -> list[str]:
    """Powers whose source is in this seed: Norselands' come from the god puzzles on particular islands."""
    campaign = world.campaign
    if campaign.is_campaign_wide:
        return list(campaign.powers)
    puzzles = {f"Power of {campaign.islands[i].puzzle}" for i in world.islands_in_play if campaign.islands[i].puzzle}
    return [p for p in campaign.powers if p in puzzles]


def milestone_interval(world, category: str) -> int:
    option = MILESTONES[category].interval_option
    return getattr(world.options, option).value if option else 1


def milestone_per_island(world, category: str) -> Fraction:
    """How many checks of this category each island to liberate adds."""
    if category in BUILDING_RATES:
        return BUILDING_RATES[category] if world.options.building_sanity else Fraction(0)
    return Fraction(getattr(world.options, MILESTONES[category].count_option).value)


def _max_island_content() -> dict[int, IslandContent]:
    """Per island, the most of each kind any numbered campaign has."""
    result: dict[int, IslandContent] = {}
    for island in range(1, MAX_ISLANDS + 1):
        per_kind = {
            kind: max((getattr(c.islands[island], kind) for c in CAMPAIGNS.values() if island in c.islands), default=0)
            for kind in PROGRESSIVE_KINDS
        }
        result[island] = IslandContent(**per_kind)
    return result


def _all_location_names_to_ids() -> dict[str, int]:
    table: dict[str, int] = {}
    for island, content in _max_island_content().items():
        island_base = LOC_BASE + island * 1000
        table[boat_name(island)] = island_base + 0
        if island > 1:
            table[arrive_name(island)] = island_base + 1
        table[liberate_name(island)] = island_base + 2
        for kind, (_, slot) in PROGRESSIVE_KINDS.items():
            for k in range(1, getattr(content, kind) + 1):
                table[island_progressive_name(island, kind, k)] = island_base + slot * 100 + k
        for i, god in enumerate(PUZZLE_GODS, start=1):
            if any(c.islands.get(island) and c.islands[island].puzzle == god for c in CAMPAIGNS.values()):
                table[puzzle_name(island, god)] = island_base + 800 + i
        if any(c.quarry_island == island for c in CAMPAIGNS.values()):
            table[quarry_name(island)] = island_base + 10
        if any(c.mine_island == island for c in CAMPAIGNS.values()):
            table[mine_name(island)] = island_base + 11
    table[DOG_LOCATION] = LOC_BASE + 2000 + 3

    olympus = CAMPAIGNS["call_of_olympus"].totals
    for kind, (_, slot) in PROGRESSIVE_KINDS.items():
        for k in range(1, getattr(olympus, kind) + 1):
            table[campaign_progressive_name(kind, k)] = OLYMPUS_BASE + slot * 100 + k
    for i, island in enumerate(OLYMPUS_ISLANDS):
        table[olympus_reach_name(island)] = OLYMPUS_BASE + 900 + i
    for i, god in enumerate(OLYMPUS_GOD_QUESTS):
        table[olympus_god_quest_name(god)] = OLYMPUS_BASE + 950 + i
    for i, place in enumerate(OLYMPUS_OTHER_QUESTS):
        table[olympus_other_quest_name(place)] = OLYMPUS_BASE + 960 + i
    table[OLYMPUS_DOG_LOCATION] = OLYMPUS_BASE + 990
    table[quarry_name(0)] = OLYMPUS_BASE + 980
    table[mine_name(0)] = OLYMPUS_BASE + 981
    table[OLYMPUS_GOAL_LOCATION] = OLYMPUS_BASE + 999

    global_base = LOC_BASE + 10_000
    for level in CASTLE_LEVELS:
        table[castle_level_name(level)] = global_base + level
    table[FULL_UPGRADE_LOCATION] = global_base + 100
    for i, name in enumerate(HERMIT_BUILDINGS.values()):
        table[name] = global_base + 300 + i
    table[GRASS_LOCATION] = global_base + 400
    table[WATER_LOCATION] = global_base + 401

    for category, milestone in MILESTONES.items():
        if category in BUILDING_RATES:
            max_count = floor(BUILDING_RATES[category] * MAX_ISLANDS)
        else:
            max_count = MAX_MILESTONES_PER_ISLAND * MAX_ISLANDS
        thresholds = {interval * i for interval in milestone.intervals for i in range(1, max_count + 1)}
        for n in sorted(thresholds):
            table[milestone_name(category, n)] = MILESTONE_BASE + milestone.id_slot * 100_000 + n
    return table


LOCATION_NAME_TO_ID: dict[str, int] = _all_location_names_to_ids()

LOCATION_NAME_GROUPS: dict[str, set[str]] = {
    f"Island {island}": {name for name in LOCATION_NAME_TO_ID if name.startswith(f"Island {island}:")}
    for island in range(1, MAX_ISLANDS + 1)
}
LOCATION_NAME_GROUPS["Portals"] = {name for name in LOCATION_NAME_TO_ID if "Destroy Portal" in name}
LOCATION_NAME_GROUPS["Chests"] = {name for name in LOCATION_NAME_TO_ID if "Chest" in name}
LOCATION_NAME_GROUPS["Gem Costs"] = {
    name for name in LOCATION_NAME_TO_ID
    if any(f": {PROGRESSIVE_KINDS[kind][0]} " in name or name.startswith(f"{PROGRESSIVE_KINDS[kind][0]} ")
           for kind in GEM_KINDS)
}
LOCATION_NAME_GROUPS["Castle"] = {castle_level_name(level) for level in CASTLE_LEVELS} | {FULL_UPGRADE_LOCATION}
LOCATION_NAME_GROUPS["Milestones"] = {
    name for name in LOCATION_NAME_TO_ID
    if name.startswith(("Slay ", "Survive ", "Fell ", "Build ", "Recruit ", "Hunt ", "Bribe ", "Spend "))
    and ":" not in name and not name.startswith("Build the ")
}
LOCATION_NAME_GROUPS["Buildings"] = {
    name for name in LOCATION_NAME_TO_ID if name.startswith(("Build ", "Hermit Building")) or ": Build the" in name
}
LOCATION_NAME_GROUPS["Powers"] = {name for name in LOCATION_NAME_TO_ID if "Puzzle" in name or "'s Quest" in name}


def milestone_thresholds(world, category: str) -> list[int]:
    if MILESTONE_REQUIREMENTS[category][1] > available_islands(world):
        return []
    interval = milestone_interval(world, category)
    count = floor(milestone_per_island(world, category) * goal_islands(world))
    return [interval * i for i in range(1, count + 1)]


def _add(world, region_name: str, name: str) -> None:
    region = world.multiworld.get_region(region_name, world.player)
    region.locations.append(KingdomTwoCrownsLocation(world.player, name, LOCATION_NAME_TO_ID[name], region))


def create_all_locations(world) -> None:
    campaign = world.campaign
    if campaign.is_campaign_wide:
        region = world.home_region
        for kind in PROGRESSIVE_KINDS:
            for k in range(1, getattr(campaign.totals, kind) + 1):
                _add(world, region, campaign_progressive_name(kind, k))
        for island in OLYMPUS_ISLANDS:
            _add(world, region, olympus_reach_name(island))
        for god in OLYMPUS_GOD_QUESTS:
            _add(world, region, olympus_god_quest_name(god))
        for place in OLYMPUS_OTHER_QUESTS:
            _add(world, region, olympus_other_quest_name(place))
        _add(world, region, OLYMPUS_DOG_LOCATION)
        _add(world, region, OLYMPUS_GOAL_LOCATION)
        _add(world, region, quarry_name(0))
        _add(world, region, mine_name(0))
    else:
        for island in world.islands_in_play:
            content = campaign.islands[island]
            region = f"Island {island}"
            _add(world, region, boat_name(island))
            if island > 1:
                _add(world, region, arrive_name(island))
            _add(world, region, liberate_name(island))
            for kind in PROGRESSIVE_KINDS:
                for k in range(1, getattr(content, kind) + 1):
                    _add(world, region, island_progressive_name(island, kind, k))
            if content.puzzle:
                _add(world, region, puzzle_name(island, content.puzzle))
        for island, name in ((campaign.dog_island, DOG_LOCATION), (campaign.quarry_island, quarry_name(campaign.quarry_island)),
                             (campaign.mine_island, mine_name(campaign.mine_island))):
            if island in world.islands_in_play:
                _add(world, f"Island {island}", name)

    for level in CASTLE_LEVELS:
        _add(world, world.home_region, castle_level_name(level))
    _add(world, world.home_region, FULL_UPGRADE_LOCATION)
    if world.options.hermit_building_checks:
        for hermit in hermits_in_play(world):
            _add(world, world.home_region, HERMIT_BUILDINGS[hermit])
    if world.options.grass_check:
        _add(world, world.home_region, GRASS_LOCATION)
    if world.options.sunk_coins_check:
        _add(world, world.home_region, WATER_LOCATION)

    for category in MILESTONES:
        for threshold in milestone_thresholds(world, category):
            _add(world, world.home_region, milestone_name(category, threshold))
