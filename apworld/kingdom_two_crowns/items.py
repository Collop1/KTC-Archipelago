"""Item table for Kingdom Two Crowns.

The mod only ever refers to items by *name* (ids come from the server's data
package at runtime), so this file is the single source of truth for ids. The
names themselves must match mod/Archipelago/GameData.cs exactly.
"""
from typing import NamedTuple

from BaseClasses import Item, ItemClassification

ITEM_BASE = 7_550_000


class KingdomTwoCrownsItem(Item):
    game = "Kingdom Two Crowns"


class ItemData(NamedTuple):
    id: int
    classification: ItemClassification
    group: str
    count: int = 1  # copies placed in the pool when this item is part of it


P = ItemClassification.progression
U = ItemClassification.useful
F = ItemClassification.filler
T = ItemClassification.trap

ITEM_TABLE: dict[str, ItemData] = {
    # --- Building tiers ---
    # Each copy lets the castle be upgraded one more level (Castle 1 -> 2 needs one copy, ... -> 7 needs six).
    "Progressive Castle": ItemData(ITEM_BASE + 0, P, "Buildings", 6),
    "Stone Technology": ItemData(ITEM_BASE + 1, P, "Buildings"),
    "Iron Technology": ItemData(ITEM_BASE + 2, P, "Buildings"),

    # --- Islands (only in the pool when island_unlocks is on) ---
    "Island 2 Unlock": ItemData(ITEM_BASE + 10, P, "Islands"),
    "Island 3 Unlock": ItemData(ITEM_BASE + 11, P, "Islands"),
    "Island 4 Unlock": ItemData(ITEM_BASE + 12, P, "Islands"),
    "Island 5 Unlock": ItemData(ITEM_BASE + 13, P, "Islands"),

    # --- Statues: unlocking one with gems is a check; activating its blessing needs the item ---
    "Statue of Archery": ItemData(ITEM_BASE + 20, U, "Statues"),
    "Statue of Building": ItemData(ITEM_BASE + 21, U, "Statues"),
    "Statue of Knights": ItemData(ITEM_BASE + 22, U, "Statues"),
    "Statue of Farming": ItemData(ITEM_BASE + 23, U, "Statues"),

    # --- Hermits: freeing one from its cabin is a check; paying it to build needs the item ---
    "Ballista Hermit": ItemData(ITEM_BASE + 30, U, "Hermits"),
    "Bakery Hermit": ItemData(ITEM_BASE + 31, U, "Hermits"),
    "Horn Hermit": ItemData(ITEM_BASE + 32, U, "Hermits"),
    "Stable Hermit": ItemData(ITEM_BASE + 33, U, "Hermits"),
    "Warrior Hermit": ItemData(ITEM_BASE + 34, U, "Hermits"),

    # --- Mounts: paying a mount shrine is a check; riding that mount needs the item ---
    "Griffin Mount": ItemData(ITEM_BASE + 40, U, "Mounts"),
    "Stag Mount": ItemData(ITEM_BASE + 41, U, "Mounts"),
    "Warhorse Mount": ItemData(ITEM_BASE + 42, U, "Mounts"),
    "Bear Mount": ItemData(ITEM_BASE + 43, U, "Mounts"),
    "Lizard Mount": ItemData(ITEM_BASE + 44, U, "Mounts"),
    "Unicorn Mount": ItemData(ITEM_BASE + 45, U, "Mounts"),
    "Stamina Horse": ItemData(ITEM_BASE + 46, U, "Mounts"),
    "Burst Horse": ItemData(ITEM_BASE + 47, U, "Mounts"),
    "Fast Horse": ItemData(ITEM_BASE + 48, U, "Mounts"),

    # --- Filler ---
    "Coin Pouch": ItemData(ITEM_BASE + 100, F, "Filler"),  # +5 coins
    "Coin Sack": ItemData(ITEM_BASE + 101, F, "Filler"),  # +10 coins
    "Gem": ItemData(ITEM_BASE + 102, F, "Filler"),  # +1 gem
    "Vagrants": ItemData(ITEM_BASE + 103, F, "Filler"),  # spawns vagrants_per_item at the town centre

    # Junk: does nothing, just flavour.
    "Sunken Gold": ItemData(ITEM_BASE + 120, F, "Junk"),
    "Broken Crown": ItemData(ITEM_BASE + 121, F, "Junk"),
    "Tuft of Grass": ItemData(ITEM_BASE + 122, F, "Junk"),
    "Greed Mask Shard": ItemData(ITEM_BASE + 123, F, "Junk"),
    "Soggy Bread": ItemData(ITEM_BASE + 124, F, "Junk"),
    "Bent Arrow": ItemData(ITEM_BASE + 125, F, "Junk"),
    "Lost Banner": ItemData(ITEM_BASE + 126, F, "Junk"),

    # --- Traps ---
    "Trip Trap": ItemData(ITEM_BASE + 200, T, "Traps"),  # crown is knocked off your head
    "Slippery Pouch Trap": ItemData(ITEM_BASE + 201, T, "Traps"),  # drop all coins and gems
    "Wave Back Trap": ItemData(ITEM_BASE + 202, T, "Traps"),  # portal retaliation wave right now
    "Waves Gone By Trap": ItemData(ITEM_BASE + 203, T, "Traps"),  # future waves on this island are N days stronger
    "Damned Night Trap": ItemData(ITEM_BASE + 204, T, "Traps"),  # night falls immediately
    "Decay Trap": ItemData(ITEM_BASE + 205, T, "Traps"),  # outermost walls drop a tier
}

