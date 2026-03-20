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
                    LayoutEmptyRoom(bp, rng, room);
                    break;
                case RoomType.NPC:
                    LayoutNpcRoom(bp, rng, room);
                    break;
                case RoomType.Treasure:
                    LayoutTreasureRoom(bp, rng, room);
                    break;
                case RoomType.Boss:
                    LayoutBossRoom(bp, rng, room);
                    break;
                case RoomType.Event:
                    LayoutEventRoom(bp, rng, room);
                    break;
            }

            // Step 7: Clear props/collision from roads so they're walkable
            ClearRoads(bp);

            // Step 8: Set player spawn based on entries
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

        private static void LayoutEmptyRoom(RoomBlueprint bp, System.Random rng, RoomData room)
        {
            // 40% chance of road
            if (rng.NextDouble() < 0.4)
                ConnectDoors(bp, room);

            // Pick a variant for variety (weighted towards buildings)
            int variant = rng.Next(5); // 0-4, two building variants
            if (variant == 0)
            {
                // Forest clearing — lots of trees
                ScatterProps(bp, rng, 6);
            }
            else if (variant == 1)
            {
                // Abandoned cottage in the woods
                ConnectDoors(bp, room);
                TryPlaceStamp(bp, PropStampLibrary.SmallCottage(), rng, 8, 5);
                ScatterProps(bp, rng, 3);
            }
            else if (variant == 2)
            {
                // Pond area
                TryPlaceStamp(bp, PropStampLibrary.SmallPond(), rng);
                ScatterProps(bp, rng, 4);
            }
            else if (variant == 3)
            {
                // Ruins — scattered building parts and rocks
                ConnectDoors(bp, room);
                TryPlaceStamp(bp, PropStampLibrary.SmallHouse(), rng, 12, 5);
                TryPlaceStamp(bp, PropStampLibrary.FenceHorizontal(), rng, 12, 4);
                TryPlaceStamp(bp, PropStampLibrary.RockCluster(), rng);
                ScatterProps(bp, rng, 3);
            }
            else
            {
                // Outpost — tower + cottage
                ConnectDoors(bp, room);
                TryPlaceStamp(bp, PropStampLibrary.Tower(), rng, 12, 5);
                TryPlaceStamp(bp, PropStampLibrary.SmallCottage(), rng, 12, 5);
                TryPlaceStamp(bp, PropStampLibrary.Well(), rng, 12, 4);
                ScatterProps(bp, rng, 2);
            }
        }

        private static void LayoutNpcRoom(RoomBlueprint bp, System.Random rng, RoomData room)
        {
            int count = Mathf.Max(room.NpcCount, 1);
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Connect doors with roads
            ConnectDoors(bp, room);

            // Place NPC spawn markers in a spread pattern
            int spacing = (INNER_RIGHT - INNER_LEFT - 4) / Mathf.Max(count, 1);
            for (int i = 0; i < count; i++)
            {
                int x = INNER_LEFT + 3 + i * spacing;
                int y = cy + rng.Next(-3, 4);
                x = Mathf.Clamp(x, INNER_LEFT + 2, INNER_RIGHT - 3);
                y = Mathf.Clamp(y, INNER_BOTTOM + 2, INNER_TOP - 3);
                bp.Set(x, y, TileKey.MarkerNpcSpawn);
            }

            // Buildings along the road
            if (rng.NextDouble() < 0.6)
            {
                TryPlaceStamp(bp, PropStampLibrary.SmallHouse(), rng, 12, 5);
                if (rng.NextDouble() < 0.5)
                    TryPlaceStamp(bp, PropStampLibrary.MarketStall(), rng, 12, 4);
            }
            else
            {
                TryPlaceStamp(bp, PropStampLibrary.MarketStall(), rng, 12, 4);
            }

            // Scatter lighter props
            ScatterProps(bp, rng, 2);
        }

        private static void LayoutTreasureRoom(RoomBlueprint bp, System.Random rng, RoomData room)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Road from doors to treasure
            ConnectDoors(bp, room);

            // Center chest
            PropStampLibrary.TreasureChest().PlaceOn(bp, cx, cy);
            bp.Set(cx, cy, TileKey.MarkerTreasure);

            // Rock clusters framing the chest
            PropStampLibrary.RockCluster().PlaceOn(bp, cx - 4, cy - 1);
            PropStampLibrary.RockCluster().PlaceOn(bp, cx + 3, cy - 1);

            // Corner trees
            PropStampLibrary.LargeTree().PlaceOn(bp, INNER_LEFT + 4, INNER_TOP - 8);
            PropStampLibrary.LargeTree().PlaceOn(bp, INNER_RIGHT - 5, INNER_TOP - 8);
            PropStampLibrary.SmallTree().PlaceOn(bp, INNER_LEFT + 3, INNER_BOTTOM + 2);
            PropStampLibrary.SmallTree().PlaceOn(bp, INNER_RIGHT - 4, INNER_BOTTOM + 2);

            // Fence around the treasure area
            PropStampLibrary.FenceHorizontal().PlaceOn(bp, cx - 4, cy - 3);
            PropStampLibrary.FenceHorizontal().PlaceOn(bp, cx + 1, cy - 3);

            ScatterProps(bp, rng, 1);
        }

        private static void LayoutBossRoom(RoomBlueprint bp, System.Random rng, RoomData room)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Boss spawn in upper center
            bp.Set(cx, cy + 6, TileKey.MarkerBossSpawn);

            // Grand road from south door to boss
            ConnectDoors(bp, room);

            // Rock pillars framing the arena
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_LEFT + 5, INNER_TOP - 7);
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_RIGHT - 7, INNER_TOP - 7);
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_LEFT + 5, INNER_BOTTOM + 3);
            PropStampLibrary.RockCluster().PlaceOn(bp, INNER_RIGHT - 7, INNER_BOTTOM + 3);

            // Trees along the edges
            PropStampLibrary.LargeTree().PlaceOn(bp, INNER_LEFT + 3, INNER_TOP - 10);
            PropStampLibrary.LargeTree().PlaceOn(bp, INNER_RIGHT - 4, INNER_TOP - 10);

            // Towers flanking the boss area
            if (rng.NextDouble() < 0.5)
            {
                TryPlaceStamp(bp, PropStampLibrary.Tower(), rng, 5, 8);
                TryPlaceStamp(bp, PropStampLibrary.Tower(), rng, 5, 8);
            }
        }

        private static void LayoutEventRoom(RoomBlueprint bp, System.Random rng, RoomData room)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;

            // Roads connecting doors through a village center
            ConnectDoors(bp, room);

            // Village layout: buildings around a central area
            int variant = rng.Next(3);

            if (variant == 0)
            {
                // Small village: 2-3 houses around center
                TryPlaceStamp(bp, PropStampLibrary.SmallHouse(), rng, 8, 5);
                TryPlaceStamp(bp, PropStampLibrary.SmallHouse(), rng, 8, 5);
                TryPlaceStamp(bp, PropStampLibrary.MarketStall(), rng, 8, 4);
                TryPlaceStamp(bp, PropStampLibrary.Well(), rng, 8, 4);
            }
            else if (variant == 1)
            {
                // Outpost: tower + cottage + fences
                TryPlaceStamp(bp, PropStampLibrary.Tower(), rng, 8, 5);
                TryPlaceStamp(bp, PropStampLibrary.SmallCottage(), rng, 8, 5);
                TryPlaceStamp(bp, PropStampLibrary.FenceHorizontal(), rng, 8, 4);
                TryPlaceStamp(bp, PropStampLibrary.FenceVertical(), rng, 8, 4);
            }
            else
            {
                // Market area: stalls + crates
                TryPlaceStamp(bp, PropStampLibrary.MarketStall(), rng, 8, 4);
                TryPlaceStamp(bp, PropStampLibrary.MarketStall(), rng, 8, 4);
                TryPlaceStamp(bp, PropStampLibrary.CrateStack(), rng, 8, 4);
                TryPlaceStamp(bp, PropStampLibrary.CrateStack(), rng, 8, 4);
                TryPlaceStamp(bp, PropStampLibrary.Well(), rng, 8, 4);
            }

            bp.Set(cx, cy, TileKey.MarkerNpcSpawn);

            // Scatter natural props around the village
            ScatterProps(bp, rng, 2);
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

        // ─────────── Road Drawing ───────────

        /// <summary>
        /// Draws a straight 3-wide path segment (horizontal or vertical).
        /// </summary>
        private static void DrawRoadSegment(RoomBlueprint bp, int x1, int y1, int x2, int y2, int width = 3)
        {
            int half = width / 2;
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Fill the road width perpendicular to direction
                    bool isHorizontal = (maxX - minX) >= (maxY - minY);
                    if (isHorizontal)
                    {
                        for (int w = -half; w <= half; w++)
                            if (RoomBlueprint.InBounds(x, y + w))
                                bp.Ground[x, y + w] = TileKey.Path;
                    }
                    else
                    {
                        for (int w = -half; w <= half; w++)
                            if (RoomBlueprint.InBounds(x + w, y))
                                bp.Ground[x + w, y] = TileKey.Path;
                    }
                }
            }
        }

        /// <summary>
        /// Connects all doors to the room center with paths.
        /// Fills junction areas to avoid corner gaps.
        /// Then clears props/collision from road tiles.
        /// </summary>
        private static void ConnectDoors(RoomBlueprint bp, RoomData room)
        {
            int cx = RoomBlueprint.WIDTH / 2;
            int cy = RoomBlueprint.HEIGHT / 2;
            int half = 1; // road half-width

            // Draw each road segment from door to center
            if (room.ExitUp)
                DrawRoadSegment(bp, cx, cy, cx, RoomBlueprint.HEIGHT - WALL);
            if (room.ExitDown)
                DrawRoadSegment(bp, cx, WALL - 1, cx, cy);
            if (room.ExitLeft)
                DrawRoadSegment(bp, WALL - 1, cy, cx, cy);
            if (room.ExitRight)
                DrawRoadSegment(bp, cx, cy, RoomBlueprint.WIDTH - WALL, cy);

            // Fill the center junction (3x3 block of path)
            for (int dx = -half; dx <= half; dx++)
                for (int dy = -half; dy <= half; dy++)
                    if (RoomBlueprint.InBounds(cx + dx, cy + dy))
                        bp.Ground[cx + dx, cy + dy] = TileKey.Path;

            // Add path edges along roads (only where ground is not already path)
            for (int x = 0; x < RoomBlueprint.WIDTH; x++)
            {
                for (int y = 0; y < RoomBlueprint.HEIGHT; y++)
                {
                    if (bp.Ground[x, y] != TileKey.Path) continue;

                    // Check each neighbor — if not path, add edge
                    if (RoomBlueprint.InBounds(x, y + 1) && bp.Ground[x, y + 1] != TileKey.Path
                        && bp.Ground[x, y + 1] != TileKey.PathEdgeN && bp.Ground[x, y + 1] != TileKey.PathEdgeS)
                        bp.Ground[x, y + 1] = TileKey.PathEdgeN;
                    if (RoomBlueprint.InBounds(x, y - 1) && bp.Ground[x, y - 1] != TileKey.Path
                        && bp.Ground[x, y - 1] != TileKey.PathEdgeN && bp.Ground[x, y - 1] != TileKey.PathEdgeS)
                        bp.Ground[x, y - 1] = TileKey.PathEdgeS;
                    if (RoomBlueprint.InBounds(x + 1, y) && bp.Ground[x + 1, y] != TileKey.Path
                        && bp.Ground[x + 1, y] != TileKey.PathEdgeE && bp.Ground[x + 1, y] != TileKey.PathEdgeW)
                        bp.Ground[x + 1, y] = TileKey.PathEdgeE;
                    if (RoomBlueprint.InBounds(x - 1, y) && bp.Ground[x - 1, y] != TileKey.Path
                        && bp.Ground[x - 1, y] != TileKey.PathEdgeE && bp.Ground[x - 1, y] != TileKey.PathEdgeW)
                        bp.Ground[x - 1, y] = TileKey.PathEdgeW;
                }
            }
        }

        /// <summary>
        /// Clears props, details, and collision from any tile that has a path on the ground layer.
        /// Call AFTER all props have been placed.
        /// </summary>
        private static void ClearRoads(RoomBlueprint bp)
        {
            for (int x = 0; x < RoomBlueprint.WIDTH; x++)
            {
                for (int y = 0; y < RoomBlueprint.HEIGHT; y++)
                {
                    if (bp.Ground[x, y] == TileKey.Path ||
                        bp.Ground[x, y] == TileKey.PathEdgeN || bp.Ground[x, y] == TileKey.PathEdgeS ||
                        bp.Ground[x, y] == TileKey.PathEdgeE || bp.Ground[x, y] == TileKey.PathEdgeW)
                    {
                        bp.Detail[x, y] = TileKey.Empty;
                        bp.PropsBelow[x, y] = TileKey.Empty;
                        bp.PropsAbove[x, y] = TileKey.Empty;
                        bp.Collision[x, y] = TileKey.Empty;
                        bp.Shadow[x, y] = TileKey.Empty;
                    }
                }
            }
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
