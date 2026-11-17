from test.bases import WorldTestBase

from ..campaigns import CAMPAIGNS
from ..items import ITEM_TABLE, TRAP_WEIGHT_OPTIONS
from ..locations import LOCATION_NAME_GROUPS, LOCATION_NAME_TO_ID


class KingdomTwoCrownsTestBase(WorldTestBase):
    game = "Kingdom Two Crowns"


def _names(test) -> set[str]:
    return {loc.name for loc in test.multiworld.get_locations(test.player)}


def _pool(test) -> list[str]:
    return [item.name for item in test.multiworld.itempool if item.player == test.player]


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

    def test_liberation_needs_the_islands_bombs(self) -> None:
        liberate = "Island 1: Liberate"
        self.collect_by_name(["Stone Technology", "Iron Technology"])
        self.collect(self.get_items_by_name("Progressive Castle")[:5])
        self.assertFalse(self.can_reach_location(liberate))
        self.collect_by_name("Island 2 Bombs")
        self.assertFalse(self.can_reach_location(liberate))
        self.collect_by_name("Island 1 Bombs")
        self.assertTrue(self.can_reach_location(liberate))

    def test_gem_costs_need_pouches(self) -> None:
        statue = "Island 1: Statue 1"
        self.assertFalse(self.can_reach_location(statue))
        self.collect(self.get_items_by_name("Gem Pouch")[:2])
        self.assertFalse(self.can_reach_location(statue))  # island 1 needs 8 gems
        self.collect(self.get_items_by_name("Gem Pouch")[2:3])
        self.assertTrue(self.can_reach_location(statue))

    def test_island_one_has_no_regular_portal(self) -> None:
        names = _names(self)
        self.assertNotIn("Island 1: Destroy Portal 1", names)
        self.assertIn("Island 5: Destroy Portal 6", names)

    def test_building_sanity_per_island(self) -> None:
        names = _names(self)
        for name in ("Island 1: Build Walls 5", "Island 4: Build Archer Towers 5", "Island 2: Build Farms 1",
                     "Island 5: Build Citizen Houses 2"):
            self.assertIn(name, names)
        for name in ("Island 1: Build Walls 10", "Build Walls 5", "Island 1: Build Citizen Houses 3"):
            self.assertNotIn(name, names)
        self.assertFalse(self.can_reach_location("Island 3: Build Walls 5"))
        self.collect_by_name("Island 3 Unlock")
        self.assertTrue(self.can_reach_location("Island 3: Build Walls 5"))

    def test_island_milestones(self) -> None:
        names = _names(self)
        self.assertIn("Island 1: Slay Greed 300", names)  # 3 per island, one every 100
        self.assertNotIn("Island 1: Slay Greed 400", names)
        self.assertIn("Island 5: Hunt Deer 10", names)
        self.assertIn("Island 3: Survive Nights 10", names)
        self.assertNotIn("Slay Greed 100", names)  # not campaign-wide any more
        self.assertFalse(self.can_reach_location("Island 3: Slay Greed 100"))
        self.collect_by_name("Island 3 Unlock")
        self.assertTrue(self.can_reach_location("Island 3: Slay Greed 100"))

    def test_campaign_milestones(self) -> None:
        names = _names(self)
        self.assertIn("Recruit Knights 10", names)  # under the cap of 4 per island x 5 islands
        self.assertIn("Slay Floaters 25", names)
        self.assertFalse(any(name.startswith(("Slay Chargers", "Slay Greed Knights")) for name in names))
        self.assertIn("Island 2: Sink Coins 100", names)

    def test_removed_and_renamed(self) -> None:
        self.assertNotIn("Fully Upgrade an Island", LOCATION_NAME_TO_ID)
        self.assertNotIn("Coin Sack", ITEM_TABLE)
        self.assertNotIn("Coin Pouch", ITEM_TABLE)
        self.assertNotIn("Wave Back Trap", ITEM_TABLE)
        self.assertNotIn("Sink 25 Coins", LOCATION_NAME_TO_ID)
        self.assertEqual(ITEM_TABLE["Tuft of Grass"].group, "Filler")
        self.assertIn("Red Moon Trap", TRAP_WEIGHT_OPTIONS.values())
        self.assertIn("Island 2: Hunt Deer 10", LOCATION_NAME_GROUPS["Hunt Deer"])

    def test_junk_is_labelled(self) -> None:
        junk = [name for name, data in ITEM_TABLE.items() if data.group == "Junk"]
        self.assertTrue(junk)
        self.assertTrue(all(name.endswith(" (Junk)") for name in junk))

    def test_slot_data(self) -> None:
        data = self.world.fill_slot_data()
        self.assertEqual(data["death_link_effect"], 2)
        self.assertTrue(data["bomb_unlocks"])
        self.assertEqual(data["progressive_coin_sacks"], 5)
        self.assertEqual(data["milestones"]["greed"], {"interval": 100, "count": 3, "per_island": True})
        self.assertEqual(data["milestones"]["walls"], {"interval": 5, "count": 1, "per_island": True})
        self.assertEqual(data["milestones"]["breeders"]["per_island"], False)

    def test_sacks_and_filler(self) -> None:
        pool = _pool(self)
        self.assertEqual(pool.count("Progressive Coin Sack"), 5)
        self.assertNotIn("Hermes Sack", pool)
        self.assertNotIn("Coin Pouch", pool)


