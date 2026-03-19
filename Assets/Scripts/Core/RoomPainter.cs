using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Core
{
    /// <summary>
    /// Paints a RoomBlueprint onto a set of Tilemaps using a TilePaletteDef.
    /// Handles creating the tilemap hierarchy at runtime if needed.
    /// This is the bridge between abstract layouts and visible tiles.
    /// </summary>
    public static class RoomPainter
    {
        // Layer names match SortingLayerSetup and RoomTemplateEditorWindow
        private static readonly string[] LAYER_NAMES = {
            "Ground", "Detail", "Shadow", "PropsBelow", "PropsAbove", "Overlay", "Collision"
        };

        private static readonly string[] SORTING_LAYERS = {
            "Ground", "Detail", "Shadow", "PropsBelow", "PropsAbove", "Overlay", "Default"
        };

        /// <summary>
        /// Paints a blueprint onto tilemaps at the given world position.
        /// Creates a Grid + Tilemap hierarchy under the parent transform.
        /// Returns the root GameObject so it can be destroyed when leaving the room.
        /// </summary>
        public static GameObject Paint(RoomBlueprint blueprint, TilePaletteDef palette,
            Vector3 worldPosition, Transform parent)
        {
            if (blueprint == null || palette == null) return null;

            var lookup = palette.BuildLookup();

            // Create root Grid object
            var root = new GameObject("PaintedRoom");
            root.transform.SetParent(parent);
            root.transform.position = worldPosition;

            var grid = root.AddComponent<Grid>();
            grid.cellSize = new Vector3(0.25f, 0.25f, 0f); // 8px tiles at 32 PPU

            // Create one Tilemap per layer
            var tilemaps = new Tilemap[LAYER_NAMES.Length];
            for (int i = 0; i < LAYER_NAMES.Length; i++)
            {
                var layerGO = new GameObject(LAYER_NAMES[i]);
                layerGO.transform.SetParent(root.transform);
                layerGO.transform.localPosition = Vector3.zero;

                var tilemap = layerGO.AddComponent<Tilemap>();
                var renderer = layerGO.AddComponent<TilemapRenderer>();

                renderer.sortingLayerName = SORTING_LAYERS[i];
                renderer.sortingOrder = 0;

                // Collision layer: add collider + composite for smooth edges
                if (LAYER_NAMES[i] == "Collision")
                {
                    var rb = layerGO.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;

                    var collider = layerGO.AddComponent<TilemapCollider2D>();
                    collider.compositeOperation = Collider2D.CompositeOperation.Merge;

                    var composite = layerGO.AddComponent<CompositeCollider2D>();
                    composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

                    renderer.enabled = false; // Invisible
                }

                tilemaps[i] = tilemap;
            }

            // Paint each layer
            PaintLayer(tilemaps[0], blueprint.Ground, lookup);
            PaintLayer(tilemaps[1], blueprint.Detail, lookup);
            PaintLayer(tilemaps[2], blueprint.Shadow, lookup);
            PaintLayer(tilemaps[3], blueprint.PropsBelow, lookup);
            PaintLayer(tilemaps[4], blueprint.PropsAbove, lookup);
            PaintLayer(tilemaps[5], blueprint.Overlay, lookup);

            // Collision layer gets a runtime-generated tile — no palette mapping needed
            PaintCollisionLayer(tilemaps[6], blueprint.Collision);

            return root;
        }

        /// <summary>
        /// Paints a single layer grid onto a tilemap.
        /// Positions are offset so the room is centered on the Grid's origin.
        /// </summary>
        private static void PaintLayer(Tilemap tilemap, TileKey[,] grid,
            Dictionary<TileKey, TileBase> lookup)
        {
            // Offset so the room is centered: (-WIDTH/2, -HEIGHT/2)
            int offsetX = -RoomBlueprint.WIDTH / 2;
            int offsetY = -RoomBlueprint.HEIGHT / 2;

            for (int x = 0; x < RoomBlueprint.WIDTH; x++)
            {
                for (int y = 0; y < RoomBlueprint.HEIGHT; y++)
                {
                    TileKey key = grid[x, y];
                    if (key == TileKey.Empty) continue;

                    if (lookup.TryGetValue(key, out TileBase tile))
                    {
                        tilemap.SetTile(new Vector3Int(x + offsetX, y + offsetY, 0), tile);
                    }
                }
            }
        }

        // Cached collision tile — created once, reused for all collision cells
        private static Tile collisionTile;

        /// <summary>
        /// Paints the collision layer using a runtime-generated tile.
        /// No palette mapping needed — any non-empty collision cell gets a solid tile
        /// that the CompositeCollider2D uses for physics shapes.
        /// </summary>
        private static void PaintCollisionLayer(Tilemap tilemap, TileKey[,] grid)
        {
            if (collisionTile == null)
            {
                collisionTile = ScriptableObject.CreateInstance<Tile>();
                // Create a tiny 1x1 white sprite for the tile
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                collisionTile.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 4f);
                collisionTile.color = Color.white;
                collisionTile.colliderType = Tile.ColliderType.Grid;
            }

            int offsetX = -RoomBlueprint.WIDTH / 2;
            int offsetY = -RoomBlueprint.HEIGHT / 2;

            for (int x = 0; x < RoomBlueprint.WIDTH; x++)
            {
                for (int y = 0; y < RoomBlueprint.HEIGHT; y++)
                {
                    TileKey key = grid[x, y];
                    if (key == TileKey.CollisionSolid || key == TileKey.CollisionWater)
                    {
                        tilemap.SetTile(new Vector3Int(x + offsetX, y + offsetY, 0), collisionTile);
                    }
                }
            }
        }

        /// <summary>
        /// Converts a blueprint tile coordinate to world position (relative to room center).
        /// Useful for placing player/NPCs at marker positions.
        /// </summary>
        public static Vector3 TileToWorld(Vector2Int tilePos, Vector3 roomWorldCenter)
        {
            float offsetX = (tilePos.x - RoomBlueprint.WIDTH / 2f + 0.5f) * 0.25f;
            float offsetY = (tilePos.y - RoomBlueprint.HEIGHT / 2f + 0.5f) * 0.25f;
            return roomWorldCenter + new Vector3(offsetX, offsetY, 0f);
        }

        /// <summary>
        /// Clears all tiles from an existing painted room (for reuse without destroying).
        /// </summary>
        public static void Clear(GameObject paintedRoom)
        {
            if (paintedRoom == null) return;
            var tilemaps = paintedRoom.GetComponentsInChildren<Tilemap>();
            foreach (var tm in tilemaps)
                tm.ClearAllTiles();
        }
    }
}
