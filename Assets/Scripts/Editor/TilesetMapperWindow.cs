using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Visual editor for mapping Minifantasy sprites to TileKeys.
    /// Left panel: TileKey slots grouped by layer.
    /// Right panel: browseable sprite grid from the selected biome.
    /// Click a TileKey slot, then click a sprite to assign it.
    /// Saves as a TilePaletteDef ScriptableObject.
    /// Arcane Atlas > Tileset Mapper
    /// </summary>
    public class TilesetMapperWindow : EditorWindow
    {
        // ── State ──
        private string[] biomeNames;
        private int selectedBiomeIndex = 0;
        private string paletteName = "NewPalette";
        private TilePaletteDef editingPalette;

        // Sprite browsing
        private List<Sprite> biomeSprites = new List<Sprite>();
        private Vector2 spriteScrollPos;
        private int spriteGridColumns = 12;
        private float spritePreviewSize = 40f;
        private string spriteFilter = "";

        // TileKey mapping
        private Dictionary<TileKey, Sprite> mappings = new Dictionary<TileKey, Sprite>();
        private TileKey? selectedKey = null;
        private Vector2 keyScrollPos;

        // Character sprite slots (legacy — used when no CharacterDefs)
        private Sprite playerSprite;
        private Sprite npcSprite;
        private Sprite bossSprite;
        private enum CharSlot { None, Player, Npc, Boss }
        private CharSlot selectedCharSlot = CharSlot.None;

        // CharacterDef dropdowns
        private CharacterDef[] availableCharacters;
        private string[] characterNames;
        private int playerCharIndex = 0;
        private int npcCharIndex = 0;
        private int bossCharIndex = 0;

        // Layout
        private float leftPanelWidth = 280f;

        // TileKey groups for organized display
        private static readonly (string Group, TileKey[] Keys)[] KEY_GROUPS = new[]
        {
            ("Ground", new[] {
                TileKey.Ground, TileKey.GroundAlt, TileKey.Path
            }),
            ("Walls", new[] {
                TileKey.WallN, TileKey.WallS, TileKey.WallE, TileKey.WallW,
                TileKey.WallCornerNW, TileKey.WallCornerNE, TileKey.WallCornerSW, TileKey.WallCornerSE,
                TileKey.WallInnerNW, TileKey.WallInnerNE, TileKey.WallInnerSW, TileKey.WallInnerSE,
            }),
            ("Doors", new[] {
                TileKey.DoorN, TileKey.DoorS, TileKey.DoorE, TileKey.DoorW,
            }),
            ("Detail", new[] {
                TileKey.GrassDetail, TileKey.FlowerDetail, TileKey.CrackDetail, TileKey.MossDetail,
            }),
            ("Shadow", new[] {
                TileKey.ShadowWallN, TileKey.ShadowWallW, TileKey.ShadowCornerNW, TileKey.ShadowFull,
            }),
            ("Props Below", new[] {
                TileKey.TreeBase, TileKey.RockSmall, TileKey.RockLarge, TileKey.BushBase,
                TileKey.Crate, TileKey.Chest, TileKey.Water, TileKey.WaterEdge,
            }),
            ("Props Above", new[] {
                TileKey.TreeCrown, TileKey.BushTop, TileKey.RockTop,
            }),
            ("Overlay", new[] {
                TileKey.OverlayVines, TileKey.OverlayFog,
            }),
            ("Collision", new[] {
                TileKey.CollisionSolid, TileKey.CollisionWater,
            }),
        };

        [MenuItem("Arcane Atlas/Tileset Mapper", false, 36)]
        public static void ShowWindow()
        {
            var window = GetWindow<TilesetMapperWindow>("Tileset Mapper");
            window.minSize = new Vector2(700, 500);
        }

        void OnEnable()
        {
            RefreshBiomeList();
            RefreshCharacterList();
        }

        void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════
        //  TOOLBAR
        // ════════════════════════════════════════════

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Biome selector
            EditorGUILayout.LabelField("Biome:", GUILayout.Width(45));
            if (biomeNames != null && biomeNames.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup(selectedBiomeIndex, biomeNames, GUILayout.Width(180));
                if (newIndex != selectedBiomeIndex)
                {
                    selectedBiomeIndex = newIndex;
                    LoadBiomeSprites();
                }
            }

            GUILayout.Space(10);

            // Palette name
            EditorGUILayout.LabelField("Palette:", GUILayout.Width(50));
            paletteName = EditorGUILayout.TextField(paletteName, GUILayout.Width(140));

            GUILayout.Space(10);

            // Load existing
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)))
                LoadExistingPalette();

            // Save
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                SavePalette();

            // Clear
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Clear Mappings", "Clear all sprite assignments?", "Yes", "Cancel"))
                    mappings.Clear();
            }

            GUILayout.FlexibleSpace();

            // Stats
            int mapped = mappings.Count(m => m.Value != null);
            int total = 0;
            foreach (var g in KEY_GROUPS) total += g.Keys.Length;
            EditorGUILayout.LabelField($"Mapped: {mapped}/{total}", GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════
        //  LEFT PANEL — TileKey Slots
        // ════════════════════════════════════════════

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(leftPanelWidth));

            EditorGUILayout.LabelField("Assignments", EditorStyles.boldLabel);

            if (selectedKey.HasValue)
            {
                EditorGUILayout.HelpBox($"Selected: {selectedKey.Value}\nClick a sprite on the right to assign it.", MessageType.Info);
            }
            else if (selectedCharSlot != CharSlot.None)
            {
                EditorGUILayout.HelpBox($"Selected: {selectedCharSlot} Sprite\nClick a sprite on the right to assign it.", MessageType.Info);
            }

            keyScrollPos = EditorGUILayout.BeginScrollView(keyScrollPos);

            // ── Character Definitions (dropdown selectors) ──
            EditorGUILayout.LabelField("Characters", EditorStyles.miniBoldLabel);

            if (availableCharacters != null && availableCharacters.Length > 0)
            {
                DrawCharDropdown("Player", ref playerCharIndex);
                DrawCharDropdown("NPC", ref npcCharIndex);
                DrawCharDropdown("Boss", ref bossCharIndex);

                if (GUILayout.Button("Refresh Character List", EditorStyles.miniButton))
                    RefreshCharacterList();
            }
            else
            {
                EditorGUILayout.HelpBox("No characters imported yet.\nUse Arcane Atlas > Character Importer first.", MessageType.Info);
                if (GUILayout.Button("Refresh", EditorStyles.miniButton))
                    RefreshCharacterList();

                // Fallback: manual sprite slots
                EditorGUILayout.LabelField("Manual Sprites (fallback)", EditorStyles.miniLabel);
                DrawCharSlot("Player", CharSlot.Player, ref playerSprite);
                DrawCharSlot("NPC", CharSlot.Npc, ref npcSprite);
                DrawCharSlot("Boss", CharSlot.Boss, ref bossSprite);
            }
            GUILayout.Space(6);

            foreach (var group in KEY_GROUPS)
            {
                EditorGUILayout.LabelField(group.Group, EditorStyles.miniBoldLabel);

                foreach (var key in group.Keys)
                {
                    DrawKeySlot(key);
                }

                GUILayout.Space(6);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawKeySlot(TileKey key)
        {
            bool isSelected = selectedKey.HasValue && selectedKey.Value == key;
            Sprite assigned = mappings.ContainsKey(key) ? mappings[key] : null;

            // Highlight selected row
            Color prevBg = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Sprite preview (32x32)
            Rect previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
            if (assigned != null)
            {
                DrawSpritePreview(previewRect, assigned);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
            }

            // Key name
            GUILayout.Space(4);
            EditorGUILayout.LabelField(key.ToString(), GUILayout.Width(140), GUILayout.Height(32));

            // Select button
            if (GUILayout.Button(isSelected ? "●" : "○", GUILayout.Width(24), GUILayout.Height(32)))
            {
                selectedKey = isSelected ? (TileKey?)null : key;
                selectedCharSlot = CharSlot.None; // Deselect character slot
            }

            // Clear button
            if (assigned != null)
            {
                if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(32)))
                {
                    mappings.Remove(key);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevBg;
        }

        private void DrawCharSlot(string label, CharSlot slot, ref Sprite sprite)
        {
            bool isSelected = selectedCharSlot == slot;

            Color prevBg = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // Orange highlight for chars

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Rect previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
            if (sprite != null)
                DrawSpritePreview(previewRect, sprite);
            else
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.Space(4);
            EditorGUILayout.LabelField(label, GUILayout.Width(140), GUILayout.Height(32));

            if (GUILayout.Button(isSelected ? "●" : "○", GUILayout.Width(24), GUILayout.Height(32)))
            {
                selectedCharSlot = isSelected ? CharSlot.None : slot;
                selectedKey = null; // Deselect tile key
            }

            if (sprite != null)
            {
                if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(32)))
                    sprite = null;
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevBg;
        }

        private void DrawCharDropdown(string label, ref int index)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Preview sprite
            Rect previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
            if (index > 0 && index < availableCharacters.Length && availableCharacters[index] != null)
            {
                var preview = availableCharacters[index].PreviewSprite;
                if (preview != null) DrawSpritePreview(previewRect, preview);
                else EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
            }

            GUILayout.Space(4);
            EditorGUILayout.LabelField(label, GUILayout.Width(50), GUILayout.Height(32));
            index = EditorGUILayout.Popup(index, characterNames, GUILayout.Height(32));

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshCharacterList()
        {
            var chars = new List<CharacterDef>();

            // Search broadly for CharacterDef assets
            var guids = AssetDatabase.FindAssets("t:CharacterDef");
            foreach (string guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<CharacterDef>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && !string.IsNullOrEmpty(def.CharacterName) && def.PreviewSprite != null)
                    chars.Add(def);
            }

            chars.Sort((a, b) => string.Compare(a.CharacterName, b.CharacterName));

            // Insert "None" at index 0
            availableCharacters = new CharacterDef[chars.Count + 1];
            characterNames = new string[chars.Count + 1];
            availableCharacters[0] = null;
            characterNames[0] = "(None)";

            for (int i = 0; i < chars.Count; i++)
            {
                availableCharacters[i + 1] = chars[i];
                characterNames[i + 1] = chars[i].CharacterName;
            }
        }

        private int FindCharIndex(CharacterDef def)
        {
            if (def == null || availableCharacters == null) return 0;
            for (int i = 1; i < availableCharacters.Length; i++)
            {
                if (availableCharacters[i] == def) return i;
            }
            return 0;
        }

        // ════════════════════════════════════════════
        //  RIGHT PANEL — Sprite Browser
        // ════════════════════════════════════════════

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            // Filter and size controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel, GUILayout.Width(50));
            spriteFilter = EditorGUILayout.TextField(spriteFilter, EditorStyles.toolbarSearchField);
            EditorGUILayout.LabelField("Size:", GUILayout.Width(32));
            spritePreviewSize = EditorGUILayout.Slider(spritePreviewSize, 24f, 80f, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            if (biomeSprites.Count == 0)
            {
                EditorGUILayout.HelpBox("Select a biome and sprites will appear here.\nIf empty, run the Tileset Importer first.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Filter sprites
            var filtered = biomeSprites;
            if (!string.IsNullOrEmpty(spriteFilter))
                filtered = biomeSprites.Where(s => s.name.ToLowerInvariant().Contains(spriteFilter.ToLowerInvariant())).ToList();

            EditorGUILayout.LabelField($"{filtered.Count} sprites ({biomeSprites.Count} total)", EditorStyles.miniLabel);

            spriteScrollPos = EditorGUILayout.BeginScrollView(spriteScrollPos);

            // Calculate grid
            float availWidth = position.width - leftPanelWidth - 30f;
            spriteGridColumns = Mathf.Max(1, Mathf.FloorToInt(availWidth / (spritePreviewSize + 4f)));

            int col = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < filtered.Count; i++)
            {
                var sprite = filtered[i];
                if (col >= spriteGridColumns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    col = 0;
                }

                // Check if this sprite is already mapped to something
                bool isMapped = mappings.ContainsValue(sprite);

                Rect btnRect = GUILayoutUtility.GetRect(spritePreviewSize + 2, spritePreviewSize + 2,
                    GUILayout.Width(spritePreviewSize + 2), GUILayout.Height(spritePreviewSize + 2));

                // Tint mapped sprites green
                if (isMapped)
                    EditorGUI.DrawRect(btnRect, new Color(0.2f, 0.5f, 0.2f, 0.3f));

                // Draw sprite
                Rect spriteRect = new Rect(btnRect.x + 1, btnRect.y + 1, spritePreviewSize, spritePreviewSize);
                DrawSpritePreview(spriteRect, sprite);

                // Click to assign
                if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
                {
                    if (selectedKey.HasValue)
                    {
                        mappings[selectedKey.Value] = sprite;
                        AdvanceToNextUnmappedKey(selectedKey.Value);
                        Event.current.Use();
                        Repaint();
                    }
                    else if (selectedCharSlot != CharSlot.None)
                    {
                        // Assign to character sprite slot
                        switch (selectedCharSlot)
                        {
                            case CharSlot.Player: playerSprite = sprite; break;
                            case CharSlot.Npc: npcSprite = sprite; break;
                            case CharSlot.Boss: bossSprite = sprite; break;
                        }
                        selectedCharSlot = CharSlot.None;
                        Event.current.Use();
                        Repaint();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Nothing Selected",
                            $"Click a ○ button on the left panel first to select which slot to assign.\n\nSprite: {sprite.name}",
                            "OK");
                    }
                }

                // Tooltip on hover
                if (btnRect.Contains(Event.current.mousePosition))
                {
                    string tooltip = sprite.name;
                    if (isMapped)
                    {
                        var mappedKey = mappings.FirstOrDefault(m => m.Value == sprite).Key;
                        tooltip += $"\n(mapped to {mappedKey})";
                    }
                    GUI.Label(new Rect(Event.current.mousePosition.x + 10, Event.current.mousePosition.y - 20, 300, 40),
                        tooltip, EditorStyles.helpBox);
                }

                col++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════

        private void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;

            Rect texCoords = new Rect(
                sprite.rect.x / sprite.texture.width,
                sprite.rect.y / sprite.texture.height,
                sprite.rect.width / sprite.texture.width,
                sprite.rect.height / sprite.texture.height
            );

            GUI.DrawTextureWithTexCoords(rect, sprite.texture, texCoords);
        }

        private void AdvanceToNextUnmappedKey(TileKey current)
        {
            // Find the group containing the current key
            foreach (var group in KEY_GROUPS)
            {
                bool foundCurrent = false;
                foreach (var key in group.Keys)
                {
                    if (key == current)
                    {
                        foundCurrent = true;
                        continue;
                    }
                    if (foundCurrent && (!mappings.ContainsKey(key) || mappings[key] == null))
                    {
                        selectedKey = key;
                        return;
                    }
                }

                // If we found current but all remaining in group are mapped, clear selection
                if (foundCurrent)
                {
                    selectedKey = null;
                    return;
                }
            }
        }

        private void RefreshBiomeList()
        {
            string tilesetsRoot = "Assets/Art/Sprites/tilesets";
            if (!AssetDatabase.IsValidFolder(tilesetsRoot))
            {
                biomeNames = new string[0];
                return;
            }

            var dirs = Directory.GetDirectories(
                Path.Combine(Application.dataPath, "Art/Sprites/tilesets"));
            biomeNames = dirs.Select(d => Path.GetFileName(d)).OrderBy(n => n).ToArray();

            if (biomeNames.Length > 0 && biomeSprites.Count == 0)
                LoadBiomeSprites();
        }

        private void LoadBiomeSprites()
        {
            biomeSprites.Clear();
            if (biomeNames == null || biomeNames.Length == 0) return;

            string biomeName = biomeNames[selectedBiomeIndex];
            string folder = $"Assets/Art/Sprites/tilesets/{biomeName}";

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite)
                    {
                        // Skip full-sheet sprites (same size as texture)
                        if (sprite.rect.width <= 16 && sprite.rect.height <= 16)
                            biomeSprites.Add(sprite);
                    }
                }
            }

            // Sort by name for consistent ordering
            biomeSprites.Sort((a, b) => string.Compare(a.name, b.name));

            paletteName = biomeName;
        }

        // ════════════════════════════════════════════
        //  SAVE / LOAD
        // ════════════════════════════════════════════

        private void SavePalette()
        {
            if (string.IsNullOrEmpty(paletteName))
            {
                EditorUtility.DisplayDialog("Error", "Enter a palette name first.", "OK");
                return;
            }

            int mappedCount = mappings.Count(m => m.Value != null);
            if (mappedCount == 0)
            {
                EditorUtility.DisplayDialog("Error", "No sprites mapped. Assign sprites to TileKeys first.", "OK");
                return;
            }

            string tileFolder = $"Assets/Art/Tiles/mapped_{paletteName}";
            string paletteFolder = "Assets/Resources/TilePalettes";
            EnsureFolder(tileFolder);
            EnsureFolder(paletteFolder);

            var tileMappings = new List<TileMapping>();

            foreach (var kvp in mappings)
            {
                if (kvp.Value == null) continue;

                TileKey key = kvp.Key;
                Sprite sprite = kvp.Value;

                // Create or update a Tile asset for this mapping
                string tilePath = $"{tileFolder}/tile_{key}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                tile.color = Color.white;
                EditorUtility.SetDirty(tile);

                tileMappings.Add(new TileMapping { Key = key, Tile = tile });
            }

            // Create or update the TilePaletteDef
            string defPath = $"{paletteFolder}/{paletteName}.asset";
            var paletteDef = AssetDatabase.LoadAssetAtPath<TilePaletteDef>(defPath);
            if (paletteDef == null)
            {
                paletteDef = ScriptableObject.CreateInstance<TilePaletteDef>();
                AssetDatabase.CreateAsset(paletteDef, defPath);
            }
            paletteDef.PaletteName = paletteName;
            paletteDef.Mappings = tileMappings.ToArray();
            paletteDef.PlayerSprite = playerSprite;
            paletteDef.NpcSprite = npcSprite;
            paletteDef.BossSprite = bossSprite;

            // Save CharacterDef references
            paletteDef.PlayerCharacter = (playerCharIndex > 0 && playerCharIndex < availableCharacters.Length) ? availableCharacters[playerCharIndex] : null;
            paletteDef.NpcCharacter = (npcCharIndex > 0 && npcCharIndex < availableCharacters.Length) ? availableCharacters[npcCharIndex] : null;
            paletteDef.BossCharacter = (bossCharIndex > 0 && bossCharIndex < availableCharacters.Length) ? availableCharacters[bossCharIndex] : null;
            EditorUtility.SetDirty(paletteDef);

            AssetDatabase.SaveAssets();

            editingPalette = paletteDef;
            Debug.Log($"[TilesetMapper] Saved palette '{paletteName}' with {tileMappings.Count} mappings to {defPath}");
            EditorUtility.DisplayDialog("Saved",
                $"Palette '{paletteName}' saved with {tileMappings.Count} mappings.\n\nAssign it to ExplorationManager.tilePalette to use at runtime.",
                "OK");
        }

        private void LoadExistingPalette()
        {
            string path = EditorUtility.OpenFilePanel("Load TilePaletteDef", "Assets/Resources/TilePalettes", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convert absolute path to relative
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            var palette = AssetDatabase.LoadAssetAtPath<TilePaletteDef>(path);
            if (palette == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load TilePaletteDef from that file.", "OK");
                return;
            }

            editingPalette = palette;
            paletteName = palette.PaletteName;
            mappings.Clear();

            // Restore character sprites (legacy)
            playerSprite = palette.PlayerSprite;
            npcSprite = palette.NpcSprite;
            bossSprite = palette.BossSprite;

            // Restore CharacterDef selections
            RefreshCharacterList();
            playerCharIndex = FindCharIndex(palette.PlayerCharacter);
            npcCharIndex = FindCharIndex(palette.NpcCharacter);
            bossCharIndex = FindCharIndex(palette.BossCharacter);

            if (palette.Mappings != null)
            {
                foreach (var m in palette.Mappings)
                {
                    if (m.Tile is Tile tile && tile.sprite != null)
                        mappings[m.Key] = tile.sprite;
                }
            }

            Debug.Log($"[TilesetMapper] Loaded palette '{paletteName}' with {mappings.Count} mappings");
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
