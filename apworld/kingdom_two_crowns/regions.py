"""Region layout for Kingdom Two Crowns.

The mod doesn't gate sailing between islands behind any AP item (the boat is
always available), so every island is reachable straight from the hub.
Item-based gating (Stone Technology / Iron Technology) is applied per-location
in rules.py instead of as entrance rules, since it only blocks *some*
locations on an island (portals, liberation) and not others (chests, mounts).
"""
from BaseClasses import Region

from .locations import LOCATIONS_BY_REGION

ISLAND_REGION_NAMES = list(LOCATIONS_BY_REGION.keys())


def create_all_regions(world) -> None:
    menu = Region("Menu", world.player, world.multiworld)
    world.multiworld.regions.append(menu)

    for region_name in ISLAND_REGION_NAMES:
        region = Region(region_name, world.player, world.multiworld)
        world.multiworld.regions.append(region)


def connect_regions(world) -> None:
    menu = world.multiworld.get_region("Menu", world.player)
    for region_name in ISLAND_REGION_NAMES:
        region = world.multiworld.get_region(region_name, world.player)
        menu.connect(region, f"Menu -> {region_name}")


def create_and_connect_regions(world) -> None:
    create_all_regions(world)
    connect_regions(world)
