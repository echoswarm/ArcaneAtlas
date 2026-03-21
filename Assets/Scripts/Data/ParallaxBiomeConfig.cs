using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    [CreateAssetMenu(menuName = "Arcane Atlas/Parallax Biome Config", fileName = "ParallaxBiome_New")]
    public class ParallaxBiomeConfig : ScriptableObject
    {
        [Tooltip("Matches the zone's BiomePalette string for auto-loading at runtime (e.g. AncientForest).")]
        public string biomeName;

        [Tooltip("Layers ordered back to front — index 0 = farthest/sky (scroll 0), last = nearest (scroll highest).")]
        public List<ParallaxLayerData> layers = new List<ParallaxLayerData>();

        [Min(0f)]
        [Tooltip("Base scroll speed in UV units per second. Each layer multiplies this by its scrollFactor.")]
        public float masterSpeed = 0.04f;

        [Tooltip("Overlay color rendered on top of all layers. Alpha controls darkness of the combat dim effect.")]
        public Color dimColor = new Color(0f, 0f, 0.05f, 0.45f);
    }
}
