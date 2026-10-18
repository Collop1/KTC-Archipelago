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

    def test_gem_costs_need_pouches(self) -> None:
        statue = "Island 1: Statue 1"
        self.assertFalse(self.can_reach_location(statue))
        self.collect(self.get_items_by_name("Gem Pouch")[:2])
        self.assertFalse(self.can_reach_location(statue))  # island 1 needs 8 gems
        self.collect(self.get_items_by_name("Gem Pouch")[2:3])
        self.assertTrue(self.can_reach_location(statue))

    def test_island_one_has_no_regular_portal(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Island 1: Destroy Portal 1", names)
        self.assertIn("Island 5: Destroy Portal 6", names)

    def test_building_sanity_totals(self) -> None:
        # Five islands to liberate: floor(5 * 20/3) walls, 25 towers, 8 farms, 8 houses.
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in ("Build Wall 33", "Build Archer Tower 25", "Build Farm 8", "Build Citizen House 8"):
            self.assertIn(name, names)
        for name in ("Build Wall 34", "Build Archer Tower 26", "Build Farm 9", "Build Citizen House 9"):
            self.assertNotIn(name, names)

    def test_later_milestones_need_more_islands(self) -> None:
        self.assertTrue(self.can_reach_location("Build Wall 6"))
        self.assertFalse(self.can_reach_location("Build Wall 7"))
        self.collect_by_name("Island 2 Unlock")
        self.assertTrue(self.can_reach_location("Build Wall 7"))
        # Three Greed checks per island: the fourth expects a second island.
        self.collect_by_name("Island 2 Unlock")
        self.assertTrue(self.can_reach_location("Slay 400 Greed"))

    def test_milestones_scale_with_goal(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertIn("Slay 1500 Greed", names)  # 3 per island x 5 islands x 100
        self.assertNotIn("Slay 1600 Greed", names)
        self.assertIn("Survive 50 Nights", names)

    def test_junk_is_labelled(self) -> None:
        junk = [name for name, data in ITEM_TABLE.items() if data.group == "Junk"]
        self.assertTrue(junk)
        self.assertTrue(all(name.endswith(" (Junk)") for name in junk))

    def test_death_link_default_is_both(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["death_link_effect"], 2)


class TestShortGoal(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 1, "death_link_effect": "harsh"}

    def test_short_lists(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertIn("Slay 300 Greed", names)
        self.assertNotIn("Slay 400 Greed", names)
        self.assertIn("Build Wall 6", names)
        self.assertNotIn("Build Wall 7", names)
        self.assertIn("Build Farm 1", names)
        self.assertNotIn("Build Farm 2", names)

    def test_harsh_alias(self) -> None:
        self.assertEqual(self.world.options.death_link_effect.value, 2)


def _names(test) -> set[str]:
    return {loc.name for loc in test.multiworld.get_locations(test.player)}


def _pool(test) -> list[str]:
    return [item.name for item in test.multiworld.itempool if item.player == test.player]


class TestOneIsland(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 1}

    def test_only_island_one(self) -> None:
        names = _names(self)
        self.assertIn("Island 1: Liberate", names)
        self.assertFalse(any(name.startswith("Island 2") for name in names))
        self.assertNotIn("Island 2: Free the Dog", names)
        self.assertNotIn("Island 2: Build the Quarry", names)
        self.assertEqual(self.world.islands_in_play, [1])
        self.assertEqual(self.world.fill_slot_data()["play_islands"], 1)

    def test_pool_fits_one_island(self) -> None:
        pool = _pool(self)
        self.assertFalse(any(name.endswith(" Unlock") for name in pool))
        self.assertIn("Ballista Hermit", pool)  # island 1's hermit
        self.assertNotIn("Horn Hermit", pool)  # island 5's
        self.assertEqual(pool.count("Gem Pouch"), 4)  # island 1 costs 8 gems, x1.5, 3 per pouch

    def test_rare_greed_left_out(self) -> None:
        names = _names(self)
        self.assertFalse(any(name.startswith(("Slay ", "Hunt ")) and ("Floaters" in name or "Breeders" in name
                                                                       or "Crown Stealers" in name) for name in names))
        self.assertIn("Slay 100 Greed", names)

    def test_goal_is_island_one(self) -> None:
        self.assertFalse(self.multiworld.has_beaten_game(self.multiworld.state))
        self.collect_by_name(["Stone Technology", "Iron Technology"])
        self.collect(self.get_items_by_name("Progressive Castle")[:5])
        self.assertTrue(self.multiworld.has_beaten_game(self.multiworld.state))


class TestThreeIslands(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 3}

    def test_islands_one_to_three(self) -> None:
        names = _names(self)
        self.assertIn("Island 3: Liberate", names)
        self.assertIn("Island 2: Build the Quarry", names)  # the quarry is on island 2
        self.assertIn("Island 2: Free the Dog", names)
        self.assertFalse(any(name.startswith(("Island 4", "Island 5")) for name in names))
        self.assertIn("Slay 10 Floaters", names)
        self.assertIn("Slay 3 Breeders", names)
        pool = _pool(self)
        self.assertIn("Island 3 Unlock", pool)
        self.assertNotIn("Island 4 Unlock", pool)
        self.assertNotIn("Warrior Hermit", pool)

    def test_cannot_reach_far_islands(self) -> None:
        regions = {region.name for region in self.multiworld.get_regions(self.player)}
        self.assertNotIn("Island 4", regions)


class TestAllIslandsWhenOff(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 1, "only_goal_islands": False}

    def test_every_island_in_seed(self) -> None:
        names = _names(self)
        self.assertIn("Island 5: Liberate", names)
        self.assertIn("Island 5 Unlock", _pool(self))
        self.assertEqual(self.world.fill_slot_data()["play_islands"], 5)


class TestNorselandsOneIsland(KingdomTwoCrownsTestBase):
    options = {"campaign": "norselands", "islands_to_liberate": 1}

    def test_only_heimdall(self) -> None:
        pool = _pool(self)
        self.assertIn("Power of Heimdall", pool)
        self.assertNotIn("Power of Thor", pool)
        self.assertIn("Island 1: Heimdall's Puzzle", _names(self))


class TestSequentialIslands(KingdomTwoCrownsTestBase):
    options = {"island_unlocks": False}

    def test_all_islands_open(self) -> None:
        for island in range(1, 6):
            self.assertTrue(self.can_reach_region(f"Island {island}"))


class TestMinimalChecks(KingdomTwoCrownsTestBase):
    # One island with almost every check switched off leaves more items than locations; the least important ones
    # (statues, mounts) start in your inventory instead.
    options = {
        "greed_kill_checks": 0,
        "greedling_kill_checks": 0,
        "floater_kill_checks": 0,
        "breeder_kill_checks": 0,
        "crown_stealer_kill_checks": 0,
        "night_checks": 0,
        "tree_checks": 0,
        "building_sanity": False,
        "villager_checks": 0, "archer_checks": 0, "builder_checks": 0, "farmer_checks": 0, "knight_checks": 0,
        "animal_checks": 0, "deer_checks": 0, "bribe_checks": 0, "spend_checks": 0,
        "hermit_building_checks": False, "grass_check": False, "sunk_coins_check": False,
        "islands_to_liberate": 1,
    }

    def test_overflow_starts_in_inventory(self) -> None:
        if self.world.campaign.is_campaign_wide:
            return
        start = [item.name for item in self.multiworld.precollected_items[self.player]]
        self.assertTrue(start)
        self.assertNotIn("Progressive Castle", start)
        self.assertLessEqual(len(_pool(self)), len(_names(self)))


class TestMaxChecksAllTraps(KingdomTwoCrownsTestBase):
    options = {
        "campaign": "norselands",
        "islands_to_liberate": 6,
        "greed_kill_interval": 200,
        "greed_kill_checks": 10,
        "greedling_kill_checks": 10,
        "floater_kill_checks": 10,
        "breeder_kill_checks": 10,
        "crown_stealer_kill_checks": 10,
        "night_checks": 10,
        "tree_checks": 10,
        "villager_interval": 25, "villager_checks": 10, "archer_checks": 10, "builder_checks": 10,
        "farmer_checks": 10, "knight_checks": 10, "animal_checks": 10, "deer_checks": 10, "rabbit_checks": 10,
        "boar_checks": 10, "bribe_checks": 10, "spend_interval": 500, "spend_checks": 10,
        "trap_percentage": 100,
    }


class CampaignTest(KingdomTwoCrownsTestBase):
    campaign = ""

    def test_campaign_pool(self) -> None:
        if not self.campaign:
            self.skipTest("base class")
        campaign = CAMPAIGNS[self.campaign]
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        pool += [item.name for item in self.multiworld.precollected_items[self.player]]
        for name in campaign.statues + campaign.hermits + campaign.mounts + campaign.powers:
            self.assertIn(name, pool)
        self.assertGreaterEqual(3 * pool.count("Gem Pouch"), campaign.total_gem_cost)


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

    def test_building_sanity_totals(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in ("Build Wall 40", "Build Archer Tower 30", "Build Farm 10", "Build Citizen House 10"):
            self.assertIn(name, names)
        self.assertNotIn("Build Wall 41", names)

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

    def test_default_is_every_island(self) -> None:
        self.assertEqual(self.world.islands_in_play, [1, 2, 3, 4, 5, 6])


class TestMinimalOlympus(CampaignTest):
    campaign = "call_of_olympus"
    options = {**TestMinimalChecks.options, "campaign": "call_of_olympus"}


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
