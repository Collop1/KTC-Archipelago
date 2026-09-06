from worlds.AutoWorld import World

from . import items, locations, regions, rules
from .items import KingdomTwoCrownsItem
from .options import KingdomTwoCrownsOptions
from .web_world import KingdomTwoCrownsWebWorld


class KingdomTwoCrownsWorld(World):
    """
    Kingdom Two Crowns is a side-scrolling kingdom-building game: ride out
    each day to gather coins and gems, spend them on statues, mounts and
    hermits, then defend your castle each night. The goal is to liberate all
    five islands from the Greed.
    """

    game = "Kingdom Two Crowns"
    web = KingdomTwoCrownsWebWorld()

    options_dataclass = KingdomTwoCrownsOptions
    options: KingdomTwoCrownsOptions

    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID

    origin_region_name = "Menu"

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def create_items(self) -> None:
        items.create_all_items(self)

    def create_item(self, name: str) -> KingdomTwoCrownsItem:
        return items.create_item(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_filler_item_name(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def fill_slot_data(self) -> dict:
        return {
            "death_link": bool(self.options.death_link),
            "no_text": bool(self.options.no_text),
        }
