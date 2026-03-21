# Cowork Update for Claude Web

**Date:** March 17, 2026
**Sessions Completed:** A (Zone Run System), B (Quest System + Narrative)
**Bug Fixes:** 4 (Encounter buttons, shop element filter Phase 2, shop element filter Phase 3, save/load persistence)
**New Spec:** SESSION_D.md (Tilemap Pipeline + Room Template System)

---

## Session A: Zone Run System — COMPLETE ✓

All changes from SESSION_A.md implemented and tested.

### Files Created/Rewritten
- **RoomData.cs** — Added `RoomType` enum (Empty, NPC, Treasure, Event, Boss), `DifficultyTier` (1-6), `IsDiscovered`, `IsCleared`, `SpawnedNpcs` list
- **RoomGenerator.cs** — Complete rewrite. Random walk + BFS difficulty assignment. Boss placed at max BFS distance (T6). Room type distribution: 60% NPC, 15% Treasure, 10% Event, 15% Empty
- **NpcData.cs** — Added `CardPool` field, `GenerateOpponentPool(int difficultyTier)` for curated per-NPC pools scaling T1-T6, `GetZoneElement()` helper, lore dialogue lines per zone
- **MinimapUI.cs** — Rewritten with room-type-aware coloring. Boss always visible (red), undiscovered=gray, discovered=type colors, cleared=green
- **ExplorationManager.cs** — Major rewrite. Room discovery, persistent NPC spawning via `SpawnedNpcs`, treasure/event room handlers, zone entry narrative text, quest hooks

### Combat Changes
- **CombatManager.cs** — `GetAICardPool()` now uses opponent's stored `CardPool` instead of full element pool

---

## Session B: Quest System + Narrative — COMPLETE ✓

All changes from SESSION_B.md implemented and tested.

### New Files
- **QuestData.cs** — `QuestType` enum (CollectCards, DefeatAllNPCs, DefeatBoss), `QuestStatus` enum, `ZoneQuest` class with Title, NarrativeDescription, RewardDescription, Target, Progress, RunNPCsDefeated
- **QuestManager.cs** — Static class managing 12 quests (3 per zone). Key methods: `Initialize()`, `OnCardAdded()`, `OnNPCDefeated()`, `OnBossDefeated()`, `OnZoneEntered()`, `CompleteQuest()`, `CheckZoneUnlocks()`. Zone unlock chain: 10 Earth→Volcanic Wastes, 10 Fire→Sky Peaks, 10 Wind→Coral Depths
- **QuestLogUI.cs** — Modal overlay with scrollable quest entries per zone, progress bars, narrative text, Atlas progress counter

### Updated Files
- **EncounterManager.cs** — Added quest hooks in `OnCombatComplete()`: `QuestManager.OnNPCDefeated()`, `QuestManager.OnBossDefeated()`
- **ExplorationManager.cs** — Added `QuestManager.OnZoneEntered()` call, `QuestManager.OnCardAdded()` in treasure rewards
- **PackOpeningUI.cs** — Added `QuestManager.OnCardAdded(card)` after collection add
- **MainMenuUI.cs** — Added `QuestManager.Initialize()` in `OnNewJourney()`
- **ZoneData.cs** — Added `ZoneEntryText`, `NextZoneName`, `AdjacentElement`, `GetByName()` helper
- **WorldMapUI.cs** — New fields: `infoCards`, `infoQuests`, `atlasProgressText`, `zoneLabels[]`. Shows card/quest counts per zone, Atlas progress, unlock requirements
- **SaveSystem.cs** — Added `SerializedQuest` class, quest save/load, element filter persistence
- **ExplorationHUD.cs** — Added `btnQuests`, `questLog` reference, `ToggleQuestLog()`

### CanvasBuilderTool.cs Updates
- **BuildQuestLog()** — New method. Full-screen overlay with ScrollRect, viewport+mask, VerticalLayoutGroup content, close button, Atlas progress text. Wired to QuestLogUI
- **BuildExplorationHUD()** — Added quest button (right side of bottom bar), wired to quest log panel
- **BuildWorldMap()** — Added InfoCards, InfoQuests TMP elements in info panel. Added AtlasProgress text top-right. Added 4 zoneLabels near markers. All wired to WorldMapUI

---

## Bug Fixes

### 1. Encounter Dialogue Missing Duel Button — FIXED
**Symptom:** NPC encounters only showed "Leave" button, no "Duel" option.
**Root Cause:** `ExplorationManager.ShowRoomMessage()` hid btnDuel/btnTrade with `SetActive(false)` for treasure/event popups. `EncounterUI.ShowChoices()` only enabled `choicePanel` without re-enabling individual buttons.
**Fix:** Added explicit `SetActive(true)` for btnDuel, btnTrade, btnLeave in `EncounterUI.ShowChoices()`.

