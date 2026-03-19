namespace ArcaneAtlas.Data
{
    /// <summary>
    /// Pre-built PropStamp definitions for common environmental objects.
    /// Each stamp defines a multi-tile structure across the appropriate layers.
    /// The generator picks from these based on room type and biome.
    /// </summary>
    public static class PropStampLibrary
    {
        // ═══════════════════════════════════════
        //  TREES
        // ═══════════════════════════════════════

        /// <summary>
        /// Small tree: 3 wide × 4 tall.
        /// Trunk on PropsBelow, canopy on PropsAbove, shadow underneath.
        ///    [TL][TC][TR]  ← PropsAbove (canopy top)
        ///    [BL][BC][BR]  ← PropsAbove (canopy bottom)
        ///       [TM]       ← PropsBelow (trunk mid)
        ///       [TB]       ← PropsBelow (trunk base) + Collision
        ///    [shadow  ]    ← Shadow
        /// </summary>
        public static PropStamp SmallTree()
        {
            var s = new PropStamp(3, 4, 1, 0) { Name = "SmallTree" };
            // Trunk on PropsAbove so it renders in front of player
            s.PropsAbove[1, 0] = TileKey.TreeTrunkBottom;
            s.PropsAbove[1, 1] = TileKey.TreeTrunkMid;
            // Canopy (full width, top 2 rows)
            s.PropsAbove[0, 2] = TileKey.TreeCanopyBL;
            s.PropsAbove[1, 2] = TileKey.TreeCanopyBC;
            s.PropsAbove[2, 2] = TileKey.TreeCanopyBR;
            s.PropsAbove[0, 3] = TileKey.TreeCanopyTL;
            s.PropsAbove[1, 3] = TileKey.TreeCanopyTC;
            s.PropsAbove[2, 3] = TileKey.TreeCanopyTR;
            // Collision — full width at base so player can't squeeze alongside trunk
            s.Collision[0, 0] = TileKey.CollisionSolid;
            s.Collision[1, 0] = TileKey.CollisionSolid;
            s.Collision[2, 0] = TileKey.CollisionSolid;
            s.Collision[0, 1] = TileKey.CollisionSolid;
            s.Collision[1, 1] = TileKey.CollisionSolid;
            s.Collision[2, 1] = TileKey.CollisionSolid;
            // Shadow below trunk
            s.Shadow[0, 0] = TileKey.ShadowMedium;
            s.Shadow[1, 0] = TileKey.ShadowMedium;
            s.Shadow[2, 0] = TileKey.ShadowMedium;
            return s;
        }

        /// <summary>
        /// Large tree: 5 wide × 6 tall. Bigger canopy, thicker trunk.
        /// </summary>
        public static PropStamp LargeTree()
        {
            var s = new PropStamp(5, 6, 2, 0) { Name = "LargeTree" };
            // Trunk on PropsAbove so it renders in front of player
            s.PropsAbove[2, 0] = TileKey.TreeTrunkBottom;
            s.PropsAbove[2, 1] = TileKey.TreeTrunkMid;
            s.PropsAbove[2, 2] = TileKey.TreeTrunkMid;
            // Roots
            s.PropsAbove[1, 0] = TileKey.TreeRoots;
            s.PropsAbove[3, 0] = TileKey.TreeRoots;
            // Canopy (5 wide, top 3 rows)
            s.PropsAbove[0, 3] = TileKey.TreeCanopyBL;
            s.PropsAbove[1, 3] = TileKey.TreeCanopyBC;
            s.PropsAbove[2, 3] = TileKey.TreeCanopyBC;
            s.PropsAbove[3, 3] = TileKey.TreeCanopyBC;
            s.PropsAbove[4, 3] = TileKey.TreeCanopyBR;
            s.PropsAbove[0, 4] = TileKey.TreeCanopyTL;
            s.PropsAbove[1, 4] = TileKey.TreeCanopyTC;
            s.PropsAbove[2, 4] = TileKey.TreeCanopyTC;
            s.PropsAbove[3, 4] = TileKey.TreeCanopyTC;
            s.PropsAbove[4, 4] = TileKey.TreeCanopyTR;
            s.PropsAbove[1, 5] = TileKey.TreeCanopyTL;
            s.PropsAbove[2, 5] = TileKey.TreeCanopyTC;
            s.PropsAbove[3, 5] = TileKey.TreeCanopyTR;
            // Collision — wide footprint so player can't squeeze alongside trunk
            for (int cx = 0; cx < 5; cx++)
                s.Collision[cx, 0] = TileKey.CollisionSolid; // full base row
            s.Collision[1, 1] = TileKey.CollisionSolid;
            s.Collision[2, 1] = TileKey.CollisionSolid;
            s.Collision[3, 1] = TileKey.CollisionSolid;
            s.Collision[1, 2] = TileKey.CollisionSolid;
            s.Collision[2, 2] = TileKey.CollisionSolid;
            s.Collision[3, 2] = TileKey.CollisionSolid;
            // Shadow
            for (int x = 0; x < 5; x++)
                s.Shadow[x, 0] = TileKey.ShadowLarge;
            return s;
        }

        // ═══════════════════════════════════════
        //  ROCKS
        // ═══════════════════════════════════════

        /// <summary>
        /// Rock cluster: 2×2 formation of varied rocks.
        /// </summary>
        public static PropStamp RockCluster()
        {
            var s = new PropStamp(2, 2, 0, 0) { Name = "RockCluster" };
            s.PropsAbove[0, 0] = TileKey.RockClusterBL;
            s.PropsAbove[1, 0] = TileKey.RockClusterBR;
            s.PropsAbove[0, 1] = TileKey.RockClusterTL;
            s.PropsAbove[1, 1] = TileKey.RockClusterTR;
            // Collision on ALL 4 tiles
            s.Collision[0, 0] = TileKey.CollisionSolid;
            s.Collision[1, 0] = TileKey.CollisionSolid;
            s.Collision[0, 1] = TileKey.CollisionSolid;
            s.Collision[1, 1] = TileKey.CollisionSolid;
            s.Shadow[0, 0] = TileKey.ShadowSmall;
            s.Shadow[1, 0] = TileKey.ShadowSmall;
            return s;
        }

        /// <summary>
        /// Single small rock with shadow.
        /// </summary>
        public static PropStamp SingleRock()
        {
            var s = new PropStamp(1, 1, 0, 0) { Name = "SingleRock" };
            s.PropsAbove[0, 0] = TileKey.RockSmall;
            s.Collision[0, 0] = TileKey.CollisionSolid;
            return s;
        }

        // ═══════════════════════════════════════
        //  BUSHES
        // ═══════════════════════════════════════

        /// <summary>
        /// Bush group: 3 wide × 2 tall cluster.
        /// </summary>
        public static PropStamp BushGroup()
        {
            var s = new PropStamp(3, 2, 1, 0) { Name = "BushGroup" };
            s.PropsAbove[0, 0] = TileKey.BushSmall;
            s.PropsAbove[1, 0] = TileKey.BushWide;
            s.PropsAbove[2, 0] = TileKey.BushSmall;
            s.PropsAbove[0, 1] = TileKey.BushTop;
            s.PropsAbove[1, 1] = TileKey.BushTop;
            s.PropsAbove[2, 1] = TileKey.BushTop;
            // Collision on all bush tiles
            s.Collision[0, 0] = TileKey.CollisionSolid;
            s.Collision[1, 0] = TileKey.CollisionSolid;
            s.Collision[2, 0] = TileKey.CollisionSolid;
            s.Collision[0, 1] = TileKey.CollisionSolid;
            s.Collision[1, 1] = TileKey.CollisionSolid;
            s.Collision[2, 1] = TileKey.CollisionSolid;
            s.Shadow[0, 0] = TileKey.ShadowSmall;
            s.Shadow[2, 0] = TileKey.ShadowSmall;
            return s;
        }

        /// <summary>
        /// Single small bush.
        /// </summary>
        public static PropStamp SingleBush()
        {
            var s = new PropStamp(1, 1, 0, 0) { Name = "SingleBush" };
            s.PropsAbove[0, 0] = TileKey.BushSmall;
            s.Collision[0, 0] = TileKey.CollisionSolid;
            return s;
        }

        // ═══════════════════════════════════════
        //  BUILDINGS
        // ═══════════════════════════════════════

        /// <summary>
        /// Small cottage: 5 wide × 5 tall.
        /// Walls and door on PropsBelow, roof on PropsAbove.
        /// </summary>
        public static PropStamp SmallCottage()
        {
            var s = new PropStamp(5, 5, 2, 0) { Name = "SmallCottage" };
            // Floor (interior)
            for (int x = 1; x < 4; x++)
                for (int y = 0; y < 2; y++)
                    s.Ground[x, y] = TileKey.BuildFloor;
            // Walls bottom row
            s.PropsAbove[0, 0] = TileKey.BuildCornerBL;
            s.PropsAbove[4, 0] = TileKey.BuildCornerBR;
            s.PropsAbove[1, 0] = TileKey.BuildWallH;
            s.PropsAbove[2, 0] = TileKey.BuildDoor;
            s.PropsAbove[3, 0] = TileKey.BuildWallH;
            // Walls side
            s.PropsAbove[0, 1] = TileKey.BuildWallV;
            s.PropsAbove[4, 1] = TileKey.BuildWallV;
            // Walls top
            s.PropsAbove[0, 2] = TileKey.BuildCornerTL;
            s.PropsAbove[1, 2] = TileKey.BuildWallH;
            s.PropsAbove[2, 2] = TileKey.BuildWindow;
            s.PropsAbove[3, 2] = TileKey.BuildWallH;
            s.PropsAbove[4, 2] = TileKey.BuildCornerTR;
            // Roof (above player)
            s.PropsAbove[0, 3] = TileKey.RoofBL;
            s.PropsAbove[1, 3] = TileKey.RoofBC;
            s.PropsAbove[2, 3] = TileKey.RoofBC;
            s.PropsAbove[3, 3] = TileKey.RoofBC;
            s.PropsAbove[4, 3] = TileKey.RoofBR;
            s.PropsAbove[1, 4] = TileKey.RoofTL;
            s.PropsAbove[2, 4] = TileKey.RoofPeak;
            s.PropsAbove[3, 4] = TileKey.RoofTR;
            // Collision (walls block movement)
            for (int x = 0; x < 5; x++)
            {
                s.Collision[x, 0] = TileKey.CollisionSolid;
                s.Collision[x, 2] = TileKey.CollisionSolid;
            }
            s.Collision[0, 1] = TileKey.CollisionSolid;
            s.Collision[4, 1] = TileKey.CollisionSolid;
            // Door is passable
            s.Collision[2, 0] = TileKey.Empty;
            // Shadow
            for (int x = 0; x < 5; x++)
                s.Shadow[x, 0] = TileKey.ShadowLarge;
            return s;
        }

        // ═══════════════════════════════════════
        //  FENCES
        // ═══════════════════════════════════════

        /// <summary>
        /// Horizontal fence: 4 tiles wide with posts at ends.
        /// </summary>
        public static PropStamp FenceHorizontal()
        {
            var s = new PropStamp(4, 1, 0, 0) { Name = "FenceH" };
            s.PropsAbove[0, 0] = TileKey.FencePostBL;
            s.PropsAbove[1, 0] = TileKey.FenceH;
            s.PropsAbove[2, 0] = TileKey.FenceH;
            s.PropsAbove[3, 0] = TileKey.FencePostBR;
            for (int x = 0; x < 4; x++)
                s.Collision[x, 0] = TileKey.CollisionSolid;
            return s;
        }

        // ═══════════════════════════════════════
        //  MISC
        // ═══════════════════════════════════════

        /// <summary>
        /// Stump and log: 3 wide × 1 tall scenic detail.
        /// </summary>
        public static PropStamp StumpAndLog()
        {
            var s = new PropStamp(3, 1, 1, 0) { Name = "StumpAndLog" };
            s.PropsAbove[0, 0] = TileKey.StumpSmall;
            s.PropsAbove[1, 0] = TileKey.LogH;
            s.PropsAbove[2, 0] = TileKey.LogH;
            // All solid
            s.Collision[0, 0] = TileKey.CollisionSolid;
            s.Collision[1, 0] = TileKey.CollisionSolid;
            s.Collision[2, 0] = TileKey.CollisionSolid;
            return s;
        }

        /// <summary>
        /// Treasure chest on a slightly raised area.
        /// </summary>
        public static PropStamp TreasureChest()
        {
            var s = new PropStamp(1, 1, 0, 0) { Name = "TreasureChest" };
            s.PropsAbove[0, 0] = TileKey.Chest;
            s.Collision[0, 0] = TileKey.CollisionSolid;
            return s;
        }

        /// <summary>
        /// Crate stack: 2×2 with crates.
        /// </summary>
        public static PropStamp CrateStack()
        {
            var s = new PropStamp(2, 2, 0, 0) { Name = "CrateStack" };
            s.PropsAbove[0, 0] = TileKey.Crate;
            s.PropsAbove[1, 0] = TileKey.Crate;
            s.PropsAbove[0, 1] = TileKey.Crate;
            // All solid
            s.Collision[0, 0] = TileKey.CollisionSolid;
            s.Collision[1, 0] = TileKey.CollisionSolid;
            s.Collision[0, 1] = TileKey.CollisionSolid;
            return s;
        }

        /// <summary>
        /// Small pond: 3×3 water with edges.
        /// </summary>
        public static PropStamp SmallPond()
        {
            var s = new PropStamp(3, 3, 1, 1) { Name = "SmallPond" };
            // Edges
            s.Ground[1, 2] = TileKey.WaterEdgeN;
            s.Ground[1, 0] = TileKey.WaterEdgeS;
            s.Ground[0, 1] = TileKey.WaterEdgeW;
            s.Ground[2, 1] = TileKey.WaterEdgeE;
            // Center water
            s.Ground[1, 1] = TileKey.Water;
            // Corners as water edges
            s.Ground[0, 0] = TileKey.WaterEdge;
            s.Ground[2, 0] = TileKey.WaterEdge;
            s.Ground[0, 2] = TileKey.WaterEdge;
            s.Ground[2, 2] = TileKey.WaterEdge;
            // Collision
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    s.Collision[x, y] = TileKey.CollisionWater;
            return s;
        }
    }
}
