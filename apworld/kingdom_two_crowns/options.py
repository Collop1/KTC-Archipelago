"""Player options for Kingdom Two Crowns.

Everything the mod needs to know is sent to it through slot_data (see
world.fill_slot_data), so the mod has no config file of its own.
"""
from dataclasses import dataclass

from Options import Choice, DeathLink, DefaultOnToggle, NamedRange, OptionGroup, PerGameCommonOptions, Range


def _island_milestone_options(prefix: str, label: str, what: str, intervals: tuple[int, ...], default_interval: int,
                              default_per_island: int, note: str = ""):
    """The "<label> Interval" / "<label> Checks per Island" pair for a milestone counted separately on each island."""
    interval_ns = {
        "__doc__": f"{what} this many per check.{note}",
        "__module__": __name__,
        "display_name": f"{label} Interval",
        "default": default_interval,
        **{f"option_{n}": n for n in intervals},
    }
    count_ns = {
        "__doc__": f"How many '{label}' checks each island has. Only what you do on that island counts towards them, "
                   "so you have to move on to get them all. Call of Olympus has no numbered islands and gets five "
                   "islands' worth over the whole campaign instead. 0 turns them off.",
        "__module__": __name__,
        "display_name": f"{label} Checks per Island",
        "range_start": 0,
        "range_end": MAX_ISLAND_MILESTONE_CHECKS,
        "default": default_per_island,
    }
    return (type(Choice)(f"{prefix}Interval", (Choice,), interval_ns),
            type(Range)(f"{prefix}Checks", (Range,), count_ns))


def _campaign_milestone_options(prefix: str, label: str, what: str, intervals: tuple[int, ...], default_interval: int,
                                default_count: int, note: str = ""):
    """The "<label> Interval" / "<label> Checks" pair for a milestone counted over the whole campaign."""
    interval_ns = {
        "__doc__": f"{what} this many per check.{note}",
        "__module__": __name__,
        "display_name": f"{label} Interval",
        "default": default_interval,
        **{f"option_{n}": n for n in intervals},
    }
    count_ns = {
        "__doc__": f"How many '{label}' checks there are, counted over the whole campaign. 0 turns them off. Later "
                   "checks expect more of the seed's islands to be open.",
        "__module__": __name__,
        "display_name": f"{label} Checks",
        "range_start": 0,
        "range_end": MAX_CAMPAIGN_MILESTONE_CHECKS,
        "default": default_count,
    }
    return (type(Choice)(f"{prefix}Interval", (Choice,), interval_ns),
            type(Range)(f"{prefix}Checks", (Range,), count_ns))


MAX_ISLAND_MILESTONE_CHECKS = 10
MAX_CAMPAIGN_MILESTONE_CHECKS = 30


class Campaign(Choice):
    """Which campaign you're playing. Start a new campaign of this kind in the game for the seed.
    two_crowns: the base game's campaign (5 islands).
    shogun: the Shogun campaign (5 islands, its own mounts).
    dead_lands: the Dead Lands campaign (5 islands, ruler statues to free).
    norselands: the Norselands DLC (6 islands, god puzzles that grant Items of Power).
    call_of_olympus: the Call of Olympus DLC (oracle, god and quest islands; goal is defeating the World
    Serpent on Mount Olympus). Its islands open through its own quests, so Island Unlocks doesn't apply."""
    display_name = "Campaign"
    option_two_crowns = 0
    option_shogun = 1
    option_dead_lands = 2
    option_norselands = 3
    option_call_of_olympus = 4
    default = 0


class IslandsToLiberate(Range):
    """How many islands must be liberated (cliff portal destroyed) to win. Capped at the campaign's island count
    (5, or 6 for Norselands), so the default means every island. With Only Goal Islands on, the seed only uses
    that many islands, so a smaller number makes a shorter game (building sanity scales with it too; lower the
    milestone check counts as well for a really short one).
    Not used for Call of Olympus."""
    display_name = "Islands to Liberate"
    range_start = 1
    range_end = 6
    default = 6


