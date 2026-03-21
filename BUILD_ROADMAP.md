# Arcane Atlas — Build Roadmap

## Architecture

Single Canvas with panel-based screen management. Each screen is a child GameObject that gets SetActive(true/false). Some panels are full-screen opaque (backgrounds), others are overlays (HUD, dialogue, modals). A simple ScreenManager script controls transitions.

```
Canvas (Screen Space - Overlay, 1920x1080)
├── Screen_Town          ← Default after "New Journey"
│   ├── BG_Town          ← Full-screen town background
│   ├── HUD_Persistent   ← Gold, packs, zone name (always visible)
│   └── NPC_Interact     ← Clickable NPCs in town
├── Screen_WorldMap      ← Zone select overlay
│   ├── BG_WorldMap      ← Cartographic map background
│   ├── ZoneButtons      ← Clickable zone markers
│   └── Legend_Panel     ← Zone info sidebar
├── Screen_Exploration   ← Ancient Forest (and future zones)
│   ├── BG_Zone          ← Tilemap background (room-based)
│   ├── Player           ← Player sprite + movement
│   ├── NPCs_Zone        ← Encounter NPCs
│   ├── HUD_Zone         ← Minimap, zone name, prompts
│   └── RoomTransition   ← Camera slide between rooms
├── Screen_Encounter     ← Pre-combat dialogue
│   ├── Overlay_Dim      ← Semi-transparent darkener
│   ├── Panel_Dialogue   ← Portrait + text + buttons
│   └── Buttons_Choice   ← Duel / Trade / Leave
├── Screen_Combat        ← Core gameplay
│   ├── BG_Arena         ← Dark arena background
│   ├── Board_Enemy      ← 5 slots (3 front, 2 back)
│   ├── Board_Player     ← 5 slots (3 front, 2 back)
│   ├── Panel_Shop       ← 5 card offers + reroll + end phase
│   ├── HUD_Combat       ← HP bars, round, gold
│   └── VFX_Layer        ← Damage numbers, attack effects
├── Screen_PackOpening   ← Post-victory rewards
│   ├── BG_Celebration   ← Dark + sparkles
│   ├── Banner_Victory   ← "VICTORY" banner
│   ├── Cards_Revealed   ← 5 fanned cards
│   └── Buttons_Post     ← Open next / Collection / Continue
├── Screen_Collection    ← Pool curation
│   ├── Panel_TypeToggles← Fire/Water/Earth/Wind toggles
│   ├── Grid_Cards       ← Scrollable card grid
│   ├── Panel_Detail     ← Selected card enlarged
│   └── Bar_ActivePool   ← Active count + Sell Duplicates
├── Screen_TownShop      ← NPC trade screen
│   ├── BG_ShopInterior  ← Indoor background
│   ├── Portrait_Keeper  ← Shopkeeper portrait
│   ├── Panel_YourCards  ← Duplicates for sale
│   ├── Panel_ShopItems  ← Reroll tokens, packs, cosmetics
│   └── Buttons_Trade    ← Buy / Sell
├── Screen_Settings      ← Options overlay
│   ├── Overlay_Dim
│   ├── Panel_Settings   ← Tabs + sliders + toggles
│   └── Buttons_Apply    ← Apply / Back
├── Screen_MainMenu      ← Title screen (active on launch)
│   ├── BG_Title         ← Four-biome blend background
│   ├── Logo             ← "ARCANE ATLAS"
│   └── Buttons_Menu     ← New Journey / Continue / Collection / Options
└── ScreenManager.cs     ← Controls all transitions
```

## Build Order (Sessions)

Each session = one SESSION.md handoff to Claude Code. Each produces a testable, confirmable result before moving to the next.

---

### SESSION 01: Project Scaffold + Title Screen
**Goal:** Unity project exists. Press Play, see the title screen.

**Deliverables:**
- New Unity 2D project with correct settings (1920x1080, Point filter, no compression)
- Canvas with ScreenManager script
- Screen_MainMenu with placeholder background (solid color with element quadrants)
- Logo text ("ARCANE ATLAS" in TextMeshPro)
- 4 buttons: New Journey, Continue, Collection, Options
- New Journey button activates Screen_Town, deactivates Screen_MainMenu
- Other buttons: placeholder (log message to console)
- CLAUDE.md, GDD.md (copied from our docs)

**Confirm:** Press Play → see title screen → click New Journey → screen transitions to blank town panel

---

### SESSION 02: Town Screen + HUD
**Goal:** Town loads with a background. Persistent HUD overlay appears.

