using UnityEngine;
using UnityEditor;

namespace ArcaneAtlas.Editor
{
    public static class SpriteImportFixer
    {
        private const string SPRITES_PATH = "Assets/Art/Sprites";

        public static void FixAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SPRITES_PATH });

            if (guids.Length == 0)
            {
                Debug.Log($"[SpriteImportFixer] No textures found in {SPRITES_PATH}");
                return;
            }

            int fixedCount = 0;
            int skippedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool needsFix = false;

                if (importer.textureType != TextureImporterType.Sprite)
                    needsFix = true;
                if (importer.spriteImportMode != SpriteImportMode.Single)
                    needsFix = true;
                if (importer.spritePixelsPerUnit != 32)
                    needsFix = true;
                if (importer.filterMode != FilterMode.Point)
                    needsFix = true;
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    needsFix = true;
                if (importer.maxTextureSize != 2048)
                    needsFix = true;

                if (!needsFix)
                {
                    skippedCount++;
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();

                fixedCount++;
            }

            Debug.Log($"[SpriteImportFixer] Fixed {fixedCount} sprites, {skippedCount} already correct ({guids.Length} total)");
        }
    }
}
