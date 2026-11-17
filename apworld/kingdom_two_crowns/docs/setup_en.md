# Kingdom Two Crowns Setup Guide

This guide walks through installing the mod, making your options file and connecting to a multiworld. If something
goes wrong, see [Troubleshooting](#troubleshooting) at the end.

## What you need

- *Kingdom Two Crowns* on Steam, on Windows, plus any DLC campaign you want to play. Steam Deck and Linux should
  work too with one extra launch option (see [Troubleshooting](#troubleshooting)).
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6 or newer
- Two files from the [KTC-Archipelago releases page](https://github.com/Collop1/KTC-Archipelago/releases):
  - `KTC-Archipelago-v<version>.zip`: the mod. It already includes BepInEx, the mod loader, so there's nothing
    else to download.
  - `kingdom_two_crowns.apworld`: teaches Archipelago about the game.

## Installing the mod

1. Open the game's folder: in Steam, right-click *Kingdom Two Crowns* > **Manage** > **Browse local files**. It's
   the folder with `KingdomTwoCrowns.exe` in it, usually
   `C:\Program Files (x86)\Steam\steamapps\common\Kingdom Two Crowns`.
2. Open the mod zip and copy everything in it into that folder. Afterwards these should sit right next to
   `KingdomTwoCrowns.exe`:
   - the `BepInEx` and `dotnet` folders
   - `winhttp.dll`, `doorstop_config.ini` and `.doorstop_version`

   Some unzip tools put everything inside an extra folder named after the zip. If that happens, move its contents
   up next to the game's exe.
3. Launch the game from Steam. A black console window opens alongside it: that's BepInEx, so leave it open. The
   first launch takes a few minutes while BepInEx prepares itself for the game, and it needs an internet connection
   to do so. Later launches are quick.
4. Once the game is up, a message in the bottom-right corner says **"Archipelago ready. Press F2 to open the
   menu."** The mod is working.

**Updating:** unzip the new version over the old one, replacing the files. Your connection details and progress
are kept.

**Already using BepInEx for another mod** (such as Kingdom Enhanced)? If it's BepInEx 6 (IL2CPP), you only need the
zip's `BepInEx/plugins/Archipelago` folder: copy it into your own `BepInEx/plugins` folder.

## Installing the apworld

Anyone who makes an options file or generates the multiworld needs the apworld. Double-click
`kingdom_two_crowns.apworld` and the Archipelago Launcher installs it. You can also copy it into the
`custom_worlds` folder of your Archipelago install (`C:\ProgramData\Archipelago\custom_worlds` by default) and
restart the launcher.

## Making your options file

Your options file (a YAML) tells Archipelago which campaign you're playing and how you'd like your game set up.

1. Open the Archipelago Launcher and click **Options Creator**.
2. Pick *Kingdom Two Crowns*.
3. Choose your **Campaign** (Two Crowns, Shogun, Dead Lands, Norselands or Call of Olympus) and change anything else
   you like. The defaults are a good place to start. For a shorter game, lower **Islands to Liberate**.
4. Save it and send it to whoever is generating the multiworld.

The [game page](https://github.com/Collop1/KTC-Archipelago/blob/main/apworld/kingdom_two_crowns/docs/en_Kingdom%20Two%20Crowns.md)
explains what's randomized, the items and traps you can get, and what the goal is.

## Generating and hosting

*Kingdom Two Crowns* isn't one of Archipelago's built-in games, so the archipelago.gg website can't generate a
multiworld with it. Whoever generates needs the apworld installed, and does it on their own computer:

1. Put everyone's options files in the `Players` folder of the Archipelago install.
2. In the Archipelago Launcher, click **Generate**. The multiworld is saved as a zip in the `output` folder.
3. Host it: upload that zip at [archipelago.gg/uploads](https://archipelago.gg/uploads) to get a room, or click
   **Host** in the launcher to run the server yourself.

## Connecting

1. Launch the game and start a new campaign of the kind your options file names. Connect before you start
   playing: puzzles and quests finished before you first connect don't count.
2. Press **F2** to open the Archipelago menu. In the **Connect** tab, click each box and type:
   - the server address with its port, for example `archipelago.gg:38281`
   - your slot name (the name in your options file)
   - the room password, if there is one
3. Click **Connect**. A green "Connected to Archipelago" message confirms it. Your details are remembered for next
   time, and you can turn on auto-connect.

If the campaign you have loaded doesn't match your seed, the menu says so, and nothing is sent or received until you
load the right one. Checks you make while disconnected are saved and sent when you reconnect.

See the [in-game menu guide](https://github.com/Collop1/KTC-Archipelago/blob/main/mod/docs/README.md) for what
each tab does.

## Troubleshooting

### No console window and no "Archipelago ready" message

BepInEx isn't loading.

- Check the files ended up in the right place (step 2 of [Installing the mod](#installing-the-mod)): `BepInEx`,
  `dotnet` and `winhttp.dll` right next to `KingdomTwoCrowns.exe`, not inside an extra folder.
- Some antivirus programs quarantine `winhttp.dll`, which is how BepInEx starts up. Check your antivirus's
  quarantine and allow the file.
- **Steam Deck and Linux:** in Steam, right-click the game > **Properties** > **General**, and set **Launch
  Options** to `WINEDLLOVERRIDES="winhttp=n,b" %command%`.

### The first launch seems stuck or fails

The first launch can take several minutes, and the console window shows what it's doing. It needs an internet
connection to download some Unity files. If it fails partway, close the game, delete the `interop`, `unity-libs`
and `cache` folders inside `BepInEx`, and launch again.

### The game crashes on start, or the mod stopped working after a game update

Delete the `interop` and `cache` folders inside `BepInEx` and launch again: BepInEx rebuilds them for the new game
version. If that doesn't help, check the [releases page](https://github.com/Collop1/KTC-Archipelago/releases) for
a newer version of the mod.

### "Archipelago ready" appears but the menu won't open

The message names the key that opens the menu; it's F2 unless it's been changed in the menu's **Settings** tab. If
you also use Kingdom Enhanced, don't set it to F1, F3 or F4, which Kingdom Enhanced uses.

### Can't connect

- Include the port in the address: `archipelago.gg:38281`, not just `archipelago.gg`.
- Your slot name has to match the name in your options file exactly, capital letters included.
- Rooms on archipelago.gg go to sleep when nobody has been connected for a while. Open the room's page in your
  browser to wake it up, and check the port, which can change when it does.
- The mod and the apworld the multiworld was generated with should be the same version. The menu warns you if the
  seed was made with an older one.

### The menu says "wrong campaign"

The save you have loaded is a different campaign from the one in your seed. Load or start a campaign of the kind
your options file names.

### Still stuck?

[Open an issue](https://github.com/Collop1/KTC-Archipelago/issues) and attach `BepInEx/LogOutput.log` from the
game folder. The log includes your slot name but never your password.
