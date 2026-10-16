"""Region layout for Kingdom Two Crowns.

Menu -> Island 1 is free. Islands 2-5 either need their "Island N Unlock" item (island_unlocks on) or open
up in order like the base game (island_unlocks off: reach island N-1, build the boat, sail on).

Global checks (castle levels, milestones) live in Island 1 since you can always do them at home.
"""
from BaseClasses import Region

from .locations import ISLANDS


def island_unlock_item(island: int) -> str:
    return f"Island {island} Unlock"


def create_and_connect_regions(world) -> None:
    player = world.player
    multiworld = world.multiworld

    menu = Region("Menu", player, multiworld)
    multiworld.regions.append(menu)
    islands = {}
    for island in ISLANDS:
        islands[island] = Region(f"Island {island}", player, multiworld)
        multiworld.regions.append(islands[island])

    menu.connect(islands[1], "Start Reign")
    for island in ISLANDS:
        if island == 1:
            continue
        if world.options.island_unlocks:
            item = island_unlock_item(island)
            menu.connect(islands[island], f"Sail to Island {island}", lambda state, i=item: state.has(i, player))
        else:
            islands[island - 1].connect(islands[island], f"Sail to Island {island}")
