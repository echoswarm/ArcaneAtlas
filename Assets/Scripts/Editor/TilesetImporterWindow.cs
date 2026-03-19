using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
// Suppress CS0618 for TextureImporter.spritesheet — still functional in 2022.3 LTS
#pragma warning disable CS0618

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Editor window for importing Minifantasy tilesets into the Unity project.
    /// Scans the Minifantasy folder, copies PNGs, configures sprite import settings,
    /// and auto-slices at 8x8. Imports ONE PACK AT A TIME to avoid freezing Unity.
    /// No individual Tile assets are created — use Unity's Tile Palette to drag sprites directly.
    /// Arcane Atlas > Tileset Importer
    /// </summary>
    public class TilesetImporterWindow : EditorWindow
    {
        private string minifantasyRoot = "";
        private Vector2 scrollPos;
        private List<PackInfo> discoveredPacks = new List<PackInfo>();
        private bool hasScanned = false;
        private string statusMessage = "";

        // Import settings
        private int pixelsPerUnit = 32;
        private int tileSize = 8; // 8x8 Minifantasy standard
        private FilterMode filterMode = FilterMode.Point;

        private class PackInfo
        {
            public string PackName;
            public string AssetsFolder; // Full path to the *_Assets folder
            public string TilesetFolder;
            public string PropsFolder;
            public bool Selected;
            public bool AlreadyImported;
            public int TilesetCount; // Number of PNG files found
        }

        public static void ShowWindow()
        {
            var window = GetWindow<TilesetImporterWindow>("Tileset Importer");
            window.minSize = new Vector2(480, 600);
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(minifantasyRoot))
            {
                string[] searchPaths = new string[]
                {
                    Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "Minifantasy"),
                    @"C:\Users\" + System.Environment.UserName + @"\OneDrive\Desktop\Minifantasy",
                };

                foreach (string path in searchPaths)
                {
                    if (Directory.Exists(path))
                    {
                        minifantasyRoot = path;
                        break;
                    }
                }
            }
        }

        void OnGUI()
        {
            GUILayout.Label("Minifantasy Tileset Importer", EditorStyles.boldLabel);
            GUILayout.Space(8);

            // Source folder selection
            EditorGUILayout.BeginHorizontal();
            minifantasyRoot = EditorGUILayout.TextField("Minifantasy Root", minifantasyRoot);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Minifantasy Folder", minifantasyRoot, "");
                if (!string.IsNullOrEmpty(folder))
                    minifantasyRoot = folder;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Import settings
            EditorGUILayout.BeginHorizontal();
            pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit);
            tileSize = EditorGUILayout.IntField("Tile Size (px)", tileSize);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Scan button
            if (GUILayout.Button("Scan for Packs", GUILayout.Height(28)))
            {
                ScanForPacks();
            }

            GUILayout.Space(4);

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }

            if (!hasScanned || discoveredPacks.Count == 0)
                return;

            // Pack list
            GUILayout.Label($"Discovered {discoveredPacks.Count} packs:", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // Select all / none buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
                foreach (var p in discoveredPacks) p.Selected = true;
            if (GUILayout.Button("Select None", GUILayout.Width(80)))
                foreach (var p in discoveredPacks) p.Selected = false;
            if (GUILayout.Button("Select Unimported", GUILayout.Width(120)))
                foreach (var p in discoveredPacks) p.Selected = !p.AlreadyImported;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Scrollable pack list
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var pack in discoveredPacks)
            {
                EditorGUILayout.BeginHorizontal();

                pack.Selected = EditorGUILayout.Toggle(pack.Selected, GUILayout.Width(20));

                string label = pack.PackName;
                if (pack.AlreadyImported)
                    label += " (imported)";
                label += $"  [{pack.TilesetCount} files]";

                GUIStyle style = new GUIStyle(EditorStyles.label);
                if (pack.AlreadyImported)
                    style.normal.textColor = new Color(0.5f, 0.8f, 0.5f);

                GUILayout.Label(label, style);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            // Import button
            int selectedCount = discoveredPacks.Count(p => p.Selected);
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"Import Selected ({selectedCount} packs)", GUILayout.Height(32)))
            {
                ImportSelected();
            }
            GUI.enabled = true;
        }

        private void ScanForPacks()
        {
            discoveredPacks.Clear();

            if (string.IsNullOrEmpty(minifantasyRoot) || !Directory.Exists(minifantasyRoot))
            {
                statusMessage = "Invalid Minifantasy root folder!";
                hasScanned = false;
                return;
            }

            var dirs = Directory.GetDirectories(minifantasyRoot, "minifantasy-*");
            foreach (string packDir in dirs)
            {
                string packName = Path.GetFileName(packDir);

                string assetsFolder = FindAssetsFolder(packDir);
                if (assetsFolder == null) continue;

                string tilesetFolder = Path.Combine(assetsFolder, "Tileset");
                string propsFolder = Path.Combine(assetsFolder, "Props");

                bool hasTilesets = Directory.Exists(tilesetFolder);
                bool hasProps = Directory.Exists(propsFolder);
                if (!hasTilesets && !hasProps) continue;

                int count = 0;
                if (hasTilesets) count += Directory.GetFiles(tilesetFolder, "*.png", SearchOption.AllDirectories).Length;
                if (hasProps) count += Directory.GetFiles(propsFolder, "*.png", SearchOption.TopDirectoryOnly).Length;

                string safeName = SanitizeBiomeName(packName);
                bool imported = AssetDatabase.IsValidFolder($"Assets/Art/Sprites/tilesets/{safeName}");

                discoveredPacks.Add(new PackInfo
                {
                    PackName = packName,
                    AssetsFolder = assetsFolder,
                    TilesetFolder = hasTilesets ? tilesetFolder : null,
                    PropsFolder = hasProps ? propsFolder : null,
                    Selected = false,
                    AlreadyImported = imported,
                    TilesetCount = count,
                });
            }

            discoveredPacks.Sort((a, b) => string.Compare(a.PackName, b.PackName));
            hasScanned = true;
            statusMessage = $"Found {discoveredPacks.Count} packs with tilesets/props.";
        }

        /// <summary>
        /// Finds the deepest *_Assets folder inside a pack directory.
        /// Handles: Pack/Inner/Inner_Assets/, Pack/Inner_v1.0/Inner_Assets/, etc.
        /// </summary>
        private string FindAssetsFolder(string packDir)
        {
            var allDirs = Directory.GetDirectories(packDir, "*_Assets", SearchOption.AllDirectories);
            if (allDirs.Length == 0) return null;
            return allDirs.OrderByDescending(d => d.Length).First();
        }

        /// <summary>
        /// Imports all selected packs in 3 bulk phases:
        /// Phase 1: Copy ALL files (suppressed imports)
        /// Phase 2: Single AssetDatabase.Refresh
        /// Phase 3: Configure + slice ALL textures (suppressed reimports), then one final reimport
        /// </summary>
        private void ImportSelected()
        {
            var selected = discoveredPacks.Where(p => p.Selected).ToList();
            if (selected.Count == 0) return;

            int totalFiles = 0;

            // ── Phase 1: Copy all PNG files across all packs ──
            EditorUtility.DisplayProgressBar("Tileset Importer", "Phase 1/3: Copying files...", 0f);
            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < selected.Count; i++)
                {
                    var pack = selected[i];
                    string safeName = SanitizeBiomeName(pack.PackName);
                    string destBase = $"Assets/Art/Sprites/tilesets/{safeName}";
                    EnsureFolder(destBase);

                    EditorUtility.DisplayProgressBar("Tileset Importer",
                        $"Phase 1/3: Copying {pack.PackName} ({i + 1}/{selected.Count})...",
                        (float)i / selected.Count * 0.3f);

                    if (pack.TilesetFolder != null)
                        totalFiles += CopyPNGs(pack.TilesetFolder, destBase, SearchOption.AllDirectories);

                    if (pack.PropsFolder != null)
                        totalFiles += CopyPNGs(pack.PropsFolder, destBase, SearchOption.TopDirectoryOnly);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[TilesetImporter] Phase 1 done: copied {totalFiles} files from {selected.Count} packs");

            // ── Phase 2: Single refresh to register all new files ──
            EditorUtility.DisplayProgressBar("Tileset Importer",
                $"Phase 2/3: Refreshing asset database ({totalFiles} files)...", 0.35f);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log("[TilesetImporter] Phase 2 done: asset database refreshed");

            // ── Phase 3: Configure + slice all textures in one suppressed batch ──
            // StartAssetEditing defers all reimports until StopAssetEditing
            EditorUtility.DisplayProgressBar("Tileset Importer", "Phase 3/3: Configuring sprites...", 0.5f);

            int totalTextures = 0;
            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < selected.Count; i++)
                {
                    var pack = selected[i];
                    string safeName = SanitizeBiomeName(pack.PackName);
                    string folderPath = $"Assets/Art/Sprites/tilesets/{safeName}";

                    EditorUtility.DisplayProgressBar("Tileset Importer",
                        $"Phase 3/3: Slicing {pack.PackName} ({i + 1}/{selected.Count})...",
                        0.5f + (float)i / selected.Count * 0.4f);

                    totalTextures += ConfigureAndSlicePack(folderPath);
                    pack.AlreadyImported = true;
                }
            }
            finally
            {
                // This ONE call triggers bulk reimport of all modified textures at once
                EditorUtility.DisplayProgressBar("Tileset Importer",
                    $"Phase 3/3: Bulk reimporting {totalTextures} textures...", 0.92f);
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            statusMessage = $"Done! Imported {totalFiles} files ({totalTextures} textures) from {selected.Count} packs.";
            Debug.Log($"[TilesetImporter] {statusMessage}");
        }

        /// <summary>
        /// Copies PNGs from source folder to Unity asset folder, flattening subfolders into filename prefixes.
        /// </summary>
        private int CopyPNGs(string sourceFolder, string destBase, SearchOption searchOption)
        {
            var files = Directory.GetFiles(sourceFolder, "*.png", searchOption);
            int count = 0;

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                // Skip non-tileset files (guidelines, licenses, readmes, previews)
                string lower = fileName.ToLowerInvariant();
                if (lower.Contains("guideline") || lower.Contains("license") ||
                    lower.Contains("readme") || lower.Contains("preview") ||
                    lower.Contains("example") || lower.Contains("_use_"))
                    continue;
                string relativePath = file.Substring(sourceFolder.Length + 1).Replace('\\', '/');
                string subFolder = Path.GetDirectoryName(relativePath)?.Replace('\\', '/').Replace('/', '_') ?? "";

                string destFileName = string.IsNullOrEmpty(subFolder) ? fileName : $"{subFolder}_{fileName}";
                string destPath = Path.Combine(destBase, destFileName).Replace('\\', '/');

                if (File.Exists(destPath))
                    File.Delete(destPath);

                File.Copy(file, destPath);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Configures all textures in a pack folder: sprite type, Point filter,
        /// no compression, 32 PPU, Multiple mode, 8x8 grid slicing.
        /// Does NOT call SaveAndReimport — caller must wrap in StartAssetEditing/StopAssetEditing
        /// so all reimports happen in one bulk operation.
        /// Returns number of textures configured.
        /// </summary>
        private int ConfigureAndSlicePack(string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            int count = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = filterMode;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                // Auto-slice into 8x8 grid — read dimensions from PNG header
                int texWidth = 0, texHeight = 0;
                ReadPngDimensions(path, out texWidth, out texHeight);

                // Auto-slice into 8x8 grid
                if (texWidth >= tileSize && texHeight >= tileSize)
                {
                    var spriteSheet = new List<SpriteMetaData>();
                    int cols = texWidth / tileSize;
                    int rows = texHeight / tileSize;

                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < cols; x++)
                        {
                            var meta = new SpriteMetaData
                            {
                                name = $"{Path.GetFileNameWithoutExtension(path)}_{y}_{x}",
                                rect = new Rect(x * tileSize, (rows - 1 - y) * tileSize, tileSize, tileSize),
                                alignment = (int)SpriteAlignment.Center,
                                pivot = new Vector2(0.5f, 0.5f),
                            };
                            spriteSheet.Add(meta);
                        }
                    }

                    importer.spritesheet = spriteSheet.ToArray();
                }

                AssetDatabase.WriteImportSettingsIfDirty(path);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Reads PNG width/height from the IHDR chunk without loading into Unity.
        /// </summary>
        private void ReadPngDimensions(string assetPath, out int width, out int height)
        {
            width = 0;
            height = 0;
            string fullPath = Path.Combine(Application.dataPath, "..", assetPath).Replace('\\', '/');
            if (!File.Exists(fullPath)) return;

            try
            {
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    // PNG signature (8) + IHDR chunk length (4) + "IHDR" (4) = offset 16
                    fs.Seek(16, SeekOrigin.Begin);
                    byte[] wBytes = br.ReadBytes(4);
                    byte[] hBytes = br.ReadBytes(4);
                    if (System.BitConverter.IsLittleEndian)
                    {
                        System.Array.Reverse(wBytes);
                        System.Array.Reverse(hBytes);
                    }
                    width = System.BitConverter.ToInt32(wBytes, 0);
                    height = System.BitConverter.ToInt32(hBytes, 0);
                }
            }
            catch { /* fallback: dimensions stay 0, skip slicing */ }
        }

        private string SanitizeBiomeName(string packName)
        {
            // "minifantasy-ancient-forests" → "ancient_forests"
            string name = packName;
            if (name.StartsWith("minifantasy-"))
                name = name.Substring("minifantasy-".Length);
            return name.Replace('-', '_');
        }

        private void EnsureFolder(string path)
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
