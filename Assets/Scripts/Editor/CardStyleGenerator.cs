using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Generates CardStyleDef assets by loading existing card art sprites.
    /// Default: uses placeholder creature art from Resources/CardArt/
    /// MiniFantasyDefault: imports Minifantasy creature sprites and maps them to elements.
    /// Arcane Atlas > Generate Card Style
    /// </summary>
    public static class CardStyleGenerator
    {
        [MenuItem("Arcane Atlas/Generate Card Style", priority = 41)]
        public static void Generate()
        {
            GenerateDefaultStyle();
            GenerateMiniFantasyStyle();
        }

        private static void GenerateDefaultStyle()
        {
            var style = CreateOrLoadStyle("Default");
            AssignFramesAndIcons(style);
            style.TintFire = new Color(0.95f, 0.30f, 0.20f);
            style.TintWater = new Color(0.25f, 0.50f, 0.90f);
            style.TintEarth = new Color(0.35f, 0.70f, 0.30f);
            style.TintWind = new Color(0.80f, 0.80f, 0.90f);

            // Copy creature art into Resources so Resources.Load can find them at runtime
            CopyCreatureArtToResources();

            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            Debug.Log("[CardStyleGenerator] Generated 'Default' card style.");
        }

        /// <summary>
        /// Copies creature sprites from Art/Sprites/cards/creatures/ to Resources/CardArt/
        /// so they're loadable at runtime via Resources.Load.
        /// </summary>
        private static void CopyCreatureArtToResources()
        {
            string[] elements = { "earth", "fire", "water", "wind" };
            int totalCopied = 0;

            foreach (string elem in elements)
            {
                string srcFolder = $"Assets/Art/Sprites/cards/creatures/{elem}";
                string destFolder = $"Assets/Resources/CardArt/{elem}";
                EnsureFolder(destFolder);

                if (!AssetDatabase.IsValidFolder(srcFolder)) continue;

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { srcFolder });
                foreach (string guid in guids)
                {
                    string srcPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(srcPath);
                    string destPath = $"{destFolder}/{fileName}";

                    // Only copy if not already there
                    string fullDest = Path.Combine(Application.dataPath, "..", destPath).Replace('\\', '/');
                    if (!File.Exists(fullDest))
                    {
                        string fullSrc = Path.Combine(Application.dataPath, "..", srcPath).Replace('\\', '/');
                        File.Copy(fullSrc, fullDest, true);
                        totalCopied++;
                    }
                }
            }

            if (totalCopied > 0)
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Always configure ALL creature sprites (fixes wrong import settings from previous runs)
            int configured = 0;
            foreach (string elem in elements)
            {
                string destFolder = $"Assets/Resources/CardArt/{elem}";
                if (!AssetDatabase.IsValidFolder(destFolder)) continue;

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { destFolder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool needsUpdate = importer.spriteImportMode != SpriteImportMode.Single ||
                                       importer.textureType != TextureImporterType.Sprite;

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;

                    if (needsUpdate)
                    {
                        importer.SaveAndReimport();
                        configured++;
                    }
                }
            }

            Debug.Log($"[CardStyleGenerator] Creature art: {totalCopied} copied, {configured} reconfigured to Single sprite mode.");
        }

        private static void GenerateMiniFantasyStyle()
        {
            var style = CreateOrLoadStyle("MiniFantasyDefault");
            AssignFramesAndIcons(style);

            // Deeper, richer tints for Minifantasy art
            style.TintFire = new Color(0.85f, 0.20f, 0.10f);
            style.TintWater = new Color(0.10f, 0.35f, 0.75f);
            style.TintEarth = new Color(0.20f, 0.55f, 0.15f);
            style.TintWind = new Color(0.70f, 0.72f, 0.85f);

            // Import Minifantasy creature sprites as overrides
            var overrides = new List<CreatureArtMapping>();
            string minifantasyRoot = FindMinifantasyRoot();

            if (!string.IsNullOrEmpty(minifantasyRoot))
            {
                // Earth creatures: forest dwellers, base humanoids
                ImportCreaturesForElement(minifantasyRoot, ElementType.Earth, overrides, new[] {
                    "Creatures*Assets/Monsters/Centaur/CentaurIdle.png",
                    "Creatures*Assets/Monsters/Minotaur/MinotaurIdle.png",
                    "Creatures*Assets/Monsters/Trasgo/TrasgoIdle.png",
                    "Creatures*Assets/Monsters/Cyclop/CyclopIdle.png",
                    "Creatures*Assets/Monsters/Pumpkin_Horror/PumpkinHorrorBaseIdleActivation.png",
                    "Creatures*Assets/Base_Humanoids/Dwarf/Base_Dwarf/DwarfIdle.png",
                    "forest_dwellers*Assets/*/General_Animations/*Idle*.png",
                });

                // Fire creatures: hellscape monsters
                ImportCreaturesForElement(minifantasyRoot, ElementType.Fire, overrides, new[] {
                    "true_heroes*Assets/Dark_Priest/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Demonologist/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Blood_Mage/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Ninja_Assassin/General_Animations/*Idle*.png",
                });

                // Water creatures: aquatic
                ImportCreaturesForElement(minifantasyRoot, ElementType.Water, overrides, new[] {
                    "aquatic*Assets/Creatures/Dolphin/Base_Water/Base_Idle.png",
                    "aquatic*Assets/Creatures/Frogfolk/On_Land*/Frogfolk_Warrior/Land_Idle.png",
                    "aquatic*Assets/Creatures/Frogfolk/On_Land*/Frogfolk_Villager/Land_Idle.png",
                    "aquatic*Assets/Creatures/Kraken/Kraken_Body/Base_Water/Base_Idle.png",
                    "aquatic*Assets/Creatures/Otter/On_Land*/Land_Idle.png",
                });

                // Wind creatures: heroes, rangers
                ImportCreaturesForElement(minifantasyRoot, ElementType.Wind, overrides, new[] {
                    "true_heroes*Assets/Ranger/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Wizard/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Bard/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Druid/General_Animations/*Idle*.png",
                    "true_heroes*Assets/Rogue/General_Animations/*Idle*.png",
                });
            }

            style.CreatureOverrides = overrides.ToArray();
            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CardStyleGenerator] Generated 'MiniFantasyDefault' with {overrides.Count} creature overrides.");
        }

        private static void ImportCreaturesForElement(string root, ElementType element,
            List<CreatureArtMapping> overrides, string[] patterns)
        {
            int index = overrides.Count(o => o.Element == element) + 1;

            foreach (string pattern in patterns)
            {
                if (index > 25) break; // Max 25 per element

                // Search for matching files
                var files = FindMatchingFiles(root, pattern);
                foreach (string file in files)
                {
                    if (index > 25) break;

                    // Copy to project and import
                    string safeName = $"mf_{element.ToString().ToLower()}_{index:D2}";
                    string destPath = $"Assets/Resources/CardArt/{element.ToString().ToLower()}/{safeName}.png";
                    string fullDest = Path.Combine(Application.dataPath, "..", destPath).Replace('\\', '/');

                    string destDir = Path.GetDirectoryName(fullDest);
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    if (!File.Exists(fullDest))
                    {
                        File.Copy(file, fullDest, true);
                        AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceSynchronousImport);

                        // Configure import
                        var importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
                        if (importer != null)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.spritePixelsPerUnit = 32;
                            importer.filterMode = FilterMode.Point;
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.SaveAndReimport();
                        }
                    }

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destPath);
                    if (sprite != null)
                    {
                        overrides.Add(new CreatureArtMapping
                        {
                            Element = element,
                            SpriteIndex = index,
                            Sprite = sprite,
                        });
                        index++;
                    }
                }
            }
        }

        private static List<string> FindMatchingFiles(string root, string pattern)
        {
            var results = new List<string>();

            // Pattern format: "partial_path*more_path/file.png"
            // Split on * and search recursively
            string[] parts = pattern.Split('*');
            string searchDir = root;

            // Find directories matching the first part
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
            {
                var dirs = Directory.GetDirectories(root, $"*{parts[0]}*", SearchOption.AllDirectories);
                if (dirs.Length == 0) return results;
                searchDir = dirs[0];
            }

            // Search for files matching the last part
            string filePattern = parts.Length > 1 ? parts[parts.Length - 1] : "*.png";
            filePattern = Path.GetFileName(filePattern);

            try
            {
                var files = Directory.GetFiles(searchDir, filePattern, SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    if (f.EndsWith(".png") && !f.Contains("Shadow") &&
                        !f.Contains("_GIF") && !f.Contains("IdleEnd"))
                        results.Add(f);
                }
            }
            catch { }

            return results.Take(3).ToList(); // Limit per pattern
        }

        // ═══════════════════════════════════════
        //  SHARED HELPERS
        // ═══════════════════════════════════════

        private static CardStyleDef CreateOrLoadStyle(string name)
        {
            string folder = "Assets/Resources/CardStyles";
            EnsureFolder(folder);

            string defPath = $"{folder}/{name}.asset";
            var style = AssetDatabase.LoadAssetAtPath<CardStyleDef>(defPath);
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<CardStyleDef>();
                style.name = name; // Must match filename for Resources.Load
                AssetDatabase.CreateAsset(style, defPath);
            }
            else if (style.name != name)
            {
                // Fix name mismatch
                style.name = name;
                EditorUtility.SetDirty(style);
            }
            style.StyleName = name;
            return style;
        }

        private static void AssignFramesAndIcons(CardStyleDef style)
        {
            string mc = "Assets/Art/Sprites/ModularCards/Assets";

            // Legacy frames (kept for fallback)
            style.FrameCommon = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_common.png");
            style.FrameUncommon = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_uncommon.png");
            style.FrameRare = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_rare.png");
            style.FrameLegendary = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_legendary.png");

            style.DetailCommon = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_detail_common.png");
            style.DetailUncommon = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_detail_uncommon.png");
            style.DetailRare = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_detail_rare.png");
            style.DetailLegendary = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_detail_legendary.png");

            style.ShopFrameCommon = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_shop_common.png");
            style.ShopFrameUncommon = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_shop_uncommon.png");
            style.ShopFrameRare = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_shop_rare.png");
            style.ShopFrameLegendary = LoadSprite("Assets/Art/Sprites/cards/frames/card_frame_shop_legendary.png");

            // Element icons
            style.IconFire = LoadSprite("Assets/Art/Sprites/icons/elements/icon_fire.png");
            style.IconWater = LoadSprite("Assets/Art/Sprites/icons/elements/icon_water.png");
            style.IconEarth = LoadSprite("Assets/Art/Sprites/icons/elements/icon_earth.png");
            style.IconWind = LoadSprite("Assets/Art/Sprites/icons/elements/icon_wind.png");

            // ── Modular Card Layers ──
            // Backgrounds per element
            style.ModBgFire = LoadSprite($"{mc}/back_red.png");
            style.ModBgWater = LoadSprite($"{mc}/back_blue.png");
            style.ModBgEarth = LoadSprite($"{mc}/back_green.png");
            style.ModBgWind = LoadSprite($"{mc}/back_purple.png");

            // Borders per element
            style.ModBorderFire = LoadSprite($"{mc}/border_red.png");
            style.ModBorderWater = LoadSprite($"{mc}/border_blue.png");
            style.ModBorderEarth = LoadSprite($"{mc}/border_green.png");
            style.ModBorderWind = LoadSprite($"{mc}/border_purple.png");

            // Frames per rarity
            style.ModFrameCommon = LoadSprite($"{mc}/frame_gray_light.png");
            style.ModFrameRare = LoadSprite($"{mc}/frame_blue.png");
            style.ModFrameLegendary = LoadSprite($"{mc}/frame_gold.png");

            // Caption bars per element (for card name)
            style.ModCaptionFire = LoadSprite($"{mc}/caption_red.png");
            style.ModCaptionWater = LoadSprite($"{mc}/caption_blue.png");
            style.ModCaptionEarth = LoadSprite($"{mc}/caption_green.png");
            style.ModCaptionWind = LoadSprite($"{mc}/caption_purple.png");

            // Shared components
            style.ModTitleBar = LoadSprite($"{mc}/title_brown.png");
            style.ModDescriptionBar = LoadSprite($"{mc}/description_brown.png");
            style.ModCostCircle = LoadSprite($"{mc}/circle_gold.png");
            style.ModStatSquare = LoadSprite($"{mc}/square_black.png");

            // Glow effects for rare/legendary
            style.ModGlowRare = LoadSprite($"{mc}/glow_blue.png");
            style.ModGlowLegendary = LoadSprite($"{mc}/glow_gold.png");

            style.BadgeBronze = LoadSprite("Assets/Art/Sprites/cards/badges/tier_bronze.png");
            style.BadgeSilver = LoadSprite("Assets/Art/Sprites/cards/badges/tier_silver.png");
            style.BadgeGold = LoadSprite("Assets/Art/Sprites/cards/badges/tier_gold.png");

            style.CardBack = LoadSprite("Assets/Art/Sprites/cards/card_back.png");
            style.CardBackShop = LoadSprite("Assets/Art/Sprites/cards/card_back_shop.png");
        }

        private static string FindMinifantasyRoot()
        {
            string[] searchPaths = new string[]
            {
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "Minifantasy"),
                @"C:\Users\" + System.Environment.UserName + @"\OneDrive\Desktop\Minifantasy",
            };
            foreach (string path in searchPaths)
                if (Directory.Exists(path)) return path;
            return null;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[CardStyleGenerator] Sprite not found: {path}");
            return sprite;
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
