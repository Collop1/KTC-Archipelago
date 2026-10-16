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
from typing import NamedTuple

from BaseClasses import Location

from .campaigns import (
    CAMPAIGNS, MAX_ISLANDS, OLYMPUS_GOD_QUESTS, OLYMPUS_ISLANDS, OLYMPUS_OTHER_QUESTS, IslandContent,
)

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


DOG_LOCATION = "Island 2: Free the Dog"
OLYMPUS_DOG_LOCATION = "Free the Dog"
OLYMPUS_GOAL_LOCATION = "Defeat the World Serpent"
FULL_UPGRADE_LOCATION = "Fully Upgrade an Island"


def castle_level_name(level: int) -> str:
    return f"Castle Level {level}"


CASTLE_LEVELS = range(2, 8)


class Milestone(NamedTuple):
    template: str  # formatted with n = threshold
    interval_option: str
    count_option: str
    intervals: tuple[int, ...]
    id_slot: int
    max_count: int = 20


MILESTONES: dict[str, Milestone] = {
    "greed": Milestone("Slay {n} Greed", "greed_kill_interval", "greed_kill_checks", (10, 20, 25, 50), 0),
    "greedlings": Milestone("Slay {n} Greedlings", "greedling_kill_interval", "greedling_kill_checks",
                            (10, 20, 25, 50), 1),
    "floaters": Milestone("Slay {n} Floaters", "floater_kill_interval", "floater_kill_checks", (2, 3, 5, 10), 2),
    "breeders": Milestone("Slay {n} Breeders", "breeder_kill_interval", "breeder_kill_checks", (1, 2, 3, 5), 3),
    "crown_stealers": Milestone("Slay {n} Crown Stealers", "crown_stealer_kill_interval",
                                "crown_stealer_kill_checks", (1, 2, 3, 5), 4),
    "nights": Milestone("Survive {n} Nights", "night_interval", "night_checks", (3, 5, 10), 5),
    "trees": Milestone("Fell {n} Trees", "tree_interval", "tree_checks", (5, 10, 20, 25), 6),
}


def milestone_name(category: str, threshold: int) -> str:
    return MILESTONES[category].template.format(n=threshold)


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
    table[OLYMPUS_GOAL_LOCATION] = OLYMPUS_BASE + 999

    global_base = LOC_BASE + 10_000
    for level in CASTLE_LEVELS:
        table[castle_level_name(level)] = global_base + level
    table[FULL_UPGRADE_LOCATION] = global_base + 100

    for category, milestone in MILESTONES.items():
        thresholds = {interval * i for interval in milestone.intervals for i in range(1, milestone.max_count + 1)}
        for n in sorted(thresholds):
            table[milestone_name(category, n)] = MILESTONE_BASE + milestone.id_slot * 10_000 + n
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
    name for name in LOCATION_NAME_TO_ID if name.startswith(("Slay ", "Survive ", "Fell "))
}
LOCATION_NAME_GROUPS["Powers"] = {name for name in LOCATION_NAME_TO_ID if "Puzzle" in name or "'s Quest" in name}


def milestone_thresholds(world, category: str) -> list[int]:
    milestone = MILESTONES[category]
    interval = getattr(world.options, milestone.interval_option).value
    count = getattr(world.options, milestone.count_option).value
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
    else:
        for island, content in campaign.islands.items():
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
        _add(world, f"Island {campaign.dog_island}", DOG_LOCATION)

    for level in CASTLE_LEVELS:
        _add(world, world.home_region, castle_level_name(level))
    _add(world, world.home_region, FULL_UPGRADE_LOCATION)

    for category in MILESTONES:
        for threshold in milestone_thresholds(world, category):
            _add(world, world.home_region, milestone_name(category, threshold))
