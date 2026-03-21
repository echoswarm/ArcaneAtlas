using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Generates card portrait art by extracting frames from imported Minifantasy
    /// character sprites (CharacterFrames) and saving them as card creature art.
    /// Arcane Atlas > Generate Card Portraits
    /// </summary>
    public static class CardPortraitGenerator
    {
        // Map characters to elements (thematic grouping)
        private static readonly Dictionary<ElementType, string[]> ELEMENT_CHARACTERS = new Dictionary<ElementType, string[]>
        {
            { ElementType.Fire, new[] {
                "Barbarian", "Fighter", "Dark_Priest", "Demonologist", "Blood_Mage",
                "Ninja_Assassin", "Supreme_Necromancer", "Tech_Augmented_Gunslinger"
            }},
            { ElementType.Water, new[] {
                "Frogfolk_Warrior", "Frogfolk_Villager", "Cooker", "Butcher",
                "Alchemist", "Cleric", "Tailor"
            }},
            { ElementType.Earth, new[] {
                "Blacksmith", "Carpenter", "Dyer", "Furrier", "Jeweller",
                "Paladin", "Druid"
            }},
            { ElementType.Wind, new[] {
                "Wizard", "Bard", "Ranger", "Rogue",
                "Alchemist", "Cleric", "Tailor"
            }},
        };

        [MenuItem("Arcane Atlas/Generate Card Portraits", priority = 42)]
        public static void Generate()
        {
            string charFramesRoot = "Assets/Resources/CharacterFrames";
            int totalGenerated = 0;

            foreach (var kvp in ELEMENT_CHARACTERS)
            {
                ElementType element = kvp.Key;
                string[] characters = kvp.Value;
                string elem = element.ToString().ToLower();

                string destFolder = $"Assets/Resources/CardArt/{elem}";
                EnsureFolder(destFolder);

                int cardIndex = 1;
                int charIdx = 0;

                // Generate 25 portraits per element, cycling through available characters
                while (cardIndex <= 25)
                {
                    string charName = characters[charIdx % characters.Length];
                    string idlePath = $"{charFramesRoot}/{charName}/{charName}_Idle.png";
                    string walkPath = $"{charFramesRoot}/{charName}/{charName}_Walk.png";

                    // Try Idle first, fall back to Walk
                    string sourcePath = AssetDatabase.LoadAssetAtPath<Texture2D>(idlePath) != null ? idlePath : walkPath;
                    var sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);

                    if (sourceTex == null)
                    {
                        charIdx++;
                        if (charIdx >= characters.Length * 2) break; // Prevent infinite loop
                        continue;
                    }

                    // Make source readable
                    var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
                    bool wasReadable = importer != null && importer.isReadable;
                    if (importer != null && !wasReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                        sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                    }

                    // Get all sub-sprites from the sheet
                    var subSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath)
                        .OfType<Sprite>()
                        .OrderBy(s => s.rect.x)
                        .ThenBy(s => s.rect.y)
                        .ToArray();

                    if (subSprites.Length == 0)
                    {
                        // Single sprite — use the whole texture
                        SavePortrait(sourceTex, new Rect(0, 0, sourceTex.width, sourceTex.height),
                            destFolder, elem, cardIndex);
                        totalGenerated++;
                        cardIndex++;
                    }
                    else
                    {
                        // Pick a few distinct frames from this character (spread across the sheet)
                        int framesToUse = Mathf.Min(3, subSprites.Length);
                        int step = Mathf.Max(1, subSprites.Length / framesToUse);

                        for (int f = 0; f < framesToUse && cardIndex <= 25; f++)
                        {
                            int frameIdx = f * step;
                            if (frameIdx >= subSprites.Length) frameIdx = subSprites.Length - 1;

                            var sprite = subSprites[frameIdx];
                            SavePortrait(sourceTex, sprite.rect, destFolder, elem, cardIndex);
                            totalGenerated++;
                            cardIndex++;
                        }
                    }

                    // Restore readability
                    if (importer != null && !wasReadable)
                    {
                        importer.isReadable = false;
                        importer.SaveAndReimport();
                    }

                    charIdx++;
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Configure all generated portraits as Single sprites
            foreach (string elem in new[] { "fire", "water", "earth", "wind" })
            {
                string folder = $"Assets/Resources/CardArt/{elem}";
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;
                    if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single)
                        continue; // Already configured

                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = 32;
                    imp.filterMode = FilterMode.Point;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
            }

            // Bake all creature sprites into CardStyleDef assets as CreatureOverrides
            // This guarantees runtime loading via direct asset references (no Resources.Load needed)
            BakeIntoCardStyles();

            Debug.Log($"[CardPortraitGenerator] Generated {totalGenerated} card portraits from Minifantasy characters.");
        }

        /// <summary>
        /// Loads all creature sprites from Resources/CardArt/ and bakes them
        /// into all CardStyleDef assets as CreatureOverrides for guaranteed runtime loading.
        /// </summary>
        private static void BakeIntoCardStyles()
        {
            var overrides = new List<CreatureArtMapping>();

            foreach (string elem in new[] { "fire", "water", "earth", "wind" })
            {
                ElementType element;
                switch (elem)
                {
                    case "fire": element = ElementType.Fire; break;
                    case "water": element = ElementType.Water; break;
                    case "earth": element = ElementType.Earth; break;
                    case "wind": element = ElementType.Wind; break;
                    default: continue;
                }

                string folder = $"Assets/Resources/CardArt/{elem}";
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                for (int i = 1; i <= 25; i++)
                {
                    string path = $"{folder}/creature_{elem}_{i:D2}.png";

                    // LoadAssetAtPath<Sprite> is unreliable — use LoadAllAssetsAtPath instead
                    Sprite sprite = null;
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in allAssets)
                    {
                        if (asset is Sprite s) { sprite = s; break; }
                    }

                    if (sprite != null)
                    {
                        overrides.Add(new CreatureArtMapping
                        {
                            Element = element,
                            SpriteIndex = i,
                            Sprite = sprite,
                        });
                    }
                }
            }

            // Apply to all CardStyleDef assets in Resources/CardStyles/
            string stylesFolder = "Assets/Resources/CardStyles";
            if (!AssetDatabase.IsValidFolder(stylesFolder)) return;

            var styleGuids = AssetDatabase.FindAssets("t:CardStyleDef", new[] { stylesFolder });
            foreach (string guid in styleGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var style = AssetDatabase.LoadAssetAtPath<CardStyleDef>(path);
                if (style == null) continue;

                style.CreatureOverrides = overrides.ToArray();
                EditorUtility.SetDirty(style);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CardPortraitGenerator] Baked {overrides.Count} creature overrides into {styleGuids.Length} card styles.");
        }

        private static void SavePortrait(Texture2D sourceTex, Rect spriteRect, string destFolder, string elem, int index)
        {
            int w = (int)spriteRect.width;
            int h = (int)spriteRect.height;

            var portrait = new Texture2D(w, h, TextureFormat.RGBA32, false);
            portrait.filterMode = FilterMode.Point;

            try
            {
                var pixels = sourceTex.GetPixels((int)spriteRect.x, (int)spriteRect.y, w, h);
                portrait.SetPixels(pixels);
                portrait.Apply();

                string destPath = $"{destFolder}/creature_{elem}_{index:D2}.png";
                string fullDest = Path.Combine(Application.dataPath, "..", destPath).Replace('\\', '/');
                File.WriteAllBytes(fullDest, portrait.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(portrait);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
