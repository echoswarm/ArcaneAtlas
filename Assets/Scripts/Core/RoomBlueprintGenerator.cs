using ArcaneAtlas.Data;
using UnityEngine;

namespace ArcaneAtlas.Core
{
    /// <summary>
    /// Generates RoomBlueprints from RoomData. Takes abstract room properties
    /// (type, exits, difficulty) and produces a tile layout.
    /// No art knowledge — just TileKeys on a grid.
    /// </summary>
    public static class RoomBlueprintGenerator
    {
        // Wall thickness in tiles
        private const int WALL = 2;
        // Door opening width in tiles
        private const int DOOR_WIDTH = 6;
        // Interior bounds (inside walls)
        private const int INNER_LEFT = WALL;
        private const int INNER_RIGHT = RoomBlueprint.WIDTH - WALL;
        private const int INNER_BOTTOM = WALL;
        private const int INNER_TOP = RoomBlueprint.HEIGHT - WALL;

        /// <summary>
        /// Main entry point. Generates a complete blueprint from room data.
        /// Uses room.RoomVariant as a seed for deterministic randomness.
        /// </summary>
        public static RoomBlueprint Generate(RoomData room)
        {
            var bp = new RoomBlueprint();
            var rng = new System.Random(room.GridX * 1000 + room.GridY * 37 + room.RoomVariant);

            // Step 1: Fill entire floor with ground
            FillGround(bp, rng);

            // Step 2: Build walls around edges
            BuildWalls(bp);

            // Step 3: Carve door openings for exits
            CarveDoors(bp, room);

            // Step 4: Add shadows along walls
            AddShadows(bp);

            // Step 5: Scatter ground details
            ScatterDetails(bp, rng, room.DifficultyTier);

            // Step 6: Place props based on room type
            switch (room.Type)
            {
                case RoomType.Empty:
                    LayoutEmptyRoom(bp, rng);
                    break;
                case RoomType.NPC:
                    LayoutNpcRoom(bp, rng, room.NpcCount);
                    break;
                case RoomType.Treasure:
                    LayoutTreasureRoom(bp, rng);
                    break;
                case RoomType.Boss:
                    LayoutBossRoom(bp, rng);
                    break;
                case RoomType.Event:
                    LayoutEventRoom(bp, rng);
                    break;
            }

            // Step 7: Set player spawn based on entries
            SetPlayerSpawn(bp, room);

            return bp;
        }

        // ─────────── Ground ───────────

        private static void FillGround(RoomBlueprint bp, System.Random rng)
        {
            for (int x = 0; x < RoomBlueprint.WIDTH; x++)
            {
                for (int y = 0; y < RoomBlueprint.HEIGHT; y++)
                {
                    // ~15% chance of ground variation
                    bp.Ground[x, y] = rng.NextDouble() < 0.15 ? TileKey.GroundAlt : TileKey.Ground;
                }
            }
        }

        // ─────────── Walls ───────────

        private static void BuildWalls(RoomBlueprint bp)
        {
            int w = RoomBlueprint.WIDTH;
            int h = RoomBlueprint.HEIGHT;

            // Corners first (2x2 blocks)
            SetBlock(bp.Ground, 0, h - WALL, WALL, WALL, TileKey.WallCornerNW);
            SetBlock(bp.Ground, w - WALL, h - WALL, WALL, WALL, TileKey.WallCornerNE);
            SetBlock(bp.Ground, 0, 0, WALL, WALL, TileKey.WallCornerSW);
            SetBlock(bp.Ground, w - WALL, 0, WALL, WALL, TileKey.WallCornerSE);

            // North wall (top edge, between corners)
            for (int x = WALL; x < w - WALL; x++)
                for (int y = h - WALL; y < h; y++)
                {
                    bp.Ground[x, y] = TileKey.WallN;
                    bp.Collision[x, y] = TileKey.CollisionSolid;
                }

            // South wall (bottom edge, between corners)
            for (int x = WALL; x < w - WALL; x++)
                for (int y = 0; y < WALL; y++)
                {
                    bp.Ground[x, y] = TileKey.WallS;
                    bp.Collision[x, y] = TileKey.CollisionSolid;
                }

            // West wall (left edge, between corners)
            for (int x = 0; x < WALL; x++)
                for (int y = WALL; y < h - WALL; y++)
                {
                    bp.Ground[x, y] = TileKey.WallW;
                    bp.Collision[x, y] = TileKey.CollisionSolid;
                }

            // East wall (right edge, between corners)
            for (int x = w - WALL; x < w; x++)
                for (int y = WALL; y < h - WALL; y++)
                {
                    bp.Ground[x, y] = TileKey.WallE;
                    bp.Collision[x, y] = TileKey.CollisionSolid;
                }

            // Corner collision
            for (int x = 0; x < WALL; x++)
                for (int y = 0; y < WALL; y++)
                    bp.Collision[x, y] = TileKey.CollisionSolid;
            for (int x = w - WALL; x < w; x++)
                for (int y = 0; y < WALL; y++)
                    bp.Collision[x, y] = TileKey.CollisionSolid;
            for (int x = 0; x < WALL; x++)
                for (int y = h - WALL; y < h; y++)
                    bp.Collision[x, y] = TileKey.CollisionSolid;
            for (int x = w - WALL; x < w; x++)
                for (int y = h - WALL; y < h; y++)
                    bp.Collision[x, y] = TileKey.CollisionSolid;
        }

