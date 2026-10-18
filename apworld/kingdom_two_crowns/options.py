"""Player options for Kingdom Two Crowns.

Everything the mod needs to know is sent to it through slot_data (see
world.fill_slot_data), so the mod has no config file of its own.
"""
from dataclasses import dataclass

from Options import Choice, DeathLink, DefaultOnToggle, OptionGroup, PerGameCommonOptions, Range


def _milestone_options(prefix: str, label: str, what: str, intervals: tuple[int, ...], default_interval: int,
                       default_per_island: int, note: str = ""):
    """Builds the "<label> Interval" / "<label> Checks per Island" option pair for one milestone category."""
    interval_ns = {
        "__doc__": f"{what} this many per check.{note}",
        "__module__": __name__,
        "display_name": f"{label} Interval",
        "default": default_interval,
        **{f"option_{n}": n for n in intervals},
    }
    count_ns = {
        "__doc__": f"How many '{label}' checks there are for each island you need to liberate (Islands to Liberate; "
                   "Call of Olympus counts as 5), so a short goal gets a short list. 0 turns them off. Later checks "
                   "also expect more islands to be open.",
        "__module__": __name__,
        "display_name": f"{label} Checks per Island",
        "range_start": 0,
        "range_end": MAX_MILESTONES_PER_ISLAND,
        "default": default_per_island,
    }
    return (type(Choice)(f"{prefix}Interval", (Choice,), interval_ns),
            type(Range)(f"{prefix}Checks", (Range,), count_ns))


MAX_MILESTONES_PER_ISLAND = 10


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
    that many islands, so a smaller number makes a shorter game. Milestone and building checks also scale with it.
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
# Each milestone category is "one check every <interval> things". Intervals are fixed choices so every possible
# location name can be registered ahead of time. The defaults aim for a long game: the checks keep coming until
# roughly the end of each island rather than running out in the first few nights.

GreedKillInterval, GreedKillChecks = _milestone_options(
    "GreedKill", "Greed Kill", "Kill Greed (of any kind)", (25, 50, 100, 200), 100, 3)
GreedlingKillInterval, GreedlingKillChecks = _milestone_options(
    "GreedlingKill", "Greedling Kill", "Kill Greedlings (the small ones, masked or not)", (25, 50, 100, 200), 100, 2)
FloaterKillInterval, FloaterKillChecks = _milestone_options(
    "FloaterKill", "Floater Kill", "Kill Floaters", (5, 10, 20, 50), 10, 1)
BreederKillInterval, BreederKillChecks = _milestone_options(
    "BreederKill", "Breeder Kill", "Kill Breeders", (2, 3, 5, 10), 3, 1)
CrownStealerKillInterval, CrownStealerKillChecks = _milestone_options(
    "CrownStealerKill", "Crown Stealer Kill", "Kill Crown Stealers", (2, 3, 5, 10), 3, 1)
NightInterval, NightChecks = _milestone_options(
    "Night", "Night", "Survive nights", (3, 5, 10, 15), 5, 2,
    " Nights count across islands, but losing your crown resets the count to 0.")
TreeInterval, TreeChecks = _milestone_options(
    "Tree", "Tree", "Fell trees (cut down by your builders)", (5, 10, 20, 25), 10, 2)


# --- Building sanity --------------------------------------------------------------------------------

class BuildingSanity(DefaultOnToggle):
    """One check for every new wall, archer tower, farm and citizen house you build, up to a fixed number that
    grows with Islands to Liberate: about 6-7 walls, 5 towers, and 1-2 farms and citizen houses per island (40 walls,
    30 towers, 10 farms and 10 houses for all six Norselands islands). That stays under what the hardest
    difficulty guarantees. Buildings are counted over the whole campaign. Upgrading a wall, or rebuilding one where
    another stood, doesn't count."""
    display_name = "Building Sanity"


class HermitBuildingChecks(DefaultOnToggle):
    """One check for the first building each hermit makes for you (bakery, ballista, stable, ...)."""
    display_name = "Hermit Building Checks"


# --- Unit sanity ------------------------------------------------------------------------------------
VillagerInterval, VillagerChecks = _milestone_options(
    "Villager", "Villager", "Recruit vagrants into villagers", (5, 10, 20, 25), 10, 3)
ArcherInterval, ArcherChecks = _milestone_options("Archer", "Archer", "Recruit archers", (3, 5, 10, 20), 5, 2)
BuilderInterval, BuilderChecks = _milestone_options("Builder", "Builder", "Recruit builders", (3, 5, 10), 5, 1)
FarmerInterval, FarmerChecks = _milestone_options("Farmer", "Farmer", "Recruit farmers", (2, 3, 5, 10), 3, 1)
KnightInterval, KnightChecks = _milestone_options("Knight", "Knight", "Recruit knights", (1, 2, 3, 5), 2, 1)

# --- Animal sanity ----------------------------------------------------------------------------------
AnimalInterval, AnimalChecks = _milestone_options(
    "Animal", "Animal", "Hunt animals of any kind (killed by anyone)", (25, 50, 100, 200), 100, 3)
DeerInterval, DeerChecks = _milestone_options("Deer", "Deer", "Hunt deer", (5, 10, 20, 50), 10, 2)
RabbitInterval, RabbitChecks = _milestone_options(
    "Rabbit", "Rabbit", "Hunt rabbits", (10, 25, 50, 100), 50, 0, " Off by default: not every campaign is guaranteed to have them.")