**Deliverables:**
- Screen_Town with placeholder town background (warm brown rectangle with "TOWN" label)
- HUD_Persistent overlay: gold counter, pack counter, 4 element type indicators, zone name
- Navigation buttons: "World Map" button, "Collection" button, "Shop" button
- Buttons activate/deactivate respective screen panels
- Back buttons on each screen return to town

**Confirm:** New Journey → town with HUD → click World Map → see placeholder map panel → Back → town

---

### SESSION 03: World Map + Zone Select
**Goal:** World map displays with selectable zones. Only Ancient Forest is unlocked.

**Deliverables:**
- Screen_WorldMap with placeholder cartographic background
- 4 zone buttons positioned on map (Volcanic Wastes, Coral Depths, Ancient Forest, Sky Peaks)
- Only Ancient Forest is interactable; others show "Locked" tooltip
- Zone info panel on the right showing name, element type, difficulty
- Clicking Ancient Forest transitions to Screen_Exploration
- Back button returns to town

**Confirm:** Town → World Map → click Ancient Forest → transitions to exploration screen

---

### SESSION 04: Exploration + Player Movement
**Goal:** Player character moves around a room-based zone using Unity's new Input System.

**Deliverables:**
- Screen_Exploration with a simple room (colored rectangle, 1920x1080)
- Player character (colored square, 32x32 at 4x scale = 128x128)
- Movement using Unity Input System (WASD / arrow keys)
- Player bounded within room edges
- Room exits at edges (top, bottom, left, right)
- Walking into an exit triggers room transition

**Confirm:** Enter Ancient Forest → move player with WASD → walk to edge → room transitions

---

### SESSION 05: Room-Based Map Generation
**Goal:** Procedural room generation creates a small map of connected rooms. Zelda-style camera slide between rooms.

**Deliverables:**
- Room grid system (e.g., 5x5 grid, not all rooms populated)
- Procedural room layout: generate 8-12 connected rooms with exits matching
- Camera slide transition: when player crosses room boundary, camera pans smoothly to next room, player repositioned
- Room visual variation: 3-4 different background color variants
- 2-3 NPC opponent sprites placed randomly in rooms (not interactive yet)
- Minimap in corner showing room layout and player position

**Confirm:** Explore multiple rooms → camera slides between them → minimap updates → NPCs visible

---

### SESSION 06: Encounter System + Dialogue
**Goal:** Walking near an NPC triggers an encounter dialogue with choices.

**Deliverables:**
- NPC interaction trigger (proximity-based or press E)
- Screen_Encounter overlay activates with dimmed background
- Dialogue panel with NPC portrait placeholder, name, dialogue text
- 3 choice buttons: Duel, Trade, Leave
- "Leave" closes dialogue and returns to exploration
- "Duel" transitions to Screen_Combat
- "Trade" shows placeholder "Coming soon" message
- NPC data: name, element type, difficulty, dialogue lines
- Simple dialogue system supporting multiple lines with "next" button

**Confirm:** Walk near NPC → press E → dialogue appears → click Duel → combat screen loads

---

### SESSION 07: Combat System (Core)
**Goal:** Full combat loop works — shop drafting, board placement, auto-battle resolution.

**Deliverables:**
- Card data system (ScriptableObjects for 20 starter cards, 5 per element)
- Screen_Combat with board (5 slots per side), shop panel (5 offers), HUD (HP, gold, round)
- Shop phase: click to buy, auto-place in valid slot, reroll, sell (right-click)
- AI opponent: simple buy-and-place logic
- Auto-battle: front row attacks first, back row after, damage resolution
- Round progression: gold increases, tier unlocks by round
- Triple merge: 3 copies → upgrade tier (bronze → silver → gold)
- Match end: HP reaches 0, show WIN/LOSE banner
- End Phase button triggers battle
- After battle resolves, next round starts (shop refreshes, gold increases)

**Confirm:** Full match from round 1 to win/loss — buy cards, watch battles, see rounds progress

---

### SESSION 08: Pack Opening + Collection
**Goal:** Winning a fight rewards packs. Cards are added to a persistent collection.

**Deliverables:**
- Screen_PackOpening: victory banner, 5 cards fanning out, "NEW" badges
- Pack opening animation (simple: cards flip from back to front one by one)
- Cards added to persistent collection (ScriptableObject or JSON save)
- Screen_Collection: type toggles, scrollable card grid, detail panel
- Active/inactive toggle per type (affects shop pool in future fights)
- "Active: X/Y" counter
- Sell duplicates button (placeholder functionality)
- Navigation: combat win → pack opening → continue → back to exploration

**Confirm:** Win fight → pack opens → see new cards → go to collection → toggle types → continue exploring

---

### SESSION 09: Pool Curation + Shop Integration
**Goal:** Collection toggles actually affect what appears in combat shops.