        // ─────────── Doors ───────────

        private static void CarveDoors(RoomBlueprint bp, RoomData room)
        {
            int w = RoomBlueprint.WIDTH;
            int h = RoomBlueprint.HEIGHT;
            int cx = w / 2;
            int cy = h / 2;
            int half = DOOR_WIDTH / 2;

            if (room.ExitUp)
            {
                for (int x = cx - half; x < cx + half; x++)
                    for (int y = h - WALL; y < h; y++)
                    {
                        bp.Ground[x, y] = TileKey.DoorN;
                        bp.Collision[x, y] = TileKey.Empty;
                    }
            }

            if (room.ExitDown)
            {
                for (int x = cx - half; x < cx + half; x++)
                    for (int y = 0; y < WALL; y++)
                    {
                        bp.Ground[x, y] = TileKey.DoorS;
                        bp.Collision[x, y] = TileKey.Empty;
                    }
            }

            if (room.ExitLeft)
            {
                for (int x = 0; x < WALL; x++)
                    for (int y = cy - half; y < cy + half; y++)
                    {
                        bp.Ground[x, y] = TileKey.DoorW;
                        bp.Collision[x, y] = TileKey.Empty;
                    }
            }

            if (room.ExitRight)
            {
                for (int x = w - WALL; x < w; x++)
                    for (int y = cy - half; y < cy + half; y++)
                    {
                        bp.Ground[x, y] = TileKey.DoorE;
                        bp.Collision[x, y] = TileKey.Empty;
                    }
            }
        }

        // ─────────── Shadows ───────────

        private static void AddShadows(RoomBlueprint bp)
        {
            // Shadow strip along inner edge of north wall
            for (int x = WALL; x < RoomBlueprint.WIDTH - WALL; x++)
            {
                int y = RoomBlueprint.HEIGHT - WALL - 1;
                if (bp.Ground[x, y] != TileKey.DoorN)
                    bp.Shadow[x, y] = TileKey.ShadowWallN;
            }

            // Shadow strip along inner edge of west wall
            for (int y = WALL; y < RoomBlueprint.HEIGHT - WALL; y++)
            {
                int x = WALL;
                if (bp.Ground[x, y] != TileKey.DoorW)
                    bp.Shadow[x, y] = TileKey.ShadowWallW;
            }

            // Corner shadow
            bp.Shadow[WALL, RoomBlueprint.HEIGHT - WALL - 1] = TileKey.ShadowCornerNW;
        }

        // ─────────── Details ───────────

        private static void ScatterDetails(RoomBlueprint bp, System.Random rng, int difficulty)
        {
            for (int x = INNER_LEFT + 1; x < INNER_RIGHT - 1; x++)
            {
                for (int y = INNER_BOTTOM + 1; y < INNER_TOP - 1; y++)
                {
                    if (bp.Ground[x, y] != TileKey.Ground && bp.Ground[x, y] != TileKey.GroundAlt)
                        continue;

                    double roll = rng.NextDouble();
                    if (roll < 0.05)
                        bp.Detail[x, y] = TileKey.GrassDetail;
                    else if (roll < 0.08)
                        bp.Detail[x, y] = TileKey.FlowerDetail;
                    else if (roll < 0.09)
                        bp.Detail[x, y] = TileKey.MushroomDetail;
                    else if (roll < 0.10)
                        bp.Detail[x, y] = TileKey.PebbleDetail;
                    else if (roll < 0.12 && difficulty >= 3)
                        bp.Detail[x, y] = TileKey.CrackDetail;
                }
            }
        }

