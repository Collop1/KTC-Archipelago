"""Location table for Kingdom Two Crowns, mirroring mod/Archipelago/APIds.cs.

Every id here must match the corresponding APIDRegistry.AddNew(...) call in the
mod exactly - the client and the generator have to agree on the wire format.

Per-island grouping below is taken from the `Game Flow` design notes
(docs/Game Flow), which give an exact per-island count of statues, mounts,
hermits, coin chests, gem chests and portals that lines up with the ids
registered in the mod:
    Island 1: 3 coin chests, 1 portal
    Island 2: 2 coin chests, 3 gem chests, 3 portals
    Island 3: 3 coin chests, 3 gem chests, 5 portals
    Island 4: 1 coin chest,  3 gem chests, 7 portals
    Island 5:                3 gem chests, 7 portals
    (9 coin chests / 12 gem chests / 23 portals total - matches exactly)

The mod also registers 17 mount-unlock locations, but Game Flow only names 7
base-game mounts (one per island, two on Island 3, two on Island 4). The
other 10 (Wolf P1, Horse Fast, Griffin Skull P1, Horse Stamina,
Wolf_norselands, Reindeer, Horse Burst, Rainbow Pony, Warhorse Plague P1,
Wolf Norselands P1, Unicorn Dark) look like hidden/secret or DLC mount
variants that aren't covered by those notes. They're grouped into a
"Secrets" region below as a placeholder - please correct which island (or
none) actually holds each one.
"""
from BaseClasses import Location


class KingdomTwoCrownsLocation(Location):
    game = "Kingdom Two Crowns"


def _portal_range(first: int, last: int) -> dict[str, int]:
    # Portal N is registered at id 755066 + N (Destroy Portal Progressive 1 = 755067).
    return {f"Destroy Portal Progressive {n}": 755066 + n for n in range(first, last + 1)}


ISLAND_1_LOCATIONS: dict[str, int] = {
    "Archery Unlocks": 755171,
    "Griffin P1 Unlocks": 755140,
    "Hermit Ballista Unlock": 755096,
    "Island 1 Liberation": 755106,
    "Progressive Coin Chest 1": 755107,
    "Progressive Coin Chest 2": 755108,
    "Progressive Coin Chest 3": 755109,
    **_portal_range(1, 1),
}

ISLAND_2_LOCATIONS: dict[str, int] = {
    "Scythe Unlocks": 755172,
    "Stag Unlocks": 755134,
    "Hermit Stable Unlock": 755102,
    "Island 2 Dog": 755116,
    "Island 2 Liberation": 755110,
    "Progressive Coin Chest 4": 755111,
    "Progressive Coin Chest 5": 755112,
    "Progressive Coin Gem 1": 755113,
    "Progressive Coin Gem 2": 755114,
    "Progressive Coin Gem 3": 755115,
    **_portal_range(2, 4),
}

ISLAND_3_LOCATIONS: dict[str, int] = {
    "Building Unlocks": 755173,
    "Warhorse P1 Unlocks": 755138,
    # "Draft horse" from Game Flow - best guess mapping, please confirm:
    "Horse Fast Unlocks": 755139,
    "Hermit Bakery Unlock": 755098,
    "Island 3 Liberation": 755117,
    "Progressive Coin Chest 6": 755118,
    "Progressive Coin Chest 7": 755119,
    "Progressive Coin Chest 8": 755120,
    "Progressive Coin Gem 4": 755121,
    "Progressive Coin Gem 5": 755122,
    "Progressive Coin Gem 6": 755123,
    **_portal_range(5, 9),
}

ISLAND_4_LOCATIONS: dict[str, int] = {
    "Bear Unlocks": 755133,
    "Lizard Unlocks": 755135,
    "Hermit Warrior Unlock": 755104,
    "Island 4 Liberation": 755124,
    "Progressive Coin Chest 9": 755125,
    "Progressive Coin Gem 7": 755126,
    "Progressive Coin Gem 8": 755127,
    "Progressive Coin Gem 9": 755128,
    **_portal_range(10, 16),
}

ISLAND_5_LOCATIONS: dict[str, int] = {
    "Knights Unlocks": 755174,
    "Unicorn Unlocks": 755136,
    "Hermit Horn Unlock": 755100,
    "Island 5 Liberation": 755129,
    "Progressive Coin Gem 10": 755130,
    "Progressive Coin Gem 11": 755131,
    "Progressive Coin Gem 12": 755132,
    **_portal_range(17, 23),
}

# Placeholder grouping for mount unlocks not accounted for by Game Flow - see
# module docstring. Not tied to a specific island/region requirement yet.
SECRET_LOCATIONS: dict[str, int] = {
    "Wolf P1 Unlocks": 755137,
    "Griffin Skull P1 Unlocks": 755141,
    "Horse Stamina Unlocks": 755142,
    "Wolf_norselands Unlocks": 755143,
    "Reindeer Unlocks": 755144,
    "Horse Burst Unlocks": 755145,
    "Rainbow Pony Unlocks": 755146,
    "Warhorse Plague P1 Unlocks": 755147,
    "Wolf Norselands P1 Unlocks": 755148,
    "Unicorn Dark Unlocks": 755149,
}

LOCATIONS_BY_REGION: dict[str, dict[str, int]] = {
    "Island 1": ISLAND_1_LOCATIONS,
    "Island 2": ISLAND_2_LOCATIONS,
    "Island 3": ISLAND_3_LOCATIONS,
    "Island 4": ISLAND_4_LOCATIONS,
    "Island 5": ISLAND_5_LOCATIONS,
    "Secrets": SECRET_LOCATIONS,
}

LOCATION_NAME_TO_ID: dict[str, int] = {
    name: location_id
    for region_locations in LOCATIONS_BY_REGION.values()
    for name, location_id in region_locations.items()
}


def create_all_locations(world) -> None:
    for region_name, region_locations in LOCATIONS_BY_REGION.items():
        region = world.multiworld.get_region(region_name, world.player)
        for name, location_id in region_locations.items():
            region.locations.append(KingdomTwoCrownsLocation(world.player, name, location_id, region))
