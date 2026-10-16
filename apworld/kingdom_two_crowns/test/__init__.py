from test.bases import WorldTestBase

from ..campaigns import CAMPAIGNS
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
        portal = "Island 2: Destroy Portal 1"
        self.collect_by_name("Island 2 Unlock")
        self.assertFalse(self.can_reach_location(portal))
        self.collect_by_name("Stone Technology")
        self.assertFalse(self.can_reach_location(portal))
        self.collect(self.get_items_by_name("Progressive Castle")[:4])
        self.assertTrue(self.can_reach_location(portal))

    def test_island_one_has_no_regular_portal(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Island 1: Destroy Portal 1", names)
        self.assertIn("Island 5: Destroy Portal 6", names)


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
        "trap_percentage": 100,
    }


class CampaignTest(KingdomTwoCrownsTestBase):
    campaign = ""

    def test_campaign_pool(self) -> None:
        if not self.campaign:
            self.skipTest("base class")
        campaign = CAMPAIGNS[self.campaign]
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        for name in campaign.statues + campaign.hermits + campaign.mounts + campaign.powers:
            self.assertIn(name, pool)


class TestShogun(CampaignTest):
    campaign = "shogun"
    options = {"campaign": "shogun"}


class TestDeadLands(CampaignTest):
    campaign = "dead_lands"
    options = {"campaign": "dead_lands"}

    def test_ruler_statues(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for island in (2, 3, 4):
            self.assertIn(f"Island {island}: Ruler Statue 1", names)


class TestNorselands(CampaignTest):
    campaign = "norselands"
    options = {"campaign": "norselands", "islands_to_liberate": 6}

    def test_six_islands_and_puzzles(self) -> None:
        self.assertFalse(self.can_reach_region("Island 6"))
        self.collect_by_name("Island 6 Unlock")
        self.assertTrue(self.can_reach_region("Island 6"))
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in ("Island 1: Heimdall's Puzzle", "Island 3: Thor's Puzzle", "Island 4: Hel's Puzzle",
                     "Island 6: Loki's Puzzle"):
            self.assertIn(name, names)


class TestNorselandsSequential(CampaignTest):
    campaign = "norselands"
    options = {"campaign": "norselands", "island_unlocks": False}


class TestOlympus(CampaignTest):
    campaign = "call_of_olympus"
    options = {"campaign": "call_of_olympus"}

    def test_goal_needs_powers(self) -> None:
        goal = "Defeat the World Serpent"
        self.collect_all_but(["Power of Hermes"])
        self.assertFalse(self.can_reach_location(goal))
        self.collect_by_name("Power of Hermes")
        self.assertTrue(self.can_reach_location(goal))

    def test_no_island_unlocks(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertFalse(any(name.startswith("Island ") for name in pool))
