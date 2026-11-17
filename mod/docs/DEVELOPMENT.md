# Developing KTC-Archipelago

Notes for working on the mod and the apworld. Players don't need any of this; see the [main README](../../README.md).

## How the repo fits together

- **[`mod/`](../)**: the in-game BepInEx 6 / IL2CPP mod (C#). It's an Archipelago client (the F2 menu and the
  notification feed) that sends location checks, applies received items and traps, and gates castle upgrades,
  statues, hermits, mounts, bombs and sailing behind items.
- **[`apworld/kingdom_two_crowns/`](../../apworld/kingdom_two_crowns/)**: the Archipelago apworld (Python) used to
  generate seeds: item and location tables, options, regions and access logic.
  [`campaigns.py`](../../apworld/kingdom_two_crowns/campaigns.py) holds each campaign's per-island content, taken
  from the game's own level configs.

The mod refers to items and locations only by name and looks their ids up in the server's data package, so the
apworld is the single source of truth for ids. Names still have to match: `mod/Archipelago/GameData.cs` mirrors the
names and campaign tables in `items.py`, `locations.py` and `campaigns.py`, so a change to one needs the matching
change in the other. Options reach the mod through slot data (`world.fill_slot_data`).

## Building

- **Mod:** `dotnet build -c Release` in `mod/`. The csproj points at the game's `BepInEx/interop` folder through
  `GamePath`, so the game needs BepInEx installed and launched once. Copy `APKingdom2Crown.dll`,
  `Archipelago.MultiClient.Net.dll` and `Newtonsoft.Json.dll` from `mod/bin/Release/net6.0/` into
  `BepInEx/plugins/Archipelago/`.
- **apworld:** zip the `apworld/kingdom_two_crowns` folder (the folder itself at the zip's root) and rename it to
  `kingdom_two_crowns.apworld`. The packed manifest also needs `"version": 7` and `"compatible_version": 7`, which
  the source `archipelago.json` must not have (Archipelago's tests reject them there).
- **Tests:** copy the apworld folder into `worlds/` of an Archipelago source checkout and run
  `pytest worlds/kingdom_two_crowns/test/__init__.py`, plus `test/general` for the checks every world must pass.

## Making a release

`python mod/make_release.py` builds the mod and writes `mod/release/` (gitignored):

- `kingdom_two_crowns.apworld`, with the packaging version added to its manifest
- `KTC-Archipelago-v<version>.zip`, a drop-in install for the game folder: BepInEx's core files, its .NET runtime
  and loader taken from your game folder's BepInEx install, plus the mod

It never packages `BepInEx/config` (your own server address, slot name and saved password), BepInEx's generated
folders (`cache`, `interop`, `unity-libs`) or logs. `--game` points it at another game folder and `--no-build` skips
the build. The version comes from `<Version>` in the csproj; keep `world_version` in `archipelago.json` in step.

## Testing in game with the Debug tab

Most game hooks were written against the game's method names without being able to step through its code, so
each one is worth a quick check in a throwaway save. The Debug tab plus `BepInEx/LogOutput.log` (look for `[AP]`)
make this quick:

1. **Traps:** give each trap and watch the feed message and the game. Trip should drop the crown so you can pick it
   back up. Slippery Pouch should scatter coins and gems. Damned Night should jump to dusk. Decay should lower or
   break the outermost walls. Waves Gone By only shows on the next waves; the log prints `SpawnWave ... offset=` for
   every wave. Stam Dam should tire your mount. Desertion should turn villagers and workers back into vagrants that
   vanish a moment later. Tool Theft should empty rack slots first, then turn workers back into villagers. Red Moon
   should send a blood-moon wave from both sides.
2. **Gating:** turn on "Apply AP rules while not connected", then check that the castle can't be upgraded without
   Progressive Castle, statues, hermits and mounts refuse payment without their item, and the boat won't sail
   without an island unlock. Grant the item and the block should lift within half a second.
3. **Island unlocks:** with only Island 3 granted, sail from island 1. The map should only let you pick island 3.
   The Game State card shows `LAND_ONE` and the land/island mapping; the log shows any redirect.
4. **Checks:** open a chest, destroy a portal, pay a statue, shrine or cabin and watch the Checks tab. "Refresh
   island census" compares what's on the island with what the apworld expects.
5. **Kills and trees:** the Checks tab counters should tick up as Greed die and trees fall. Trees only count once a
   builder has chopped them down.
6. **DLC campaigns:** Dead Lands ruler statues, Norselands god puzzles and Call of Olympus quests each send their
   own check, and Items of Power refuse to activate until their "Power of ..." item arrives. Puzzles and quests
   finished before you first connected to a seed don't count.
7. **Sanity checks** (everyday ones count on the island you're on): chests should open with nothing inside. Build a
   wall, tower, farm or citizen house, recruit a vagrant, buy a bow or hammer, hunt a deer, let a Greedling grab a
   coin, feed your mount on grass and drop a coin in the sea: each should tick its counter in the Checks tab. The
   log prints `New Walls at x=...` for buildings and `Animal killed: ...` for hunts. Buildings already standing the
   first time you visit an island don't count. Building the quarry, mine or a hermit's building should send its
   check straight away.
8. **DeathLink:** Debug > Simulate events > "DeathLink". Your coins should vanish, your gems should show as stolen in
   the Items tab, and the crown should land off screen towards the nearest portal; the log prints
   `Flinging crown from x=... to x=...`.
9. **Coin sacks:** the sack has no capacity number; its coins and gems are physics bodies and whatever doesn't fit
   falls out. Progressive Coin Sacks change how much fits by resizing the coins inside (capacity goes with about
   size^-1.9, measured in game: the usual sack settles at about 52 items). With "Apply AP rules while not
   connected" on, the coins should grow and spill until the sack holds about half as much, and each Progressive Coin
   Sack from the Give Item card should shrink them again, to about 3x the usual amount with all of them. In Call of
   Olympus, Hermes's sack shrine should refuse payment.
10. **Filler:** Tuft of Grass should fully rest your mount anywhere. Sunken Gold should pop a coin out that sinks
    (without counting as a sunk coin), and Greed Mask Shard should drop a mask beside you that vanishes after a
    while.

## Where the per-island numbers come from

Debug > "Dump campaign data" writes `BepInEx/config/APKingdom2Crown/campaign_dump.txt`: every campaign's island
layout rules straight from the game's level configs (how many chests, portals, statues, shrines and hermits each
island gets). `campaigns.py` and `GameData.cs` are built from that dump and have to be updated together if a game
update changes it.

## Why the menu is drawn the way it is

The menu is drawn into a cached picture whenever something could have changed (mouse movement, clicks, keys, typing,
and a few times a second otherwise), and that picture is shown in between. Drawing IMGUI every frame through the
IL2CPP interop made enough garbage to trigger the game's full (non-incremental) garbage collections several times a
second, which tanked the frame rate while the menu was open. The game's own text field is stripped from this build,
so the menu reads the keyboard itself.
