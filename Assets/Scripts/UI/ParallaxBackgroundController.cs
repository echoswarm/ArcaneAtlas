using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Renders scrolling parallax layers inside a Canvas panel.
    /// Works for full-screen panels (combat) and sub-panels (title screen quadrants).
    /// Attach to a RectTransform; add RectMask2D on the same object to clip layers.
    ///
    /// Layer layout rules:
    ///   scrollFactor == 0  →  sky: stretch to fill panel vertically (UV Y = 1, no repeat).
    ///   scrollFactor  > 0  →  foreground: bottom-anchored, native aspect-ratio height,
    ///                         only tiles horizontally (UV X scrolls, UV Y = 1 always).
    ///
    /// UV widths and foreground heights are computed lazily on the first Update() frame
    /// after the RectTransform has valid dimensions.
    /// </summary>
    public class ParallaxBackgroundController : MonoBehaviour
    {
        [SerializeField] private ParallaxBiomeConfig config;

        private readonly List<RawImage> _layerImages = new List<RawImage>();
        private RawImage _dimOverlay;
        private bool _playing = true;
        private ParallaxBiomeConfig _active;
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
            // and foreground layer heights using the real panel dimensions.
            if (!_uvReady)
            {
                var rt = GetComponent<RectTransform>();
                if (rt.rect.width > 1f)
                {
                    float dispW = rt.rect.width;
                    for (int i = 0; i < _layerImages.Count && i < _active.layers.Count; i++)
                    {
                        var layer = _active.layers[i];
                        var tex   = layer.texture;
                        if (_layerImages[i] == null || tex == null) continue;

                        // Correct UV X width — Y stays 1 (no vertical tiling ever)
                        var uv = _layerImages[i].uvRect;
                        uv.width = dispW / tex.width;
                        _layerImages[i].uvRect = uv;

                        // Correct foreground height to native aspect ratio at actual panel width
                        if (layer.scrollFactor > 0f)
                        {
                            _layerImages[i].rectTransform.sizeDelta =
                                new Vector2(0f, (float)tex.height / tex.width * dispW);
                        }
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
            // Use actual size if laid out; fall back to full-screen for first-frame builds.
            // Lazy init in Update() will correct this once the real rect is known.
            float dispW = rt.rect.width  > 0 ? rt.rect.width  : 1920f;

            foreach (var layer in cfg.layers)
            {
                var go = new GameObject($"PLX_{layer.label}");
                go.transform.SetParent(transform, false);

                var img = go.AddComponent<RawImage>();
                img.color = layer.tint;
                img.raycastTarget = false;

                if (layer.texture != null)
                {
                    // Repeat only needed in X for horizontal scrolling.
                    // UV Y is always 1 — vertical tiling is never wanted.
                    layer.texture.wrapMode = TextureWrapMode.Repeat;
                    img.texture = layer.texture;
                    img.uvRect  = new Rect(0f, 0f, dispW / layer.texture.width, 1f);
                }

                var layerRt = go.GetComponent<RectTransform>();

                if (layer.scrollFactor == 0f)
                {
                    // Sky / static background — stretch to fill the panel.
                    // UV Y = 1 so it shows exactly once, scaled to the panel height.
                    layerRt.anchorMin = Vector2.zero;
                    layerRt.anchorMax = Vector2.one;
                    layerRt.offsetMin = Vector2.zero;
                    layerRt.offsetMax = Vector2.zero;
                }
                else
                {
                    // Foreground layer — anchor to bottom, native aspect-ratio height.
                    // This prevents vertical repetition: the sprite appears once at the
                    // bottom of the panel, clipped above by RectMask2D if needed.
                    float layerH = layer.texture != null
                        ? (float)layer.texture.height / layer.texture.width * dispW
                        : dispW * 9f / 16f;

                    layerRt.anchorMin       = new Vector2(0f, 0f);
                    layerRt.anchorMax       = new Vector2(1f, 0f);
                    layerRt.pivot           = new Vector2(0.5f, 0f);
                    layerRt.sizeDelta       = new Vector2(0f, layerH);
                    layerRt.anchoredPosition = Vector2.zero;
                }

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