### 2. Shop Ignoring Element Filter (Phase 2 + Phase 3) — FIXED
**Symptom:** Player unchecks Fire element in collection, but Fire cards still appear in combat shop.
**Root Cause:** `CardDatabase.GetActivePool()` had three phases. Phase 1 correctly filtered by element. Phase 2 (padding with owned-but-inactive cards) and Phase 3 (last-resort T1 commons) both ignored `PlayerCollection.IsElementActive()`.
**Fix:** Added `PlayerCollection.IsElementActive(c.Element)` filter to both Phase 2 and Phase 3 LINQ queries.

### 3. Save/Load Not Persisting Collection State — FIXED
**Symptom:** Collection reset to defaults on Continue. Element filters lost.
**Root Cause:** Two issues:
1. `PlayerCollection.FireActive/WaterActive/EarthActive/WindActive` were never serialized — static fields reset to `true` on app restart
2. `SaveSystem.Save()` was only called on zone exit and after combat. Collection changes (card toggles, element filters), pack opening, and town shop purchases never triggered a save.
**Fix:**
- Added 4 element filter bools to `SaveData`, save writes them, load restores them (with legacy save detection: all-false → default all-true)
- Added `SaveSystem.Save()` calls to: CollectionUI (on back), TownShopUI (on back), PackOpeningUI (after reveal)
- Added element filter reset to `MainMenuUI.OnNewJourney()`

---

## Save System Coverage (Current)

| Trigger | Location |
|---------|----------|
| Leave zone | ExplorationManager |
| After combat | CombatManager |
| Leave collection screen | CollectionUI |
| Leave town shop | TownShopUI |
| After pack opening | PackOpeningUI |

### What Gets Saved
Gold, Packs, RerollTokens, CurrentZone, ZonesUnlocked[], ZonesCompleted, Collection (CardName, Count, IsActive, IsStarter), BossDefeatCounts, Quests (ZoneName, Type, Status, Progress), HasSeenCurationTip, HasSeenIntro, FireActive, WaterActive, EarthActive, WindActive

---

## Ready for Session C

Session C (Minimap Legend + Boss Mechanics + World Map Polish) can proceed. All dependencies from Sessions A and B are in place:
- Room types and difficulty tiers (Session A) ✓
- Quest system and cornerstone card rewards (Session B) ✓
- Minimap already has room-type coloring (Session A) — Session C adds legend and polish
- Boss placement at max distance works (Session A) — Session C adds dual-element pools
- World map has card/quest count fields wired (Session B) — Session C adds visual polish

---

## SESSION_D.md — Tilemap Pipeline + Room Template System (NEW SPEC)

Quinn wants to replace flat-color room backgrounds with real Minifantasy tilemap art and build a level design pipeline in the Unity editor. Full spec is in `SESSION_D.md`.

### What It Does
1. **Sorting Layer Stack** — 8 layers: Ground, Detail, Shadow, PropsBelow, Player, PropsAbove, Overlay, Collision. Enables walk-behind-trees (trunk on PropsBelow, crown on PropsAbove, collision at base only).
2. **Tileset Importer** — Editor window that scans the Minifantasy folder, copies PNGs into Unity, auto-slices at 8x8, creates Tile Palettes per biome. Handles varying folder naming across packs.
3. **Room Template System** — Prefabs with 7 tilemap layers + spawn points + exit zones. Each template tagged with biome, allowed room types, and difficulty tier range. Procedural gen picks from pool at runtime.
4. **Room Template Editor** — Editor window for creating/browsing/validating templates. Creates prefab with full layer hierarchy, opens in Prefab Mode for painting. Quick actions: fill ground, add border walls, mirror.
5. **Runtime Integration** — `ExplorationManager` swaps `SpriteRenderer roomBackground` for tilemap-based room instantiation. Falls back to colored rooms when no template matches (keeps game playable during development).
6. **Starter Pack** — 4 hand-painted Ancient Forest templates (Empty, NPC, Treasure, Boss) to prove the pipeline.

### Biome Mapping
| Zone | Primary Minifantasy Pack | Already Imported? |
|------|--------------------------|-------------------|
| Ancient Forest | minifantasy-ancient-forests | Partially (green/dark/autumn forest tilesets) |
| Volcanic Wastes | minifantasy-hellscape | No |
| Sky Peaks | minifantasy-icy-wilderness | No |
| Coral Depths | minifantasy-aquatic-adventures | No |

### Key Architecture Decisions
- 8x8 pixel tiles at 32 PPU → 0.25 Unity units per tile → room is 40×32 tiles (10×8 units)
- Room templates are prefabs stored at `Assets/Prefabs/RoomTemplates/{BiomeName}/`
- `RoomTemplateData` ScriptableObject stores metadata (biome, types, tiers, exits, spawn points)
- Random walk room generation stays unchanged — templates are visual skins applied at runtime
- No Rule Tiles or animated tiles in v1 (keep it simple)

### Dependencies
- Sessions A and B complete ✓
- Does NOT depend on Session C (can run in parallel)
