# Archipelago mod for *Kingdom Two Crowns*

### What is randomized?

See [the apworld's game page](../../apworld/kingdom_two_crowns/docs/en_Kingdom%20Two%20Crowns.md) for the full
list of checks, items and traps.

### How to install the mod?

There is no mod loader for K2C, so BepInEx has to be installed by hand:

First, download BepInEx from https://builds.bepinex.dev/projects/bepinex_be, version
`BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.735+5fef357.zip` (replace `win-x64` with your operating system).
*Make sure the last number is 735 or later, and that it is the IL2CPP build, not Mono.*

Unzip it in the game's root folder (Steam: right-click the game > Manage > Browse local files, usually
`[Steam]/steamapps/common/Kingdom Two Crowns`).

Do the same for CPP2IL.Patch (https://github.com/abevol/KingdomMod/releases/download/2.4.0/Cpp2IL.Patch.zip).

The directory should now look like this:
<img width="714" height="461" alt="K2CDirectoryWithBepin" src="https://github.com/user-attachments/assets/386df788-973a-4457-84c4-489d2313895c" />
*(the zip files don't have to stay here)*

Run the game once so BepInEx can initialize, then close it.

Finally, put `APKingdom2Crown.dll`, `Archipelago.MultiClient.Net.dll` and `Newtonsoft.Json.dll` in
`BepInEx/plugins/Archipelago/`.

### Playing

Press **F2** in game to open the Archipelago menu:

| Tab | What it's for |
| --- | --- |
| Connect | Server address, slot, password, connect/disconnect, DeathLink toggle |
| Items | What you've unlocked (castle tiers, tech, islands, statues, hermits, mounts) and every item received |
| Checks | Milestone counters and what's next, checks on your current island, every missing check with a Hint button |
| Hints | Hint points, request item/location hints, all hints for your slot |
| Chat | The full server log (everyone's items, chat, command replies) and a chat box for `!hint`, `!remaining` etc. |
| Debug | Give yourself any item or trap, simulate events, see game state, send a check by hand |
| Settings | Window scale, opacity, menu key, and the notification feed (corner, duration, size) |

Text boxes are typed into directly (click, type, Enter). The game's own text field is stripped from this build, so the
menu reads the keyboard itself and stops your monarch moving while you type.

The notification feed in the bottom-right corner shows items you send and receive, traps and connection news even
with the menu closed.

### Testing new features with the Debug tab

Most of the game hooks were written against the game's method names without being able to step through its code, so
each one is worth a quick check in a throwaway save. The Debug tab plus `BepInEx/LogOutput.log` (look for `[AP]`) make
this quick:

1. **Traps**: give each trap and watch the feed message and the game.
   Trip should drop the crown so you can pick it back up. Slippery Pouch should scatter coins and gems. Wave Back
   should send Greed from a portal. Damned Night should jump to dusk. Decay should lower or break the outermost walls.
   Waves Gone By only shows on the next waves; the log prints `SpawnWave ... offset=` for every wave.
2. **Gating**: turn on "Apply AP rules while not connected", then check that the castle can't be upgraded without
   Progressive Castle, statues/hermits/mounts refuse payment without their item, and the boat won't sail without an
   island unlock. Grant the item and the block should lift within half a second.
3. **Island unlocks**: with only Island 3 granted, sail from island 1. The map should only let you pick island 3.
   The Game State card shows `LAND_ONE` and the land/island mapping; the log shows any redirect.
4. **Checks**: open a chest, destroy a portal, pay a statue/shrine/cabin and watch the Checks tab. "Refresh island
   census" compares what's on the island with what the apworld expects.
5. **Kills / trees**: the Checks tab counters should tick up as Greed die and trees fall. Trees only count once a
   builder has chopped them down.
6. **DLC campaigns**: Dead Lands ruler statues, Norselands god puzzles and Call of Olympus quests each send their own
   check, and Items of Power refuse to activate until their "Power of ..." item arrives. Puzzles and quests already
   done before you first connected to a seed don't count, so start the campaign after connecting.

### Where the per-island numbers come from

Debug > "Dump campaign data" writes `BepInEx/config/APKingdom2Crown/campaign_dump.txt`: every campaign's island layout
rules straight from the game's level configs (how many chests, portals, statues, shrines and hermits each island
gets). `apworld/kingdom_two_crowns/campaigns.py` and `mod/Archipelago/GameData.cs` are built from that dump and have to
be updated together if a game update changes it.
