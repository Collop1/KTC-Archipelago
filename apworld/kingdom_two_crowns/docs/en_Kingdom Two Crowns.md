# Kingdom Two Crowns

## Which campaigns are supported?

Pick one with the `campaign` option and start a new campaign of that kind in the game:

- **Two Crowns** (the base campaign) and **Shogun**: 5 islands
- **Dead Lands**: 5 islands, plus caged rulers to free at ruler statues
- **Norselands**: 6 islands, plus four god puzzles (Heimdall, Thor, Hel, Loki) that grant Items of Power
- **Call of Olympus**: an oracle island, four god islands, four quest islands and Mount Olympus; the goal is
  defeating the World Serpent

The mod notices if the loaded campaign doesn't match the seed and pauses checks and items until it does.

## What is randomized?

Things you do send checks:

- Opening coin and gem chests, destroying portals
- Paying gems to uncover a statue, wake a mount shrine or free a hermit from its cabin
- Freeing the dog, building the boat, arriving on a new island, liberating an island
- Dead Lands: freeing a caged ruler. Norselands: solving a god's puzzle. Call of Olympus: reaching each island
  and completing the oracle's, island and god quests.
- Upgrading your castle (Castle Level 2-7) and fully upgrading an island
- Milestones: slaying Greed (in total and per type: Greedlings, Floaters, Breeders, Crown Stealers), surviving
  nights and felling trees. How often these give a check is set in your YAML.

On numbered islands, checks are progressive per island: the first coin chest you open on island 3 is
"Island 3: Coin Chest 1", the next one "Island 3: Coin Chest 2", no matter which physical chest it was. Call of
Olympus scatters its statues, shrines, hermits and chests at random each campaign, so there they are counted over
the whole campaign ("Coin Chest 7").

## What items can be received?

- **Progressive Castle** (x6): each copy lets your castle be upgraded one more level
- **Stone Technology** and **Iron Technology**
- **Island Unlocks** (with `island_unlocks` on): you can only sail to islands you have unlocked, in any order
- **Statues, hermits and mounts** for your campaign: uncovering them is a check, but activating a statue's
  blessing, paying a hermit to build or riding a mount needs its item
- **Powers** (Norselands and Call of Olympus): earning an Item of Power is a check, but using it needs its
  "Power of ..." item
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

Liberate a number of islands (default 5, up to 6 in Norselands) by destroying their cliff portals. In Call of
Olympus, defeat the World Serpent on Mount Olympus.

## Is there an in-game client?

Yes. Press **F2** in game (changeable) for the Archipelago menu: connect to the server, see received items and
remaining checks, request hints, chat and run server commands. A corner feed shows items as you send and receive
them, and a Debug tab lets you give yourself any item or trap.