ITEM_NAME_TO_ID: dict[str, int] = {name: data.id for name, data in ITEM_TABLE.items()}

ITEM_NAME_GROUPS: dict[str, set[str]] = {}
for _name, _data in ITEM_TABLE.items():
    ITEM_NAME_GROUPS.setdefault(_data.group, set()).add(_name)

JUNK_ITEMS = sorted(ITEM_NAME_GROUPS["Junk"])

# option attribute name -> trap item
TRAP_WEIGHT_OPTIONS: dict[str, str] = {
    "trip_trap_weight": "Trip Trap",
    "slippery_pouch_trap_weight": "Slippery Pouch Trap",
    "wave_back_trap_weight": "Wave Back Trap",
    "waves_gone_by_trap_weight": "Waves Gone By Trap",
    "damned_night_trap_weight": "Damned Night Trap",
    "decay_trap_weight": "Decay Trap",
}


def create_item(world, name: str) -> KingdomTwoCrownsItem:
    data = ITEM_TABLE[name]
    return KingdomTwoCrownsItem(name, data.classification, data.id, world.player)


def get_filler_item_name(world) -> str:
    options = world.options
    weights = {
        "coins": options.coin_filler_weight.value,
        "gems": options.gem_filler_weight.value,
        "vagrants": options.vagrant_filler_weight.value,
        "junk": options.junk_filler_weight.value,
    }
    if sum(weights.values()) == 0:
        weights["coins"] = 1
    kind = world.random.choices(list(weights), list(weights.values()))[0]
    if kind == "coins":
        return world.random.choice(["Coin Pouch", "Coin Pouch", "Coin Sack"])
    if kind == "gems":
        return "Gem"
    if kind == "vagrants":
        return "Vagrants"
    return world.random.choice(JUNK_ITEMS)


def _progression_item_names(world) -> list[str]:
    names: list[str] = []
    for name, data in ITEM_TABLE.items():
        if data.group in ("Buildings", "Statues", "Hermits", "Mounts"):
            names += [name] * data.count
        elif data.group == "Islands" and world.options.island_unlocks:
            names.append(name)
    return names


def create_all_items(world) -> None:
    itempool = [create_item(world, name) for name in _progression_item_names(world)]

    unfilled = len(world.multiworld.get_unfilled_locations(world.player))
    remaining = max(unfilled - len(itempool), 0)

    trap_weights = {
        trap: getattr(world.options, option).value for option, trap in TRAP_WEIGHT_OPTIONS.items()
    }
    trap_count = 0
    if sum(trap_weights.values()) > 0:
        trap_count = round(remaining * world.options.trap_percentage.value / 100)
    traps = list(trap_weights)
    weights = list(trap_weights.values())
    for _ in range(trap_count):
        itempool.append(create_item(world, world.random.choices(traps, weights)[0]))

    for _ in range(remaining - trap_count):
        itempool.append(world.create_filler())

    world.multiworld.itempool += itempool
