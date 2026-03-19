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
    /// Editor window for creating, browsing, and validating room templates.
    /// Creates prefabs with the full 7-layer tilemap hierarchy + spawn points + exit zones.
    /// Arcane Atlas > Room Template Editor
    /// </summary>
    public class RoomTemplateEditorWindow : EditorWindow
    {
        // Create mode
        private string newTemplateName = "Forest_Empty_01";
        private int selectedBiome = 0;
        private bool[] allowedTypes = new bool[] { true, false, false, false, false }; // Empty, NPC, Treasure, Event, Boss
        private int minTier = 1;
        private int maxTier = 6;
        private bool exitUp = true, exitDown = true, exitLeft = true, exitRight = true;

        // Browse mode
        private Vector2 browseScroll;
        private string filterBiome = "";
        private int filterType = -1; // -1 = all
        private List<RoomTemplateData> cachedTemplates;

        // UI state
        private int currentTab = 0;
        private string statusMessage = "";

        private static readonly string[] BiomeNames = {
            "AncientForest", "VolcanicWastes", "SkyPeaks", "CoralDepths"
        };

        private static readonly string[] RoomTypeNames = {
            "Empty", "NPC", "Treasure", "Event", "Boss"
        };

        // Tilemap layer definitions
        private static readonly LayerDef[] Layers = new LayerDef[]
        {
            new LayerDef("Ground",     "Ground",     0, true,  false),
            new LayerDef("Detail",     "Detail",     0, true,  false),
            new LayerDef("Shadow",     "Shadow",     0, true,  false),
            new LayerDef("PropsBelow", "PropsBelow", 0, true,  false),
            new LayerDef("PropsAbove", "PropsAbove", 0, true,  false),
            new LayerDef("Overlay",    "Overlay",    0, true,  false),
            new LayerDef("Collision",  "Default",    0, false, true),
        };

        private struct LayerDef
        {
            public string Name;
            public string SortingLayer;
            public int SortingOrder;
            public bool HasRenderer;
            public bool HasCollider;

            public LayerDef(string name, string sortingLayer, int order, bool renderer, bool collider)
            {
                Name = name; SortingLayer = sortingLayer; SortingOrder = order;
                HasRenderer = renderer; HasCollider = collider;
            }
        }

        public static void ShowWindow()
        {
            var window = GetWindow<RoomTemplateEditorWindow>("Room Template Editor");
            window.minSize = new Vector2(500, 650);
        }

        void OnGUI()
        {
            GUILayout.Label("Room Template Editor", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // Tab bar
            currentTab = GUILayout.Toolbar(currentTab, new string[] { "Create New", "Browse Templates", "Quick Actions" });
            GUILayout.Space(8);

            switch (currentTab)
            {
                case 0: DrawCreateTab(); break;
                case 1: DrawBrowseTab(); break;
                case 2: DrawQuickActionsTab(); break;
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        // ═══════════════════════════════════════
        //  Create Tab
        // ═══════════════════════════════════════

        private void DrawCreateTab()
        {
            GUILayout.Label("Template Settings", EditorStyles.boldLabel);

            newTemplateName = EditorGUILayout.TextField("Template Name", newTemplateName);
            selectedBiome = EditorGUILayout.Popup("Biome", selectedBiome, BiomeNames);

            GUILayout.Space(4);
            GUILayout.Label("Allowed Room Types:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < RoomTypeNames.Length; i++)
                allowedTypes[i] = GUILayout.Toggle(allowedTypes[i], RoomTypeNames[i]);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            minTier = EditorGUILayout.IntSlider("Min Tier", minTier, 1, 6);
            maxTier = EditorGUILayout.IntSlider("Max Tier", maxTier, 1, 6);
            EditorGUILayout.EndHorizontal();
            if (maxTier < minTier) maxTier = minTier;

            GUILayout.Space(4);
            GUILayout.Label("Exits:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            exitUp = GUILayout.Toggle(exitUp, "Up");
            exitDown = GUILayout.Toggle(exitDown, "Down");
            exitLeft = GUILayout.Toggle(exitLeft, "Left");
            exitRight = GUILayout.Toggle(exitRight, "Right");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12);

            if (GUILayout.Button("Create Room Template Prefab", GUILayout.Height(32)))
            {
                CreateRoomTemplate();
            }
        }

        private void CreateRoomTemplate()
        {
            string biomeName = BiomeNames[selectedBiome];
            string prefabFolder = $"Assets/Prefabs/RoomTemplates/{biomeName}";
            string dataFolder = $"Assets/Resources/RoomTemplates/{biomeName}";
            EnsureFolder(prefabFolder);
            EnsureFolder(dataFolder);

            // Build the prefab hierarchy
            var rootGO = new GameObject($"RT_{newTemplateName}");
            rootGO.tag = "RoomTemplate";

            // Grid component (cell size for 8px tiles at 32 PPU = 0.25 units)
            var grid = new GameObject("Grid");
            grid.transform.SetParent(rootGO.transform, false);
            var gridComp = grid.AddComponent<Grid>();
            gridComp.cellSize = new Vector3(0.25f, 0.25f, 0f);
            gridComp.cellGap = Vector3.zero;
            gridComp.cellLayout = GridLayout.CellLayout.Rectangle;

            // Create tilemap layers
            foreach (var layer in Layers)
            {
                var layerGO = new GameObject(layer.Name);
                layerGO.transform.SetParent(grid.transform, false);

                var tilemap = layerGO.AddComponent<Tilemap>();
                tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

                if (layer.HasRenderer)
                {
                    var renderer = layerGO.AddComponent<TilemapRenderer>();
                    renderer.sortingLayerName = layer.SortingLayer;
                    renderer.sortingOrder = layer.SortingOrder;

                    // Shadow and Overlay layers need transparency
                    if (layer.Name == "Shadow" || layer.Name == "Overlay")
                    {
                        renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                    }
                }

                if (layer.HasCollider)
                {
                    var collider = layerGO.AddComponent<TilemapCollider2D>();
                    collider.compositeOperation = Collider2D.CompositeOperation.None;

                    // Also add a Rigidbody2D (static) for the collider to work
                    var rb = layerGO.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;
                }
            }

            // Spawn points
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(rootGO.transform, false);

            float roomHalfW = 5f;  // 40 tiles * 0.25 / 2
            float roomHalfH = 4f;  // 32 tiles * 0.25 / 2

            Vector2[] defaultNpcSpawns = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(-2f, 1f),
                new Vector2(2f, -1f),
            };

            foreach (var pos in defaultNpcSpawns)
            {
                var marker = new GameObject($"NPC_Spawn_{System.Array.IndexOf(defaultNpcSpawns, pos)}");
                marker.transform.SetParent(spawnPoints.transform, false);
                marker.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            }

            // Entry points at room edges
            CreateSpawnMarker("Entry_Up", spawnPoints.transform, new Vector3(0f, roomHalfH - 0.5f, 0f));
            CreateSpawnMarker("Entry_Down", spawnPoints.transform, new Vector3(0f, -roomHalfH + 0.5f, 0f));
            CreateSpawnMarker("Entry_Left", spawnPoints.transform, new Vector3(-roomHalfW + 0.5f, 0f, 0f));
            CreateSpawnMarker("Entry_Right", spawnPoints.transform, new Vector3(roomHalfW - 0.5f, 0f, 0f));

            // Exit zone triggers
            var exitZones = new GameObject("ExitZones");
            exitZones.transform.SetParent(rootGO.transform, false);

            if (exitUp) CreateExitZone("Exit_Up", exitZones.transform, new Vector3(0f, roomHalfH, 0f), new Vector2(2f, 0.5f));
            if (exitDown) CreateExitZone("Exit_Down", exitZones.transform, new Vector3(0f, -roomHalfH, 0f), new Vector2(2f, 0.5f));
            if (exitLeft) CreateExitZone("Exit_Left", exitZones.transform, new Vector3(-roomHalfW, 0f, 0f), new Vector2(0.5f, 2f));
            if (exitRight) CreateExitZone("Exit_Right", exitZones.transform, new Vector3(roomHalfW, 0f, 0f), new Vector2(0.5f, 2f));

            // Save as prefab
            string prefabPath = $"{prefabFolder}/{newTemplateName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            Object.DestroyImmediate(rootGO);

            // Create the ScriptableObject data asset
            var templateData = ScriptableObject.CreateInstance<RoomTemplateData>();
            templateData.TemplateName = newTemplateName;
            templateData.BiomeName = biomeName;
            templateData.Prefab = prefab;
            templateData.MinDifficultyTier = minTier;
            templateData.MaxDifficultyTier = maxTier;
            templateData.HasExitUp = exitUp;
            templateData.HasExitDown = exitDown;
            templateData.HasExitLeft = exitLeft;
            templateData.HasExitRight = exitRight;

            // Set allowed types
            var types = new List<RoomType>();
            if (allowedTypes[0]) types.Add(RoomType.Empty);
            if (allowedTypes[1]) types.Add(RoomType.NPC);
            if (allowedTypes[2]) types.Add(RoomType.Treasure);
            if (allowedTypes[3]) types.Add(RoomType.Event);
            if (allowedTypes[4]) types.Add(RoomType.Boss);
            templateData.AllowedTypes = types.ToArray();

            // Default spawn points
            templateData.NpcSpawnPoints = defaultNpcSpawns;
            templateData.PlayerSpawnUp = new Vector2(0f, roomHalfH - 0.5f);
            templateData.PlayerSpawnDown = new Vector2(0f, -roomHalfH + 0.5f);
            templateData.PlayerSpawnLeft = new Vector2(-roomHalfW + 0.5f, 0f);
            templateData.PlayerSpawnRight = new Vector2(roomHalfW - 0.5f, 0f);

            string dataPath = $"{dataFolder}/{newTemplateName}.asset";
            AssetDatabase.CreateAsset(templateData, dataPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            statusMessage = $"Created template: {prefabPath}\nData: {dataPath}\nDouble-click prefab to edit in Prefab Mode.";
            Debug.Log($"[RoomTemplateEditor] {statusMessage}");

            // Open the prefab in prefab mode
            AssetDatabase.OpenAsset(prefab);

            // Ping the ScriptableObject in Project window
            EditorGUIUtility.PingObject(templateData);
        }

        // ═══════════════════════════════════════
        //  Browse Tab
        // ═══════════════════════════════════════

        private void DrawBrowseTab()
        {
            EditorGUILayout.BeginHorizontal();
            filterBiome = EditorGUILayout.TextField("Filter Biome", filterBiome);
            filterType = EditorGUILayout.Popup("Type", filterType + 1,
                new string[] { "All", "Empty", "NPC", "Treasure", "Event", "Boss" }) - 1;
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                cachedTemplates = null;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            if (cachedTemplates == null)
                RefreshTemplateCache();

            if (cachedTemplates.Count == 0)
            {
                EditorGUILayout.HelpBox("No room templates found. Create some in the 'Create New' tab.", MessageType.Warning);
                return;
            }

            GUILayout.Label($"{cachedTemplates.Count} templates found:", EditorStyles.boldLabel);

            browseScroll = EditorGUILayout.BeginScrollView(browseScroll);
            foreach (var template in cachedTemplates)
            {
                if (template == null) continue;

                // Apply filters
                if (!string.IsNullOrEmpty(filterBiome) && template.BiomeName != null &&
                    !template.BiomeName.ToLower().Contains(filterBiome.ToLower()))
                    continue;

                if (filterType >= 0)
                {
                    RoomType rt = (RoomType)filterType;
                    if (template.AllowedTypes == null || !template.AllowedTypes.Contains(rt))
                        continue;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Info
                EditorGUILayout.BeginVertical();
                GUILayout.Label(template.TemplateName, EditorStyles.boldLabel);
                string types = template.AllowedTypes != null
                    ? string.Join(", ", template.AllowedTypes.Select(t => t.ToString()))
                    : "None";
                GUILayout.Label($"{template.BiomeName} | Types: {types} | Tier {template.MinDifficultyTier}-{template.MaxDifficultyTier}", EditorStyles.miniLabel);

                string exits = "";
                if (template.HasExitUp) exits += "Up ";
                if (template.HasExitDown) exits += "Down ";
                if (template.HasExitLeft) exits += "Left ";
                if (template.HasExitRight) exits += "Right ";
                GUILayout.Label($"Exits: {exits}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                // Actions
                EditorGUILayout.BeginVertical(GUILayout.Width(80));
                if (GUILayout.Button("Edit", GUILayout.Width(70)))
                {
                    if (template.Prefab != null)
                        AssetDatabase.OpenAsset(template.Prefab);
                }
                if (GUILayout.Button("Select", GUILayout.Width(70)))
                {
                    Selection.activeObject = template;
                    EditorGUIUtility.PingObject(template);
                }
                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    DuplicateTemplate(template);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshTemplateCache()
        {
            cachedTemplates = new List<RoomTemplateData>();
            var guids = AssetDatabase.FindAssets("t:RoomTemplateData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<RoomTemplateData>(path);
                if (data != null) cachedTemplates.Add(data);
            }
            cachedTemplates.Sort((a, b) => string.Compare(a.TemplateName, b.TemplateName));
        }

        private void DuplicateTemplate(RoomTemplateData source)
        {
            string srcPath = AssetDatabase.GetAssetPath(source);
            string newName = source.TemplateName + "_Copy";
            string newPath = srcPath.Replace(source.TemplateName, newName);
            AssetDatabase.CopyAsset(srcPath, newPath);
            cachedTemplates = null; // Force refresh
            statusMessage = $"Duplicated to: {newPath}";
        }

        // ═══════════════════════════════════════
        //  Quick Actions Tab
        // ═══════════════════════════════════════

        private void DrawQuickActionsTab()
        {
            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Select a room template prefab in the Project window, then use these actions.\n" +
                "Most actions work best when editing a prefab in Prefab Mode.", MessageType.Info);
            GUILayout.Space(8);

            if (GUILayout.Button("Validate Selected Template", GUILayout.Height(28)))
            {
                ValidateSelectedTemplate();
            }

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Sorting Layers", GUILayout.Height(28)))
            {
                SortingLayerSetup.Setup();
                statusMessage = "Sorting layers configured.";
            }

            GUILayout.Space(4);

            if (GUILayout.Button("Open Tile Palette Window", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            }

            GUILayout.Space(12);
            GUILayout.Label("Batch Operations", EditorStyles.boldLabel);
            GUILayout.Space(4);

            if (GUILayout.Button("Validate ALL Templates", GUILayout.Height(24)))
            {
                ValidateAllTemplates();
            }
        }

        private void ValidateSelectedTemplate()
        {
            var selected = Selection.activeObject as RoomTemplateData;
            if (selected == null)
            {
                // Try to find from prefab selection
                var go = Selection.activeGameObject;
                if (go != null)
                {
                    statusMessage = "Select a RoomTemplateData asset in the Project window (the .asset file, not the prefab).";
                    return;
                }
                statusMessage = "No RoomTemplateData selected.";
                return;
            }

            var issues = new List<string>();

            if (selected.Prefab == null)
                issues.Add("Missing prefab reference");

            if (selected.AllowedTypes == null || selected.AllowedTypes.Length == 0)
                issues.Add("No allowed room types set");

            if (string.IsNullOrEmpty(selected.BiomeName))
                issues.Add("No biome name set");

            if (selected.NpcSpawnPoints == null || selected.NpcSpawnPoints.Length == 0)
                issues.Add("No NPC spawn points defined");

            if (selected.MinDifficultyTier > selected.MaxDifficultyTier)
                issues.Add($"Min tier ({selected.MinDifficultyTier}) > Max tier ({selected.MaxDifficultyTier})");

            // Check prefab layers
            if (selected.Prefab != null)
            {
                var grid = selected.Prefab.GetComponentInChildren<Grid>();
                if (grid == null)
                    issues.Add("Prefab missing Grid component");
                else
                {
                    var tilemaps = grid.GetComponentsInChildren<Tilemap>();
                    if (tilemaps.Length < 7)
                        issues.Add($"Expected 7 tilemap layers, found {tilemaps.Length}");

                    // Check if ground layer has any tiles
                    var groundMap = tilemaps.FirstOrDefault(t => t.gameObject.name == "Ground");
                    if (groundMap != null)
                    {
                        groundMap.CompressBounds();
                        if (groundMap.GetUsedTilesCount() == 0)
                            issues.Add("Ground layer has no tiles painted");
                    }
                }
            }

            if (issues.Count == 0)
                statusMessage = $"'{selected.TemplateName}' passed validation!";
            else
                statusMessage = $"'{selected.TemplateName}' has {issues.Count} issues:\n" + string.Join("\n- ", issues);
        }

        private void ValidateAllTemplates()
        {
            if (cachedTemplates == null) RefreshTemplateCache();

            int pass = 0, fail = 0;
            foreach (var t in cachedTemplates)
            {
                bool hasPrefab = t.Prefab != null;
                bool hasTypes = t.AllowedTypes != null && t.AllowedTypes.Length > 0;
                bool hasSpawns = t.NpcSpawnPoints != null && t.NpcSpawnPoints.Length > 0;

                if (hasPrefab && hasTypes && hasSpawns)
                    pass++;
                else
                {
                    fail++;
                    Debug.LogWarning($"[RoomTemplateEditor] Validation failed: {t.TemplateName} — Prefab:{hasPrefab} Types:{hasTypes} Spawns:{hasSpawns}");
                }
            }

            statusMessage = $"Validated {cachedTemplates.Count} templates: {pass} pass, {fail} fail.";
        }

        // ═══════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════

        private void CreateSpawnMarker(string name, Transform parent, Vector3 pos)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = pos;
        }

        private void CreateExitZone(string name, Transform parent, Vector3 pos, Vector2 size)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.localPosition = pos;
            var col = zone.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
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