        // ─────────── Stamp Helpers ───────────

        /// <summary>
        /// Tries to place a stamp at a random position within the inner room area.
        /// Retries up to maxAttempts times to find a non-overlapping spot.
        /// </summary>
        private static bool TryPlaceStamp(RoomBlueprint bp, PropStamp stamp, System.Random rng,
            int maxAttempts = 10, int margin = 3)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = rng.Next(INNER_LEFT + margin, INNER_RIGHT - margin);
                int y = rng.Next(INNER_BOTTOM + margin, INNER_TOP - margin - stamp.Height);

                if (stamp.FitsAt(bp, x, y))
                {
                    stamp.PlaceOn(bp, x, y);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Scatters a mix of prop stamps throughout the room for visual interest.
        /// </summary>
        private static void ScatterProps(RoomBlueprint bp, System.Random rng, int density)
        {
            // Large trees (1-3 based on density)
            int largeTreeCount = Mathf.Clamp(density / 2, 0, 3);
            for (int i = 0; i < largeTreeCount; i++)
                TryPlaceStamp(bp, PropStampLibrary.LargeTree(), rng);

            // Small trees
            int smallTreeCount = Mathf.Clamp(density, 1, 5);
            for (int i = 0; i < smallTreeCount; i++)
                TryPlaceStamp(bp, PropStampLibrary.SmallTree(), rng);

            // Rock clusters
            int rockCount = rng.Next(1, 3);
            for (int i = 0; i < rockCount; i++)
            {
                if (rng.NextDouble() < 0.5)
                    TryPlaceStamp(bp, PropStampLibrary.RockCluster(), rng);
                else
                    TryPlaceStamp(bp, PropStampLibrary.SingleRock(), rng);
            }

            // Bushes
            int bushCount = rng.Next(1, 3);
            for (int i = 0; i < bushCount; i++)
            {
                if (rng.NextDouble() < 0.4)
                    TryPlaceStamp(bp, PropStampLibrary.BushGroup(), rng);
                else
                    TryPlaceStamp(bp, PropStampLibrary.SingleBush(), rng);
            }

            // Occasional scenic elements
            if (rng.NextDouble() < 0.3)
                TryPlaceStamp(bp, PropStampLibrary.StumpAndLog(), rng);
        }

        // ─────────── Room Type Layouts ───────────

        private static void LayoutEmptyRoom(RoomBlueprint bp, System.Random rng)
        {
            // Scatter a moderate amount of natural props
            ScatterProps(bp, rng, 4);

            // Occasional pond
            if (rng.NextDouble() < 0.2)
                TryPlaceStamp(bp, PropStampLibrary.SmallPond(), rng);
        }

        private static void LayoutNpcRoom(RoomBlueprint bp, System.Random rng, int npcCount)
        {
            int count = Mathf.Max(npcCount, 1);

            // Place NPC spawn markers in a spread pattern
            int spacing = (INNER_RIGHT - INNER_LEFT - 4) / Mathf.Max(count, 1);
            for (int i = 0; i < count; i++)
            {
                int x = INNER_LEFT + 3 + i * spacing;
                int y = RoomBlueprint.HEIGHT / 2 + rng.Next(-3, 4);
                x = Mathf.Clamp(x, INNER_LEFT + 2, INNER_RIGHT - 3);
                y = Mathf.Clamp(y, INNER_BOTTOM + 2, INNER_TOP - 3);
                bp.Set(x, y, TileKey.MarkerNpcSpawn);
            }

            // Scatter lighter props (fewer than empty rooms to leave space for NPCs)
            ScatterProps(bp, rng, 2);

            // Some decorative props around edges
            for (int i = 0; i < 3; i++)
            {
                int x = rng.Next(INNER_LEFT + 1, INNER_RIGHT - 1);
                int y = rng.Next(INNER_BOTTOM + 1, INNER_BOTTOM + 4);
                if (bp.PropsBelow[x, y] == TileKey.Empty)
                    bp.Set(x, y, TileKey.RockSmall);
            }
        }

        private static void LayoutTreasureRoom(RoomBlueprint bp, System.Random rng)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Center chest
            PropStampLibrary.TreasureChest().PlaceOn(bp, cx, cy);
            bp.Set(cx, cy, TileKey.MarkerTreasure);

            // Rock clusters framing the chest
            PropStampLibrary.RockCluster().PlaceOn(bp, cx - 4, cy - 1);
            PropStampLibrary.RockCluster().PlaceOn(bp, cx + 3, cy - 1);

            // Corner trees — large ones for dramatic framing
            var largeTree = PropStampLibrary.LargeTree();
            largeTree.PlaceOn(bp, INNER_LEFT + 4, INNER_TOP - 8);
            largeTree.PlaceOn(bp, INNER_RIGHT - 5, INNER_TOP - 8);

            var smallTree = PropStampLibrary.SmallTree();
            smallTree.PlaceOn(bp, INNER_LEFT + 3, INNER_BOTTOM + 2);
            smallTree.PlaceOn(bp, INNER_RIGHT - 4, INNER_BOTTOM + 2);

            // Fence around the treasure area
            PropStampLibrary.FenceHorizontal().PlaceOn(bp, cx - 4, cy - 3);
            PropStampLibrary.FenceHorizontal().PlaceOn(bp, cx + 1, cy - 3);

            // Scatter a few bushes
            ScatterProps(bp, rng, 1);
        }

