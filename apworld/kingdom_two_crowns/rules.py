"""Access rules for Kingdom Two Crowns.

Kept deliberately on the strict side, since being too lenient would put a required item behind a check
the player can't actually do yet:
  - Destroying portals needs Stone Technology and a castle upgraded at least four times (bombs).
  - Liberating an island (cliff portal) needs Stone + Iron Technology and five castle upgrades.
  - Statues, mount shrines and hermit cabins cost gems, which only come from islands 2+ (Olympus has gems
    from the start).
  - Castle Level 5+ needs Stone Technology, Castle Level 7 needs Iron Technology.
  - Later milestone checks (kills, nights) expect a sturdier kingdom and, for the rarer Greed, more islands.
  - Call of Olympus: the god quests expect a couple of castle upgrades, Mount Olympus expects all four god
    quests done, and defeating the serpent expects a strong castle, both techs and all four god powers.
"""
from math import floor

from worlds.generic.Rules import set_rule

from .campaigns import OLYMPUS_GOD_QUESTS, OLYMPUS_ISLANDS
from .locations import (
    CASTLE_LEVELS, FULL_UPGRADE_LOCATION, GEM_KINDS, MILESTONES, OLYMPUS_GOAL_LOCATION, PROGRESSIVE_KINDS,
    campaign_progressive_name, castle_level_name, island_progressive_name, liberate_name, milestone_name,
    milestone_thresholds, olympus_god_quest_name, olympus_reach_name,
)

CASTLE = "Progressive Castle"
STONE = "Stone Technology"
IRON = "Iron Technology"

# category -> (extra castle upgrades on top of the tier, minimum islands reachable)
MILESTONE_REQUIREMENTS: dict[str, tuple[int, int]] = {
    "greed": (0, 1),
    "greedlings": (0, 1),
    "floaters": (1, 2),
    "breeders": (2, 3),
    "crown_stealers": (1, 2),
    "nights": (0, 1),
    "trees": (-99, 1),
}


def can_destroy_portals(state, player: int) -> bool:
    return state.has(STONE, player) and state.has(CASTLE, player, 4)


def can_liberate(state, player: int) -> bool:
    return state.has_all((STONE, IRON), player) and state.has(CASTLE, player, 5)


def set_all_rules(world) -> None:
    player = world.player
    multiworld = world.multiworld
    campaign = world.campaign

    def location(name: str):
        return multiworld.get_location(name, player)

    if campaign.is_campaign_wide:
        _set_olympus_rules(world, location)
        islands_reachable = lambda state: 5  # noqa: E731 - Olympus opens its islands through quests
    else:
        islands = list(campaign.islands)

        def islands_reachable(state) -> int:
            return sum(state.can_reach_region(f"Island {i}", player) for i in islands)

        def has_gem_source(state) -> bool:
            return any(state.can_reach_region(f"Island {i}", player) for i in islands if i > 1)

        for island, content in campaign.islands.items():
            for kind in PROGRESSIVE_KINDS:
                for k in range(1, getattr(content, kind) + 1):
                    name = island_progressive_name(island, kind, k)
                    if kind == "portals":
                        set_rule(location(name), lambda state: can_destroy_portals(state, player))
                    elif kind in GEM_KINDS:
                        set_rule(location(name), has_gem_source)
            set_rule(location(liberate_name(island)), lambda state: can_liberate(state, player))

        goal = min(world.options.islands_to_liberate.value, campaign.island_count)
        liberations = [liberate_name(island) for island in islands]
        multiworld.completion_condition[player] = lambda state: sum(
            state.can_reach_location(name, player) for name in liberations
        ) >= goal

    for level in CASTLE_LEVELS:
        needed = [STONE] if level >= 5 else []
        if level >= 7:
            needed.append(IRON)
        set_rule(location(castle_level_name(level)),
                 lambda state, n=level - 1, req=tuple(needed): state.has(CASTLE, player, n)
                 and state.has_all(req, player))
    set_rule(location(FULL_UPGRADE_LOCATION),
             lambda state: state.has(CASTLE, player, 6) and state.has_all((STONE, IRON), player))

    for category in MILESTONES:
        thresholds = milestone_thresholds(world, category)
        extra_castle, min_islands = MILESTONE_REQUIREMENTS[category]
        for i, threshold in enumerate(thresholds, start=1):
            tier = floor(3 * (i - 1) / len(thresholds))
            castle_needed = max(0, tier + extra_castle)
            set_rule(location(milestone_name(category, threshold)),
                     lambda state, c=castle_needed, m=min_islands: state.has(CASTLE, player, c)
                     and (m <= 1 or islands_reachable(state) >= m))


def _set_olympus_rules(world, location) -> None:
    player = world.player
    campaign = world.campaign

    for k in range(1, campaign.totals.portals + 1):
        set_rule(location(campaign_progressive_name("portals", k)), lambda state: can_destroy_portals(state, player))

    for island in OLYMPUS_ISLANDS:
        if island.endswith("Quest Island"):
            set_rule(location(olympus_reach_name(island)), lambda state: state.has(CASTLE, player, 1))
    for god in OLYMPUS_GOD_QUESTS:
        set_rule(location(olympus_god_quest_name(god)), lambda state: state.has(CASTLE, player, 2))

    god_quests = [olympus_god_quest_name(god) for god in OLYMPUS_GOD_QUESTS]
    set_rule(location(olympus_reach_name("Mount Olympus")),
             lambda state: state.has(STONE, player) and state.has(CASTLE, player, 3)
             and all(state.can_reach_location(name, player) for name in god_quests))
    powers = campaign.powers
    set_rule(location(OLYMPUS_GOAL_LOCATION),
             lambda state: state.has_all((STONE, IRON) + powers, player) and state.has(CASTLE, player, 5)
             and state.can_reach_location(olympus_reach_name("Mount Olympus"), player))
    world.multiworld.completion_condition[player] = \
        lambda state: state.can_reach_location(OLYMPUS_GOAL_LOCATION, player)
