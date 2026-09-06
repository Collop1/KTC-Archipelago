"""Player options for Kingdom Two Crowns.

Both options here correspond 1:1 to what mod/Archipelago/APConnection.cs
already sends as connection tags in TryConnectAndLogin - keep them in sync.
"""
from dataclasses import dataclass

from Options import DeathLink, PerGameCommonOptions, Toggle


class NoText(Toggle):
    """Suppress the mod's in-game text/log popups (the "NoText" connection tag)."""
    display_name = "No Text"


@dataclass
class KingdomTwoCrownsOptions(PerGameCommonOptions):
    death_link: DeathLink
    no_text: NoText