        private static void LayoutBossRoom(RoomBlueprint bp, System.Random rng)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Boss spawn in upper center
            bp.Set(cx, cy + 6, TileKey.MarkerBossSpawn);

            // Rock pillars framing the arena
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_LEFT + 5, INNER_TOP - 7);
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_RIGHT - 7, INNER_TOP - 7);
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_LEFT + 5, INNER_BOTTOM + 3);
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_RIGHT - 7, INNER_BOTTOM + 3);

            // Trees along the edges for atmosphere
            PropStampLibrary.LargeTree().PlaceOn(bp, INNER_LEFT + 3, INNER_TOP - 10);
            PropStampLibrary.LargeTree().PlaceOn(bp, INNER_RIGHT - 4, INNER_TOP - 10);

            // Path leading to boss
            for (int y = INNER_BOTTOM + 1; y < cy + 5; y++)
            {
                bp.Ground[cx - 1, y] = TileKey.Path;
                bp.Ground[cx, y] = TileKey.Path;
                bp.Ground[cx + 1, y] = TileKey.Path;
            }
            // Path edges
            for (int y = INNER_BOTTOM + 1; y < cy + 5; y++)
            {
                bp.Ground[cx - 2, y] = TileKey.PathEdgeW;
                bp.Ground[cx + 2, y] = TileKey.PathEdgeE;
            }
        }

        private static void LayoutEventRoom(RoomBlueprint bp, System.Random rng)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Trees framing a clearing
            PropStampLibrary.SmallTree().PlaceOn(bp, cx - 5, cy + 1);
            PropStampLibrary.SmallTree().PlaceOn(bp, cx + 5, cy + 1);

            // Bush groups around edges
            PropStampLibrary.BushGroup().PlaceOn(bp, cx - 3, cy - 2);
            PropStampLibrary.BushGroup().PlaceOn(bp, cx + 2, cy - 2);

            bp.Set(cx, cy, TileKey.MarkerNpcSpawn);

            // Crate stacks nearby
            PropStampLibrary.CrateStack().PlaceOn(bp, cx - 2, cy + 2);
            PropStampLibrary.CrateStack().PlaceOn(bp, cx + 2, cy + 2);

            // A few scattered natural elements
            ScatterProps(bp, rng, 1);
        }

        // ─────────── Player Spawn ───────────

        private static void SetPlayerSpawn(RoomBlueprint bp, RoomData room)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Default to center — will be overridden by entry direction at runtime
            // These are "suggested" spawns near each exit for directional entry
            if (room.ExitDown)
                bp.PlayerSpawn = new Vector2Int(cx, WALL + 2);
            else if (room.ExitLeft)
                bp.PlayerSpawn = new Vector2Int(WALL + 2, cy);
            else if (room.ExitRight)
                bp.PlayerSpawn = new Vector2Int(RoomBlueprint.WIDTH - WALL - 3, cy);
            else if (room.ExitUp)
                bp.PlayerSpawn = new Vector2Int(cx, RoomBlueprint.HEIGHT - WALL - 3);
            else
                bp.PlayerSpawn = new Vector2Int(cx, cy);
        }

        // ─────────── Helpers ───────────

        private static void SetBlock(TileKey[,] grid, int x, int y, int w, int h, TileKey key)
        {
            for (int tx = x; tx < x + w && tx < RoomBlueprint.WIDTH; tx++)
                for (int ty = y; ty < y + h && ty < RoomBlueprint.HEIGHT; ty++)
                    grid[tx, ty] = key;
        }
    }
}