class OnlyGoalIslands(DefaultOnToggle):
    """Only use the islands you need to liberate: with Islands to Liberate at 2, only islands 1 and 2 have checks,
    the boat can't take you any further, and the goal is liberating both. Hermits and Norselands powers from
    islands you won't visit are left out, and fewer Gem Pouches are needed.

    When off, every island is in the seed and liberating any that many of them wins, so items can still send you
    to the far islands. Not used for Call of Olympus."""
    display_name = "Only Goal Islands"


class IslandUnlocks(DefaultOnToggle):
    """Every island after the first is an item. You can only sail to an island once you have received its
    unlock, so you may end up going from island 1 straight to island 3 to chase a hinted check.

    When off, islands open up in order like the base game. Not used for Call of Olympus."""
    display_name = "Island Unlocks"


# --- Milestone checks ------------------------------------------------------------------------------
# Everyday milestones are counted per island ("Island 3: Slay Greed 100"), so farming one island doesn't finish
# them: you have to move on. Rarer things are counted over the whole campaign. Intervals are fixed choices so every
# possible location name can be registered ahead of time. The defaults are sized from a full 23-hour Norselands run:
# each island's set takes roughly an hour there.

# --- Greed found in every campaign ---
GreedKillInterval, GreedKillChecks = _island_milestone_options(
    "GreedKill", "Greed Kill", "Kill Greed (of any kind)", (25, 50, 100, 200), 100, 3)
GreedlingKillInterval, GreedlingKillChecks = _island_milestone_options(
    "GreedlingKill", "Greedling Kill", "Kill Greedlings (the small ones, masked or not)", (25, 50, 100, 200), 100, 2)
BreederKillInterval, BreederKillChecks = _campaign_milestone_options(
    "BreederKill", "Breeder Kill", "Kill Breeders (every campaign has them)", (1, 2, 3, 5), 3, 5,
    " Counted over the whole campaign, and only in seeds with at least three islands: they turn up on the later ones.")
CrownStealerKillInterval, CrownStealerKillChecks = _campaign_milestone_options(
    "CrownStealerKill", "Crown Stealer Kill", "Kill Crown Stealers (every campaign has them)", (2, 3, 5, 10), 3, 5,
    " Counted over the whole campaign, and only in seeds with at least two islands.")

# --- Greed only some campaigns have: these are left out automatically when your campaign doesn't have them ---
FloaterKillInterval, FloaterKillChecks = _campaign_milestone_options(
    "FloaterKill", "Floater Kill",
    "Kill Floaters. Campaigns with Floaters: Two Crowns, Shogun, Dead Lands and Call of Olympus (not Norselands).",
    (2, 3, 5, 10), 5, 5, " Counted over the whole campaign, and only in seeds with at least two islands.")
ChargerKillInterval, ChargerKillChecks = _campaign_milestone_options(
    "ChargerKill", "Charger Kill",
    "Kill Chargers. Campaigns with Chargers: Norselands, and Call of Olympus (only when a portal strikes back).",
    (1, 2, 3, 5), 2, 5, " Counted over the whole campaign, and only in seeds with at least two islands.")
GreedKnightKillInterval, GreedKnightKillChecks = _campaign_milestone_options(
    "GreedKnightKill", "Greed Knight Kill",
    "Kill Greed Knights (the shielded head-butters with archers behind them). Campaigns with Greed Knights: Call of "
    "Olympus only, and only when a portal strikes back.",
    (1, 2, 3), 1, 3, " Counted over the whole campaign.")

NightInterval, NightChecks = _island_milestone_options(
    "Night", "Night", "Survive nights on the island", (2, 3, 5, 10), 5, 2,
    " Losing your crown doesn't reset them; only nights spent on that island count.")
TreeInterval, TreeChecks = _island_milestone_options(
    "Tree", "Tree", "Fell trees (cut down by your builders)", (5, 10, 20, 25), 10, 3,
    " Checks above 100 trees on an island are left out.")


