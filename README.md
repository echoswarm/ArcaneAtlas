# Arcane Atlas

An open-world card RPG with auto-battler combat built in Unity.

## Overview

Arcane Atlas is a single-player game where players explore themed zones, battle opponents using shop-based card drafting, earn booster packs, and curate their collection pool. The game features procedurally generated room-based exploration with tilemap rendering, animated character sprites, and a card combat system.

## Key Features

- **Exploration** — Procedurally generated room-based zones with tilemap rendering using Minifantasy pixel art
- **Card Combat** — Shop drafting into 5-slot board placement (3 front, 2 back) with auto-battle resolution
- **Card System** — 4 elements (Fire, Water, Earth, Wind), 4 rarities, triple-merge upgrades (Bronze → Silver → Gold)
- **Collection** — Win fights to earn booster packs, unlock cards, curate your active pool
- **Character System** — Animated player/NPC sprites with idle and walk animations from imported sprite sheets

## Tech Stack

- Unity 2022.3 LTS (2D, 1920×1080 landscape, PC target)
- C# with TextMeshPro and Unity Input System
- Single scene, panel-based UI architecture
- Procedural room generation with tilemap blueprint painting
- Custom editor tools for tileset importing, character sprite importing, room template creation, and tile palette mapping

## Editor Tools

- **Tileset Importer** — Scans and imports Minifantasy tileset packs with auto-slicing
- **Character Importer** — Imports character sprite sheets, auto-detects animation frames
- **Tileset Mapper** — Visual editor for mapping abstract tile types to specific sprites
- **Room Template Editor** — Creates tilemap room prefabs with proper sorting layers
- **Placeholder Palette Generator** — Creates colored placeholder tiles for development

## Art

Built with [Minifantasy](https://krishna-palacio.itch.io/) pixel art assets (8×8 base tiles, displayed at 32 PPU).

## License

All Rights Reserved © echoswarm 2026. See [LICENSE](LICENSE) for details.
