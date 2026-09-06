from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld


class KingdomTwoCrownsWebWorld(WebWorld):
    theme = "grassFlowers"
    game = "Kingdom Two Crowns"
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
