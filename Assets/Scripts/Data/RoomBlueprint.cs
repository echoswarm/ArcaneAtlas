using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    /// <summary>
    /// A room layout expressed as abstract TileKeys on a 2D grid.
    /// Independent of any specific art — pair with a TilePaletteDef to render.
    /// Room size is 40x32 tiles (matching 10x8 units at 0.25 unit grid cells).
    /// </summary>
    public class RoomBlueprint
    {
        public const int WIDTH = 68;   // tiles across (17 units / 0.25)
        public const int HEIGHT = 40;  // tiles tall (10 units / 0.25)

        // One grid per tilemap layer. Each cell holds a TileKey (Empty = nothing).
        public TileKey[,] Ground;       // Floor, walls, doors, water
        public TileKey[,] Detail;       // Decorative ground overlays
        public TileKey[,] Shadow;       // Shadow tiles
        public TileKey[,] PropsBelow;   // Props below player (tree base, rocks, crates)
        public TileKey[,] PropsAbove;   // Props above player (tree crown, walk-behind)
        public TileKey[,] Overlay;      // Top-most visual (vines, fog)
        public TileKey[,] Collision;    // Invisible collision

        // Marker positions extracted during generation (not painted as tiles)
        public Vector2Int PlayerSpawn;
        public List<Vector2Int> NpcSpawns = new List<Vector2Int>();
        public List<Vector2Int> TreasureSpawns = new List<Vector2Int>();
        public Vector2Int BossSpawn;

        public RoomBlueprint()
        {
            Ground = new TileKey[WIDTH, HEIGHT];
            Detail = new TileKey[WIDTH, HEIGHT];
            Shadow = new TileKey[WIDTH, HEIGHT];
            PropsBelow = new TileKey[WIDTH, HEIGHT];
            PropsAbove = new TileKey[WIDTH, HEIGHT];
            Overlay = new TileKey[WIDTH, HEIGHT];
            Collision = new TileKey[WIDTH, HEIGHT];

            PlayerSpawn = new Vector2Int(WIDTH / 2, HEIGHT / 2);
        }

        /// <summary>
        /// Returns the layer grid matching the given TileLayer enum.
        /// </summary>
        public TileKey[,] GetLayer(TileLayer layer)
        {
            switch (layer)
            {
                case TileLayer.Ground: return Ground;
                case TileLayer.Detail: return Detail;
                case TileLayer.Shadow: return Shadow;
                case TileLayer.PropsBelow: return PropsBelow;
                case TileLayer.PropsAbove: return PropsAbove;
                case TileLayer.Overlay: return Overlay;
                case TileLayer.Collision: return Collision;
                default: return null;
            }
        }

        /// <summary>
        /// Sets a tile on the correct layer automatically based on TilePaletteDef.GetLayer().
        /// Convenience method so the generator doesn't have to think about layers.
        /// </summary>
        public void Set(int x, int y, TileKey key)
        {
            if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT) return;
            if (key == TileKey.Empty) return;

            // Handle markers — store position, don't paint
            switch (key)
            {
                case TileKey.MarkerPlayerSpawn:
                    PlayerSpawn = new Vector2Int(x, y);
                    return;
                case TileKey.MarkerNpcSpawn:
                    NpcSpawns.Add(new Vector2Int(x, y));
                    return;
                case TileKey.MarkerBossSpawn:
                    BossSpawn = new Vector2Int(x, y);
                    return;
                case TileKey.MarkerTreasure:
                    TreasureSpawns.Add(new Vector2Int(x, y));
                    return;
            }

            TileLayer layer = TilePaletteDef.GetLayer(key);
            TileKey[,] grid = GetLayer(layer);
            if (grid != null)
                grid[x, y] = key;
        }

        /// <summary>
        /// Places a tree: base on PropsBelow, crown on PropsAbove (one tile up),
        /// collision at the base position.
        /// </summary>
        public void PlaceTree(int x, int y)
        {
            Set(x, y, TileKey.TreeBase);
            Set(x, y, TileKey.CollisionSolid);
            if (y + 1 < HEIGHT)
                Set(x, y + 1, TileKey.TreeCrown);
        }

        /// <summary>
        /// Places a large rock: base on PropsBelow, top one tile up,
        /// collision at base.
        /// </summary>
        public void PlaceRock(int x, int y)
        {
            Set(x, y, TileKey.RockLarge);
            Set(x, y, TileKey.CollisionSolid);
            if (y + 1 < HEIGHT)
                Set(x, y + 1, TileKey.RockTop);
        }

        /// <summary>
        /// Places a bush: base + top, collision at base.
        /// </summary>
        public void PlaceBush(int x, int y)
        {
            Set(x, y, TileKey.BushBase);
            Set(x, y, TileKey.CollisionSolid);
            if (y + 1 < HEIGHT)
                Set(x, y + 1, TileKey.BushTop);
        }

        /// <summary>
        /// Fills a rectangular area on the correct layer with the given key.
        /// </summary>
        public void FillRect(int x, int y, int w, int h, TileKey key)
        {
            for (int tx = x; tx < x + w && tx < WIDTH; tx++)
                for (int ty = y; ty < y + h && ty < HEIGHT; ty++)
                    Set(tx, ty, key);
        }

        /// <summary>
        /// Checks if a tile position is within bounds.
        /// </summary>
        public static bool InBounds(int x, int y)
        {
            return x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT;
        }
    }
}