# --- Building sanity: new buildings on each island (upgrades and rebuilding where one stood don't count) ---
WallInterval, WallChecks = _island_milestone_options(
    "Wall", "Wall", "Build new walls", (1, 2, 3, 5), 5, 1,
    " Checks above 8 walls on an island are left out (the hardest difficulty guarantees about that many).")
TowerInterval, TowerChecks = _island_milestone_options(
    "Tower", "Archer Tower", "Build archer towers", (1, 2, 3, 5), 5, 1,
    " Checks above 5 towers on an island are left out (the hardest difficulty guarantees about that many).")
FarmInterval, FarmChecks = _island_milestone_options(
    "Farm", "Farm", "Build farms", (1,), 1, 1,
    " Only one per island: the hardest difficulty doesn't guarantee more farm spots than that.")
CitizenHouseInterval, CitizenHouseChecks = _island_milestone_options(
    "CitizenHouse", "Citizen House", "Build citizen houses (on vagrant camps)", (1, 2), 1, 2,
    " Checks above 2 houses on an island are left out: every island has at least two vagrant camps.")


class HermitBuildingChecks(DefaultOnToggle):
    """One check for the first building each hermit makes for you (bakery, ballista, stable, ...)."""
    display_name = "Hermit Building Checks"


# --- Unit sanity ------------------------------------------------------------------------------------
VillagerInterval, VillagerChecks = _island_milestone_options(
    "Villager", "Villager", "Recruit villagers (vagrants given a coin, or from citizen houses)", (5, 10, 20, 25), 10, 2)
ArcherInterval, ArcherChecks = _island_milestone_options("Archer", "Archer", "Recruit archers", (3, 5, 10, 20), 5, 2)
BuilderInterval, BuilderChecks = _island_milestone_options("Builder", "Builder", "Recruit builders", (3, 5, 10), 5, 2)
FarmerInterval, FarmerChecks = _island_milestone_options("Farmer", "Farmer", "Recruit farmers", (2, 3, 5, 10), 5, 1)
KnightInterval, KnightChecks = _campaign_milestone_options(
    "Knight", "Knight", "Recruit knights", (1, 2, 3, 5), 2, 5,
    " Checks above 4 knights per island in the seed are left out, since that's all you get without losses.")

# --- Animal sanity ----------------------------------------------------------------------------------
AnimalInterval, AnimalChecks = _island_milestone_options(
    "Animal", "Animal", "Hunt animals of any kind (killed by anyone)", (25, 50, 100, 200), 100, 2)
DeerInterval, DeerChecks = _island_milestone_options("Deer", "Deer", "Hunt deer", (5, 10, 20, 50), 5, 2)
RabbitInterval, RabbitChecks = _island_milestone_options(
    "Rabbit", "Rabbit", "Hunt rabbits", (10, 25, 50, 100), 50, 0, " Off by default: not every campaign is guaranteed to have them.")
BoarInterval, BoarChecks = _campaign_milestone_options(
    "Boar", "Boar", "Hunt boars (about one a winter, so very few per game)", (1, 2, 3), 1, 0,
    " Off by default: not every campaign is guaranteed to have them. Only from the second island on.")

# --- Coins ------------------------------------------------------------------------------------------
BribeInterval, BribeChecks = _island_milestone_options(
    "Bribe", "Bribe", "Let the Greed pick up this many coins (on purpose or not)", (10, 25, 50, 100), 50, 2)
SpendInterval, SpendChecks = _island_milestone_options(
    "Spend", "Spend", "Spend this many coins on anything", (50, 100, 200, 500), 100, 3)
SinkInterval, SinkChecks = _island_milestone_options(
    "Sink", "Sunk Coin", "Lose this many coins to the water (dropped there, or spilling from an over-full sack, "
    "usually walking past farms)", (10, 25, 50, 100), 50, 2)


