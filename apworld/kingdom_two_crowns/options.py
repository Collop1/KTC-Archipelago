"""Player options for Kingdom Two Crowns.

Everything the mod needs to know is sent to it through slot_data (see
world.fill_slot_data), so the mod has no config file of its own.
"""
from dataclasses import dataclass

from Options import Choice, DeathLink, DefaultOnToggle, OptionGroup, PerGameCommonOptions, Range


class IslandsToLiberate(Range):
    """How many islands must be liberated (cliff portal destroyed) to win. Any islands count."""
    display_name = "Islands to Liberate"
    range_start = 1
    range_end = 5
    default = 5


class IslandUnlocks(DefaultOnToggle):
    """Islands 2-5 are items. You can only sail to an island once you have received its unlock, so you may
    end up going from island 1 straight to island 3 to chase a hinted check.

    When off, islands open up in order like the base game."""
    display_name = "Island Unlocks"


# --- Milestone checks ------------------------------------------------------------------------------
# Each milestone category is "every <interval> things, up to <checks> checks". Intervals are fixed
# choices so every possible location name can be registered ahead of time.

class GreedKillInterval(Choice):
    """Kill this many Greed (of any kind) per check."""
    display_name = "Greed Kill Interval"
    option_10 = 10
    option_20 = 20
    option_25 = 25
    option_50 = 50
    default = 20


class GreedKillChecks(Range):
    """Number of 'Slay N Greed' checks."""
    display_name = "Greed Kill Checks"
    range_start = 0
    range_end = 20
    default = 10


class GreedlingKillInterval(Choice):
    """Kill this many Greedlings (the small ones, masked or not) per check."""
    display_name = "Greedling Kill Interval"
    option_10 = 10
    option_20 = 20
    option_25 = 25
    option_50 = 50
    default = 25


class GreedlingKillChecks(Range):
    """Number of 'Slay N Greedlings' checks."""
    display_name = "Greedling Kill Checks"
    range_start = 0
    range_end = 20
    default = 6


class FloaterKillInterval(Choice):
    """Kill this many Floaters per check."""
    display_name = "Floater Kill Interval"
    option_2 = 2
    option_3 = 3
    option_5 = 5
    option_10 = 10
    default = 5


class FloaterKillChecks(Range):
    """Number of 'Slay N Floaters' checks."""
    display_name = "Floater Kill Checks"
    range_start = 0
    range_end = 20
    default = 4


class BreederKillInterval(Choice):
    """Kill this many Breeders per check."""
    display_name = "Breeder Kill Interval"
    option_1 = 1
    option_2 = 2
    option_3 = 3
    option_5 = 5
    default = 2


class BreederKillChecks(Range):
    """Number of 'Slay N Breeders' checks."""
    display_name = "Breeder Kill Checks"
    range_start = 0
    range_end = 20
    default = 3


class CrownStealerKillInterval(Choice):
    """Kill this many Crown Stealers per check."""
    display_name = "Crown Stealer Kill Interval"
    option_1 = 1
    option_2 = 2
    option_3 = 3
    option_5 = 5
    default = 2


class CrownStealerKillChecks(Range):
    """Number of 'Slay N Crown Stealers' checks."""
    display_name = "Crown Stealer Kill Checks"
    range_start = 0
    range_end = 20
    default = 3


class NightInterval(Choice):
    """Survive this many nights (waves) per check. Nights are counted across all islands."""
    display_name = "Night Interval"
    option_3 = 3
    option_5 = 5
    option_10 = 10
    default = 5


class NightChecks(Range):
    """Number of 'Survive N Nights' checks."""
    display_name = "Night Checks"
    range_start = 0
    range_end = 20
    default = 8


class TreeInterval(Choice):
    """Fell this many trees per check."""
    display_name = "Tree Interval"
    option_5 = 5
    option_10 = 10
    option_20 = 20
    option_25 = 25
    default = 10


class TreeChecks(Range):
    """Number of 'Fell N Trees' checks."""
    display_name = "Tree Checks"
    range_start = 0
    range_end = 20
    default = 10


class ForestChecks(Range):
    """Number of 'Clear Forest N' checks. A forest counts once every tree in it has been chopped down, e.g. a
    patch of woods sitting between two clearings. Experimental: set to 0 if it never triggers for you."""
    display_name = "Forest Clearing Checks"
    range_start = 0
    range_end = 5
    default = 1


# --- Filler and traps --------------------------------------------------------------------------------

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
    crown: your crown is knocked off (like the Trip trap).
    wallet: you drop everything you are carrying (like the Slippery Pouch trap).
    both: both of the above."""
    display_name = "DeathLink Effect"
    option_crown = 0
    option_wallet = 1
    option_both = 2
    default = 0


@dataclass
class KingdomTwoCrownsOptions(PerGameCommonOptions):
    islands_to_liberate: IslandsToLiberate
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
    forest_checks: ForestChecks
    trap_percentage: TrapPercentage
    trip_trap_weight: TripTrapWeight
    slippery_pouch_trap_weight: SlipperyPouchTrapWeight
    wave_back_trap_weight: WaveBackTrapWeight
    waves_gone_by_trap_weight: WavesGoneByTrapWeight
    damned_night_trap_weight: DamnedNightTrapWeight
    decay_trap_weight: DecayTrapWeight
    waves_gone_by_days: WavesGoneByDays
    coin_filler_weight: CoinFillerWeight
    gem_filler_weight: GemFillerWeight
    vagrant_filler_weight: VagrantFillerWeight
    junk_filler_weight: JunkFillerWeight
    vagrants_per_item: VagrantsPerItem
    death_link: DeathLink
    death_link_effect: DeathLinkEffect


OPTION_GROUPS = [
    OptionGroup("Goal & Islands", [IslandsToLiberate, IslandUnlocks]),
    OptionGroup("Milestone Checks", [
        GreedKillInterval, GreedKillChecks,
        GreedlingKillInterval, GreedlingKillChecks,
        FloaterKillInterval, FloaterKillChecks,
        BreederKillInterval, BreederKillChecks,
        CrownStealerKillInterval, CrownStealerKillChecks,
        NightInterval, NightChecks,
        TreeInterval, TreeChecks,
        ForestChecks,
    ]),
    OptionGroup("Traps", [
        TrapPercentage, TripTrapWeight, SlipperyPouchTrapWeight, WaveBackTrapWeight,
        WavesGoneByTrapWeight, DamnedNightTrapWeight, DecayTrapWeight, WavesGoneByDays,
    ]),
    OptionGroup("Filler", [CoinFillerWeight, GemFillerWeight, VagrantFillerWeight, JunkFillerWeight, VagrantsPerItem]),
    OptionGroup("DeathLink", [DeathLink, DeathLinkEffect]),
]
