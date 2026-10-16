from typing import Any

from worlds.AutoWorld import World

from . import items, locations, regions, rules
from .campaigns import CAMPAIGN_BY_OPTION, CAMPAIGNS, Campaign
from .items import KingdomTwoCrownsItem
from .locations import MILESTONES
from .options import KingdomTwoCrownsOptions
from .regions import OLYMPUS_REGION
from .web_world import KingdomTwoCrownsWebWorld

# Bump when slot_data changes shape so the mod can warn about a mismatched apworld.
SLOT_DATA_VERSION = 3


class KingdomTwoCrownsWorld(World):
    """
    Kingdom Two Crowns is a side-scrolling kingdom-building game: ride out
    each day to gather coins and gems, spend them on statues, mounts and
    hermits, then defend your castle each night. Liberate the islands from
    the Greed to win.
    """

    game = "Kingdom Two Crowns"
    web = KingdomTwoCrownsWebWorld()

    options_dataclass = KingdomTwoCrownsOptions
    options: KingdomTwoCrownsOptions

    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID
    location_name_groups = locations.LOCATION_NAME_GROUPS
    item_name_groups = items.ITEM_NAME_GROUPS

    origin_region_name = "Menu"

    campaign: Campaign

    def generate_early(self) -> None:
        self.campaign = CAMPAIGNS[CAMPAIGN_BY_OPTION[self.options.campaign.value]]

    @property
    def home_region(self) -> str:
        """Where global checks (castle levels, milestones) live: always reachable."""
        return OLYMPUS_REGION if self.campaign.is_campaign_wide else "Island 1"

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

    def fill_slot_data(self) -> dict[str, Any]:
        data: dict[str, Any] = {
            "slot_data_version": SLOT_DATA_VERSION,
            "campaign": self.campaign.key,
            "biome_index": self.campaign.biome_index,
            **self.options.as_dict(
                "islands_to_liberate",
                "island_unlocks",
                "waves_gone_by_days",
                "vagrants_per_item",
                "death_link",
                "death_link_effect",
            ),
        }
        # The mod rebuilds every milestone location name from these.
        data["milestones"] = {
            category: {
                "interval": getattr(self.options, milestone.interval_option).value,
                "count": getattr(self.options, milestone.count_option).value,
            }
            for category, milestone in MILESTONES.items()
        }
        return data