class GrassCheck(DefaultOnToggle):
    """One check once your mount has eaten grass 100 times."""
    display_name = "Grass Check"


class TrapPercentage(Range):
    """Percentage of filler items that are replaced by traps."""
    display_name = "Trap Percentage"
    range_start = 0
    range_end = 100
    default = 20


class TripTrapWeight(Range):
    """Trip: your crown is knocked off your head. Grab it before the Greed do."""
    display_name = "Trip Trap Weight"
    range_start = 0
    range_end = 10
    default = 2


class SlipperyPouchTrapWeight(Range):
    """Slippery Pouch: every coin and gem you carry spills out and scatters (some may roll into water)."""
    display_name = "Slippery Pouch Trap Weight"
    range_start = 0
    range_end = 10
    default = 3


class WavesGoneByTrapWeight(Range):
    """Waves Gone By: waves on your current island become as strong as if some extra days had passed."""
    display_name = "Waves Gone By Trap Weight"
    range_start = 0
    range_end = 10
    default = 1


class DamnedNightTrapWeight(Range):
    """Damned Night: night falls immediately."""
    display_name = "Damned Night Trap Weight"
    range_start = 0
    range_end = 10
    default = 2


class DecayTrapWeight(Range):
    """Decay: the outermost wall on each side drops a tier, or crumbles if it is already at the lowest tier."""
    display_name = "Decay Trap Weight"
    range_start = 0
    range_end = 10
    default = 2


class StamDamTrapWeight(Range):
    """Stam Dam: your mount loses all its stamina."""
    display_name = "Stam Dam Trap Weight"
    range_start = 0
    range_end = 10
    default = 2


class RedMoonTrapWeight(Range):
    """Red Moon: a blood-moon wave, as big as a real one on your island and day, attacks from both sides right now."""
    display_name = "Red Moon Trap Weight"
    range_start = 0
    range_end = 10
    default = 1


class DesertionTrapWeight(Range):
    """Desertion: some of your subjects give up and turn back into vagrants."""
    display_name = "Desertion Trap Weight"
    range_start = 0
    range_end = 10
    default = 2


class ToolTheftTrapWeight(Range):
    """Tool Theft: the Greed steal tools, first from the shop racks, then out of your units' hands."""
    display_name = "Tool Theft Trap Weight"
    range_start = 0
    range_end = 10
    default = 2


class BadKingAmount(Range):
    """How many subjects a Desertion trap takes, and how many tools a Tool Theft trap steals."""
    display_name = "Desertion / Tool Theft Amount"
    range_start = 1
    range_end = 10
    default = 3


class WavesGoneByDays(Range):
    """How many days of wave strength a Waves Gone By trap adds to your current island."""
    display_name = "Waves Gone By Days"
    range_start = 1
    range_end = 30
    default = 10


class ProgressiveCoinSacks(NamedRange):
    """Your coin sack starts at half its usual size (coins spill out sooner), and each Progressive Coin Sack makes it
    bigger, up to three times the usual size with all of them. Set how many there are (5-10), or disabled to keep the
    normal sack. In Call of Olympus this replaces the sack upgrade you can buy there; with it disabled, that upgrade
    is an item (Hermes Sack) instead."""
    display_name = "Progressive Coin Sacks"
    range_start = 5
    range_end = 10
    default = 5
    special_range_names = {"disabled": 0}


class CoinFillerWeight(Range):
    """Weight of Coins (3 to 7 coins each) in the filler pool."""
    display_name = "Coin Filler Weight"
    range_start = 0
    range_end = 10
    default = 3


class GemFillerWeight(Range):
    """Weight of Gems in the filler pool."""
    display_name = "Gem Filler Weight"
    range_start = 0
    range_end = 10
    default = 1


class VagrantFillerWeight(Range):
    """Weight of Vagrants (spawned at your town centre) in the filler pool."""
    display_name = "Vagrant Filler Weight"
    range_start = 0
    range_end = 10
    default = 1


