namespace ArcaneAtlas.Data
{
    /// <summary>
    /// Abstract tile types that describe PURPOSE, not appearance.
    /// Each TileKey maps to a specific sprite via TilePaletteDef.
    /// This is the abstraction layer between room layout logic and visual art.
    /// </summary>
    public enum TileKey : byte
    {
        Empty = 0,

        // ── Ground layer ──
        Ground,             // Basic floor tile
        GroundAlt,          // Floor variation (dirt patch, different stone)
        Path,               // Walkable path / road

        // ── Walls (outer edges of the room) ──
        WallN,              // North-facing wall segment
        WallS,              // South-facing wall segment
        WallE,              // East-facing wall segment
        WallW,              // West-facing wall segment

        // ── Wall corners (outer corners where walls meet) ──
        WallCornerNW,       // Top-left outer corner
        WallCornerNE,       // Top-right outer corner
        WallCornerSW,       // Bottom-left outer corner
        WallCornerSE,       // Bottom-right outer corner

        // ── Wall inner corners (concave turns) ──
        WallInnerNW,        // Top-left inner corner
        WallInnerNE,        // Top-right inner corner
        WallInnerSW,        // Bottom-left inner corner
        WallInnerSE,        // Bottom-right inner corner

        // ── Exits / doorways ──
        DoorN,              // North exit opening
        DoorS,              // South exit opening
        DoorE,              // East exit opening
        DoorW,              // West exit opening

        // ── Detail layer (decorative ground overlays) ──
        GrassDetail,        // Grass tuft on ground
        FlowerDetail,       // Small flowers on ground
        CrackDetail,        // Floor cracks
        MossDetail,         // Moss patches

        // ── Shadow layer ──
        ShadowWallN,        // Shadow cast by north wall
        ShadowWallW,        // Shadow cast by west wall
        ShadowCornerNW,     // Shadow in corner
        ShadowFull,         // Full shadow (under props, dense areas)

        // ── Props (below player — walk-over or lower half of tall objects) ──
        TreeBase,           // Lower trunk of tree (player walks in front)
        RockSmall,          // Small rock
        RockLarge,          // Large rock base
        BushBase,           // Bush lower half
        Crate,              // Crate / barrel
        Chest,              // Treasure chest
        Water,              // Water tile
        WaterEdge,          // Water-to-ground transition

        // ── Props (above player — walk-behind upper halves) ──
        TreeCrown,          // Upper canopy (player walks behind)
        BushTop,            // Bush upper half
        RockTop,            // Large rock top

        // ── Overlay (top-most visual layer) ──
        OverlayVines,       // Hanging vines from top
        OverlayFog,         // Fog/mist effect

        // ── Collision (invisible, blocks movement) ──
        CollisionSolid,     // Impassable tile
        CollisionWater,     // Water collision (could allow swim later)

        // ── Markers (used by generator, not painted visually) ──
        MarkerPlayerSpawn,  // Where player enters
        MarkerNpcSpawn,     // NPC placement point
        MarkerBossSpawn,    // Boss placement point
        MarkerTreasure,     // Treasure spawn point
    }
}
