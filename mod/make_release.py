"""Builds the release folder, mod/release/: the apworld and a drop-in mod zip.

The mod zip's contents go straight into the Kingdom Two Crowns folder (next to KingdomTwoCrowns.exe): BepInEx's core
files, its .NET runtime, the doorstop loader and this plugin. BepInEx is taken from an installed copy in the game folder.
It never includes BepInEx/config, which holds your own server address, slot name and saved password, or BepInEx's
generated folders (cache, interop, unity-libs) and logs, which it rebuilds on first launch.

    python make_release.py [--game "<Kingdom Two Crowns folder>"] [--no-build]
"""
import argparse
import json
import re
import subprocess
import sys
import zipfile
from pathlib import Path

MOD = Path(__file__).resolve().parent
REPO = MOD.parent
APWORLD = REPO / "apworld" / "kingdom_two_crowns"
OUT = MOD / "release"
DEFAULT_GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Kingdom Two Crowns")

# APContainer packaging scheme for .apworld files (Archipelago's worlds/Files.py); only the packed manifest has it.
APWORLD_CONTAINER_VERSION = 7

# From the game folder: BepInEx itself.
GAME_FILES = ["winhttp.dll", "doorstop_config.ini", ".doorstop_version"]
GAME_DIRS = ["dotnet", "BepInEx/core"]
EMPTY_DIRS = ["BepInEx/patchers/", "BepInEx/plugins/"]
# From the build: the plugin and the libraries it brings.
PLUGIN_FILES = ["APKingdom2Crown.dll", "Archipelago.MultiClient.Net.dll", "Newtonsoft.Json.dll"]
PLUGIN_DIR = "BepInEx/plugins/Archipelago"

NEVER = ("BepInEx/config", "BepInEx/cache", "BepInEx/interop", "BepInEx/unity-libs")


def mod_version() -> str:
    csproj = (MOD / "APKingdom2Crown.csproj").read_text(encoding="utf-8")
    match = re.search(r"<Version>([^<]+)</Version>", csproj)
    if not match:
        sys.exit("No <Version> in APKingdom2Crown.csproj")
    return match.group(1).strip()


def build_apworld(path: Path) -> None:
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
        for file in sorted(APWORLD.rglob("*")):
            if file.is_dir() or "__pycache__" in file.parts or file.suffix == ".pyc":
                continue
            arcname = f"{APWORLD.name}/{file.relative_to(APWORLD).as_posix()}"
            if file.name == "archipelago.json" and file.parent == APWORLD:
                manifest = json.loads(file.read_text(encoding="utf-8"))
                manifest["version"] = APWORLD_CONTAINER_VERSION
                manifest["compatible_version"] = APWORLD_CONTAINER_VERSION
                zf.writestr(arcname, json.dumps(manifest, indent=4) + "\n")
            else:
                zf.write(file, arcname)


def build_mod_zip(path: Path, game: Path) -> None:
    entries: list[tuple[Path, str]] = []
    for name in GAME_FILES:
        entries.append((game / name, name))
    for folder in GAME_DIRS:
        root = game / folder
        if not root.is_dir():
            sys.exit(f"{root} is missing: install BepInEx in the game folder first")
        entries += [(f, f.relative_to(game).as_posix()) for f in sorted(root.rglob("*")) if f.is_file()]
    built = MOD / "bin" / "Release" / "net6.0"
    entries += [(built / name, f"{PLUGIN_DIR}/{name}") for name in PLUGIN_FILES]

    for source, arcname in entries:
        if not source.is_file():
            sys.exit(f"{source} is missing")
        if arcname.startswith(NEVER):
            sys.exit(f"Refusing to package {arcname}")

    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
        for folder in EMPTY_DIRS:
            zf.writestr(folder, "")
        for source, arcname in entries:
            zf.write(source, arcname)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--game", type=Path, default=DEFAULT_GAME, help="Kingdom Two Crowns folder with BepInEx installed")
    parser.add_argument("--no-build", action="store_true", help="package the existing Release build")
    args = parser.parse_args()

    version = mod_version()
    world_version = json.loads((APWORLD / "archipelago.json").read_text(encoding="utf-8")).get("world_version")
    if world_version != version:
        print(f"Warning: mod is v{version} but the apworld's world_version is {world_version}")

    if not args.no_build:
        subprocess.run(["dotnet", "build", "-c", "Release", "-nologo", "-v", "q"], cwd=MOD, check=True)

    OUT.mkdir(exist_ok=True)
    for old in [*OUT.glob("*.zip"), *OUT.glob("*.apworld")]:
        old.unlink()

    apworld = OUT / f"{APWORLD.name}.apworld"
    build_apworld(apworld)
    mod_zip = OUT / f"KTC-Archipelago-v{version}.zip"
    build_mod_zip(mod_zip, args.game)

    for file in (apworld, mod_zip):
        print(f"{file.relative_to(REPO)}  ({file.stat().st_size / 1_000_000:.1f} MB)")


if __name__ == "__main__":
    main()
