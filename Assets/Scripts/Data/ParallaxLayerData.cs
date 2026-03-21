using System;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    [Serializable]
    public class ParallaxLayerData
    {
        [Tooltip("Back-to-front label shown in the editor (e.g. Sky, Far, Mid, Close).")]
        public string label = "Layer";

        [Tooltip("Texture to scroll horizontally. Set Wrap Mode to Repeat in import settings.")]
        public Texture2D texture;

        [Range(0f, 1f)]
        [Tooltip("Speed multiplier relative to masterSpeed. 0 = stationary (sky), 1 = fastest (nearest).")]
        public float scrollFactor = 0.1f;

        [Tooltip("Per-layer color tint. Alpha controls layer opacity.")]
        public Color tint = Color.white;
    }
}
