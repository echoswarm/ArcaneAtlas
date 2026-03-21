using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Renders scrolling parallax layers inside a Canvas panel.
    /// Works for full-screen panels (combat) and sub-panels (title screen quadrants).
    /// Attach to a RectTransform; add RectMask2D on the same object to clip layers
    /// to any panel shape (required for quadrant use on the title screen).
    ///
    /// UV tiling is computed lazily once the RectTransform has valid dimensions.
    /// Call LoadByBiomeName() or LoadConfig() to switch biomes at runtime.
    /// </summary>
    public class ParallaxBackgroundController : MonoBehaviour
    {
        [SerializeField] private ParallaxBiomeConfig config;

        private readonly List<RawImage> _layerImages = new List<RawImage>();
        private RawImage _dimOverlay;
        private bool _playing = true;
        private ParallaxBiomeConfig _active;
        // UV widths are set lazily once the RectTransform has been laid out
        private bool _uvReady;

        void Awake()
        {
            if (config != null)
                BuildLayers(config);
        }

        void Update()
        {
            if (!_playing || _active == null) return;

            // First frame after BuildLayers: wait for a valid rect, then fix UV widths
            if (!_uvReady)
            {
                var rt = GetComponent<RectTransform>();
                if (rt.rect.width > 1f)
                {
                    float dispW = rt.rect.width;
                    float dispH = rt.rect.height;
                    for (int i = 0; i < _layerImages.Count && i < _active.layers.Count; i++)
                    {
                        var tex = _active.layers[i].texture;
                        if (_layerImages[i] != null && tex != null)
                            _layerImages[i].uvRect = new Rect(0f, 0f,
                                dispW / tex.width, dispH / tex.height);
                    }
                    _uvReady = true;
                }
                return;
            }

            for (int i = 0; i < _layerImages.Count && i < _active.layers.Count; i++)
            {
                var img = _layerImages[i];
                if (img == null || img.texture == null) continue;

                var r = img.uvRect;
                r.x += _active.layers[i].scrollFactor * _active.masterSpeed * Time.deltaTime;
                img.uvRect = r;
            }
        }

        /// <summary>Loads a biome config and rebuilds all layer objects.</summary>
        public void LoadConfig(ParallaxBiomeConfig newConfig)
        {
            ClearLayers();
            if (newConfig != null)
                BuildLayers(newConfig);
        }

        /// <summary>
        /// Loads a ParallaxBiomeConfig from Resources/ParallaxConfigs/{biomeName}.
        /// Silently does nothing if no asset is found.
        /// </summary>
        public void LoadByBiomeName(string biomeName)
        {
            if (string.IsNullOrEmpty(biomeName)) return;
            var cfg = Resources.Load<ParallaxBiomeConfig>("ParallaxConfigs/" + biomeName);
            if (cfg != null)
                LoadConfig(cfg);
        }

        /// <summary>Pauses or resumes scrolling without destroying layers.</summary>
        public void SetPlaying(bool playing) => _playing = playing;

        /// <summary>Overrides the dim overlay alpha (0 = invisible, 1 = fully opaque).</summary>
        public void SetDim(float alpha)
        {
            if (_dimOverlay == null) return;
            var c = _dimOverlay.color;
            c.a = Mathf.Clamp01(alpha);
            _dimOverlay.color = c;
        }

        private void ClearLayers()
        {
            foreach (var img in _layerImages)
                if (img != null) Destroy(img.gameObject);
            _layerImages.Clear();

            if (_dimOverlay != null)
            {
                Destroy(_dimOverlay.gameObject);
                _dimOverlay = null;
            }
            _active = null;
        }

        private void BuildLayers(ParallaxBiomeConfig cfg)
        {
            _active = cfg;
            _uvReady = false;

            var rt = GetComponent<RectTransform>();
            // Use rect size if already laid out; fall back to 1920x1080 for first-frame builds
            float dispW = rt.rect.width > 0 ? rt.rect.width : 1920f;
            float dispH = rt.rect.height > 0 ? rt.rect.height : 1080f;

            foreach (var layer in cfg.layers)
            {
                var go = new GameObject($"PLX_{layer.label}");
                go.transform.SetParent(transform, false);

                var img = go.AddComponent<RawImage>();
                img.color = layer.tint;
                img.raycastTarget = false;

                if (layer.texture != null)
                {
                    // Ensure Repeat wrap so UV offset scrolls seamlessly
                    layer.texture.wrapMode = TextureWrapMode.Repeat;
                    img.texture = layer.texture;
                    // uvRect width/height = display pixels / texture pixels → tiles the texture
                    img.uvRect = new Rect(0f, 0f,
                        dispW / layer.texture.width,
                        dispH / layer.texture.height);
                }

                var layerRt = go.GetComponent<RectTransform>();
                layerRt.anchorMin = Vector2.zero;
                layerRt.anchorMax = Vector2.one;
                layerRt.offsetMin = Vector2.zero;
                layerRt.offsetMax = Vector2.zero;

                _layerImages.Add(img);
            }

            // Dim overlay — last child renders on top of all parallax layers
            var dimGo = new GameObject("PLX_Dim");
            dimGo.transform.SetParent(transform, false);
            _dimOverlay = dimGo.AddComponent<RawImage>();
            _dimOverlay.color = cfg.dimColor;
            _dimOverlay.raycastTarget = false;
            var dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
        }
    }
}
