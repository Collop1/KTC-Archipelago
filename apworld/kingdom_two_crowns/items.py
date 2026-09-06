"""Item table for Kingdom Two Crowns, mirroring mod/Archipelago/APIds.cs.

Every id here must match the corresponding APIDRegistry.AddNew(...) call in the
mod exactly - the client and the generator have to agree on the wire format.
"""
from typing import NamedTuple

from BaseClasses import Item, ItemClassification


class KingdomTwoCrownsItem(Item):
    game = "Kingdom Two Crowns"


class ItemData(NamedTuple):
    id: int
    classification: ItemClassification


# --- Progressive Castle Unlock T1-T7 ---
# "Progressive" naming convention -> progression items.
_CASTLE_TIER_ITEMS = {
    "Progressive Castle Unlock T1": 755091,
    "Progressive Castle Unlock T2": 755092,
    "Progressive Castle Unlock T3": 755093,
    "Progressive Castle Unlock T4": 755094,
    "Progressive Castle Unlock T5": 755095,
    "Progressive Castle Unlock T6": 755177,
    "Progressive Castle Unlock T7": 755178,
}

# --- Hermit "Use" items: unblock payment for an already-built hermit ---
_HERMIT_USE_ITEMS = {
    "Hermit Ballista Use": 755097,
    "Hermit Bakery Use": 755099,
    "Hermit Horn Use": 755101,
    "Hermit Stable Use": 755103,
    "Hermit Warrior Use": 755105,
}

# --- Mount "Use" items: unblock payment for an already-found steed ---
_MOUNT_USE_ITEMS = {
    "Bear Use": 755150,
    "Stag Use": 755151,
    "Lizard Use": 755152,
    "Unicorn Use": 755153,
    "Wolf P1 Use": 755154,
    "Warhorse P1 Use": 755155,
    "Horse Fast Use": 755156,
    "Griffin P1 Use": 755157,
    "Griffin Skull P1 Use": 755158,
    "Horse Stamina Use": 755159,
    "Wolf_norselands Use": 755160,
    "Reindeer Use": 755161,
    "Horse Burst Use": 755162,
    "Rainbow Pony Use": 755163,
    "Warhorse Plague P1 Use": 755164,
    "Wolf Norselands P1 Use": 755165,
    "Unicorn Dark Use": 755166,
}

# --- Statue "Use" items: unblock payment for an already-found statue ---
_STATUE_USE_ITEMS = {
    "Archery Use": 755167,
    "Scythe Use": 755168,
    "Building Use": 755169,
    "Knights Use": 755170,
}

# --- Tech items: gate Stone/Iron age buildings and, per Game Flow notes,
#     Stone gates all "Destroy Portal" locations and Iron gates all
#     "Island Liberation" locations. See rules.py. ---
_TECH_ITEMS = {
    "Stone Technology": 755175,
    "Iron Technology": 755176,
}

# --- Junk / filler ---
_JUNK_ITEM_NAME = "Random Game Effect (junk)"
_JUNK_ITEM_ID = 755179

ITEM_TABLE: dict[str, ItemData] = {}
for _name, _id in _CASTLE_TIER_ITEMS.items():
    ITEM_TABLE[_name] = ItemData(_id, ItemClassification.progression)
for _name, _id in _HERMIT_USE_ITEMS.items():
    ITEM_TABLE[_name] = ItemData(_id, ItemClassification.useful)
for _name, _id in _MOUNT_USE_ITEMS.items():
    ITEM_TABLE[_name] = ItemData(_id, ItemClassification.useful)
for _name, _id in _STATUE_USE_ITEMS.items():
    ITEM_TABLE[_name] = ItemData(_id, ItemClassification.useful)
for _name, _id in _TECH_ITEMS.items():
    ITEM_TABLE[_name] = ItemData(_id, ItemClassification.progression)
ITEM_TABLE[_JUNK_ITEM_NAME] = ItemData(_JUNK_ITEM_ID, ItemClassification.filler)

ITEM_NAME_TO_ID: dict[str, int] = {name: data.id for name, data in ITEM_TABLE.items()}


def create_item(world, name: str) -> KingdomTwoCrownsItem:
    data = ITEM_TABLE[name]
    return KingdomTwoCrownsItem(name, data.classification, data.id, world.player)


def get_filler_item_name(world) -> str:
    # Only one filler type exists on the mod side today; swap this for a
    # weighted world.random.choice(...) once more junk items are added.
    return _JUNK_ITEM_NAME


def create_all_items(world) -> None:
    itempool: list[KingdomTwoCrownsItem] = []

    for name in ITEM_TABLE:
        if name == _JUNK_ITEM_NAME:
            continue
        itempool.append(create_item(world, name))

    unfilled_location_count = len(world.multiworld.get_unfilled_locations(world.player))
    needed_filler = unfilled_location_count - len(itempool)
    for _ in range(max(needed_filler, 0)):
        itempool.append(world.create_filler())

    world.multiworld.itempool += itempool