class TestShortGoal(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 1, "death_link_effect": "harsh"}

    def test_short_lists(self) -> None:
        names = _names(self)
        self.assertIn("Island 1: Slay Greed 300", names)
        self.assertIn("Island 1: Fell Trees 30", names)
        self.assertIn("Recruit Knights 4", names)
        self.assertNotIn("Recruit Knights 6", names)  # 4 knights per island in the seed
        self.assertIn("Island 1: Build Walls 5", names)
        self.assertIn("Island 1: Build Farms 1", names)

    def test_harsh_alias(self) -> None:
        self.assertEqual(self.world.options.death_link_effect.value, 2)


class TestTreeCap(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 1, "tree_interval": 25, "tree_checks": 10}

    def test_trees_capped_per_island(self) -> None:
        names = _names(self)
        self.assertIn("Island 1: Fell Trees 100", names)
        self.assertNotIn("Island 1: Fell Trees 125", names)


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
        self.assertIn("Island 1 Bombs", pool)
        self.assertNotIn("Island 2 Bombs", pool)
        self.assertIn("Ballista Hermit", pool)  # island 1's hermit
        self.assertNotIn("Horn Hermit", pool)  # island 5's
        self.assertEqual(pool.count("Gem Pouch"), 4)  # island 1 costs 8 gems, x1.5, 3 per pouch

    def test_rare_greed_left_out(self) -> None:
        names = _names(self)
        self.assertFalse(any(kind in name for name in names for kind in ("Floaters", "Breeders", "Crown Stealers")))
        self.assertIn("Island 1: Slay Greed 100", names)

    def test_goal_is_island_one(self) -> None:
        self.assertFalse(self.multiworld.has_beaten_game(self.multiworld.state))
        self.collect_by_name(["Stone Technology", "Iron Technology", "Island 1 Bombs"])
        self.collect(self.get_items_by_name("Progressive Castle")[:5])
        self.assertTrue(self.multiworld.has_beaten_game(self.multiworld.state))


class TestBuildingCaps(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 1, "wall_interval": 1, "wall_checks": 10, "tower_interval": 1,
               "tower_checks": 10, "citizen_house_checks": 10}

    def test_capped_per_island(self) -> None:
        names = _names(self)
        self.assertIn("Island 1: Build Walls 8", names)
        self.assertNotIn("Island 1: Build Walls 9", names)
        self.assertIn("Island 1: Build Archer Towers 5", names)
        self.assertNotIn("Island 1: Build Archer Towers 6", names)
        self.assertNotIn("Island 1: Build Citizen Houses 3", names)


class TestNoSacks(KingdomTwoCrownsTestBase):
    options = {"progressive_coin_sacks": "disabled"}

    def test_no_sacks(self) -> None:
        self.assertNotIn("Progressive Coin Sack", _pool(self))
        self.assertEqual(self.world.fill_slot_data()["progressive_coin_sacks"], 0)


