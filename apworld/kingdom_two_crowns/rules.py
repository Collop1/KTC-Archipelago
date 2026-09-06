"""Access rules for Kingdom Two Crowns.

Sourced from the `Game Flow` design notes:
    "Stone (Gain ability to break portals)"
    "Iron (Gain ability to liberate Islands)"
    "Liberating all 5 Islands is Goal"

Everything else (statues, hermits, mounts, coin/gem chests) is paid for with
in-game currency the mod doesn't track through Archipelago, so those checks
have no logic requirement beyond reaching their region.
"""
from worlds.generic.Rules import set_rule

from .locations import LOCATIONS_BY_REGION


def set_all_location_rules(world) -> None:
    for region_locations in LOCATIONS_BY_REGION.values():
        for name in region_locations:
            if name.startswith("Destroy Portal Progressive"):
                set_rule(
                    world.multiworld.get_location(name, world.player),
                    lambda state: state.has("Stone Technology", world.player),
                )
            elif name.endswith("Liberation"):
                set_rule(
                    world.multiworld.get_location(name, world.player),
                    lambda state: state.has("Iron Technology", world.player),
                )


def set_completion_rule(world) -> None:
    liberation_locations = [
        f"Island {n} Liberation" for n in range(1, 6)
    ]
    world.multiworld.completion_condition[world.player] = lambda state: all(
        state.can_reach_location(name, world.player) for name in liberation_locations
    )


def set_all_rules(world) -> None:
    set_all_location_rules(world)
    set_completion_rule(world)
