using UnityEngine;
using UnityEditor;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;
using System.IO;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Generates the ExplorationRoot prefab containing world-space exploration objects.
    /// Run via Arcane Atlas > Build Exploration (Ctrl+Shift+E).
    /// </summary>
    public static class ExplorationBuilderTool
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Exploration/ExplorationRoot.prefab";
        private const string SPRITE_PATH = "Assets/Art/Generated/white_square.png";

        public static void Generate()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Exploration");

            if (File.Exists(PREFAB_PATH))
            {
                AssetDatabase.DeleteAsset(PREFAB_PATH);
                Debug.Log("[ExplorationBuilderTool] Deleted existing ExplorationRoot.prefab");
            }

            Sprite whiteSprite = GetOrCreateWhiteSprite();
            if (whiteSprite == null)
            {
                Debug.LogError("[ExplorationBuilderTool] Failed to create white sprite!");
                return;
            }

            GameObject root = BuildExplorationHierarchy(whiteSprite);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
            {
                Debug.Log("[ExplorationBuilderTool] Generated ExplorationRoot.prefab");
                Debug.Log("[ExplorationBuilderTool] Objects: ExplorationManager, PlayerController, RoomBackground, Borders, ExitIndicators, NpcContainer");
                Debug.Log("[ExplorationBuilderTool] CameraController added to Main Camera at runtime by ExplorationManager");
            }
            else
            {
                Debug.LogError("[ExplorationBuilderTool] Failed to save prefab!");
            }
        }

        private static GameObject BuildExplorationHierarchy(Sprite whiteSprite)
        {
            // Root stays ACTIVE so ExplorationManager.Awake() sets Instance
            var root = new GameObject("ExplorationRoot");

            var worldObjects = new GameObject("WorldObjects");
            worldObjects.transform.SetParent(root.transform, false);

            // Room background
            var roomBG = new GameObject("RoomBackground");
            roomBG.transform.SetParent(worldObjects.transform, false);
            var bgSR = roomBG.AddComponent<SpriteRenderer>();
            bgSR.sprite = whiteSprite;
            bgSR.color = new Color(0.08f, 0.18f, 0.08f);
            bgSR.sortingOrder = -10;
            roomBG.transform.localScale = new Vector3(17f, 10f, 1f);

            // Player
            var player = new GameObject("Player");
            player.transform.SetParent(worldObjects.transform, false);
            var playerSR = player.AddComponent<SpriteRenderer>();
            playerSR.sprite = whiteSprite;
            playerSR.color = new Color(0.9f, 0.85f, 0.7f);
            playerSR.sortingLayerName = "Player";
            playerSR.sortingOrder = 0;
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var pc = player.AddComponent<PlayerController>();
            pc.spriteRenderer = playerSR;
            pc.moveSpeed = 5f;
            pc.roomMin = new Vector2(-8f, -4.5f);
            pc.roomMax = new Vector2(8f, 4.5f);

            // Room borders
            var borders = new GameObject("RoomBorder");
            borders.transform.SetParent(worldObjects.transform, false);
            Color borderColor = new Color(1f, 1f, 1f, 0.2f);

            CreateWorldSprite("Border_Top", borders.transform, whiteSprite, borderColor,
                new Vector3(0f, 5f, 0f), new Vector3(17f, 0.06f, 1f), -5);
            CreateWorldSprite("Border_Bottom", borders.transform, whiteSprite, borderColor,
                new Vector3(0f, -5f, 0f), new Vector3(17f, 0.06f, 1f), -5);
            CreateWorldSprite("Border_Left", borders.transform, whiteSprite, borderColor,
                new Vector3(-8.5f, 0f, 0f), new Vector3(0.06f, 10f, 1f), -5);
            CreateWorldSprite("Border_Right", borders.transform, whiteSprite, borderColor,
                new Vector3(8.5f, 0f, 0f), new Vector3(0.06f, 10f, 1f), -5);

            // Exit indicators — individually referenced by ExplorationManager
            var exits = new GameObject("ExitIndicators");
            exits.transform.SetParent(worldObjects.transform, false);
            Color exitColor = new Color(0.83f, 0.66f, 0.26f, 0.4f);

            var exitUpGO = CreateWorldSprite("Exit_Up", exits.transform, whiteSprite, exitColor,
                new Vector3(0f, 4.7f, 0f), new Vector3(1.5f, 0.2f, 1f), -4);
            var exitDownGO = CreateWorldSprite("Exit_Down", exits.transform, whiteSprite, exitColor,
                new Vector3(0f, -4.7f, 0f), new Vector3(1.5f, 0.2f, 1f), -4);
            var exitLeftGO = CreateWorldSprite("Exit_Left", exits.transform, whiteSprite, exitColor,
                new Vector3(-8.2f, 0f, 0f), new Vector3(0.2f, 1.5f, 1f), -4);
            var exitRightGO = CreateWorldSprite("Exit_Right", exits.transform, whiteSprite, exitColor,
                new Vector3(8.2f, 0f, 0f), new Vector3(0.2f, 1.5f, 1f), -4);

            // NPC container — empty, parent for runtime NPC spawns
            var npcContainer = new GameObject("NpcContainer");
            npcContainer.transform.SetParent(worldObjects.transform, false);

            // Room container — parent for instantiated room template prefabs
            var roomContainer = new GameObject("RoomContainer");
            roomContainer.transform.SetParent(worldObjects.transform, false);

            // EncounterManager on root (singleton, always accessible)
            root.AddComponent<EncounterManager>();

            // ExplorationManager on root — wired to all children
            var em = root.AddComponent<ExplorationManager>();
            em.explorationRoot = worldObjects;
            em.player = pc;
            em.roomBackground = bgSR;
            em.npcContainer = npcContainer.transform;
            em.roomContainer = roomContainer.transform;

            // Auto-wire placeholder palette if it exists
            var placeholder = AssetDatabase.LoadAssetAtPath<ArcaneAtlas.Data.TilePaletteDef>(
                "Assets/Resources/TilePalettes/Placeholder.asset");
            em.tilePalette = placeholder;

            em.exitUp = exitUpGO;
            em.exitDown = exitDownGO;
            em.exitLeft = exitLeftGO;
            em.exitRight = exitRightGO;
            em.roomColors = new Color[]
            {
                new Color(0.08f, 0.18f, 0.08f), // Dark forest green
                new Color(0.10f, 0.15f, 0.07f), // Mossy green
                new Color(0.06f, 0.12f, 0.10f), // Deep teal-green
            };

            // WorldObjects starts inactive
            worldObjects.SetActive(false);

            return root;
        }

        private static GameObject CreateWorldSprite(string name, Transform parent, Sprite sprite,
            Color color, Vector3 position, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        // ═══════════════════════════════════════
        //  White sprite asset
        // ═══════════════════════════════════════

        private static Sprite GetOrCreateWhiteSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
            if (existing != null) return existing;

            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Generated");

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < 16; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] pngData = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(SPRITE_PATH, pngData);
            AssetDatabase.ImportAsset(SPRITE_PATH);

            var importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 4;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 32;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