class TestThreeIslands(KingdomTwoCrownsTestBase):
    options = {"islands_to_liberate": 3}

    def test_islands_one_to_three(self) -> None:
        names = _names(self)
        self.assertIn("Island 3: Liberate", names)
        self.assertIn("Island 2: Build the Quarry", names)  # the quarry is on island 2
        self.assertIn("Island 2: Free the Dog", names)
        self.assertFalse(any(name.startswith(("Island 4", "Island 5")) for name in names))
        self.assertIn("Slay Floaters 5", names)
        self.assertIn("Slay Breeders 3", names)
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
        self.assertIn("Island 5: Slay Greed 100", names)
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
        "charger_kill_checks": 0,
        "breeder_kill_checks": 0,
        "crown_stealer_kill_checks": 0,
        "night_checks": 0,
        "tree_checks": 0,
        "wall_checks": 0, "tower_checks": 0, "farm_checks": 0, "citizen_house_checks": 0, "sink_checks": 0,
        "greed_knight_kill_checks": 0, "progressive_coin_sacks": "disabled",
        "villager_checks": 0, "archer_checks": 0, "builder_checks": 0, "farmer_checks": 0, "knight_checks": 0,
        "animal_checks": 0, "deer_checks": 0, "bribe_checks": 0, "spend_checks": 0,
        "hermit_building_checks": False, "grass_check": False,
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
        "charger_kill_checks": 30,
        "breeder_kill_checks": 30,
        "crown_stealer_kill_checks": 30,
        "night_checks": 10,
        "tree_checks": 10,
        "villager_interval": 25, "villager_checks": 10, "archer_checks": 10, "builder_checks": 10,
        "farmer_checks": 10, "knight_checks": 30, "animal_checks": 10, "deer_checks": 10, "rabbit_checks": 10,
        "boar_interval": 1, "boar_checks": 30, "bribe_checks": 10, "spend_interval": 500, "spend_checks": 10,
        "wall_interval": 1, "wall_checks": 10, "tower_interval": 1, "tower_checks": 10, "citizen_house_checks": 10,
        "sink_interval": 10, "sink_checks": 10, "progressive_coin_sacks": 10,
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
        names = _names(self)
        for island in (2, 3, 4):
            self.assertIn(f"Island {island}: Ruler Statue 1", names)


class TestNorselands(CampaignTest):
    campaign = "norselands"
    options = {"campaign": "norselands", "islands_to_liberate": 6}

    def test_buildings_on_every_island(self) -> None:
        names = _names(self)
        for island in range(1, 7):
            self.assertIn(f"Island {island}: Build Walls 5", names)

    def test_chargers_not_floaters(self) -> None:
        names = _names(self)
        self.assertIn("Slay Chargers 2", names)
        self.assertFalse(any(name.startswith(("Slay Floaters", "Slay Greed Knights")) for name in names))

    def test_six_islands_and_puzzles(self) -> None:
        self.assertFalse(self.can_reach_region("Island 6"))
        self.collect_by_name("Island 6 Unlock")
        self.assertTrue(self.can_reach_region("Island 6"))
        names = _names(self)
        for name in ("Island 1: Heimdall's Puzzle", "Island 3: Thor's Puzzle", "Island 4: Hel's Puzzle",
                     "Island 6: Loki's Puzzle", "Island 6: Slay Greed 300"):
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

    def test_no_island_items(self) -> None:
        pool = _pool(self)
        self.assertFalse(any(name.startswith("Island ") for name in pool))

    def test_island_milestones_are_campaign_wide(self) -> None:
        names = _names(self)
        self.assertIn("Slay Greed 1500", names)  # five islands' worth: 3 x 5 checks, one every 100
        self.assertIn("Build Walls 5", names)
        self.assertFalse(any(name.startswith("Island ") for name in names))

    def test_olympus_greed(self) -> None:
        names = _names(self)
        for name in ("Slay Floaters 5", "Slay Chargers 2", "Slay Greed Knights 1"):
            self.assertIn(name, names)

    def test_progressive_sacks_replace_hermes(self) -> None:
        self.assertNotIn("Hermes Sack", _pool(self))


class TestOlympusNoSacks(CampaignTest):
    campaign = "call_of_olympus"
    options = {"campaign": "call_of_olympus", "progressive_coin_sacks": "disabled"}

    def test_hermes_sack_is_an_item(self) -> None:
        pool = _pool(self)
        self.assertIn("Hermes Sack", pool)
        self.assertNotIn("Progressive Coin Sack", pool)
