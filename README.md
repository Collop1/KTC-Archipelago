# KTC-Archipelago

Archipelago multiworld randomizer support for *Kingdom Two Crowns*, forked from
[Catalyss/Archipelago-Kingdom2Crown](https://github.com/Catalyss/Archipelago-Kingdom2Crown) and updated for the
current BepInEx folder layout.

This repo holds both halves of the integration side by side:

- **[`mod/`](mod/)** - the in-game BepInEx/IL2CPP mod (C#) that connects to an Archipelago server, sends
  location checks, and receives items. See [`mod/docs/README.md`](mod/docs/README.md) for player installation
  instructions.
- **[`apworld/kingdom_two_crowns/`](apworld/kingdom_two_crowns/)** - the Archipelago "apworld" (Python) that a
  room host runs to generate a multiworld seed: item/location tables, region layout, and access logic. See
  [`apworld/kingdom_two_crowns/docs/`](apworld/kingdom_two_crowns/docs/) for the game info page and setup guide,
  and [`apworld/kingdom_two_crowns/docs/Game Flow.md`](apworld/kingdom_two_crowns/docs/Game%20Flow.md) for the
  underlying game-design notes the apworld's logic is built from.

The mod and the apworld must agree on every item/location id - `mod/Archipelago/APIds.cs` and
`apworld/kingdom_two_crowns/items.py` / `locations.py` are kept in sync by hand, so a change to one needs the
matching change in the other.
