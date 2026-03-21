# Arcane Atlas — Game Design Document

**Version:** v0.2 (Full Vision)
**Date:** March 2026
**Platform:** PC (Unity / C#)
**Genre:** Open World Card RPG / Auto-Battler

---

## 1. Vision

### 1.1 Elevator Pitch

An open-world card RPG where players explore a fantasy world, battle
opponents using a draft-based auto-battler combat system, earn booster
packs, and build a growing card collection that shapes future
encounters. Inspired by Magic: The Gathering --- Shandalar, Pokémon
Trading Card Game (GBC), and Once Upon a Galaxy.

### 1.2 Core Fantasy

The player is a wandering card battler traveling across a fantasy world.
They explore themed zones, challenge opponents, win booster packs, and
grow their collection. The cards they unlock determine what appears in
their combat shop, giving them strategic control over their progression
without traditional deck construction.

### 1.3 Design Pillars

-   **Draft, Don't Build:** No pre-constructed decks. Every fight is a
    fresh draft from a shop. Strategy comes from collection curation,
    not deck lists.

-   **Explore & Collect:** Open-world exploration with themed zones.
    Earn booster packs through victory. Slowly expand your card pool.

-   **Auto-Battle Tactics:** Combat resolves automatically. The player's
    skill is expressed through drafting, positioning, and upgrade
    decisions --- not real-time execution.

-   **Tight Scope:** 100 cards across 4 types. No crafting, no
    equipment, no side systems. All depth lives in the card game and
    collection management.

### 1.4 Inspirations

  ------------------------------- -----------------------------------------------------------------------------------------------------------
  **Game**                        **What We Take**
  MTG: Shandalar                  Open-world structure, zone-based exploration, fantasy setting, earn cards through gameplay
  Pokémon TCG (GBC)               Booster pack reward loop, structured progression through themed locations, collection-driven satisfaction
  Once Upon a Galaxy / Lunakara   Shop-based drafting during combat, auto-battle resolution, triple-merge upgrades, tiered card economy
  ------------------------------- -----------------------------------------------------------------------------------------------------------

2. Core Loop

### 2.1 Overview

The core gameplay loop has four phases that repeat throughout the entire
game:

-   **Explore:** Navigate the open world. Enter themed zones. Encounter
    opponents.

-   **Fight:** Enter a multi-round auto-battle. Draft cards from a shop
    each round. Place them on a 5-slot board. Combat resolves
    automatically.

-   **Earn:** Win the fight. Receive a booster pack (5 cards). New cards
    are added to your collection.

-   **Curate:** Manage your collection pool. Toggle which card types are
    active in your shop for future fights.

### 2.2 Progression

Progression is driven entirely by collection growth. As players unlock
more cards, their shop pools expand with more powerful and synergistic
options. Players control this growth by choosing which types/cards are
active in their pool, allowing them to focus builds or diversify.

Zone difficulty scales naturally: early zones have weaker AI opponents
with smaller card pools, while later zones feature opponents with access
to rarer, higher-tier cards and smarter draft strategies.

3. Combat System

### 3.1 Match Structure

Each encounter is a multi-round match between the player and one AI
opponent. Both sides begin with a health pool. The match continues until
one side reaches zero health.

-   **Round Start:** Both player and opponent receive gold (scaling each
    round: 1, 2, 3, etc.).

-   **Shop Phase:** Player spends gold to buy cards from a randomized
    shop. Cards are placed into board slots. Player can also sell cards
    or reroll the shop.

-   **Combat Phase:** Cards on both boards fight automatically.
    Abilities trigger based on card rules. The losing side takes damage
    equal to the power differential (TBD).

-   **Round End:** Board state carries forward. Gold resets and
    increments. Shop refreshes. Next round begins.

### 3.2 The Board

Each player has a 5-slot board arranged in two rows:

-   **Front Row (3 slots):** Primary combat units. These attack first
    and absorb damage.

-   **Back Row (2 slots):** Support units. These provide buffs, healing,
    ranged attacks, or special abilities.

Card placement matters. Front row cards are targeted first. Back row
cards are protected until the front row is cleared or a card has a
specific targeting ability.

### 3.3 The Shop

The shop presents a selection of cards each round, drawn from the
player's active collection pool. Key mechanics:

-   **Tiered Cards:** Cards have rarity tiers (Common, Uncommon, Rare,
    Legendary). Higher tiers appear as rounds progress and gold
    increases.

-   **Rerolling:** Players can spend gold to refresh the shop with new
    options.

-   **Selling:** Cards on the board can be sold back for partial gold
    value to free up a slot and fund a better card.

-   **Triple Merge:** Buying a third copy of the same card automatically
    upgrades it to a stronger version (bronze → silver → gold). This is
    a core strategic consideration.

### 3.4 Auto-Battle Resolution

Once both sides have finished their shop phase, combat resolves
automatically. Cards attack based on their speed/priority stats,
abilities trigger in sequence, and the round ends when one side's board
is cleared or a timer expires. The losing side takes damage to their
health pool based on the remaining power of the winning side's board.

### 3.5 AI Opponents

AI opponents use the same system as the player: they have a card pool
(themed to their zone), receive gold each round, and draft from their
own shop. Difficulty scales by giving AI opponents access to better card
pools and smarter purchasing heuristics.

4. Card System

### 4.1 Card Types (Colors)

There are four elemental card types. Each has a distinct combat
identity:

  ----------- -------------- ------------------------------------------------------------------------------------------------
  **Type**    **Identity**   **Playstyle**
  **Fire**    Aggression     High attack, low health. Burst damage. Rewards going fast and overwhelming the opponent early.
  **Water**   Control        Debuffs, freezes, disruption. Slows opponent's board development. Rewards patience and denial.
  **Earth**   Defense        High health, taunt, shields. Protects back row. Rewards stalling and outlasting opponents.
  **Wind**    Utility        Speed, evasion, card draw, shop manipulation. Rewards flexibility and adaptation.
  ----------- -------------- ------------------------------------------------------------------------------------------------

### 4.2 Card Count

Target: 100 cards total at launch (25 per type).

  ------------ -------------- ----------- ------------------
  **Rarity**   **Per Type**   **Total**   **Approx. Cost**
  Common       10             40          1--2 gold
  Uncommon     8              32          2--3 gold
  Rare         5              20          3--4 gold
  Legendary    2              8           5+ gold
  ------------ -------------- ----------- ------------------

### 4.3 Card Anatomy

Each card has the following attributes:

-   **Name:** Unique identifier.

-   **Type:** Fire, Water, Earth, or Wind.

-   **Rarity:** Common, Uncommon, Rare, or Legendary.

-   **Cost:** Gold cost to purchase from shop.

-   **Attack:** Damage dealt per combat action.

-   **Health:** Damage absorbed before destroyed.

-   **Ability:** Special effect that triggers during auto-battle (e.g.,
    "On attack: deal 2 damage to a random back-row enemy").

-   **Row Preference:** Front, Back, or Either. Determines valid
    placement slots.

### 4.4 Triple Merge Upgrades

When a player buys a third copy of the same card, all three merge into
an upgraded version:

-   **Bronze → Silver:** Stats increase (~+50%). Ability may gain bonus
    effect.

-   **Silver → Gold:** Stats increase significantly (~+100% from base).
    Ability becomes its strongest version.

Merged cards retain their board slot. This creates a key tension: do you
buy duplicates to chase an upgrade, or diversify with different cards to
fill your board faster?

### 4.5 Collection Pool Curation

Outside of combat, players manage their active collection pool. This is
the key strategic layer that replaces traditional deck building:

-   All unlocked cards are available in the collection screen.

-   Players toggle which types are "active" for shop pools.

-   A focused pool (e.g., only Fire + Earth) means higher chances of
    seeing specific cards and hitting triples.

-   A broad pool (all 4 types) gives more options but lower consistency.

-   Players may also toggle individual cards on/off within a type for
    finer control.

This system gives players deep strategic control over their draft
experience without requiring them to construct a fixed deck.

5. World & Exploration

### 5.1 World Structure

The game world is a fantasy overworld composed of distinct themed zones.
Players explore freely in any order, similar to Shandalar's open map.
Each zone has a visual identity tied to one of the four card types.

### 5.2 Zone Design

  ----------------- --------------- ----------------------------------------------------------------------------
  **Zone**          **Card Type**   **Description**
  Volcanic Wastes   Fire            Scorched plains, lava rivers. Opponents favor aggressive rush strategies.
  Coral Depths      Water           Underwater caverns, tidal pools. Opponents use control and debuff tactics.
  Ancient Forest    Earth           Dense woodland, overgrown ruins. Opponents build tanky, defensive boards.
  Sky Peaks         Wind            Mountain summits, floating islands. Opponents use speed and evasion.
  ----------------- --------------- ----------------------------------------------------------------------------

*Additional neutral zones (towns, crossroads, etc.) serve as hubs for
story events, NPC interactions, and collection management.*

### 5.3 Zone Encounters

Zones contain repeatable encounters that players can grind, similar to
random battles in Final Fantasy. When entering a zone:

-   Opponents are drawn from a zone-appropriate pool with themed card
    collections.

-   Difficulty scales based on zone tier and player progression.

-   Winning a fight rewards one booster pack containing 5 cards.

-   Booster packs are weighted toward the zone's card type but can
    contain cards from any type.

### 5.4 Overworld Movement

The player navigates the overworld as a character sprite on a 2D
top-down map. Movement is free-roaming within zones. Zone transitions
happen at map edges or through pathways connecting regions.

6. Story

### 6.1 Narrative Approach

The story is open-ended. Players can explore zones and encounter story
beats in any order. An overarching narrative provides direction and a
final goal, but the path is non-linear.

Story details are TBD. The narrative framework should support: a reason
for the player to travel, a reason to battle, a reason to collect, and a
final confrontation or achievement that serves as an endpoint.

### 6.2 Story Requirements

-   Open-ended exploration with non-linear story beats.

-   NPCs in zones and towns that provide context, lore, and quests.

-   A clear end goal (e.g., defeat a final boss, become the champion,
    save the world).

-   Story should be light enough to not block gameplay but present
    enough to motivate exploration.

7. Economy

### 7.1 In-Match Economy

Gold is the only in-match currency. It resets at the start of each match
and scales per round.

  ----------- ---------- --------------------------
  **Round**   **Gold**   **Available Tiers**
  1           1          Common only
  2           2          Common
  3           3          Common + Uncommon
  4           4          Common + Uncommon
  5           5          Common + Uncommon + Rare
  6+          6+         All tiers
  ----------- ---------- --------------------------

### 7.2 Overworld Economy

Outside of matches, the economy revolves around the card collection:

-   **Booster Packs:** Earned by winning fights. Each contains 5 cards
    weighted toward the zone's type.

-   **Duplicate Cards:** Excess copies can be sold for an overworld
    currency (TBD) used for shop reroll tokens, cosmetics, or other
    non-power items.

-   **No pay-to-win:** This is a premium game. All cards are earned
    through gameplay.

8. Technical

### 8.1 Engine & Platform

  --------------------- -----------------------------------
  **Detail**            **Value**
  Engine                Unity (C#)
  Primary Platform      PC (Windows / Mac)
  Secondary Platforms   iOS (separate build, post-launch)
  Orientation           Landscape
  Language              C#
  --------------------- -----------------------------------

### 8.2 Development Workflow

This project follows the established Claude-assisted workflow:

-   **Claude Web:** Planning, design, analysis. Produces SESSION.md for
    implementation handoff.

-   **Claude Code:** Implementation from SESSION.md. Generates code,
    file structures, assets.

-   **Developer (Quinn):** Planning, directing, testing. Minimal manual
    implementation.

9. Art Pipeline

### 9.1 Primary Asset Source

All environment art, character sprites, creature animations, and UI
foundations come from the Minifantasy Complete Bundle by Krishna Palacio
(73 asset packs, 8x8 resolution pixel art). This eliminates the need for
original sprite work for the overworld and card illustrations.

### 9.2 Zone-to-Tileset Mapping

  ----------------- --------------- ------------------------------------------------------
  **Zone**          **Card Type**   **Minifantasy Packs**
  Volcanic Wastes   **Fire**        Hellscape, Desolate Desert, Pharaoh Tomb
  Coral Depths      **Water**       Aquatic Adventures, Ships and Docks, Sewers
  Ancient Forest    **Earth**       Forgotten Plains, Ancient Forests, Lost Jungle
  Sky Peaks         **Wind**        Mountain Stronghold, Icy Wilderness, Towers
  Hub Towns         Neutral         Towns I & II, Medieval City, Castles and Strongholds
  ----------------- --------------- ------------------------------------------------------

### 9.3 Card Art Sources

Card illustrations are sourced from Minifantasy creature and character
packs:

-   **Creatures & Monster Creatures:** General-purpose card art for
    common and uncommon cards across all types.

-   **Forest Dwellers:** Earth-type cards (treants, woodland beasts,
    nature spirits).

-   **Undead Creatures & Dark Brotherhood:** Special faction or
    boss-tier card art.

-   **True Heroes I--IV & True Villains:** Rare and Legendary card art,
    boss NPCs.

-   **Enchanted Companions & Wildlife:** Utility and support card art
    (back-row units).

-   **RTS Humans & Orcs:** Troops and soldier-type cards.

### 9.4 UI & Icons

-   **UI Overhaul pack:** Foundation for menus, buttons, panels, and HUD
    elements.

-   **Crafting & Professions I + II:** 3600+ 8x8 icons usable as card
    ability icons, status effects, and shop currency indicators.

-   **Magic Weapons and Effects + Spell Effects I & II:** Combat VFX for
    auto-battle animations.

-   **Portrait Generator:** NPC portraits for dialogue and story scenes.

### 9.5 Custom Assets (Aseprite)

The following assets are unique to this game and will be created
manually in Aseprite:

-   Card frames and borders (per rarity tier: Common, Uncommon, Rare,
    Legendary).

-   Card type color overlays and elemental icons (Fire, Water, Earth,
    Wind).

-   Battle board layout (5-slot grid with front/back row distinction).

-   Shop interface background and card slot holders.

-   Booster pack art and pack-opening animation frames.

-   Collection management screen backgrounds and toggle UI.

-   Triple merge upgrade VFX (bronze → silver → gold glow effects).

### 9.6 Tilemap Workflow

Zone tilemaps are authored directly in Unity using its built-in Tilemap
system. Minifantasy tilesets are imported as sprite sheets, sliced at
8x8, and painted into zone maps using Unity's Tile Palette. This
leverages Unity's strong tilemap tooling and keeps all level design
within the game engine.

10. Scope Control

### 10.1 MVP Definition

The minimum viable product includes:

-   100 cards (25 per type) using Minifantasy creature sprites as card
    art.

-   4 themed zones with repeatable encounters.

-   Full combat system: 5-slot board, shop, auto-battle, triple merge.

-   Collection management with pool curation.

-   Booster pack rewards.

-   Basic overworld navigation.

-   Minimal story framework (intro + ending).

### 10.2 DO NOT List (MVP / Phase A)

**The following features are explicitly out of scope for Phase A
(Sessions 01--14). They are documented in Section 11 as future phases:**

-   Character creation with race/class selection (Phase B)

-   Ghost opponent recording and replay system (Phase B)

-   Town interiors: card shop with timed refresh, inn (Phase B)

-   Card keywords and abilities beyond attack/health (Phase B)

-   Random overworld events and quest NPCs (Phase B)

-   Heart-based damage scaling by card tier (Phase B)

-   World bosses and the 6 Great Champions (Phase C)

-   Grand Tournament and endless mode (Phase C)

-   Hundreds of cards across expanded zones (Phase C)

-   Multiplayer or PvP of any kind (never for v1)

-   Real-money monetization or gacha mechanics (never)

### 10.3 Post-MVP Phases

The full vision is developed in three phases. MVP (Phase A) must be
complete and fun before any Phase B work begins.

-   **Phase A (MVP, Sessions 01--14):** 100 cards, 4 zones, working
    combat with shop drafting, pack opening, collection curation, basic
    exploration. This is the shippable game.

-   **Phase B (Expansion):** Character creation, ghost opponent system,
    town interiors (shop + inn), timed shop refresh, card selling
    economy, quest NPCs, random events, zone-locked card sets, card
    keywords and abilities.

-   **Phase C (Endgame):** World bosses, the 6 Great Champions, Grand
    Tournament, endless mode, ascension system, hundreds of cards across
    expanded zones.

11. Full Vision (Post-MVP)

*This section describes the complete creative vision for Arcane Atlas
beyond the MVP. None of this is in scope for the initial 14-session
build. It is documented here so the MVP architecture supports future
expansion without requiring rewrites.*

### 11.1 Character Creation

When the player starts a New Game, they are presented with a character
builder before entering the world.

-   **Race selection:** Multiple fantasy races (human, elf, orc,
    halfling, goblin, dwarf). Race determines starting zone.

-   **Class selection:** Multiple classes (warrior, mage, rogue, etc.).
    Class may influence starting cards or portrait options.

-   **Portrait generator:** Random portraits generated based on
    race/class using the Minifantasy Portrait Generator.

-   **Name input:** Text field for hero name. Keyboard input for PC,
    popup keyboard for future touch support.

-   **Mechanical impact:** Race/class determines starting zone. All
    other effects are cosmetic. The player's card collection and skill
    determine progression, not character stats.

### 11.2 Game Intro & Story Scenes

After character creation, a brief story intro plays before the player
enters the world.

-   A few illustrated frames with narrative text providing backstory.

-   Scenarios pre-written and stored in JSON, selected randomly per new
    character for replayability.

-   Random background art for story scenes drawn from Minifantasy
    tileset compositions.

-   Ends with the player placed in their starting zone based on chosen
    race/class.

### 11.3 Open World Structure

The overworld is a large, persistent, Zelda-inspired top-down world with
room-based navigation.

#### 11.3.1 Room-Based Navigation

The player moves between discrete rooms (screen-sized areas) with camera
sliding between them, exactly like the original Legend of Zelda. Each
room is a tile-based environment that can contain NPCs, opponents,
events, or nothing.

#### 11.3.2 Persistent World Layout

Landmarks and towns are placed procedurally on first generation but
persist permanently once placed. If the player walks 5 rooms left and
comes back 5 rooms right, everything is where they left it. The world is
seeded so each playthrough has a unique but consistent layout.

#### 11.3.3 Zone Connections

Long roads connect towns and zones. Along these roads, opponents wait to
duel. Moving through zones too quickly without building a collection
puts the player at a disadvantage --- opponents ahead will have cards
the player doesn't, creating natural difficulty gating without
artificial locks.

#### 11.3.4 Difficulty Philosophy

The game is intentionally brutal, inspired by EverQuest's unforgiving
world design. Players who rush ahead will encounter opponents with far
superior cards. Smart players explore thoroughly, collect carefully, and
advance when ready. The visual style references Realm of the Mad God ---
tiny pixel art in a dangerous open world.

### 11.4 Towns & Interiors

Towns are safe zones discovered during exploration. Each town contains
buildings the player can enter.

#### 11.4.1 Card Shop

-   A shopkeeper NPC sells individual cards (singles) and booster packs.

-   The shop inventory refreshes on a timer, like a slot machine with
    random cards at near-random prices based on rarity.

-   Players can see cards they can't yet afford, creating aspirational
    goals.

-   Booster packs come in multiple price tiers: cheap packs (mostly
    commons), mid packs (chance of rares), expensive packs (chance of
    legendaries).

-   Players sell duplicate cards here for gold via a "Sell All Extras"
    button.

#### 11.4.2 The Inn

-   Resting at the inn restores the player's HP to full.

-   Exhausted cards in the collection are refreshed (if card fatigue is
    implemented).

-   The inn serves as a save point and safe harbor.

### 11.5 Ghost Opponent System

The ghost system is the primary method of generating opponents. It
records player behavior and replays it as AI opponents for future
encounters.

#### 11.5.1 How Ghosts Work

-   Every duel the player completes is recorded: which cards were
    drafted, in which slots, in what order, for each round.

-   This recording is saved as a "ghost" --- a string-encoded replay of
    the player's draft decisions (similar to Lunakara's approach).

-   Future opponents in the same zone may replay the player's own ghost
    data, creating opponents that feel human.

-   The first opponent in any zone is always a pre-built generic starter
    ghost. After that, the pool fills with the player's own ghosts and
    randomly generated ghosts.

#### 11.5.2 Difficulty Scaling via Ghosts

-   Early ghosts are weak because the player was weak when they were
    recorded.

-   Later ghosts reflect the player's improved collection and strategy,
    creating a natural difficulty curve.

-   Quests may task the player with "seeding" an area by creating ghost
    opponents through deliberate challenge matches.

-   Zone-specific ghosts only use cards from that zone's card set.

### 11.6 Expanded Combat: Keywords & Abilities

The MVP ships with stat-only cards (attack + health). The full vision
adds keyword abilities that create deep strategic interactions:

-   **Taunt:** Enemies must attack this card first.

-   **Windfury:** This card attacks twice per combat round.

-   **Poison:** Any damage dealt by this card destroys the target
    regardless of remaining health.

-   **First Strike:** This card deals damage before the opponent can
    retaliate.

-   **Divine Shield:** Absorbs the first instance of damage completely.

-   **Shadow:** Cannot be targeted until it attacks.

-   **Flying:** Can only be blocked by other Flying or Reach units.

-   **Spells:** Cards that cast an effect when played instead of placing
    a creature (direct damage, buffs, healing, etc.).

#### 11.6.1 Back Row Support Abilities

Back row cards provide passive support to front row cards they are
positioned behind. The 3x2 grid creates adjacency: back-left (slot 4)
supports front-left and front-center (slots 1--2). Back-right (slot 5)
supports front-center and front-right (slots 2--3).

-   Buff stats of adjacent front-row units (+1 attack, +1 health, etc.).

-   Grant keywords to adjacent front-row units (give taunt, give divine
    shield).

-   Generate gold if battle conditions are met (e.g., "if this unit
    survives, gain +1 gold").

-   Heal adjacent front-row units between combat rounds.

#### 11.6.2 Hearts & Damage Scaling

Players and opponents have a heart-based HP system. Some opponents have
more hearts, allowing them to survive into later rounds where
higher-tier cards become available. Higher-tier cards deal bonus heart
damage, not just more stat damage --- meaning a Legendary card might
deal 3 hearts of damage to the opponent while a Common deals 1. This
creates natural power spikes and makes tier progression feel impactful.

### 11.7 Expanded Economy

#### 11.7.1 Booster Pack Tiers

Booster packs come in multiple price points, each with different rarity
odds:

-   **Basic Pack (cheap):** 5 cards, almost all commons/uncommons. Good
    for filling out the early collection.

-   **Standard Pack (moderate):** 5 cards, guaranteed at least 1
    uncommon, small chance of rare.

-   **Premium Pack (expensive):** 5 cards, guaranteed at least 1 rare,
    small chance of legendary.

-   **Packs are zone-locked:** packs purchased or earned in the Ancient
    Forest only contain Ancient Forest (Earth) cards.

#### 11.7.2 Default Set & Collection Lock

All players begin with a default set of basic cards that cannot be
unchecked from the collection pool until replaced by newly acquired
cards. This ensures the player always has a functional shop pool. As the
player acquires new cards, they can begin unchecking default set cards
to focus their pool. The minimum pool size is always the default set
size --- you can replace cards but never go below the minimum.

#### 11.7.3 Duplicate Economy

Players will accumulate dozens to hundreds of duplicate cards through
booster packs. These can be sold in bulk at any town shop for gold. Gold
is used to buy individual cards (singles), additional booster packs, and
inn services. The economy creates a satisfying loop: win duels → earn
packs → open packs → sell dupes → buy better cards or packs.

### 11.8 Random Events & Quests

-   Not every room has opponents. Some contain wandering NPCs who offer
    quests or rewards.

-   Events are randomized by world seed but consistent within a
    playthrough.

-   Quest types: defeat a specific opponent, seed an area with ghost
    matches, collect a certain card, reach a landmark.

-   Rewards include gold, rare cards, booster packs, and story lore.

### 11.9 Endgame: Champions & Tournament

Once the player has explored all zones and completed their collection,
they can ascend to the Grand Tournament.

-   The Grand Tournament pits the player against the 6 Great Champions
    in sequence.

-   Each Champion has a unique themed deck and special rules (e.g.,
    Champion of Fire starts with bonus gold, Champion of Wind has extra
    hearts).

-   Defeating all 6 Champions wins the game. The player may retire or
    continue in Endless Mode.

-   Endless Mode: face an infinite stream of Great Champion-level ghost
    opponents until you lose or return to town.

-   This is the final content gate and the long-term replayability hook.
