# Kingdom Two Crowns

## What is randomized?

Things you do on each island send checks:

- Opening coin and gem chests, destroying portals
- Paying gems to uncover a statue, wake a mount shrine or free a hermit from its cabin
- Freeing the dog, building the boat, arriving on a new island, liberating an island
- Upgrading your castle (Castle Level 2-7) and fully upgrading an island
- Milestones: slaying Greed (in total and per type: Greedlings, Floaters, Breeders, Crown Stealers), surviving
  nights, felling trees and clearing whole forests. How often these give a check is set in your YAML.

Per-island checks are progressive: the first coin chest you open on island 3 is "Island 3: Coin Chest 1", the
next one "Island 3: Coin Chest 2", no matter which physical chest it was.

## What items can be received?

- **Progressive Castle** (x6): each copy lets your castle be upgraded one more level
- **Stone Technology** and **Iron Technology**
- **Island 2-5 Unlock** (with `island_unlocks` on): you can only sail to islands you have unlocked, in any order
- **Statues, hermits and mounts**: uncovering them is a check, but activating a statue's blessing, paying a hermit to
  build or riding a mount needs its item
- Filler: coins, gems, vagrants who show up at your town centre, and junk that does nothing (Sunken Gold, Broken
  Crown, Tuft of Grass, ...)
- Traps:
  - **Trip**: your crown is knocked off; grab it before the Greed do
  - **Slippery Pouch**: everything in your purse spills out and scatters
  - **Wave Back**: a portal retaliates and sends Greed at you right now, day or night
  - **Waves Gone By**: waves on your current island become as strong as if extra days had passed
  - **Damned Night**: night falls immediately
  - **Decay**: your outermost walls drop a tier, or crumble if they are already at the lowest tier

## What is the goal?

Liberate a number of islands (1-5, default all five) by destroying their cliff portals.

## Is there an in-game client?

Yes. Press **F2** in game for the Archipelago menu: connect to the server, see received items and remaining checks,
request hints, chat and run server commands. A corner feed shows items as you send and receive them, and a Debug
tab lets you give yourself any item or trap.

## Are DLCs supported?

Not yet. The base island campaign is supported.
