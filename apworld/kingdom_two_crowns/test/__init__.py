from test.bases import WorldTestBase


class KingdomTwoCrownsTestBase(WorldTestBase):
    game = "Kingdom Two Crowns"


class TestBasic(KingdomTwoCrownsTestBase):
    def test_fill(self) -> None:
        """A minimal smoke test: the world generates and every location gets filled."""
        self.world_setup()
        for location in self.multiworld.get_locations(self.player):
            self.assertTrue(location.item is not None)
