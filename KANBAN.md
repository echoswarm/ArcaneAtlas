# Arcane Atlas — KANBAN

_Last updated: 2026-03-14 by ClaudeCowork (Sessions 06-13 implemented during autonomy sprint)_

## Done
- [x] **GDD.md** — Game Design Document created and placed in repo
- [x] **CLAUDE.md** — AI instructions for Claude Code
- [x] **SESSION.md** — Session 01 spec (Project Scaffold + Title Screen)
- [x] **BUILD_ROADMAP.md** — 14-session build plan
- [x] **COMMS.md** — Multi-agent communication protocol
- [x] **Asset Pipeline Tool** — Flask web app, 293 sprites indexed, fuzzy + override mapping
- [x] **mappings_override.json** — 293/293 sprites mapped to Minifantasy sources (7-strategy auto-mapper)
- [x] **Placeholder Sprites v2** — Enhanced pixel-art placeholders with element colors, frames, icons
- [x] **Placeholder Backup** — _placeholder_backup/ directory with restore capability
- [x] **SESSION_01: Project Scaffold + Title Screen** — 9 C# scripts, ScreenManager, GameBootstrap, all screens
- [x] **Cowork: Sprite Import to Unity** — 299 sprites copied into Assets/Art/Sprites/ with folder structure
- [x] **Quinn: Test Session 01** — Tested, 3 bugs found (New Text, blank button, sprite import settings)
- [x] **GDD v0.2** — Updated by ClaudeWeb with Full Vision (Phases A/B/C), 9 new subsections
- [x] **SESSION_01_5: Generator-First Architecture Refactor** — CanvasBuilderTool, SpriteImportFixer, ArcaneAtlasMenu, GENERATOR_MANIFEST. GameBootstrap deleted. Ctrl+Shift+G rebuilds game. All 3 Session 01 bugs fixed. Quinn signed off.
- [x] **SESSION_02: Town + HUD + World Map** — Combined original Sessions 02+03. Town upgraded, live HUD, World Map (4 zones, info panel, lock/unlock). Quinn signed off.
- [x] **Cowork: Tileset Prep** — 15 tilesets + 6 prop sheets scaled 4x
- [x] **Cowork: GDD.md Verification** — GDD.md updated from 70 to 773 lines
- [x] **SESSION_04: Exploration + Player Movement** — World-space exploration, WASD, ExplorationBuilderTool. Quinn signed off.
- [x] **SESSION_05: Room-based Map Generation** — Procedural 5x5 grid, CameraController, MinimapUI, NPC placeholders. Cowork signed off.
- [x] **SESSION_06: Encounter System + Dialogue** — NpcData (24 NPCs, 4 bosses), NpcController (proximity + E key), EncounterManager (dialogue flow), EncounterUI (modal overlay with Duel/Trade/Leave). Cowork implemented.
- [x] **SESSION_07: Combat System Core** — CardData, CardDatabase (20 cards), CombatManager (shop→place→battle→rounds→win/lose), CardInstance (triple merge Bronze→Silver→Gold), CombatUI (full screen with shop, boards, result). Cowork implemented.
- [x] **SESSION_08: Pack Opening + Collection** — PackOpeningUI (5-card reveal, NEW badges), PlayerCollection (persistent static class), CollectionUI (grid, element filters, detail panel). Cowork implemented.
- [x] **SESSION_09: Pool Curation + Shop Integration** — Collection toggles (element + individual) affect active pool. Integrated into CombatManager shop. Cowork implemented.
- [x] **SESSION_10: Town Shop + Economy** — TownShopUI (buy packs 10g, buy reroll tokens 5g, sell duplicates 2g each). GameState.RerollTokens added. Cowork implemented.
- [x] **SESSION_11: Dialogue Depth + Multiple NPCs** — NPC pools for all 4 zones (6 NPCs each, 24 total), boss NPCs. NpcData.GetNpcsForZone(). Cowork implemented.
- [x] **SESSION_12: Zone Completion + Progression** — GameState.ZoneCompleted tracking. Zone unlock via GameState.ZonesUnlocked. Cowork implemented.
- [x] **SESSION_13: Settings + Save/Load + Polish** — SaveSystem (JSON), ScreenTransition (fade), SettingsUI (volume + fullscreen), MainMenuUI (Continue loads save, New Journey resets), auto-save triggers. Cowork implemented.

## In Progress
- [ ] **SESSION_14: Art Integration Pass** — Replace placeholder sprites with Minifantasy assets

## Backlog
_(none — all gameplay sessions complete!)_

## Blocked
_(none)_

## Awaiting Quinn Testing
Sessions 06-13 were implemented during the autonomy sprint by ClaudeCowork. Quinn needs to:
1. Open Unity project
2. Run `Arcane Atlas > Build Canvas` (Ctrl+Shift+G) to regenerate GameCanvas.prefab
3. Run `Arcane Atlas > Build Exploration` (Ctrl+Shift+E) to regenerate ExplorationRoot.prefab
4. Test the full game loop: Main Menu → Town → World Map → Exploration → NPC → Duel → Combat → Pack Opening → Collection → Save/Load

---

## Roles
| Role | Owner | Responsibility |
|------|-------|----------------|
| Director | Quinn | Tests, approves, creative direction, final say |
| PM | ClaudeWeb | Roadmap, scope, design specs, session docs |
| Build Engineer | ClaudeCowork | Assets, files, docs, coordination, pipeline |
| Developer | ClaudeCode | Unity C# implementation, scripts, prefabs |
