# Kingdom Two Crowns Setup Guide

## Required Software

- Kingdom Two Crowns (Steam)
- [BepInEx](https://builds.bepinex.dev/projects/bepinex_be) - IL2CPP build, `win-x64` (or your OS), build 735
  or later
- [Cpp2IL.Patch](https://github.com/abevol/KingdomMod/releases/download/2.4.0/Cpp2IL.Patch.zip)
- The KTC-Archipelago mod DLL from this repo's `mod/` releases

## Installation

1. Download the BepInEx IL2CPP build linked above and unzip it into the game's root folder (Steam: right-click
   Kingdom Two Crowns > Manage > Browse local files).
2. Unzip Cpp2IL.Patch into the same folder.
3. Run the game once to let BepInEx initialize itself, then close it.
4. Download the mod DLL and drop it into `BepInEx/plugins/`.
5. Launch the game, open the pause menu, and enter your Archipelago server address, slot name, and password
   (if any), then connect.

See `mod/docs/README.md` in this repository for the full walkthrough with a directory-layout screenshot.