class TuftOfGrassWeight(Range):
    """Weight of Tufts of Grass in the filler pool: your mount eats it on the spot (anywhere, even in winter or a
    forest) and is fully rested."""
    display_name = "Tuft of Grass Weight"
    range_start = 0
    range_end = 10
    default = 6


class JunkFillerWeight(Range):
    """Weight of junk in the filler pool (Sunken Gold, Broken Crown, Greed Mask Shard, ...): it does nothing useful,
    though some of it shows up in the game."""
    display_name = "Junk Filler Weight"
    range_start = 0
    range_end = 10
    default = 3


class VagrantsPerItem(Range):
    """How many vagrants a single Vagrants item spawns."""
    display_name = "Vagrants per Item"
    range_start = 1
    range_end = 5
    default = 2


class DeathLinkEffect(Choice):
    """What happens to you when someone else in the multiworld dies (DeathLink must be on).
    crown: your crown is flung off screen towards the nearest portal. Get it back before the Greed do.
    wallet: every coin you carry is gone, and the Greed steal your gems. You get the gems back by liberating the
    island they were stolen on (or after a while, see Stolen Gems Return After).
    both: crown and wallet."""
    display_name = "DeathLink Effect"
    option_crown = 0
    option_wallet = 1
    option_both = 2
    alias_harsh = 2
    default = 2


class StolenGemsReturnNights(Range):
    """Gems the Greed steal on a DeathLink come back when you liberate the island they were stolen on. If you
    haven't by then, the Greed drop them after this many nights, so a seed can never get stuck waiting on them.
    Call of Olympus has no liberation, so there only this applies."""
    display_name = "Stolen Gems Return After (Nights)"
    range_start = 1
    range_end = 30
    default = 10


@dataclass
class KingdomTwoCrownsOptions(PerGameCommonOptions):
    campaign: Campaign
    islands_to_liberate: IslandsToLiberate
    only_goal_islands: OnlyGoalIslands
    island_unlocks: IslandUnlocks
    greed_kill_interval: GreedKillInterval
    greed_kill_checks: GreedKillChecks
    greedling_kill_interval: GreedlingKillInterval
    greedling_kill_checks: GreedlingKillChecks
    floater_kill_interval: FloaterKillInterval
    floater_kill_checks: FloaterKillChecks
    charger_kill_interval: ChargerKillInterval
    charger_kill_checks: ChargerKillChecks
    greed_knight_kill_interval: GreedKnightKillInterval
    greed_knight_kill_checks: GreedKnightKillChecks
    breeder_kill_interval: BreederKillInterval
    breeder_kill_checks: BreederKillChecks
    crown_stealer_kill_interval: CrownStealerKillInterval
    crown_stealer_kill_checks: CrownStealerKillChecks
    night_interval: NightInterval
    night_checks: NightChecks
    tree_interval: TreeInterval
    tree_checks: TreeChecks
    wall_interval: WallInterval
    wall_checks: WallChecks
    tower_interval: TowerInterval
    tower_checks: TowerChecks
    farm_interval: FarmInterval
    farm_checks: FarmChecks
    citizen_house_interval: CitizenHouseInterval
    citizen_house_checks: CitizenHouseChecks
    hermit_building_checks: HermitBuildingChecks
    villager_interval: VillagerInterval
    villager_checks: VillagerChecks
    archer_interval: ArcherInterval
    archer_checks: ArcherChecks
    builder_interval: BuilderInterval
    builder_checks: BuilderChecks
    farmer_interval: FarmerInterval
    farmer_checks: FarmerChecks
    knight_interval: KnightInterval
    knight_checks: KnightChecks
    animal_interval: AnimalInterval
    animal_checks: AnimalChecks
    deer_interval: DeerInterval
    deer_checks: DeerChecks
    rabbit_interval: RabbitInterval
    rabbit_checks: RabbitChecks
    boar_interval: BoarInterval
    boar_checks: BoarChecks
    bribe_interval: BribeInterval
    bribe_checks: BribeChecks
    spend_interval: SpendInterval
    spend_checks: SpendChecks
    sink_interval: SinkInterval
    sink_checks: SinkChecks
    grass_check: GrassCheck
    trap_percentage: TrapPercentage
    trip_trap_weight: TripTrapWeight
    slippery_pouch_trap_weight: SlipperyPouchTrapWeight
    waves_gone_by_trap_weight: WavesGoneByTrapWeight
    damned_night_trap_weight: DamnedNightTrapWeight
    decay_trap_weight: DecayTrapWeight
    red_moon_trap_weight: RedMoonTrapWeight
    stam_dam_trap_weight: StamDamTrapWeight
    desertion_trap_weight: DesertionTrapWeight
    tool_theft_trap_weight: ToolTheftTrapWeight
    bad_king_amount: BadKingAmount
    waves_gone_by_days: WavesGoneByDays
    progressive_coin_sacks: ProgressiveCoinSacks
    coin_filler_weight: CoinFillerWeight
    gem_filler_weight: GemFillerWeight
    vagrant_filler_weight: VagrantFillerWeight
    tuft_of_grass_weight: TuftOfGrassWeight
    junk_filler_weight: JunkFillerWeight
    vagrants_per_item: VagrantsPerItem
    death_link: DeathLink
    death_link_effect: DeathLinkEffect
    stolen_gems_return_nights: StolenGemsReturnNights


