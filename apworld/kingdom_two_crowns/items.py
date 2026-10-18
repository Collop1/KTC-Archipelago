"""Item table for Kingdom Two Crowns.

The mod only ever refers to items by *name* (ids come from the server's data package at runtime), so this
file is the single source of truth for ids. The names must match mod/Archipelago/GameData.cs exactly.
Which statues, hermits, mounts and powers go in the pool depends on the campaign (see campaigns.py).
"""
from typing import NamedTuple

from BaseClasses import Item, ItemClassification

from .campaigns import CAMPAIGNS, Campaign
from .locations import hermits_in_play, powers_in_play

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

    # --- Islands (only with island_unlocks, and only for campaigns with numbered islands) ---
    "Island 2 Unlock": ItemData(ITEM_BASE + 10, P, "Islands"),
    "Island 3 Unlock": ItemData(ITEM_BASE + 11, P, "Islands"),
    "Island 4 Unlock": ItemData(ITEM_BASE + 12, P, "Islands"),
    "Island 5 Unlock": ItemData(ITEM_BASE + 13, P, "Islands"),
    "Island 6 Unlock": ItemData(ITEM_BASE + 14, P, "Islands"),

    # --- Statues: unlocking one with gems is a check; activating its blessing needs the item ---
    "Statue of Archery": ItemData(ITEM_BASE + 20, U, "Statues"),
    "Statue of Building": ItemData(ITEM_BASE + 21, U, "Statues"),
    "Statue of Knights": ItemData(ITEM_BASE + 22, U, "Statues"),
    "Statue of Farming": ItemData(ITEM_BASE + 23, U, "Statues"),
    "Statue of Pikemen": ItemData(ITEM_BASE + 24, U, "Statues"),

    # --- Hermits: freeing one from its cabin is a check; paying it to build needs the item ---
    "Ballista Hermit": ItemData(ITEM_BASE + 30, U, "Hermits"),
    "Bakery Hermit": ItemData(ITEM_BASE + 31, U, "Hermits"),
    "Horn Hermit": ItemData(ITEM_BASE + 32, U, "Hermits"),
    "Stable Hermit": ItemData(ITEM_BASE + 33, U, "Hermits"),
    "Warrior Hermit": ItemData(ITEM_BASE + 34, U, "Hermits"),
    "Fire Hermit": ItemData(ITEM_BASE + 35, U, "Hermits"),

    # --- Mounts: paying a mount shrine is a check; riding that mount needs the item ---
    "Griffin Mount": ItemData(ITEM_BASE + 40, U, "Mounts"),
    "Stag Mount": ItemData(ITEM_BASE + 41, U, "Mounts"),
    "Warhorse Mount": ItemData(ITEM_BASE + 42, U, "Mounts"),
    "Bear Mount": ItemData(ITEM_BASE + 43, U, "Mounts"),
    "Lizard Mount": ItemData(ITEM_BASE + 44, U, "Mounts"),
    "Unicorn Mount": ItemData(ITEM_BASE + 45, U, "Mounts"),
    "Stamina Horse": ItemData(ITEM_BASE + 46, U, "Mounts"),
    "Kirin Mount": ItemData(ITEM_BASE + 49, U, "Mounts"),
    "Gamigin Mount": ItemData(ITEM_BASE + 50, U, "Mounts"),
    "Golem Mount": ItemData(ITEM_BASE + 51, U, "Mounts"),
    "Beetle Mount": ItemData(ITEM_BASE + 52, U, "Mounts"),
    "Hrimfaxe Mount": ItemData(ITEM_BASE + 53, U, "Mounts"),
    "Cat Cart Mount": ItemData(ITEM_BASE + 54, U, "Mounts"),
    "Reindeer Mount": ItemData(ITEM_BASE + 55, U, "Mounts"),
    "Sleipnir Mount": ItemData(ITEM_BASE + 56, U, "Mounts"),
    "Kelpie Mount": ItemData(ITEM_BASE + 57, U, "Mounts"),
    "Gullinbursti Mount": ItemData(ITEM_BASE + 58, U, "Mounts"),
    "Wolf Mount": ItemData(ITEM_BASE + 59, U, "Mounts"),
    "Molossian Hound Mount": ItemData(ITEM_BASE + 60, U, "Mounts"),
    "Chimera Mount": ItemData(ITEM_BASE + 61, U, "Mounts"),
    "Spider Mount": ItemData(ITEM_BASE + 62, U, "Mounts"),
    "Donkey Mount": ItemData(ITEM_BASE + 63, U, "Mounts"),
    "Cerberus Mount": ItemData(ITEM_BASE + 64, U, "Mounts"),
    "Hippocampus Mount": ItemData(ITEM_BASE + 65, U, "Mounts"),
    "Chariot Mount": ItemData(ITEM_BASE + 66, U, "Mounts"),
    "Pegasus Mount": ItemData(ITEM_BASE + 67, U, "Mounts"),

    # --- Powers (Items of Power, Norselands and Call of Olympus): earning one in the game is a check;
    #     using it needs the item ---
    "Power of Thor": ItemData(ITEM_BASE + 80, U, "Powers"),
    "Power of Heimdall": ItemData(ITEM_BASE + 81, U, "Powers"),
    "Power of Hel": ItemData(ITEM_BASE + 82, U, "Powers"),
    "Power of Loki": ItemData(ITEM_BASE + 83, U, "Powers"),
    "Power of Artemis": ItemData(ITEM_BASE + 84, U, "Powers"),
    "Power of Athena": ItemData(ITEM_BASE + 85, U, "Powers"),
    "Power of Hephaestus": ItemData(ITEM_BASE + 86, U, "Powers"),
    "Power of Hermes": ItemData(ITEM_BASE + 87, U, "Powers"),

    # --- Gems: chests give no gems in this randomizer, so these pay for statues, shrines and cabins ---
    "Gem Pouch": ItemData(ITEM_BASE + 90, P, "Gems"),  # +3 gems

    # --- Filler ---
    "Coin Pouch": ItemData(ITEM_BASE + 100, F, "Filler"),  # +3 to 7 coins
    "Coin Sack": ItemData(ITEM_BASE + 101, F, "Filler"),  # +10 coins
    "Gem": ItemData(ITEM_BASE + 102, F, "Filler"),  # +1 gem
    "Vagrants": ItemData(ITEM_BASE + 103, F, "Filler"),  # spawns vagrants_per_item at the town centre

    # Junk: does nothing, just flavour. "(Junk)" in the name so nobody mistakes one for something useful.
    "Sunken Gold (Junk)": ItemData(ITEM_BASE + 120, F, "Junk"),
    "Broken Crown (Junk)": ItemData(ITEM_BASE + 121, F, "Junk"),
    "Tuft of Grass (Junk)": ItemData(ITEM_BASE + 122, F, "Junk"),
    "Greed Mask Shard (Junk)": ItemData(ITEM_BASE + 123, F, "Junk"),
    "Soggy Bread (Junk)": ItemData(ITEM_BASE + 124, F, "Junk"),
    "Bent Arrow (Junk)": ItemData(ITEM_BASE + 125, F, "Junk"),
    "Lost Banner (Junk)": ItemData(ITEM_BASE + 126, F, "Junk"),

    # --- Traps ---
    "Trip Trap": ItemData(ITEM_BASE + 200, T, "Traps"),  # crown is knocked off your head
    "Slippery Pouch Trap": ItemData(ITEM_BASE + 201, T, "Traps"),  # drop all coins and gems
    "Wave Back Trap": ItemData(ITEM_BASE + 202, T, "Traps"),  # portal retaliation wave right now
    "Waves Gone By Trap": ItemData(ITEM_BASE + 203, T, "Traps"),  # future waves on this island are N days stronger
    "Damned Night Trap": ItemData(ITEM_BASE + 204, T, "Traps"),  # night falls immediately
    "Decay Trap": ItemData(ITEM_BASE + 205, T, "Traps"),  # outermost walls drop a tier
    "Stam Dam Trap": ItemData(ITEM_BASE + 206, T, "Traps"),  # your mount loses all its stamina
    "Desertion Trap": ItemData(ITEM_BASE + 207, T, "Traps"),  # some subjects give up and become vagrants again
    "Tool Theft Trap": ItemData(ITEM_BASE + 208, T, "Traps"),  # Greed steal tools from the racks, then from units
}

