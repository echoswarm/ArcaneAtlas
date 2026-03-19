# CLAUDE.md — Arcane Atlas

## Project Overview
Arcane Atlas is an open-world card RPG with auto-battler combat. Players explore themed zones, battle opponents using shop-based card drafting (not deck construction), earn booster packs, and curate their collection pool.

## Architecture
- **Single scene, panel-based UI.** The entire game runs on one Canvas with child GameObjects toggled via SetActive(true/false). ScreenManager.cs controls all transitions using a stack-based history.
- **No additive scene loading.** No scene transitions. Everything is panels.
- **Unity 2D, 1920x1080 landscape, PC target.**

## Key Game Mechanics
- **Combat:** Shop drafting → 5-slot board placement (3 front, 2 back) → auto-battle resolution
- **Cards:** 4 elements (Fire, Water, Earth, Wind), 4 rarities (Common, Uncommon, Rare, Legendary)
- **Triple merge:** 3 copies of same card → upgrade tier (Bronze → Silver → Gold)
- **Progression:** Win fights → earn booster packs → unlock cards into collection → curate active pool → cards appear in future shops
- **No deck building.** Players curate which cards are available in shop pools, not pre-built decks.

## Tech Stack
- Unity 2022.3 LTS (2D)
- C# scripts
- TextMeshPro for all text
- Unity Input System (new) for player movement
- No third-party packages unless explicitly approved

## Import Settings for All Art
- Filter Mode: Point (no filter)
- Compression: None
- Pixels Per Unit: 32
- Sprite Mode: Single (unless sprite sheet)
- Max Size: 2048 or as needed

## Asset Pipeline
A separate tool (Flask web app) manages sprite assets at:
`C:\Users\quinn\OneDrive\Desktop\ArcaneAtlas\Sprites`

All 293 placeholder sprites already exist with correct names and dimensions. Creature sprites are at:
`cards/creatures/{element}/creature_{element}_XX.png` (120x120 each, 25 per element)

Card frames: `cards/frames/card_frame_{rarity}.png` (160x210)
UI panels: `ui/panels/panel_{type}.png` (96x96, 9-slice)
Buttons: `ui/buttons/btn_{type}.png` (various sizes)
Icons: `icons/elements/icon_{element}.png` (32x32)

Art source: Minifantasy Complete Bundle (8x8 pixel art, displayed at 4x scale = 32px per tile).

## Session Workflow
1. Quinn (developer) creates SESSION.md with task specs
2. Claude Code implements from SESSION.md
3. Quinn tests in Unity Editor, confirms or reports issues
4. Issues come back to Claude Web for analysis, then new SESSION.md

## Build Order (from BUILD_ROADMAP.md)
Session 01: Project scaffold + title screen
Session 02: Town screen + HUD
Session 03: World map + zone select
Session 04: Exploration + player movement
Session 05: Room-based map generation
Session 06: Encounter system + dialogue
Session 07: Combat system (core) ★
Session 08: Pack opening + collection
Session 09: Pool curation + shop integration
Session 10: Town shop + economy
Session 11: Dialogue depth + multiple NPCs
Session 12: Zone completion + progression
Session 13: Settings + save/load + polish
Session 14: Art integration pass

## Code Style
- Namespace: `ArcaneAtlas`
- Sub-namespaces: `ArcaneAtlas.Core`, `ArcaneAtlas.Combat`, `ArcaneAtlas.UI`, `ArcaneAtlas.Data`
- One class per file
- ScriptableObjects for all static data (cards, NPCs, zones)
- No singletons except ScreenManager (accessed via static instance)
- Comments only where logic is non-obvious

## DO NOT
- Do not create multiple scenes
- Do not use third-party UI packages
- Do not add multiplayer or networking
- Do not add IAP or monetization
- Do not add analytics or telemetry
- Do not over-engineer — simple solutions preferred
- Do not add features not in the current SESSION.md
