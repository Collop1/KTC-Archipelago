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
- Upgrading your castle (Castle Level 2-7) and fully upgrading an island
- Building the Stone Quarry and the Iron Mine
- Hermit buildings (with `hermit_building_checks`): the first time each hermit builds its building (ballista,
  bakery, stable, ...)
- Building sanity (`building_sanity`): one check for every new wall, archer tower, farm and citizen house, up to
  about 6-7 walls, 5 towers and 1-2 farms and houses per island you need to liberate (40 / 30 / 10 / 10 for all six
  Norselands islands, which stays under what the hardest difficulty guarantees). Counted over the whole campaign.
- Milestones, counted over the whole campaign. Your YAML sets how often each kind gives a check and how many
  checks there are **per island you need to liberate**, so a one-island goal gets a short list and a full
  campaign a long one. Later checks of a kind also expect more islands to be open, so they come up later in logic.
  - Slaying Greed (in total and per type: Greedlings, Floaters, Breeders, Crown Stealers) and felling trees.
    Floaters and Crown Stealers need a seed with at least 2 islands and Breeders at least 3, since they may
    never turn up on the first island; with fewer islands those checks are left out.
  - Surviving nights: losing your crown resets the count to 0, so the next check needs that many nights in a row
  - Unit sanity: villagers recruited, and archers, builders, farmers and knights equipped
  - Animal sanity: animals hunted in total, plus deer, rabbits and boars (rabbits and boars are off by default
    because not every campaign has them)
  - Bribing the Greed: coins any Greed picks up, on purpose or by accident
  - Coins spent on anything
- One-offs: your mount eating grass 100 times, and 25 coins lost to the water

On numbered islands, checks are progressive per island: the first coin chest you open on island 3 is
"Island 3: Coin Chest 1", the next one "Island 3: Coin Chest 2", no matter which physical chest it was. Call of
Olympus scatters its statues, shrines, hermits and chests at random each campaign, so there they are counted over
the whole campaign ("Coin Chest 7").

## What items can be received?

- **Progressive Castle** (x6): each copy lets your castle be upgraded one more level
- **Stone Technology** and **Iron Technology**
- **Island Unlocks** (with `island_unlocks` on): you can only sail to islands you have unlocked, in any order
  (only the islands in the seed have one)
- **Statues, hermits and mounts** for your campaign: uncovering them is a check, but activating a statue's
  blessing, paying a hermit to build or riding a mount needs its item
- **Powers** (Norselands and Call of Olympus): earning an Item of Power is a check, but using it needs its
  "Power of ..." item
- **Gem Pouches** (3 gems each): chests no longer hand out gems, so these are how you pay gem costs. Logic
  expects enough pouches to cover the gem costs of an island and every island before it, and the pool holds 1.5x
  that many as a margin.
- Filler: Coin Pouches (3-7 coins), Coin Sacks, single gems, vagrants who show up at your town centre, and junk
  that does nothing, marked as such (Sunken Gold (Junk), Lost Banner (Junk), ...)
- Traps:
  - **Trip**: your crown is knocked off; grab it before the Greed do
  - **Slippery Pouch**: everything in your purse spills out and scatters
  - **Wave Back**: a portal retaliates and sends Greed at you right now, day or night
  - **Waves Gone By**: waves on your current island become as strong as if extra days had passed
  - **Damned Night**: night falls immediately
  - **Decay**: your outermost walls drop a tier, or crumble if they are already at the lowest tier
  - **Stam Dam**: your mount is exhausted on the spot
  - **Desertion**: some of your subjects give up and leave your kingdom for good
  - **Tool Theft**: the Greed steal tools, from your shop racks first and then from your units' hands
  (`bad_king_amount` sets how many subjects or tools Desertion and Tool Theft take)

## What does DeathLink do?

With `death_link` on, losing your crown kills everyone else linked, and their deaths reach you. What someone
else's death does to you is set by `death_link_effect`:

- `crown`: your crown is flung off screen towards the nearest portal (well out of sight, but short of the portal
  itself). Get it back before the Greed do.
- `wallet`: every coin you carry is gone, and the Greed steal your gems. Liberating the island they were stolen on
  gives them back; if you haven't by then, they drop them after `stolen_gems_return_nights` nights (default 10),
  so a seed can never get stuck waiting on them. The Items tab shows what's been stolen.
- `both` (default): both of the above.

These only apply to deaths from DeathLink; traps work as described above.

## What is the goal?

Liberate a number of islands by destroying their cliff portals: `islands_to_liberate`, which defaults to every
island (5, or 6 in Norselands). In Call of Olympus, defeat the World Serpent on Mount Olympus.

For a shorter game, lower `islands_to_liberate`. With `only_goal_islands` on (the default) the seed then only
uses islands 1 to that number: the other islands have no checks, the boat won't take you there, and their
hermits and Norselands powers aren't in the item pool. A one-island seed is played entirely on island 1. If you
also switch most checks off, there may be more items than checks; the least important ones (statues, mounts and
the like) then start in your inventory. With `only_goal_islands` off, every island stays in the seed and
liberating any that many islands wins.

## Is there an in-game client?

Yes. Press **F2** in game (changeable) for the Archipelago menu: connect to the server, see received items and
remaining checks, request hints, chat and run server commands. A corner feed shows items as you send and receive
them, and a Debug tab lets you give yourself any item or trap.
