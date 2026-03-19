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
        PathEdgeN,          // Path border tiles
        PathEdgeS,
        PathEdgeE,
        PathEdgeW,

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
        MushroomDetail,     // Mushroom cluster
        PebbleDetail,       // Scattered pebbles

        // ── Shadow layer ──
        ShadowWallN,        // Shadow cast by north wall
        ShadowWallW,        // Shadow cast by west wall
        ShadowCornerNW,     // Shadow in corner
        ShadowFull,         // Full shadow (under props, dense areas)
        ShadowSmall,        // Small prop shadow (1 tile)
        ShadowMedium,       // Medium prop shadow (2 tiles wide)
        ShadowLarge,        // Large prop shadow (3 tiles wide)

        // ── Props (below player — walk-over or lower half of tall objects) ──
        TreeBase,           // Single-tile tree trunk (legacy)
        TreeTrunkBottom,    // Multi-tile tree: trunk base
        TreeTrunkMid,       // Multi-tile tree: trunk middle
        TreeRoots,          // Exposed roots around trunk base
        RockSmall,          // Small rock (1 tile)
        RockLarge,          // Large rock base (legacy)
        RockClusterBL,      // 2x2 rock cluster: bottom-left
        RockClusterBR,      // 2x2 rock cluster: bottom-right
        RockClusterTL,      // 2x2 rock cluster: top-left
        RockClusterTR,      // 2x2 rock cluster: top-right
        BushBase,           // Bush lower half (legacy)
        BushSmall,          // Small bush (1 tile, no walk-behind)
        BushWide,           // Wide bush base (2 tiles)
        Crate,              // Crate / barrel
        Chest,              // Treasure chest
        FenceH,             // Horizontal fence segment
        FenceV,             // Vertical fence segment
        FencePostTL,        // Fence corner post
        FencePostTR,
        FencePostBL,
        FencePostBR,
        StumpSmall,         // Tree stump
        LogH,               // Fallen log horizontal
        Water,              // Water tile
        WaterEdge,          // Water-to-ground transition
        WaterEdgeN,         // Directional water edges
        WaterEdgeS,
        WaterEdgeE,
        WaterEdgeW,

        // ── Props (above player — walk-behind upper halves) ──
        TreeCrown,          // Single-tile canopy (legacy)
        TreeCanopyBL,       // Multi-tile canopy: bottom-left
        TreeCanopyBR,       // Multi-tile canopy: bottom-right
        TreeCanopyBC,       // Multi-tile canopy: bottom-center
        TreeCanopyTL,       // Multi-tile canopy: top-left
        TreeCanopyTR,       // Multi-tile canopy: top-right
        TreeCanopyTC,       // Multi-tile canopy: top-center
        BushTop,            // Bush upper half
        RockTop,            // Large rock top

        // ── Building parts (below player) ──
        BuildWallH,         // Horizontal wall segment
        BuildWallV,         // Vertical wall segment
        BuildCornerTL,      // Building corner
        BuildCornerTR,
        BuildCornerBL,
        BuildCornerBR,
        BuildDoor,          // Building door/entrance
        BuildWindow,        // Building window
        BuildFloor,         // Interior floor

        // ── Building parts (above player — rooftops) ──
        RoofTL,             // Roof top-left
        RoofTR,             // Roof top-right
        RoofTC,             // Roof top-center
        RoofBL,             // Roof bottom-left
        RoofBR,             // Roof bottom-right
        RoofBC,             // Roof bottom-center
        RoofPeak,           // Roof peak/ridge

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
