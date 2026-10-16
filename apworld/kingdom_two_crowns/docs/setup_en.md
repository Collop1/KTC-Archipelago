# Kingdom Two Crowns Setup Guide

## Required Software

- Kingdom Two Crowns (Steam)
- [BepInEx](https://builds.bepinex.dev/projects/bepinex_be) - IL2CPP build, `win-x64` (or your OS), build 735
  or later
- [Cpp2IL.Patch](https://github.com/abevol/KingdomMod/releases/download/2.4.0/Cpp2IL.Patch.zip)
- The KTC-Archipelago mod from this repo's `mod/` releases (`APKingdom2Crown.dll`,
  `Archipelago.MultiClient.Net.dll` and `Newtonsoft.Json.dll`)

## Installation

1. Download the BepInEx IL2CPP build linked above and unzip it into the game's root folder (Steam: right-click
   Kingdom Two Crowns > Manage > Browse local files).
2. Unzip Cpp2IL.Patch into the same folder.
3. Run the game once to let BepInEx initialize itself, then close it.
4. Put the three mod DLLs in `BepInEx/plugins/Archipelago/`.
5. Launch the game and press **F2** to open the Archipelago menu.

## Connecting

1. In the **Connect** tab, click each box and type the server address (for example `archipelago.gg:38281`), your
   slot name and the room password if it has one.
2. Click **Connect**. The details are remembered for next time, and you can turn on auto-connect.
3. Start a new campaign of the kind your YAML's `campaign` option names (Two Crowns, Shogun, Dead Lands, Norselands or
   Call of Olympus). If a different campaign is loaded, the menu says so and nothing is sent or received until you
   switch. Checks you make while disconnected are saved and sent when you reconnect.

The menu key can be changed in the **Settings** tab. If you also run Kingdom Enhanced, avoid F1, F3 and F4, which it
uses. Settings and per-seed progress live in `BepInEx/config/APKingdom2Crown/`; there is no config file to edit.
