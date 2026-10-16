from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld

from .options import OPTION_GROUPS


class KingdomTwoCrownsWebWorld(WebWorld):
    theme = "grassFlowers"
    game = "Kingdom Two Crowns"
    option_groups = OPTION_GROUPS
    tutorials = [
        Tutorial(
            "Multiworld Setup Guide",
            "A guide to setting up the Kingdom Two Crowns mod for Archipelago multiworld games.",
            "English",
            "setup_en.md",
            "setup/en",
            ["KTC-Archipelago contributors"],
        )
    ]
