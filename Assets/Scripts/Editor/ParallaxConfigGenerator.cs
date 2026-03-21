using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Generates ParallaxBiomeConfig ScriptableObject assets for each game zone.
    /// Run via: Arcane Atlas > Generate Parallax Configs
    ///
    /// Assets land in Assets/Resources/ParallaxConfigs/{biomeName}.asset
    /// so ParallaxBackgroundController.LoadByBiomeName() can find them at runtime.
    ///
    /// Sprite mapping (thematic):
    ///   AncientForest  → Field of Gears / Murky Green  (earthy green tones)
    ///   VolcanicWastes → Caged Swamps / Orange Murky   (warm orange tones)
    ///   CoralDepths    → Caged Swamps / Greenish Blue  (cool blue-green)
    ///   SkyPeaks       → Field of Gears / Bright Blue  (bright open sky)
    /// </summary>
    public static class ParallaxConfigGenerator
    {
        private const string CONFIGS_PATH = "Assets/Resources/ParallaxConfigs";
        private const string ROOT         = "Assets/Art/ParallaxBackgroundSprites";

        // ── Folder shortcuts ─────────────────────────────────────────────────
        private const string CAGED_GREEN  = ROOT + "/Caged Swamps/Greenish Blue";
        private const string CAGED_ORANGE = ROOT + "/Caged Swamps/Orange Murky";
        private const string GEARS_BLUE   = ROOT + "/Field of Gears/Bright Blue";
        private const string GEARS_GREEN  = ROOT + "/Field of Gears/Murky Green";
        private const string TEEPEE_BLUE  = ROOT + "/Teepee Village/Blueish";

        // ── Scroll factor presets (0.0 = stationary sky → 0.6 = near foreground) ─
        private static readonly float[] FACTORS_6 = { 0.00f, 0.08f, 0.18f, 0.30f, 0.44f, 0.60f };
        private static readonly float[] FACTORS_7 = { 0.00f, 0.05f, 0.12f, 0.22f, 0.35f, 0.48f, 0.60f };
        private static readonly float[] FACTORS_5 = { 0.00f, 0.12f, 0.26f, 0.44f, 0.60f };

        [MenuItem("Arcane Atlas/Generate Parallax Configs", false, 51)]
        public static void Generate()
        {
            EnsureFolder();

            var defs = new[]
            {
                // AncientForest — earth/forest, calm pace
                new BiomeDef
                {
                    biomeName   = "AncientForest",
                    masterSpeed = 0.04f,
                    dimColor    = new Color(0.01f, 0.03f, 0f, 0.40f),
                    layers      = new[]
                    {
                        // Back → Front (sky first, near last)
                        L(GEARS_GREEN, "7 sky2.png",               "sky",         FACTORS_7[0]),
                        L(GEARS_GREEN, "6 BG gears2.png",          "BG gears far",FACTORS_7[1]),
                        L(GEARS_GREEN, "5 BG gears2.png",          "BG gears",    FACTORS_7[2]),
                        L(GEARS_GREEN, "4 mid2.png",               "mid",         FACTORS_7[3]),
                        L(GEARS_GREEN, "3 close but further2.png", "close far",   FACTORS_7[4]),
                        L(GEARS_GREEN, "2 close again2.png",       "close",       FACTORS_7[5]),
                        L(GEARS_GREEN, "1 close2.png",             "near",        FACTORS_7[6]),
                    }
                },

                // VolcanicWastes — fire/heat, slightly faster
                new BiomeDef
                {
                    biomeName   = "VolcanicWastes",
                    masterSpeed = 0.05f,
                    dimColor    = new Color(0.06f, 0f, 0f, 0.45f),
                    layers      = new[]
                    {
                        L(CAGED_ORANGE, "6 sky2.png",      "sky",     FACTORS_6[0]),
                        L(CAGED_ORANGE, "5 real far2.png", "real far", FACTORS_6[1]),
                        L(CAGED_ORANGE, "4 far2.png",      "far",     FACTORS_6[2]),
                        L(CAGED_ORANGE, "3 far mid2.png",  "far mid", FACTORS_6[3]),
                        L(CAGED_ORANGE, "2 mid2.png",      "mid",     FACTORS_6[4]),
                        L(CAGED_ORANGE, "1 close2.png",    "close",   FACTORS_6[5]),
                    }
                },

                // CoralDepths — water, slowest drift
                new BiomeDef
                {
                    biomeName   = "CoralDepths",
                    masterSpeed = 0.035f,
                    dimColor    = new Color(0f, 0.01f, 0.06f, 0.40f),
                    layers      = new[]
                    {
                        L(CAGED_GREEN, "6 sky1.png",      "sky",     FACTORS_6[0]),
                        L(CAGED_GREEN, "5 real far1.png", "real far", FACTORS_6[1]),
                        L(CAGED_GREEN, "4 far1.png",      "far",     FACTORS_6[2]),
                        L(CAGED_GREEN, "3 far mid1.png",  "far mid", FACTORS_6[3]),
                        L(CAGED_GREEN, "2 mid1.png",      "mid",     FACTORS_6[4]),
                        L(CAGED_GREEN, "1 close1.png",    "close",   FACTORS_6[5]),
                    }
                },

                // SkyPeaks — wind/sky, fastest
                new BiomeDef
                {
                    biomeName   = "SkyPeaks",
                    masterSpeed = 0.055f,
                    dimColor    = new Color(0f, 0.01f, 0.04f, 0.35f),
                    layers      = new[]
                    {
                        L(GEARS_BLUE, "7 sky1.png",               "sky",         FACTORS_7[0]),
                        L(GEARS_BLUE, "6 BG gears1.png",          "BG gears far",FACTORS_7[1]),
                        L(GEARS_BLUE, "5 BG gears1.png",          "BG gears",    FACTORS_7[2]),
                        L(GEARS_BLUE, "4 mid1.png",               "mid",         FACTORS_7[3]),
                        L(GEARS_BLUE, "3 close but further1.png", "close far",   FACTORS_7[4]),
                        L(GEARS_BLUE, "2 close again1.png",       "close",       FACTORS_7[5]),
                        L(GEARS_BLUE, "1 close1.png",             "near",        FACTORS_7[6]),
                    }
                },
            };

            int created = 0, updated = 0;
            foreach (var def in defs)
            {
                string path = $"{CONFIGS_PATH}/{def.biomeName}.asset";
                bool exists = System.IO.File.Exists(path);

                ParallaxBiomeConfig cfg = exists
                    ? AssetDatabase.LoadAssetAtPath<ParallaxBiomeConfig>(path)
                    : ScriptableObject.CreateInstance<ParallaxBiomeConfig>();

                cfg.biomeName   = def.biomeName;
                cfg.masterSpeed = def.masterSpeed;
                cfg.dimColor    = def.dimColor;
                cfg.layers      = new List<ParallaxLayerData>();

                foreach (var ld in def.layers)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ld.texPath);
                    if (tex == null)
                        Debug.LogWarning($"[Parallax Generator] Missing texture: {ld.texPath}");

                    cfg.layers.Add(new ParallaxLayerData
                    {
                        texture      = tex,
                        label        = ld.label,
                        scrollFactor = ld.scrollFactor,
                        tint         = Color.white
                    });
                }

                if (!exists)
                {
                    AssetDatabase.CreateAsset(cfg, path);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(cfg);
                    updated++;
                }

                Debug.Log($"[Parallax Generator] {(exists ? "Updated" : "Created")} {def.biomeName} ({cfg.layers.Count} layers)");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Parallax Generator] Done — {created} created, {updated} updated.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static LayerDef L(string folder, string file, string label, float factor)
            => new LayerDef { texPath = folder + "/" + file, label = label, scrollFactor = factor };

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(CONFIGS_PATH))
                AssetDatabase.CreateFolder("Assets/Resources", "ParallaxConfigs");
        }

        // ── Internal data types ───────────────────────────────────────────────

        private struct BiomeDef
        {
            public string biomeName;
            public float masterSpeed;
            public Color dimColor;
            public LayerDef[] layers;
        }

        private struct LayerDef
        {
            public string texPath;
            public string label;
            public float scrollFactor;
        }
    }
}
