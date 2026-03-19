using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Generates a complete TilePaletteDef with colored placeholder tiles.
    /// Each TileKey gets a distinct solid-color 8x8 tile so you can see room layouts
    /// without any Minifantasy art. Swap to a real palette later for final visuals.
    /// Arcane Atlas > Generate Placeholder Palette
    /// </summary>
    public static class PlaceholderPaletteGenerator
    {
        [MenuItem("Arcane Atlas/Generate Placeholder Palette", priority = 35)]
        public static void Generate()
        {
            string spriteFolder = "Assets/Art/Sprites/placeholder_tiles";
            string tileFolder = "Assets/Art/Tiles/placeholder";
            string paletteFolder = "Assets/Resources/TilePalettes";

            EnsureFolder(spriteFolder);
            EnsureFolder(tileFolder);
            EnsureFolder(paletteFolder);

            // Define colors for each TileKey
            var colorMap = BuildColorMap();

            var mappings = new List<TileMapping>();
            int created = 0;

            foreach (var kvp in colorMap)
            {
                TileKey key = kvp.Key;
                Color color = kvp.Value;

                // Create 8x8 texture
                string texName = $"ph_{key}";
                string texPath = $"{spriteFolder}/{texName}.png";

                if (!File.Exists(Path.Combine(Application.dataPath, "..", texPath)))
                {
                    var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    var pixels = new Color[64];
                    for (int i = 0; i < 64; i++) pixels[i] = color;

                    // Add a subtle border (1px darker) so tiles are distinguishable
                    Color border = color * 0.7f;
                    border.a = color.a;
                    for (int i = 0; i < 8; i++)
                    {
                        pixels[i] = border;            // bottom row
                        pixels[56 + i] = border;       // top row
                        pixels[i * 8] = border;        // left col
                        pixels[i * 8 + 7] = border;    // right col
                    }

                    tex.SetPixels(pixels);
                    tex.Apply();

                    File.WriteAllBytes(Path.Combine(Application.dataPath, "..", texPath), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    created++;
                }
            }

            // Refresh to import the new PNGs
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Configure all placeholder sprites
            foreach (var kvp in colorMap)
            {
                string texPath = $"{spriteFolder}/ph_{kvp.Key}.png";
                var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            // Create Tile assets and build mappings
            foreach (var kvp in colorMap)
            {
                TileKey key = kvp.Key;
                string texPath = $"{spriteFolder}/ph_{key}.png";
                string tilePath = $"{tileFolder}/tile_{key}.asset";

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
                if (sprite == null) continue;

                // Create or update tile asset
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                tile.color = Color.white;
                EditorUtility.SetDirty(tile);

                mappings.Add(new TileMapping { Key = key, Tile = tile });
            }

            // Create the TilePaletteDef ScriptableObject
            string defPath = $"{paletteFolder}/Placeholder.asset";
            var paletteDef = AssetDatabase.LoadAssetAtPath<TilePaletteDef>(defPath);
            if (paletteDef == null)
            {
                paletteDef = ScriptableObject.CreateInstance<TilePaletteDef>();
                AssetDatabase.CreateAsset(paletteDef, defPath);
            }
            paletteDef.PaletteName = "Placeholder";
            paletteDef.Mappings = mappings.ToArray();
            EditorUtility.SetDirty(paletteDef);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PlaceholderPalette] Generated {created} new textures, {mappings.Count} tile mappings. Palette at: {defPath}");
        }

        private static Dictionary<TileKey, Color> BuildColorMap()
        {
            var map = new Dictionary<TileKey, Color>();

            // ── Ground ──
            map[TileKey.Ground] = new Color(0.45f, 0.65f, 0.30f);
            map[TileKey.GroundAlt] = new Color(0.50f, 0.60f, 0.28f);
            map[TileKey.Path] = new Color(0.72f, 0.62f, 0.42f);
            map[TileKey.PathEdgeN] = new Color(0.68f, 0.58f, 0.38f);
            map[TileKey.PathEdgeS] = new Color(0.68f, 0.58f, 0.38f);
            map[TileKey.PathEdgeE] = new Color(0.68f, 0.58f, 0.38f);
            map[TileKey.PathEdgeW] = new Color(0.68f, 0.58f, 0.38f);

            // ── Walls ──
            Color wall = new Color(0.40f, 0.32f, 0.22f);
            map[TileKey.WallN] = wall; map[TileKey.WallS] = wall;
            map[TileKey.WallE] = wall; map[TileKey.WallW] = wall;
            Color corner = new Color(0.35f, 0.28f, 0.18f);
            map[TileKey.WallCornerNW] = corner; map[TileKey.WallCornerNE] = corner;
            map[TileKey.WallCornerSW] = corner; map[TileKey.WallCornerSE] = corner;
            Color innerCorner = new Color(0.48f, 0.38f, 0.26f);
            map[TileKey.WallInnerNW] = innerCorner; map[TileKey.WallInnerNE] = innerCorner;
            map[TileKey.WallInnerSW] = innerCorner; map[TileKey.WallInnerSE] = innerCorner;

            // ── Doors ──
            Color door = new Color(0.60f, 0.55f, 0.35f);
            map[TileKey.DoorN] = door; map[TileKey.DoorS] = door;
            map[TileKey.DoorE] = door; map[TileKey.DoorW] = door;

            // ── Detail ──
            map[TileKey.GrassDetail] = new Color(0.35f, 0.75f, 0.25f);
            map[TileKey.FlowerDetail] = new Color(0.85f, 0.40f, 0.55f);
            map[TileKey.CrackDetail] = new Color(0.55f, 0.50f, 0.45f);
            map[TileKey.MossDetail] = new Color(0.30f, 0.55f, 0.25f);
            map[TileKey.MushroomDetail] = new Color(0.70f, 0.55f, 0.40f);
            map[TileKey.PebbleDetail] = new Color(0.60f, 0.58f, 0.55f);

            // ── Shadow ──
            Color shadow = new Color(0f, 0f, 0f, 0.35f);
            map[TileKey.ShadowWallN] = shadow; map[TileKey.ShadowWallW] = shadow;
            map[TileKey.ShadowCornerNW] = shadow;
            map[TileKey.ShadowFull] = new Color(0f, 0f, 0f, 0.5f);
            map[TileKey.ShadowSmall] = new Color(0f, 0f, 0f, 0.25f);
            map[TileKey.ShadowMedium] = new Color(0f, 0f, 0f, 0.30f);
            map[TileKey.ShadowLarge] = new Color(0f, 0f, 0f, 0.35f);

            // ── Trees ──
            Color trunk = new Color(0.50f, 0.35f, 0.20f);
            map[TileKey.TreeBase] = trunk;
            map[TileKey.TreeTrunkBottom] = trunk;
            map[TileKey.TreeTrunkMid] = new Color(0.55f, 0.38f, 0.22f);
            map[TileKey.TreeRoots] = new Color(0.45f, 0.32f, 0.18f);
            map[TileKey.StumpSmall] = new Color(0.48f, 0.33f, 0.19f);
            map[TileKey.LogH] = new Color(0.52f, 0.36f, 0.21f);

            // ── Rocks ──
            map[TileKey.RockSmall] = new Color(0.55f, 0.55f, 0.55f);
            map[TileKey.RockLarge] = new Color(0.50f, 0.50f, 0.52f);
            Color rockCluster = new Color(0.52f, 0.52f, 0.53f);
            map[TileKey.RockClusterBL] = rockCluster; map[TileKey.RockClusterBR] = rockCluster;
            map[TileKey.RockClusterTL] = new Color(0.58f, 0.57f, 0.56f);
            map[TileKey.RockClusterTR] = new Color(0.58f, 0.57f, 0.56f);

            // ── Bushes ──
            map[TileKey.BushBase] = new Color(0.25f, 0.50f, 0.20f);
            map[TileKey.BushSmall] = new Color(0.28f, 0.52f, 0.22f);
            map[TileKey.BushWide] = new Color(0.22f, 0.48f, 0.18f);

            // ── Other props ──
            map[TileKey.Crate] = new Color(0.65f, 0.50f, 0.25f);
            map[TileKey.Chest] = new Color(0.85f, 0.70f, 0.15f);
            map[TileKey.FenceH] = new Color(0.58f, 0.45f, 0.28f);
            map[TileKey.FenceV] = new Color(0.58f, 0.45f, 0.28f);
            map[TileKey.FencePostTL] = new Color(0.52f, 0.40f, 0.24f);
            map[TileKey.FencePostTR] = new Color(0.52f, 0.40f, 0.24f);
            map[TileKey.FencePostBL] = new Color(0.52f, 0.40f, 0.24f);
            map[TileKey.FencePostBR] = new Color(0.52f, 0.40f, 0.24f);

            // ── Water ──
            map[TileKey.Water] = new Color(0.20f, 0.45f, 0.80f);
            map[TileKey.WaterEdge] = new Color(0.35f, 0.55f, 0.75f);
            map[TileKey.WaterEdgeN] = new Color(0.30f, 0.52f, 0.72f);
            map[TileKey.WaterEdgeS] = new Color(0.30f, 0.52f, 0.72f);
            map[TileKey.WaterEdgeE] = new Color(0.30f, 0.52f, 0.72f);
            map[TileKey.WaterEdgeW] = new Color(0.30f, 0.52f, 0.72f);

            // ── Building ──
            Color buildWall = new Color(0.62f, 0.58f, 0.50f);
            map[TileKey.BuildWallH] = buildWall; map[TileKey.BuildWallV] = buildWall;
            map[TileKey.BuildCornerTL] = buildWall; map[TileKey.BuildCornerTR] = buildWall;
            map[TileKey.BuildCornerBL] = buildWall; map[TileKey.BuildCornerBR] = buildWall;
            map[TileKey.BuildDoor] = new Color(0.50f, 0.35f, 0.20f);
            map[TileKey.BuildWindow] = new Color(0.55f, 0.70f, 0.85f);
            map[TileKey.BuildFloor] = new Color(0.58f, 0.52f, 0.42f);

            // ── Props above ──
            // ── Canopy (above player) ──
            Color canopy = new Color(0.20f, 0.60f, 0.15f);
            map[TileKey.TreeCrown] = canopy;
            map[TileKey.TreeCanopyBL] = canopy;
            map[TileKey.TreeCanopyBR] = canopy;
            map[TileKey.TreeCanopyBC] = new Color(0.18f, 0.55f, 0.12f);
            map[TileKey.TreeCanopyTL] = new Color(0.15f, 0.52f, 0.10f);
            map[TileKey.TreeCanopyTR] = new Color(0.15f, 0.52f, 0.10f);
            map[TileKey.TreeCanopyTC] = new Color(0.12f, 0.50f, 0.08f);
            map[TileKey.BushTop] = new Color(0.30f, 0.55f, 0.22f);
            map[TileKey.RockTop] = new Color(0.58f, 0.56f, 0.54f);

            // ── Roof (above player) ──
            Color roof = new Color(0.65f, 0.30f, 0.20f);
            map[TileKey.RoofBL] = roof; map[TileKey.RoofBR] = roof;
            map[TileKey.RoofBC] = roof;
            map[TileKey.RoofTL] = new Color(0.60f, 0.28f, 0.18f);
            map[TileKey.RoofTR] = new Color(0.60f, 0.28f, 0.18f);
            map[TileKey.RoofTC] = new Color(0.58f, 0.26f, 0.16f);
            map[TileKey.RoofPeak] = new Color(0.55f, 0.24f, 0.14f);

            // ── Overlay ──
            map[TileKey.OverlayVines] = new Color(0.15f, 0.40f, 0.10f, 0.6f);
            map[TileKey.OverlayFog] = new Color(0.80f, 0.80f, 0.85f, 0.3f);

            // ── Collision ──
            map[TileKey.CollisionSolid] = new Color(1f, 0f, 0f, 0.5f);  // Red (debug visible)
            map[TileKey.CollisionWater] = new Color(0f, 0f, 1f, 0.5f);  // Blue (debug visible)

            return map;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