OPTION_GROUPS = [
    OptionGroup("Campaign & Goal", [Campaign, IslandsToLiberate, OnlyGoalIslands, IslandUnlocks]),
    OptionGroup("Greed (every campaign)", [
        GreedKillInterval, GreedKillChecks, GreedlingKillInterval, GreedlingKillChecks,
        BreederKillInterval, BreederKillChecks, CrownStealerKillInterval, CrownStealerKillChecks,
    ]),
    OptionGroup("Greed (only some campaigns)", [
        FloaterKillInterval, FloaterKillChecks, ChargerKillInterval, ChargerKillChecks,
        GreedKnightKillInterval, GreedKnightKillChecks,
    ]),
    OptionGroup("Nights & Trees", [NightInterval, NightChecks, TreeInterval, TreeChecks]),
    OptionGroup("Building Sanity", [
        WallInterval, WallChecks, TowerInterval, TowerChecks, FarmInterval, FarmChecks,
        CitizenHouseInterval, CitizenHouseChecks, HermitBuildingChecks,
    ]),
    OptionGroup("Unit & Animal Sanity", [
        VillagerInterval, VillagerChecks, ArcherInterval, ArcherChecks, BuilderInterval, BuilderChecks,
        FarmerInterval, FarmerChecks, KnightInterval, KnightChecks,
        AnimalInterval, AnimalChecks, DeerInterval, DeerChecks, RabbitInterval, RabbitChecks, BoarInterval, BoarChecks,
    ]),
    OptionGroup("Coins & Odd Jobs", [BribeInterval, BribeChecks, SpendInterval, SpendChecks, SinkInterval, SinkChecks,
                                     GrassCheck]),
    OptionGroup("Traps", [
        TrapPercentage, TripTrapWeight, SlipperyPouchTrapWeight, RedMoonTrapWeight, WavesGoneByTrapWeight,
        DamnedNightTrapWeight, DecayTrapWeight, StamDamTrapWeight, DesertionTrapWeight, ToolTheftTrapWeight,
        WavesGoneByDays, BadKingAmount,
    ]),
    OptionGroup("Sack & Filler", [ProgressiveCoinSacks, CoinFillerWeight, GemFillerWeight, VagrantFillerWeight,
                                  TuftOfGrassWeight, JunkFillerWeight, VagrantsPerItem]),
    OptionGroup("DeathLink", [DeathLink, DeathLinkEffect, StolenGemsReturnNights]),
]