BoarInterval, BoarChecks = _milestone_options(
    "Boar", "Boar", "Hunt boars", (2, 5, 10, 20), 5, 0, " Off by default: not every campaign is guaranteed to have them.")

# --- Coins ------------------------------------------------------------------------------------------
BribeInterval, BribeChecks = _milestone_options(
    "Bribe", "Bribe", "Let the Greed pick up this many coins (on purpose or not)", (5, 10, 25, 50), 10, 2)
SpendInterval, SpendChecks = _milestone_options(
    "Spend", "Spend", "Spend this many coins on anything", (50, 100, 200, 500), 100, 3)


class GrassCheck(DefaultOnToggle):
    """One check once your mount has eaten grass 100 times."""
    display_name = "Grass Check"


class SunkCoinsCheck(DefaultOnToggle):
    """One check once 25 coins have sunk into water (dropped there, or knocked out of your purse)."""
    display_name = "Sunk Coins Check"


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


class WaveBackTrapWeight(Range):
    """Wave Back: the nearest portal retaliates and sends Greed at you right now, day or night."""
    display_name = "Wave Back Trap Weight"
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


class CoinFillerWeight(Range):
    """Weight of Coin Pouch / Coin Sack in the filler pool."""
    display_name = "Coin Filler Weight"
    range_start = 0
    range_end = 10
    default = 6


class GemFillerWeight(Range):
    """Weight of Gems in the filler pool."""
    display_name = "Gem Filler Weight"
    range_start = 0
    range_end = 10
    default = 2


class VagrantFillerWeight(Range):
    """Weight of Vagrants (spawned at your town centre) in the filler pool."""
    display_name = "Vagrant Filler Weight"
    range_start = 0
    range_end = 10
    default = 2


class JunkFillerWeight(Range):
    """Weight of junk that does nothing (Sunken Gold, Broken Crown, Tuft of Grass, ...) in the filler pool."""
    display_name = "Junk Filler Weight"
    range_start = 0
    range_end = 10
    default = 2


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
    breeder_kill_interval: BreederKillInterval
    breeder_kill_checks: BreederKillChecks
    crown_stealer_kill_interval: CrownStealerKillInterval
    crown_stealer_kill_checks: CrownStealerKillChecks
    night_interval: NightInterval
    night_checks: NightChecks
    tree_interval: TreeInterval
    tree_checks: TreeChecks
    building_sanity: BuildingSanity
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
    grass_check: GrassCheck
    sunk_coins_check: SunkCoinsCheck
    trap_percentage: TrapPercentage
    trip_trap_weight: TripTrapWeight
    slippery_pouch_trap_weight: SlipperyPouchTrapWeight
    wave_back_trap_weight: WaveBackTrapWeight
    waves_gone_by_trap_weight: WavesGoneByTrapWeight
    damned_night_trap_weight: DamnedNightTrapWeight
    decay_trap_weight: DecayTrapWeight
    stam_dam_trap_weight: StamDamTrapWeight
    desertion_trap_weight: DesertionTrapWeight
    tool_theft_trap_weight: ToolTheftTrapWeight
    bad_king_amount: BadKingAmount
    waves_gone_by_days: WavesGoneByDays
    coin_filler_weight: CoinFillerWeight
    gem_filler_weight: GemFillerWeight
    vagrant_filler_weight: VagrantFillerWeight
    junk_filler_weight: JunkFillerWeight
    vagrants_per_item: VagrantsPerItem
    death_link: DeathLink
    death_link_effect: DeathLinkEffect
    stolen_gems_return_nights: StolenGemsReturnNights


OPTION_GROUPS = [
    OptionGroup("Campaign & Goal", [Campaign, IslandsToLiberate, OnlyGoalIslands, IslandUnlocks]),
    OptionGroup("Milestone Checks", [
        GreedKillInterval, GreedKillChecks,
        GreedlingKillInterval, GreedlingKillChecks,
        FloaterKillInterval, FloaterKillChecks,
        BreederKillInterval, BreederKillChecks,
        CrownStealerKillInterval, CrownStealerKillChecks,
        NightInterval, NightChecks,
        TreeInterval, TreeChecks,
    ]),
    OptionGroup("Building Sanity", [BuildingSanity, HermitBuildingChecks]),
    OptionGroup("Unit & Animal Sanity", [
        VillagerInterval, VillagerChecks, ArcherInterval, ArcherChecks, BuilderInterval, BuilderChecks,
        FarmerInterval, FarmerChecks, KnightInterval, KnightChecks,
        AnimalInterval, AnimalChecks, DeerInterval, DeerChecks, RabbitInterval, RabbitChecks, BoarInterval, BoarChecks,
    ]),
    OptionGroup("Coins & Odd Jobs", [BribeInterval, BribeChecks, SpendInterval, SpendChecks, GrassCheck, SunkCoinsCheck]),
    OptionGroup("Traps", [
        TrapPercentage, TripTrapWeight, SlipperyPouchTrapWeight, WaveBackTrapWeight,
        WavesGoneByTrapWeight, DamnedNightTrapWeight, DecayTrapWeight, StamDamTrapWeight, DesertionTrapWeight,
        ToolTheftTrapWeight, WavesGoneByDays, BadKingAmount,
    ]),
    OptionGroup("Filler", [CoinFillerWeight, GemFillerWeight, VagrantFillerWeight, JunkFillerWeight, VagrantsPerItem]),
    OptionGroup("DeathLink", [DeathLink, DeathLinkEffect, StolenGemsReturnNights]),
]
