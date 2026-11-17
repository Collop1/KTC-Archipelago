"""Access rules for Kingdom Two Crowns.

Kept deliberately on the strict side, since being too lenient would put a required item behind a check
the player can't actually do yet:
  - Destroying portals needs Stone Technology and a castle upgraded at least four times (bombs).
  - Liberating an island (cliff portal) needs Stone + Iron Technology, five castle upgrades and that island's
    "Island N Bombs".
  - Statues, mount shrines and hermit cabins cost gems. Chests give nothing in this randomizer, so gems
    come from Gem Pouch items: a gem-cost check on island N expects enough pouches to pay for everything on
    islands 1..N (Olympus: a growing share of the whole campaign's gem costs).
  - The Stone Quarry needs a first castle upgrade; the Iron Mine needs Stone Technology and two upgrades.
  - A hermit's building needs that hermit's item and the gems to have freed it.
  - Castle Level 5+ needs Stone Technology, Castle Level 7 needs Iron Technology.
  - Island-counted milestones ("Island 3: Slay Greed 100", building sanity too) live in their island's region.
    Campaign-wide ones are spread over the seed's islands, and the rarer Greed expect a minimum number of islands.
    Later checks of a kind expect a sturdier kingdom.
  - Call of Olympus: the god quests expect a couple of castle upgrades, Mount Olympus expects all four god
    quests done, and defeating the serpent expects a strong castle, both techs and all four god powers.
"""
from math import ceil, floor

from worlds.generic.Rules import set_rule

from .campaigns import OLYMPUS_GOD_QUESTS, OLYMPUS_ISLANDS
from .items import GEMS_PER_POUCH
from .locations import (
    CASTLE_LEVELS, GEM_KINDS, HERMIT_BUILDINGS, MILESTONE_REQUIREMENTS, MILESTONES,
    OLYMPUS_GOAL_LOCATION, hermits_in_play,
    PROGRESSIVE_KINDS, campaign_progressive_name, castle_level_name, island_progressive_name, liberate_name,
    milestone_islands, milestone_name, milestone_thresholds, mine_name, olympus_god_quest_name, olympus_reach_name,
    quarry_name,
)

CASTLE = "Progressive Castle"
STONE = "Stone Technology"
IRON = "Iron Technology"


def gems(state, player: int) -> int:
    return GEMS_PER_POUCH * state.count("Gem Pouch", player)


def olympus_gem_need(campaign, kind: str, k: int) -> int:
    """The k-th Olympus statue/shrine/cabin expects a matching share of every gem cost in the campaign."""
    count = getattr(campaign.totals, kind)
    return -(-campaign.total_gem_cost * k // count)


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
        max_islands = 5
        islands_reachable = lambda state: 5  # noqa: E731 - Olympus opens its islands through quests
    else:
        islands = list(world.islands_in_play)
        max_islands = len(islands)

        def islands_reachable(state) -> int:
            return sum(state.can_reach_region(f"Island {i}", player) for i in islands)

        for island in islands:
            content = campaign.islands[island]
            gem_need = campaign.cumulative_gem_cost(island)
            for kind in PROGRESSIVE_KINDS:
                for k in range(1, getattr(content, kind) + 1):
                    name = island_progressive_name(island, kind, k)
                    if kind == "portals":
                        set_rule(location(name), lambda state: can_destroy_portals(state, player))
                    elif kind in GEM_KINDS:
                        set_rule(location(name), lambda state, n=gem_need: gems(state, player) >= n)
            set_rule(location(liberate_name(island)),
                     lambda state, b=f"Island {island} Bombs": can_liberate(state, player) and state.has(b, player))

        if campaign.quarry_island in islands:
            set_rule(location(quarry_name(campaign.quarry_island)), lambda state: state.has(CASTLE, player, 1))
        if campaign.mine_island in islands:
            set_rule(location(mine_name(campaign.mine_island)),
                     lambda state: state.has(STONE, player) and state.has(CASTLE, player, 2))
        if world.options.hermit_building_checks:
            for hermit in hermits_in_play(world):
                island = campaign.hermit_islands[hermit]
                set_rule(location(HERMIT_BUILDINGS[hermit]),
                         lambda state, h=hermit, i=island, n=campaign.cumulative_gem_cost(island):
                         state.has(h, player) and state.can_reach_region(f"Island {i}", player)
                         and gems(state, player) >= n)

        goal = min(world.options.islands_to_liberate.value, len(islands))
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

    for category in MILESTONES:
        thresholds = milestone_thresholds(world, category)
        if not thresholds:
            continue
        extra_castle, min_islands = MILESTONE_REQUIREMENTS[category]
        per_island = milestone_islands(world, category) != [0]
        for i, threshold in enumerate(thresholds, start=1):
            tier = floor(3 * (i - 1) / len(thresholds))
            castle_needed = max(0, tier + extra_castle)
            if per_island:
                islands_needed = 1  # the island's own region already requires reaching it
            else:
                spread = ceil(i * max_islands / len(thresholds))
                islands_needed = min(max(min_islands, spread), max_islands)
            for island in milestone_islands(world, category):
                set_rule(location(milestone_name(category, threshold, island)),
                         lambda state, c=castle_needed, m=islands_needed: state.has(CASTLE, player, c)
                         and (m <= 1 or islands_reachable(state) >= m))


def _set_olympus_rules(world, location) -> None:
    player = world.player
    campaign = world.campaign

    for k in range(1, campaign.totals.portals + 1):
        set_rule(location(campaign_progressive_name("portals", k)), lambda state: can_destroy_portals(state, player))
    for kind in GEM_KINDS:
        for k in range(1, getattr(campaign.totals, kind) + 1):
            set_rule(location(campaign_progressive_name(kind, k)),
                     lambda state, n=olympus_gem_need(campaign, kind, k): gems(state, player) >= n)
    set_rule(location(quarry_name(0)), lambda state: state.has(CASTLE, player, 1))
    set_rule(location(mine_name(0)), lambda state: state.has(STONE, player) and state.has(CASTLE, player, 2))
    if world.options.hermit_building_checks:
        # Any hermit could be in any cabin, so its building expects every cabin to be affordable.
        all_cabins = olympus_gem_need(campaign, "hermit_cabins", campaign.totals.hermit_cabins)
        for hermit in campaign.hermits:
            set_rule(location(HERMIT_BUILDINGS[hermit]),
                     lambda state, h=hermit: state.has(h, player) and gems(state, player) >= all_cabins)

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
