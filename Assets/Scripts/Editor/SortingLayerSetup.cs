using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Creates the sorting layers required for the tilemap room system.
    /// Run once via Arcane Atlas > Setup Sorting Layers.
    /// Idempotent — safe to run multiple times.
    /// </summary>
    public static class SortingLayerSetup
    {
        // Sorting layers in render order (bottom to top)
        private static readonly string[] RequiredLayers = new string[]
        {
            "Ground",       // Base terrain tiles
            "Detail",       // Grass tufts, cracks, flowers
            "Shadow",       // Semi-transparent darkness/AO
            "PropsBelow",   // Lower halves of tall objects (below player)
            "Player",       // Player character, NPCs
            "PropsAbove",   // Upper halves of tall objects (above player)
            "Overlay",      // Clouds, fog, weather effects
        };

        public static void Setup()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");

            int added = 0;
            foreach (string layerName in RequiredLayers)
            {
                if (SortingLayerExists(sortingLayersProp, layerName))
                {
                    Debug.Log($"[SortingLayerSetup] Layer '{layerName}' already exists — skipping");
                    continue;
                }

                AddSortingLayer(sortingLayersProp, layerName);
                added++;
                Debug.Log($"[SortingLayerSetup] Added sorting layer: {layerName}");
            }

            tagManager.ApplyModifiedProperties();

            // Add required tags
            AddTagIfMissing(tagManager, "RoomTemplate");
            tagManager.ApplyModifiedProperties();

            if (added > 0)
                Debug.Log($"[SortingLayerSetup] Added {added} sorting layers. Total: {RequiredLayers.Length}");
            else
                Debug.Log("[SortingLayerSetup] All sorting layers already exist — nothing to do");

            // Log the final layer order
            Debug.Log("[SortingLayerSetup] Layer order (bottom to top):");
            Debug.Log("  Default → Ground → Detail → Shadow → PropsBelow → Player → PropsAbove → Overlay → UI");
        }

        private static bool SortingLayerExists(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return true;
            }
            return false;
        }

        private static void AddSortingLayer(SerializedProperty layers, string name)
        {
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var newLayer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            newLayer.FindPropertyRelative("name").stringValue = name;
            // Unity auto-assigns unique IDs
            newLayer.FindPropertyRelative("uniqueID").intValue = name.GetHashCode();
        }

        private static void AddTagIfMissing(SerializedObject tagManager, string tag)
        {
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    Debug.Log($"[SortingLayerSetup] Tag '{tag}' already exists");
                    return;
                }
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            Debug.Log($"[SortingLayerSetup] Added tag: {tag}");
        }
    }
}
