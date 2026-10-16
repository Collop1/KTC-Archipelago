from test.bases import WorldTestBase

from ..items import ITEM_TABLE
from ..locations import LOCATION_NAME_TO_ID


class KingdomTwoCrownsTestBase(WorldTestBase):
    game = "Kingdom Two Crowns"


class TestDefault(KingdomTwoCrownsTestBase):
    def test_ids_unique(self) -> None:
        self.assertEqual(len(set(LOCATION_NAME_TO_ID.values())), len(LOCATION_NAME_TO_ID))
        self.assertEqual(len({data.id for data in ITEM_TABLE.values()}), len(ITEM_TABLE))

    def test_island_needs_unlock(self) -> None:
        self.assertFalse(self.can_reach_region("Island 3"))
        self.collect_by_name("Island 3 Unlock")
        self.assertTrue(self.can_reach_region("Island 3"))
        self.assertFalse(self.can_reach_region("Island 2"))

    def test_portals_need_stone_and_castle(self) -> None:
        portal = "Island 1: Destroy Portal 1"
        self.assertFalse(self.can_reach_location(portal))
        self.collect_by_name("Stone Technology")
        self.assertFalse(self.can_reach_location(portal))
        self.collect(self.get_items_by_name("Progressive Castle")[:4])
        self.assertTrue(self.can_reach_location(portal))


class TestSequentialIslands(KingdomTwoCrownsTestBase):
    options = {"island_unlocks": False}

    def test_all_islands_open(self) -> None:
        for island in range(1, 6):
            self.assertTrue(self.can_reach_region(f"Island {island}"))


class TestMinimalChecks(KingdomTwoCrownsTestBase):
    options = {
        "greed_kill_checks": 0,
        "greedling_kill_checks": 0,
        "floater_kill_checks": 0,
        "breeder_kill_checks": 0,
        "crown_stealer_kill_checks": 0,
        "night_checks": 0,
        "tree_checks": 0,
        "forest_checks": 0,
        "islands_to_liberate": 1,
    }


class TestMaxChecksAllTraps(KingdomTwoCrownsTestBase):
    options = {
        "greed_kill_interval": 50,
        "greed_kill_checks": 20,
        "greedling_kill_checks": 20,
        "floater_kill_checks": 20,
        "breeder_kill_checks": 20,
        "crown_stealer_kill_checks": 20,
        "night_checks": 20,
        "tree_checks": 20,
        "forest_checks": 5,
        "trap_percentage": 100,
    }
