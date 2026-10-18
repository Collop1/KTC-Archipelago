"""Region layout for Kingdom Two Crowns.

Numbered-island campaigns: Menu -> Island 1 is free. The other islands either need their "Island N Unlock"
item (island_unlocks on) or open up in order like the base game (reach island N-1, build the boat, sail on).
Only the islands in play exist (world.islands_in_play: islands 1..N with only_goal_islands on).
Global checks (castle levels, milestones) live in Island 1 since you can always do them at home.

Call of Olympus opens its islands through its own quests, so it's a single region and island_unlocks
doesn't apply.
"""
from BaseClasses import Region

OLYMPUS_REGION = "Olympus"


def island_unlock_item(island: int) -> str:
    return f"Island {island} Unlock"


def create_and_connect_regions(world) -> None:
    player = world.player
    multiworld = world.multiworld
    campaign = world.campaign

    menu = Region("Menu", player, multiworld)
    multiworld.regions.append(menu)

    if campaign.is_campaign_wide:
        olympus = Region(OLYMPUS_REGION, player, multiworld)
        multiworld.regions.append(olympus)
        menu.connect(olympus, "Start Reign")
        return

    islands = {}
    for island in world.islands_in_play:
        islands[island] = Region(f"Island {island}", player, multiworld)
        multiworld.regions.append(islands[island])

    menu.connect(islands[1], "Start Reign")
    for island in world.islands_in_play:
        if island == 1:
            continue
        if world.options.island_unlocks:
            item = island_unlock_item(island)
            menu.connect(islands[island], f"Sail to Island {island}", lambda state, i=item: state.has(i, player))
        else:
            islands[island - 1].connect(islands[island], f"Sail to Island {island}")
