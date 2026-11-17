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

- Opening coin and gem chests (they hold only the check: no coins or gems spill out), destroying portals
- Paying gems to uncover a statue, wake a mount shrine or free a hermit from its cabin
- Freeing the dog, building the boat, arriving on a new island, liberating an island
- Dead Lands: freeing a caged ruler. Norselands: solving a god's puzzle. Call of Olympus: reaching each island
  and completing the oracle's, island and god quests.
- Upgrading your castle (Castle Level 2-7)
- Building the Stone Quarry and the Iron Mine
- Hermit buildings (with `hermit_building_checks`): the first time each hermit builds its building (ballista,
  bakery, stable, ...)
- Island milestones, counted separately on each island so farming one island doesn't finish them: you have to move
  on. They're named after their island and kind, e.g. "Island 3: Slay Greed 100" or "Island 2: Build Walls 5". Your
  YAML sets how often each kind gives a check and how many checks each island has. The defaults are sized from a
  full 23-hour Norselands run, roughly an hour of play per island:
  - Slaying Greed (any kind) and Greedlings, felling trees (at most 100 per island)
  - Surviving nights on the island (losing your crown doesn't reset them)
  - Building sanity: new walls (up to 8 per island), archer towers (up to 5), farms (1) and citizen houses (2),
    which stays within what the hardest difficulty guarantees. Upgrades and rebuilding where one stood don't count.
  - Recruiting villagers (vagrants given a coin, or from citizen houses), archers, builders and farmers
  - Hunting animals in total, deer and rabbits (rabbits are off by default: not every campaign has them)
  - Coins spent, coins any Greed picks up (on purpose or by accident), and coins lost to the water
- Campaign milestones, for the rarer things, counted over the whole campaign ("Slay Breeders 3"): the rarer Greed
  (see below), knights (at most 4 per island in the seed) and boars (about one a winter, off by default).
- One-off: your mount eating grass 100 times

Call of Olympus has no numbered islands, so there its island milestones are counted over the whole campaign
instead, with five islands' worth of checks ("Slay Greed 1500", "Build Walls 25").

### Which Greed does each campaign have?

Not every campaign has every kind of Greed. Kill checks for a kind your campaign doesn't have are left out of the
seed automatically, so a Norselands seed never asks you to slay Floaters.

| Greed | Two Crowns | Shogun | Dead Lands | Norselands | Call of Olympus |
|---|---|---|---|---|---|
| Greed (any kind), Greedlings | yes | yes | yes | yes | yes |
| Breeders, Crown Stealers | yes | yes | yes | yes | yes |
| Floaters | yes | yes | yes | no | yes |
| Chargers | no | no | no | yes | when a portal strikes back |
| Greed Knights (shielded head-butters with archers behind them) | no | no | no | no | when a portal strikes back |

Breeders, Crown Stealers, Floaters and Chargers only turn up on the later islands, so they're also left out of
seeds with too few islands.

On numbered islands, checks are progressive per island: the first coin chest you open on island 3 is
"Island 3: Coin Chest 1", the next one "Island 3: Coin Chest 2", no matter which physical chest it was. Call of
Olympus scatters its statues, shrines, hermits and chests at random each campaign, so there they are counted over
the whole campaign ("Coin Chest 7").

## What items can be received?

- **Progressive Castle** (x6): each copy lets your castle be upgraded one more level
- **Stone Technology** and **Iron Technology**
- **Island Unlocks** (with `island_unlocks` on): you can only sail to islands you have unlocked, in any order
  (only the islands in the seed have one)
- **Island Bombs** (numbered islands): the bomb banners at an island's castle stay shut until you have that
  island's bombs, and you need a bomb to blow up its cliff portal and liberate it. They turn up over the whole game,
  so you liberate islands as you go instead of all of them at the end.
- **Progressive Coin Sacks** (`progressive_coin_sacks`, 5 by default, 5-10 or `disabled`): your coin sack starts at
  half its usual size, so coins spill out sooner, and each sack makes it bigger in equal steps up to 3x its usual
  size with all of them. (The sack itself looks the same; the coins in it are drawn bigger or smaller.) While
  they're on, Hermes's sack upgrade in Call of Olympus can't be bought.
- **Hermes Sack** (Call of Olympus with progressive sacks disabled): lets you buy Hermes's sack upgrade.
- **Statues, hermits and mounts** for your campaign: uncovering them is a check, but activating a statue's
  blessing, paying a hermit to build or riding a mount needs its item
- **Powers** (Norselands and Call of Olympus): earning an Item of Power is a check, but using it needs its
  "Power of ..." item
- **Gem Pouches** (3 gems each): chests no longer hand out gems, so these are how you pay gem costs. Logic
  expects enough pouches to cover the gem costs of an island and every island before it, and the pool holds 1.5x
  that many as a margin.
- Filler:
  - **Tuft of Grass** (the most common): your mount eats it on the spot, anywhere, even in winter or a forest, and
    is fully rested and well fed
  - **Coins** (3-7 coins), single **Gems**, and **Vagrants** who show up at your town centre
  - Junk that does nothing useful, marked as such: Sunken Gold (Junk) pops a coin out that sinks out of reach,
    Greed Mask Shard (Junk) drops a Greed mask beside you, and the rest (Lost Banner, Rusty Hammer, Banker's IOU,
    Chewed Stick, ...) are just for flavour
- Traps:
  - **Trip**: your crown is knocked off; grab it before the Greed do
  - **Slippery Pouch**: everything in your purse spills out and scatters
  - **Waves Gone By**: waves on your current island become as strong as if extra days had passed
  - **Damned Night**: night falls immediately
  - **Decay**: your outermost walls drop a tier, or crumble if they are already at the lowest tier
  - **Stam Dam**: your mount is exhausted on the spot
  - **Desertion**: some of your subjects give up and leave your kingdom for good
  - **Tool Theft**: the Greed steal tools, from your shop racks first and then from your units' hands
  - **Red Moon**: a blood-moon wave, as big as a real one on your island and day, attacks from both sides right now
  (`bad_king_amount` sets how many subjects or tools Desertion and Tool Theft take)

## What does DeathLink do?

With `death_link` on, losing your crown kills everyone else linked, and their deaths reach you. What someone
else's death does to you is set by `death_link_effect`:

- `crown`: your crown is flung just off screen towards the nearest portal (short of the portal itself). Get it
  back before the Greed do.
- `wallet`: every coin you carry is gone, and the Greed steal your gems. Liberating the island they were stolen on
  gives them back; if you haven't by then, they drop them after `stolen_gems_return_nights` nights (default 10),
  so a seed can never get stuck waiting on them. The Items tab shows what's been stolen.
- `both` (default): both of the above.

These only apply to deaths from DeathLink; traps work as described above.

## What is the goal?

Liberate a number of islands by destroying their cliff portals with bombs: `islands_to_liberate`, which defaults to every
island (5, or 6 in Norselands). In Call of Olympus, defeat the World Serpent on Mount Olympus.

For a shorter game, lower `islands_to_liberate`. With `only_goal_islands` on (the default) the seed then only
uses islands 1 to that number: the other islands have no checks, the boat won't take you there, and their
hermits and Norselands powers aren't in the item pool. A one-island seed is played entirely on island 1. If you
also switch most checks off, there may be more items than checks; the least important ones (statues, mounts and
the like) then start in your inventory. With `only_goal_islands` off, every island stays in the seed and
liberating that many islands, whichever they are, wins.

## Is there an in-game client?

Yes. Press **F2** in game (changeable) for the Archipelago menu: connect to the server, see received items and
remaining checks, request hints, chat and run server commands. A corner feed shows items as you send and receive
them, and a Debug tab lets you give yourself any item or trap.
