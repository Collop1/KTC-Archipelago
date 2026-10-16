# KTC-Archipelago

Archipelago multiworld randomizer support for *Kingdom Two Crowns*, forked from
[Catalyss/Archipelago-Kingdom2Crown](https://github.com/Catalyss/Archipelago-Kingdom2Crown) and updated for the
current BepInEx folder layout.

This repo holds both halves of the integration side by side:

- **[`mod/`](mod/)** - the in-game BepInEx/IL2CPP mod (C#): an in-game Archipelago client (F2 menu + notification
  feed) that sends location checks, applies received items and traps, and gates castle upgrades, statues, hermits,
  mounts and sailing behind items. See [`mod/docs/README.md`](mod/docs/README.md) for installation and testing.
- **[`apworld/kingdom_two_crowns/`](apworld/kingdom_two_crowns/)** - the Archipelago "apworld" (Python) that a
  room host runs to generate a multiworld seed: item/location tables, options, region layout and access logic. See
  [`apworld/kingdom_two_crowns/docs/`](apworld/kingdom_two_crowns/docs/) for the game info page and setup guide.
  Supports the base Two Crowns campaign plus Shogun, Dead Lands, Norselands and Call of Olympus
  ([`campaigns.py`](apworld/kingdom_two_crowns/campaigns.py) holds each campaign's per-island content, taken from the
  game's own level configs).

The mod refers to items and locations only by name and looks ids up in the server's data package, so the apworld is
the single source of truth for ids. Names still have to match: `mod/Archipelago/GameData.cs` mirrors the names and
campaign tables in `apworld/kingdom_two_crowns/items.py`, `locations.py` and `campaigns.py`, so a change to one needs
the matching change in the other. Options reach the mod through slot_data (`world.fill_slot_data`).

## Building

- Mod: `dotnet build -c Release` in `mod/` (the csproj points at the game's `BepInEx/interop` folder via `GamePath`).
  Copy `APKingdom2Crown.dll`, `Archipelago.MultiClient.Net.dll` and `Newtonsoft.Json.dll` from
  `mod/bin/Release/net6.0/` into `BepInEx/plugins/Archipelago/`.
- apworld: zip the `apworld/kingdom_two_crowns` folder (the folder itself at the zip root) and rename it to
  `kingdom_two_crowns.apworld`, then drop it in Archipelago's `custom_worlds` folder. Its tests run with Archipelago's
  test suite (`pytest worlds/kingdom_two_crowns/test/__init__.py` from an Archipelago source checkout).