**Deliverables:**
- Shop draws from player's active pool only (toggled types/cards)
- If pool is too small, fill remaining slots with random cards
- Collection screen shows which cards are "active" vs "inactive" clearly
- Individual card toggle (not just type-level)
- Shop offers respect tier unlock rules AND active pool
- AI opponent has its own fixed card pool (themed to zone)

**Confirm:** Toggle off Fire → enter combat → shop never shows Fire cards → toggle back on → Fire cards return

---

### SESSION 10: Town Shop + Economy
**Goal:** Town shop lets you sell duplicates and buy items.

**Deliverables:**
- Screen_TownShop with shopkeeper portrait, dual panels
- Sell duplicate cards for gold
- Buy items: reroll tokens (carry into combat), zone-specific booster packs
- Gold persists between encounters
- Overworld currency separate from combat gold
- Shopkeeper NPC in town triggers shop screen

**Confirm:** Town → talk to shopkeeper → sell dupes → buy reroll token → enter combat → use token

---

### SESSION 11: Dialogue Depth + Multiple NPCs
**Goal:** NPCs have personality, multiple dialogue paths, and varied encounters.

**Deliverables:**
- Dialogue system supports branching (choice → different response)
- NPC data includes multiple dialogue trees
- Some dialogue choices affect rewards or difficulty
- 3-4 unique NPCs per zone room (different names, elements, difficulties)
- NPCs remember if you've defeated them (don't respawn immediately)
- Boss NPC in final room of zone (harder, better rewards)

**Confirm:** Talk to NPC → make choices → different outcomes → defeat boss → special reward

---

### SESSION 12: Zone Completion + Progression
**Goal:** Clear all encounters in a zone to "complete" it. Unlock next zone.

**Deliverables:**
- Zone progress tracking: encounters completed / total
- Zone completion state saved
- Completing Ancient Forest unlocks next zone on world map
- Zone difficulty scaling (later zones have tougher AI, rarer cards)
- Return to completed zones to grind (encounters respawn but at reduced reward)
- World map shows completion status per zone

**Confirm:** Clear Ancient Forest → return to map → next zone unlocked → enter new zone with harder opponents

---

### SESSION 13: Settings + Save/Load + Polish
**Goal:** Game state persists between sessions. Settings work.

**Deliverables:**
- Save system: collection, gold, zone progress, settings (JSON to Application.persistentDataPath)
- Load on startup, auto-save on zone exit and combat win
- Screen_Settings: audio sliders (master, music, SFX), display (fullscreen, resolution), controls
- Continue button on title screen loads saved game
- Pause overlay during combat (resume / quit to town)
- Screen transitions have simple fade (0.3s black fade)

**Confirm:** Play → save → quit → relaunch → Continue → all progress intact

---

### SESSION 14: Art Integration Pass
**Goal:** Replace all placeholder art with Minifantasy assets and custom sprites.

**Deliverables:**
- Import all completed sprites from the asset pipeline tool
- Set correct import settings per sprite (PPU, filter mode, sprite mode)
- Replace placeholder backgrounds with tilemap compositions
- Replace card placeholders with framed creature art
- Replace UI rectangles with 9-slice panel sprites
- Replace HUD elements with proper icons and bars
- Adjust layouts as needed to accommodate real art dimensions
- This is the "make it look like the mockups" session

**Confirm:** Every screen matches the ChatGPT mockup style with real Minifantasy art

---

## Session Dependency Graph

```
01 Title Screen
 └→ 02 Town + HUD
     ├→ 03 World Map
     │   └→ 04 Exploration + Movement
     │       └→ 05 Room Generation
     │           └→ 06 Encounters + Dialogue
     │               └→ 07 Combat System ★ CORE
     │                   └→ 08 Pack Opening + Collection
     │                       └→ 09 Pool Curation
     │                           └→ 10 Town Shop
     │                               └→ 11 Dialogue Depth
     │                                   └→ 12 Zone Completion
     │                                       └→ 13 Save/Load/Settings
     │                                           └→ 14 Art Integration
     └→ (Settings accessible from any screen)
```

## Anti-Scope-Creep Rules

1. **No feature gets added that isn't in this roadmap** without explicit approval
2. **Each session must be completable in one Claude Code sitting**
3. **Placeholder art is acceptable at every stage** — the game must be testable without final art
4. **No multiplayer, no IAP, no analytics, no cloud saves** — ever, for v1
5. **If a session is taking too long, cut scope from that session, don't expand it**
6. **"Good enough" beats "perfect"** — we can always iterate later
7. **Test after every session** — if something breaks, fix it before moving on