GEMS_PER_POUCH = 3
# Gem Pouches in the pool cover this much more than every gem cost, so a few lost or misspent gems don't strand you.
GEM_POUCH_MARGIN = 1.5

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
    "stam_dam_trap_weight": "Stam Dam Trap",
    "desertion_trap_weight": "Desertion Trap",
    "tool_theft_trap_weight": "Tool Theft Trap",
}


def create_item(world, name: str) -> KingdomTwoCrownsItem:
    data = ITEM_TABLE[name]
    classification = data.classification
    # Item-link group worlds never run generate_early, so they have no campaign; err on the side of progression.
    campaign = getattr(world, "campaign", None)
    # Olympus' final battle is logically gated on all four god powers.
    if data.group == "Powers" and (campaign is None or campaign.is_campaign_wide):
        classification = P
    # Each hermit's building check needs that hermit (only hermits on islands in the seed have one).
    if data.group == "Hermits" and world.options.hermit_building_checks \
            and (campaign is None or name in hermits_in_play(world)):
        classification = P
    return KingdomTwoCrownsItem(name, classification, data.id, world.player)


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


def campaign_item_names(world) -> list[str]:
    campaign: Campaign = world.campaign
    names: list[str] = []
    for name, data in ITEM_TABLE.items():
        if data.group == "Buildings":
            names += [name] * data.count
    if world.options.island_unlocks and not campaign.is_campaign_wide:
        names += [f"Island {island} Unlock" for island in world.islands_in_play if island > 1]
    # Statues and mounts stay even with fewer islands: which island has which isn't fixed in every campaign.
    names += list(campaign.statues) + hermits_in_play(world) + list(campaign.mounts) + powers_in_play(world)
    names += ["Gem Pouch"] * gem_pouch_count(campaign, world.islands_in_play)
    return names


def gem_pouch_count(campaign: Campaign, islands_in_play: list[int] = ()) -> int:
    """Enough pouches for every gem cost on the islands in the seed (all of them for Olympus), plus the margin."""
    total = campaign.total_gem_cost
    if islands_in_play and not campaign.is_campaign_wide:
        total = campaign.cumulative_gem_cost(max(islands_in_play))
    return -(-int(total * GEM_POUCH_MARGIN) // GEMS_PER_POUCH)


def create_all_items(world) -> None:
    itempool = [create_item(world, name) for name in campaign_item_names(world)]

    unfilled = len(world.multiworld.get_unfilled_locations(world.player))
    if len(itempool) > unfilled:
        # A short goal with most checks switched off can leave more items than places to put them. The rest go
        # in your starting inventory, least important first: statues, mounts and the like, then Gem Pouches,
        # and only then anything else.
        world.random.shuffle(itempool)
        itempool.sort(key=lambda item: 0 if not item.advancement else 1 if item.name == "Gem Pouch" else 2)
        excess = len(itempool) - unfilled
        for item in itempool[:excess]:
            world.multiworld.push_precollected(item)
        itempool = itempool[excess:]
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


assert all(name in ITEM_TABLE for c in CAMPAIGNS.values() for name in c.statues + c.hermits + c.mounts + c.powers)
