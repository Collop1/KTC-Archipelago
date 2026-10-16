"""Location table for Kingdom Two Crowns.

The mod builds the same names (mod/Archipelago/GameData.cs) and looks their ids up in the server's data
package, so names here and there must match exactly. Ids are derived from a fixed formula so they never
shift when options or the content table change.

Per-island content counts come from the `Game Flow` notes (docs/Game Flow.md). Checks on an island are
"progressive": the first coin chest you open on island 3 sends "Island 3: Coin Chest 1", the next one
"Island 3: Coin Chest 2" and so on, regardless of which physical chest it was.
"""
from typing import NamedTuple

from BaseClasses import Location

LOC_BASE = 7_560_000
MILESTONE_BASE = 7_600_000

ISLANDS = range(1, 6)


class KingdomTwoCrownsLocation(Location):
    game = "Kingdom Two Crowns"


class IslandContent(NamedTuple):
    coin_chests: int
    gem_chests: int
    portals: int
    statues: int
    mount_shrines: int
    hermit_cabins: int


ISLAND_CONTENT: dict[int, IslandContent] = {
    1: IslandContent(coin_chests=3, gem_chests=0, portals=1, statues=1, mount_shrines=1, hermit_cabins=1),
    2: IslandContent(coin_chests=2, gem_chests=3, portals=3, statues=1, mount_shrines=1, hermit_cabins=1),
    3: IslandContent(coin_chests=3, gem_chests=3, portals=5, statues=1, mount_shrines=2, hermit_cabins=1),
    4: IslandContent(coin_chests=1, gem_chests=3, portals=7, statues=0, mount_shrines=2, hermit_cabins=1),
    5: IslandContent(coin_chests=0, gem_chests=3, portals=7, statues=1, mount_shrines=1, hermit_cabins=1),
}

# kind -> (name template, id slot). Progressive kinds get " {k}" appended.
PROGRESSIVE_KINDS: dict[str, tuple[str, int]] = {
    "portals": ("Destroy Portal", 1),
    "coin_chests": ("Coin Chest", 2),
    "gem_chests": ("Gem Chest", 3),
    "statues": ("Statue", 4),
    "mount_shrines": ("Mount Shrine", 5),
    "hermit_cabins": ("Hermit Cabin", 6),
}

# Kinds that cost gems to pay for.
GEM_KINDS = {"statues", "mount_shrines", "hermit_cabins"}


def island_progressive_name(island: int, kind: str, k: int) -> str:
    return f"Island {island}: {PROGRESSIVE_KINDS[kind][0]} {k}"


def boat_name(island: int) -> str:
    return f"Island {island}: Build the Boat"


def arrive_name(island: int) -> str:
    return f"Island {island}: Arrive"


def liberate_name(island: int) -> str:
    return f"Island {island}: Liberate"


DOG_LOCATION = "Island 2: Free the Dog"
FULL_UPGRADE_LOCATION = "Fully Upgrade an Island"


def castle_level_name(level: int) -> str:
    return f"Castle Level {level}"


def forest_name(k: int) -> str:
    return f"Clear Forest {k}"


CASTLE_LEVELS = range(2, 8)
MAX_FOREST_CHECKS = 5


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


def _all_location_names_to_ids() -> dict[str, int]:
    table: dict[str, int] = {}
    for island, content in ISLAND_CONTENT.items():
        island_base = LOC_BASE + island * 1000
        table[boat_name(island)] = island_base + 0
        if island > 1:
            table[arrive_name(island)] = island_base + 1
        table[liberate_name(island)] = island_base + 2
        for kind, (_, slot) in PROGRESSIVE_KINDS.items():
            for k in range(1, getattr(content, kind) + 1):
                table[island_progressive_name(island, kind, k)] = island_base + slot * 100 + k
    table[DOG_LOCATION] = LOC_BASE + 2000 + 3

    global_base = LOC_BASE + 10_000
    for level in CASTLE_LEVELS:
        table[castle_level_name(level)] = global_base + level
    table[FULL_UPGRADE_LOCATION] = global_base + 100
    for k in range(1, MAX_FOREST_CHECKS + 1):
        table[forest_name(k)] = global_base + 200 + k

    for category, milestone in MILESTONES.items():
        thresholds = {interval * i for interval in milestone.intervals for i in range(1, milestone.max_count + 1)}
        for n in sorted(thresholds):
            table[milestone_name(category, n)] = MILESTONE_BASE + milestone.id_slot * 10_000 + n
    return table


LOCATION_NAME_TO_ID: dict[str, int] = _all_location_names_to_ids()

LOCATION_NAME_GROUPS: dict[str, set[str]] = {
    f"Island {island}": {name for name in LOCATION_NAME_TO_ID if name.startswith(f"Island {island}:")}
    for island in ISLANDS
}
LOCATION_NAME_GROUPS["Portals"] = {name for name in LOCATION_NAME_TO_ID if "Destroy Portal" in name}
LOCATION_NAME_GROUPS["Chests"] = {name for name in LOCATION_NAME_TO_ID if "Chest" in name}
LOCATION_NAME_GROUPS["Gem Costs"] = {
    name for name in LOCATION_NAME_TO_ID
    if any(f": {PROGRESSIVE_KINDS[kind][0]} " in name for kind in GEM_KINDS)
}
LOCATION_NAME_GROUPS["Castle"] = {castle_level_name(level) for level in CASTLE_LEVELS} | {FULL_UPGRADE_LOCATION}
LOCATION_NAME_GROUPS["Milestones"] = {
    name for name in LOCATION_NAME_TO_ID if name.startswith(("Slay ", "Survive ", "Fell ", "Clear Forest"))
}


def milestone_thresholds(world, category: str) -> list[int]:
    milestone = MILESTONES[category]
    interval = getattr(world.options, milestone.interval_option).value
    count = getattr(world.options, milestone.count_option).value
    return [interval * i for i in range(1, count + 1)]


def _add(world, region_name: str, name: str) -> None:
    region = world.multiworld.get_region(region_name, world.player)
    region.locations.append(KingdomTwoCrownsLocation(world.player, name, LOCATION_NAME_TO_ID[name], region))


def create_all_locations(world) -> None:
    for island, content in ISLAND_CONTENT.items():
        region = f"Island {island}"
        _add(world, region, boat_name(island))
        if island > 1:
            _add(world, region, arrive_name(island))
        _add(world, region, liberate_name(island))
        for kind in PROGRESSIVE_KINDS:
            for k in range(1, getattr(content, kind) + 1):
                _add(world, region, island_progressive_name(island, kind, k))
    _add(world, "Island 2", DOG_LOCATION)

    for level in CASTLE_LEVELS:
        _add(world, "Island 1", castle_level_name(level))
    _add(world, "Island 1", FULL_UPGRADE_LOCATION)
    for k in range(1, world.options.forest_checks.value + 1):
        _add(world, "Island 1", forest_name(k))

    for category in MILESTONES:
        for threshold in milestone_thresholds(world, category):
            _add(world, "Island 1", milestone_name(category, threshold))
