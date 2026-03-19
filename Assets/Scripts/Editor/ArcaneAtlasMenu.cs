using UnityEngine;
using UnityEditor;

namespace ArcaneAtlas.Editor
{
    public static class ArcaneAtlasMenu
    {
        [MenuItem("Arcane Atlas/Build Canvas %#g", false, 1)]
        public static void BuildCanvas()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("  ARCANE ATLAS — BUILD CANVAS");
            Debug.Log("═══════════════════════════════════════");

            CanvasBuilderTool.Generate();

            Debug.Log("───────────────────────────────────────");
            Debug.Log("  Build Canvas complete!");
            Debug.Log("  Prefab: Assets/Prefabs/UI/GameCanvas.prefab");
            Debug.Log("  Drag into Main.scene if not already present.");
            Debug.Log("═══════════════════════════════════════");
        }

        [MenuItem("Arcane Atlas/Build Exploration %#e", false, 2)]
        public static void BuildExploration()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("  ARCANE ATLAS — BUILD EXPLORATION");
            Debug.Log("═══════════════════════════════════════");

            ExplorationBuilderTool.Generate();

            Debug.Log("───────────────────────────────────────");
            Debug.Log("  Build Exploration complete!");
            Debug.Log("  Prefab: Assets/Prefabs/Exploration/ExplorationRoot.prefab");
            Debug.Log("  Drag into Main.scene. Starts inactive.");
            Debug.Log("═══════════════════════════════════════");
        }

        [MenuItem("Arcane Atlas/Fix Sprite Imports", false, 20)]
        public static void FixSpriteImports()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("  ARCANE ATLAS — FIX SPRITE IMPORTS");
            Debug.Log("═══════════════════════════════════════");

            SpriteImportFixer.FixAll();

            Debug.Log("═══════════════════════════════════════");
        }

        // ─── Tilemap Pipeline ───────────────────

        [MenuItem("Arcane Atlas/Setup Sorting Layers", false, 30)]
        public static void SetupSortingLayers()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("  ARCANE ATLAS — SETUP SORTING LAYERS");
            Debug.Log("═══════════════════════════════════════");

            SortingLayerSetup.Setup();

            Debug.Log("═══════════════════════════════════════");
        }

        [MenuItem("Arcane Atlas/Tileset Importer", false, 31)]
        public static void OpenTilesetImporter()
        {
            TilesetImporterWindow.ShowWindow();
        }

        [MenuItem("Arcane Atlas/Room Template Editor", false, 32)]
        public static void OpenRoomTemplateEditor()
        {
            RoomTemplateEditorWindow.ShowWindow();
        }

        // Tileset Mapper and Placeholder Palette have [MenuItem] on their own classes
    }
}
