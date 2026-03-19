using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditorInternal;
using ArcaneAtlas.Data;

#pragma warning disable CS0618 // spritesheet deprecation

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Scans Minifantasy packs for character sprite sheets (heroes, creatures, NPCs, monsters).
    /// Imports PNGs, auto-slices into animation frames, and creates CharacterDef ScriptableObjects.
    /// Detects Walk and Idle animations from filenames.
    /// Frame size = sheet height (horizontal strips, all frames same size).
    /// Arcane Atlas > Character Importer
    /// </summary>
    public class CharacterSpriteImporter : EditorWindow
    {
        private string minifantasyRoot = "";
        private Vector2 scrollPos;
        private List<CharEntry> discovered = new List<CharEntry>();
        private bool hasScanned = false;
        private string statusMessage = "";

        private class CharEntry
        {
            public string CharacterName;
            public string PackPath;      // Full path to character's anim folder
            public string IdlePath;      // Full path to Idle PNG
            public string WalkPath;      // Full path to Walk PNG
            public bool Selected;
            public bool AlreadyImported;
        }

        [MenuItem("Arcane Atlas/Character Importer", false, 37)]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterSpriteImporter>("Character Importer");
            window.minSize = new Vector2(500, 500);
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
            GUILayout.Label("Character Sprite Importer", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            minifantasyRoot = EditorGUILayout.TextField("Minifantasy Root", minifantasyRoot);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Minifantasy Folder", minifantasyRoot, "");
                if (!string.IsNullOrEmpty(folder)) minifantasyRoot = folder;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            if (GUILayout.Button("Scan for Characters", GUILayout.Height(28)))
                ScanForCharacters();

            if (!string.IsNullOrEmpty(statusMessage))
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);

            if (!hasScanned || discovered.Count == 0) return;

            GUILayout.Space(4);
            GUILayout.Label($"Found {discovered.Count} characters:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
                foreach (var c in discovered) c.Selected = true;
            if (GUILayout.Button("Select None", GUILayout.Width(80)))
                foreach (var c in discovered) c.Selected = false;
            if (GUILayout.Button("Select Unimported", GUILayout.Width(120)))
                foreach (var c in discovered) c.Selected = !c.AlreadyImported;
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var entry in discovered)
            {
                EditorGUILayout.BeginHorizontal();
                entry.Selected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(20));

                string label = entry.CharacterName;
                if (entry.AlreadyImported) label += " (imported)";
                string anims = "";
                if (entry.IdlePath != null) anims += "Idle ";
                if (entry.WalkPath != null) anims += "Walk";
                label += $"  [{anims.Trim()}]";

                GUIStyle style = new GUIStyle(EditorStyles.label);
                if (entry.AlreadyImported)
                    style.normal.textColor = new Color(0.5f, 0.8f, 0.5f);
                GUILayout.Label(label, style);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);
            int selectedCount = discovered.Count(c => c.Selected);
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"Import Selected ({selectedCount} characters)", GUILayout.Height(32)))
                ImportSelected();
            GUI.enabled = true;
        }

        // ════════════════════════════════════════════
        //  SCANNING
        // ════════════════════════════════════════════

        private void ScanForCharacters()
        {
            discovered.Clear();

            if (string.IsNullOrEmpty(minifantasyRoot) || !Directory.Exists(minifantasyRoot))
            {
                statusMessage = "Invalid Minifantasy root folder!";
                hasScanned = false;
                return;
            }

            // Find all folders that contain animation PNGs
            // Pattern 1: General_Animations/{Name}Walk.png, {Name}Idle.png
            // Pattern 2: On_Land_Animations/{Name}Walk.png
            // Pattern 3: Premade_NPCs/{Name}/Walk.png, Idle.png

            var animFolders = new List<string>();

            // Search for General_Animations folders
            foreach (var dir in Directory.GetDirectories(minifantasyRoot, "General_Animations", SearchOption.AllDirectories))
                animFolders.Add(dir);

            // Search for On_Land_Animations folders
            foreach (var dir in Directory.GetDirectories(minifantasyRoot, "On_Land_Animations", SearchOption.AllDirectories))
            {
                // These sometimes have subfolders per variant
                var subDirs = Directory.GetDirectories(dir);
                if (subDirs.Length > 0)
                    animFolders.AddRange(subDirs);
                else
                    animFolders.Add(dir);
            }

            // Search NPC packs — each NPC has their own folder with Walk.png etc.
            foreach (var dir in Directory.GetDirectories(minifantasyRoot, "Premade_NPCs", SearchOption.AllDirectories))
            {
                foreach (var npcDir in Directory.GetDirectories(dir))
                    animFolders.Add(npcDir);
            }

            foreach (string animDir in animFolders)
            {
                var pngs = Directory.GetFiles(animDir, "*.png", SearchOption.TopDirectoryOnly);
                if (pngs.Length == 0) continue;

                // Find Walk and Idle PNGs (case-insensitive, skip shadows)
                string walkPng = pngs.FirstOrDefault(p =>
                    Path.GetFileName(p).ToLower().Contains("walk") &&
                    !Path.GetFileName(p).ToLower().Contains("shadow"));

                // Prefer IdleStart (full body standing) over Idle (may be partial/behind-counter)
                // Fallback chain: IdleStart → Idle (excluding IdleEnd)
                string idlePng = pngs.FirstOrDefault(p =>
                {
                    string fn = Path.GetFileName(p).ToLower();
                    return fn.Contains("idlestart") && !fn.Contains("shadow");
                });
                if (idlePng == null)
                {
                    idlePng = pngs.FirstOrDefault(p =>
                    {
                        string fn = Path.GetFileName(p).ToLower();
                        return fn.Contains("idle") && !fn.Contains("shadow") && !fn.Contains("idleend");
                    });
                }

                // Must have at least walk or idle
                if (walkPng == null && idlePng == null) continue;

                // Derive character name from folder path
                string charName = DeriveCharacterName(animDir);
                string safeName = SanitizeName(charName);

                // Check if already imported
                bool imported = AssetDatabase.IsValidFolder($"Assets/Art/Sprites/characters/{safeName}");

                discovered.Add(new CharEntry
                {
                    CharacterName = charName,
                    PackPath = animDir,
                    IdlePath = idlePng,
                    WalkPath = walkPng,
                    Selected = false,
                    AlreadyImported = imported,
                });
            }

            // Sort and deduplicate by name
            discovered = discovered
                .GroupBy(c => SanitizeName(c.CharacterName))
                .Select(g => g.First())
                .OrderBy(c => c.CharacterName)
                .ToList();

            hasScanned = true;
            statusMessage = $"Found {discovered.Count} characters with Walk/Idle animations.";
        }

        private string DeriveCharacterName(string animDir)
        {
            // Walk up from anim folder to find the character name
            // Patterns:
            //   .../Bard/General_Animations → "Bard"
            //   .../Frogfolk_Warrior (under On_Land_Animations) → "Frogfolk_Warrior"
            //   .../Premade_NPCs/Alchemist → "Alchemist"
            string dirName = Path.GetFileName(animDir);

            if (dirName == "General_Animations" || dirName == "On_Land_Animations")
                return Path.GetFileName(Path.GetDirectoryName(animDir));

            return dirName;
        }

        // ════════════════════════════════════════════
        //  IMPORTING
        // ════════════════════════════════════════════

        private void ImportSelected()
        {
            var selected = discovered.Where(c => c.Selected).ToList();
            if (selected.Count == 0) return;

            int imported = 0;

            for (int i = 0; i < selected.Count; i++)
            {
                var entry = selected[i];
                string safeName = SanitizeName(entry.CharacterName);

                EditorUtility.DisplayProgressBar("Character Importer",
                    $"Importing {entry.CharacterName} ({i + 1}/{selected.Count})...",
                    (float)i / selected.Count);

                // Sheets go into Resources for runtime loading via Resources.LoadAll
                string charFolder = $"Assets/Resources/CharacterFrames/{safeName}";
                EnsureFolder(charFolder);

                // Copy and configure Walk sheet as Multiple + auto-slice
                int walkSprites = 0;
                if (entry.WalkPath != null)
                    walkSprites = ImportSheetAsMultiple(entry.WalkPath, charFolder, $"{safeName}_Walk");

                // Copy and configure Idle sheet as Multiple + auto-slice
                int idleSprites = 0;
                if (entry.IdlePath != null)
                    idleSprites = ImportSheetAsMultiple(entry.IdlePath, charFolder, $"{safeName}_Idle");

                // Create/update CharacterDef
                string defFolder = "Assets/Resources/Characters";
                EnsureFolder(defFolder);
                string defPath = $"{defFolder}/{safeName}.asset";

                var charDef = AssetDatabase.LoadAssetAtPath<CharacterDef>(defPath);
                if (charDef == null)
                {
                    charDef = ScriptableObject.CreateInstance<CharacterDef>();
                    AssetDatabase.CreateAsset(charDef, defPath);
                }
                charDef.CharacterName = safeName;
                charDef.PackSource = entry.PackPath;
                charDef.IdleFrameRate = 4f;  // Relaxed idle speed
                charDef.WalkFrameRate = 8f;
                charDef.ClearCache();
                EditorUtility.SetDirty(charDef);

                entry.AlreadyImported = true;
                imported++;

                Debug.Log($"[CharImporter] Imported {entry.CharacterName}: Idle={idleSprites} sub-sprites, Walk={walkSprites} sub-sprites");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            statusMessage = $"Imported {imported} characters.";
            Debug.Log($"[CharImporter] {statusMessage}");
        }

        /// <summary>
        /// Copies a sprite sheet PNG into Resources, imports as Multiple mode,
        /// and auto-slices it. The sub-sprites become animation frames.
        /// Returns the number of sub-sprites detected.
        /// </summary>
        private int ImportSheetAsMultiple(string sourcePath, string destFolder, string assetName)
        {
            string destPath = $"{destFolder}/{assetName}.png";
            string fullDest = Path.Combine(Application.dataPath, "..", destPath).Replace('\\', '/');

            // Copy file
            if (File.Exists(fullDest)) File.Delete(fullDest);
            File.Copy(sourcePath, fullDest);

            // Import and configure
            AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
            if (importer == null) return 0;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; // Match tile PPU for consistent scale
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // Use Unity's automatic sprite detection to find individual character frames
            // This detects non-transparent regions and creates sprite rects for each
            importer.isReadable = true;
            importer.SaveAndReimport();

            // Load texture to run auto-detection
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
            if (texture == null) return 0;

            // Generate automatic sprite rects based on transparency
            // minimumSpriteSize=1 to avoid cropping small details like feet/legs
            // extrudeSize=0 for pixel-perfect edges
            var rects = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, 1, 0);
            if (rects == null || rects.Length == 0)
            {
                Debug.LogWarning($"[CharImporter] No sprites detected in {assetName}");
                return 0;
            }

            // Create sprite metadata from detected rects
            var metas = new SpriteMetaData[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = $"{assetName}_{i}",
                    rect = rects[i],
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }
            importer.spritesheet = metas;
            importer.SaveAndReimport();

            // Verify sub-sprites loaded
            var subSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(destPath);
            int count = 0;
            foreach (var s in subSprites)
                if (s is Sprite) count++;

            Debug.Log($"[CharImporter] {assetName}: detected {rects.Length} rects, loaded {count} sprites");
            return count;
        }

        private void ReadPngDimensions(string assetPath, out int width, out int height)
        {
            width = 0; height = 0;
            string fullPath = Path.Combine(Application.dataPath, "..", assetPath).Replace('\\', '/');
            if (!File.Exists(fullPath)) return;
            try
            {
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
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
            catch { }
        }

        private string SanitizeName(string name)
        {
            return name.Replace(' ', '_').Replace('-', '_').Replace(".", "");
        }

        private void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
