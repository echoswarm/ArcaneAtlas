using UnityEngine;

namespace ArcaneAtlas.Data
{
    /// <summary>
    /// A multi-tile template for placing props across multiple layers.
    /// Each stamp is a small 2D grid of TileKeys with a defined size and anchor point.
    /// The generator places stamps at positions — the painter resolves keys to sprites.
    /// </summary>
    public class PropStamp
    {
        public string Name;
        public int Width;   // tiles wide
        public int Height;  // tiles tall
        public int AnchorX; // which tile is the "foot" / placement point (X)
        public int AnchorY; // which tile is the "foot" / placement point (Y)

        // Tile data per layer — [x, y] where (0,0) is bottom-left of the stamp
        public TileKey[,] Ground;
        public TileKey[,] Detail;
        public TileKey[,] Shadow;
        public TileKey[,] PropsBelow;
        public TileKey[,] PropsAbove;
        public TileKey[,] Collision;

        public PropStamp(int width, int height, int anchorX = -1, int anchorY = 0)
        {
            Width = width;
            Height = height;
            AnchorX = anchorX >= 0 ? anchorX : width / 2;
            AnchorY = anchorY;

            Ground = new TileKey[width, height];
            Detail = new TileKey[width, height];
            Shadow = new TileKey[width, height];
            PropsBelow = new TileKey[width, height];
            PropsAbove = new TileKey[width, height];
            Collision = new TileKey[width, height];
        }

        /// <summary>
        /// Places this stamp onto a RoomBlueprint at the given anchor position.
        /// </summary>
        public void PlaceOn(RoomBlueprint bp, int worldX, int worldY)
        {
            int startX = worldX - AnchorX;
            int startY = worldY - AnchorY;

            for (int sx = 0; sx < Width; sx++)
            {
                for (int sy = 0; sy < Height; sy++)
                {
                    int bx = startX + sx;
                    int by = startY + sy;
                    if (!RoomBlueprint.InBounds(bx, by)) continue;

                    SetIfNotEmpty(bp.Ground, bx, by, Ground[sx, sy]);
                    SetIfNotEmpty(bp.Detail, bx, by, Detail[sx, sy]);
                    SetIfNotEmpty(bp.Shadow, bx, by, Shadow[sx, sy]);
                    SetIfNotEmpty(bp.PropsBelow, bx, by, PropsBelow[sx, sy]);
                    SetIfNotEmpty(bp.PropsAbove, bx, by, PropsAbove[sx, sy]);
                    SetIfNotEmpty(bp.Collision, bx, by, Collision[sx, sy]);
                }
            }
        }

        /// <summary>
        /// Checks if this stamp fits at the given position without overlapping existing props.
        /// </summary>
        public bool FitsAt(RoomBlueprint bp, int worldX, int worldY)
        {
            int startX = worldX - AnchorX;
            int startY = worldY - AnchorY;

            for (int sx = 0; sx < Width; sx++)
            {
                for (int sy = 0; sy < Height; sy++)
                {
                    int bx = startX + sx;
                    int by = startY + sy;
                    if (!RoomBlueprint.InBounds(bx, by)) return false;

                    // Check if any layer would collide
                    if (PropsBelow[sx, sy] != TileKey.Empty && bp.PropsBelow[bx, by] != TileKey.Empty)
                        return false;
                    if (PropsAbove[sx, sy] != TileKey.Empty && bp.PropsAbove[bx, by] != TileKey.Empty)
                        return false;
                    if (Collision[sx, sy] != TileKey.Empty && bp.Collision[bx, by] != TileKey.Empty)
                        return false;
                }
            }
            return true;
        }

        private void SetIfNotEmpty(TileKey[,] grid, int x, int y, TileKey key)
        {
            if (key != TileKey.Empty)
                grid[x, y] = key;
        }
    }
}
