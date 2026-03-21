using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Editor window for creating and configuring parallax biome backgrounds.
    /// Arcane Atlas > Parallax Editor
    ///
    /// Layout: [Config List | Layer Editor | Sprite Browser] + [Preview Strip]
    /// </summary>
    public class ParallaxEditorWindow : EditorWindow
    {
        // ── Layout constants ──────────────────────────────────────────────────
        private const float LEFT_WIDTH    = 210f;
        private const float RIGHT_WIDTH   = 230f;
        private const float PREVIEW_H     = 182f;
        private const string CONFIGS_PATH = "Assets/Resources/ParallaxConfigs";
        private const string SPRITES_ROOT = "Assets/Art/ParallaxBackgroundSprites";

        // ── Config list ───────────────────────────────────────────────────────
        private List<ParallaxBiomeConfig> _configs = new List<ParallaxBiomeConfig>();
        private int    _selectedConfigIndex = -1;
        private Vector2 _configListScroll;
        private string _newConfigName = "NewBiome";

        // ── Layer editor ──────────────────────────────────────────────────────
        private Vector2 _layerScroll;
        private int _selectedLayerIndex = -1;

        // ── Sprite browser ────────────────────────────────────────────────────
        private string          _browserFolder = SPRITES_ROOT;
        private List<string>    _subFolders    = new List<string>();
        private List<Texture2D> _browserTextures = new List<Texture2D>();
        private Vector2         _browserScroll;

        // ── Preview ───────────────────────────────────────────────────────────
        private bool    _previewPlaying = true;
        private float[] _previewOffsets = System.Array.Empty<float>();
        private double  _prevTime;

        // ── Convenience accessor ──────────────────────────────────────────────
        private ParallaxBiomeConfig Selected =>
            _selectedConfigIndex >= 0 && _selectedConfigIndex < _configs.Count
                ? _configs[_selectedConfigIndex] : null;

        // ─────────────────────────────────────────────────────────────────────
        [MenuItem("Arcane Atlas/Parallax Editor", false, 50)]
        public static void ShowWindow()
        {
            var w = GetWindow<ParallaxEditorWindow>("Parallax Editor");
            w.minSize = new Vector2(870f, 640f);
        }

        void OnEnable()
        {
            RefreshConfigList();
            BrowseFolder(SPRITES_ROOT);
            EditorApplication.update += Tick;
            _prevTime = EditorApplication.timeSinceStartup;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        // ── Preview animation tick ────────────────────────────────────────────
        void Tick()
        {
            var cfg = Selected;
            if (!_previewPlaying || cfg == null) return;

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _prevTime);
            _prevTime = now;

            int n = cfg.layers.Count;
            if (_previewOffsets.Length != n)
                _previewOffsets = new float[n];

            for (int i = 0; i < n; i++)
                _previewOffsets[i] = Mathf.Repeat(
                    _previewOffsets[i] + cfg.layers[i].scrollFactor * cfg.masterSpeed * dt, 1f);

            Repaint();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  MAIN LAYOUT
        // ═════════════════════════════════════════════════════════════════════
        void OnGUI()
        {
            DrawToolbar();

            float toolbarH  = EditorStyles.toolbar.fixedHeight;
            float contentH  = position.height - toolbarH - PREVIEW_H - 4f;

            // ── Three-column content ──────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(GUILayout.Height(contentH));

            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_WIDTH), GUILayout.ExpandHeight(true));
            DrawConfigPanel();
            EditorGUILayout.EndVertical();

            DrawVDivider();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawLayerPanel();
            EditorGUILayout.EndVertical();

            DrawVDivider();

            EditorGUILayout.BeginVertical(GUILayout.Width(RIGHT_WIDTH), GUILayout.ExpandHeight(true));
            DrawBrowserPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // ── Preview strip ─────────────────────────────────────────────────
            GUILayout.Space(2f);
            Rect previewRect = GUILayoutUtility.GetRect(position.width, PREVIEW_H);
            DrawPreview(previewRect);
        }

        // ════════════════════════════════════════════
        //  TOOLBAR
        // ════════════════════════════════════════════
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Parallax Editor", EditorStyles.boldLabel, GUILayout.Width(110f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                RefreshConfigList();
            if (GUILayout.Button("New Config", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                CreateConfig(_newConfigName);
            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════
        //  CONFIG LIST PANEL (left)
        // ════════════════════════════════════════════
        void DrawConfigPanel()
        {
            GUILayout.Label("Biome Configs", EditorStyles.boldLabel);

            _configListScroll = EditorGUILayout.BeginScrollView(_configListScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _configs.Count; i++)
            {
                bool sel   = i == _selectedConfigIndex;
                string lbl = _configs[i].biomeName;
                if (string.IsNullOrEmpty(lbl)) lbl = _configs[i].name;

                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20f));
                if (sel) EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.25f));
                if (GUI.Button(rect, lbl, EditorStyles.label))
                {
                    _selectedConfigIndex = i;
                    _selectedLayerIndex  = -1;
                    _previewOffsets      = System.Array.Empty<float>();
                    _prevTime            = EditorApplication.timeSinceStartup;
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            _newConfigName = EditorGUILayout.TextField(_newConfigName, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("+", GUILayout.Width(22f)))
                CreateConfig(_newConfigName);
            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════
        //  LAYER PANEL (center)
        // ════════════════════════════════════════════
        void DrawLayerPanel()
        {
            var cfg = Selected;
            if (cfg == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← Select or create a biome config", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            EditorGUI.BeginChangeCheck();

            // ── Biome settings ────────────────────────────────────────────────
            GUILayout.Label("Biome Settings", EditorStyles.boldLabel);
            cfg.biomeName   = EditorGUILayout.TextField("Biome Name",  cfg.biomeName);
            cfg.masterSpeed = EditorGUILayout.Slider("Master Speed", cfg.masterSpeed, 0f, 0.3f);
            cfg.dimColor    = EditorGUILayout.ColorField("Dim Overlay",  cfg.dimColor);

            GUILayout.Space(6f);

            // ── Layer list ────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Layers [{cfg.layers.Count}]   ← back (sky)   front (close) →", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add", EditorStyles.miniButton, GUILayout.Width(50f)))
            {
                cfg.layers.Add(new ParallaxLayerData { label = "Layer " + cfg.layers.Count });
                _selectedLayerIndex = cfg.layers.Count - 1;
            }
            EditorGUILayout.EndHorizontal();

            _layerScroll = EditorGUILayout.BeginScrollView(_layerScroll, GUILayout.ExpandHeight(true));

            for (int i = 0; i < cfg.layers.Count; i++)
            {
                var layer = cfg.layers[i];
                bool isSel = i == _selectedLayerIndex;

                // Row background
                var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(52f));
                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    _selectedLayerIndex = i;
                    Event.current.Use();
                }
                if (isSel) EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.55f, 1f, 0.2f));

                // Index label
                GUILayout.Label($"[{i}]", GUILayout.Width(26f));

                // Texture thumbnail
                layer.texture = (Texture2D)EditorGUI.ObjectField(
                    GUILayoutUtility.GetRect(46f, 46f, GUILayout.Width(46f)),
                    layer.texture, typeof(Texture2D), false);

                // Label + scroll factor + tint
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                layer.label        = EditorGUILayout.TextField(layer.label);
                layer.scrollFactor = EditorGUILayout.Slider(layer.scrollFactor, 0f, 1f);
                layer.tint         = EditorGUILayout.ColorField(GUIContent.none, layer.tint,
                                         false, false, false, GUILayout.Width(80f));
                EditorGUILayout.EndVertical();

                // Reorder & remove buttons
                EditorGUILayout.BeginVertical(GUILayout.Width(28f));
                if (GUILayout.Button("▲", EditorStyles.miniButton, GUILayout.Height(16f)) && i > 0)
                {
                    (cfg.layers[i], cfg.layers[i - 1]) = (cfg.layers[i - 1], cfg.layers[i]);
                    if      (_selectedLayerIndex == i)     _selectedLayerIndex--;
                    else if (_selectedLayerIndex == i - 1) _selectedLayerIndex++;
                }
                if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Height(16f)) && i < cfg.layers.Count - 1)
                {
                    (cfg.layers[i], cfg.layers[i + 1]) = (cfg.layers[i + 1], cfg.layers[i]);
                    if      (_selectedLayerIndex == i)     _selectedLayerIndex++;
                    else if (_selectedLayerIndex == i + 1) _selectedLayerIndex--;
                }
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Height(16f)))
                {
                    cfg.layers.RemoveAt(i--);
                    if (_selectedLayerIndex >= cfg.layers.Count)
                        _selectedLayerIndex = cfg.layers.Count - 1;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(1f));
            }

            EditorGUILayout.EndScrollView();

            // ── Bottom buttons ────────────────────────────────────────────────
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto Populate from Browser"))
                AutoPopulate(cfg);
            if (GUILayout.Button("Save Asset", GUILayout.Width(76f)))
            {
                EditorUtility.SetDirty(cfg);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Parallax Editor] Saved '{cfg.biomeName}'.");
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(cfg);
        }

        // ════════════════════════════════════════════
        //  SPRITE BROWSER PANEL (right)
        // ════════════════════════════════════════════
        void DrawBrowserPanel()
        {
            GUILayout.Label("Sprites", EditorStyles.boldLabel);

            // Current path + up button
            EditorGUILayout.BeginHorizontal();
            string displayPath = _browserFolder.Length > 28
                ? "..." + _browserFolder.Substring(_browserFolder.Length - 25) : _browserFolder;
            GUILayout.Label(displayPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("↑", EditorStyles.miniButton, GUILayout.Width(20f))
                && _browserFolder != SPRITES_ROOT)
            {
                BrowseFolder(_browserFolder.Substring(0, _browserFolder.LastIndexOf('/')));
            }
            EditorGUILayout.EndHorizontal();

            // Sub-folders
            foreach (var sub in _subFolders)
            {
                string name = sub.Substring(sub.LastIndexOf('/') + 1);
                if (GUILayout.Button("📁 " + name, EditorStyles.miniButton))
                    BrowseFolder(sub);
            }

            if (_subFolders.Count > 0) GUILayout.Space(4f);

            // Texture grid (2 columns)
            _browserScroll = EditorGUILayout.BeginScrollView(_browserScroll, GUILayout.ExpandHeight(true));
            const float thumb = 54f;
            const int   cols  = 2;
            for (int i = 0; i < _browserTextures.Count; i += cols)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + cols, _browserTextures.Count); j++)
                {
                    var tex = _browserTextures[j];
                    EditorGUILayout.BeginVertical(GUILayout.Width(thumb + 6f));
                    var rect = GUILayoutUtility.GetRect(thumb, thumb, GUILayout.Width(thumb));
                    if (tex != null)
                        GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        AssignToSelected(tex);
                    GUILayout.Label(tex?.name ?? "", EditorStyles.centeredGreyMiniLabel,
                        GUILayout.Width(thumb + 6f));
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // Hint text
            GUILayout.Space(2f);
            if (_selectedLayerIndex >= 0 && Selected != null)
                GUILayout.Label($"Click → assign to layer [{_selectedLayerIndex}]",
                    EditorStyles.centeredGreyMiniLabel);
            else
                GUILayout.Label("Select a layer, then click a sprite",
                    EditorStyles.centeredGreyMiniLabel);
        }

        // ════════════════════════════════════════════
        //  PREVIEW STRIP (bottom)
        // ════════════════════════════════════════════
        void DrawPreview(Rect previewRect)
        {
            // Dark background
            EditorGUI.DrawRect(previewRect, new Color(0.06f, 0.04f, 0.12f, 1f));

            var cfg = Selected;
            float controlsH = 22f;
            float drawAreaH = previewRect.height - controlsH;

            if (cfg == null || cfg.layers.Count == 0)
            {
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 };
                GUI.Label(new Rect(previewRect.x, previewRect.y, previewRect.width, drawAreaH),
                    "Preview — select a config with layers assigned", style);
            }
            else
            {
                // Compute a 16:9 draw rect centered in the strip
                float drawH = drawAreaH - 4f;
                float drawW = Mathf.Min(drawH * (16f / 9f), previewRect.width - 8f);
                drawH = drawW / (16f / 9f);

                var drawRect = new Rect(
                    previewRect.x + (previewRect.width - drawW) * 0.5f,
                    previewRect.y + 2f,
                    drawW, drawH);

                EditorGUI.DrawRect(drawRect, Color.black);

                // Sync offset array length
                int n = cfg.layers.Count;
                if (_previewOffsets.Length != n) _previewOffsets = new float[n];

                // Draw layers inside a clip rect so tiles don't spill over
                GUI.BeginClip(drawRect);
                for (int i = 0; i < n; i++)
                {
                    var layer = cfg.layers[i];
                    if (layer.texture == null) continue;

                    var oldColor = GUI.color;
                    GUI.color = layer.tint;

                    // Scale texture height to fill the preview; tile horizontally
                    float tileW  = drawH * ((float)layer.texture.width / layer.texture.height);
                    float startX = -Mathf.Repeat(_previewOffsets[i], 1f) * tileW;
                    for (float x = startX; x < drawW; x += tileW)
                        GUI.DrawTexture(new Rect(x, 0f, tileW, drawH), layer.texture,
                            ScaleMode.StretchToFill);

                    GUI.color = oldColor;
                }

                // Dim overlay inside clip
                if (cfg.dimColor.a > 0.01f)
                {
                    GUI.color = cfg.dimColor;
                    GUI.DrawTexture(new Rect(0f, 0f, drawW, drawH), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                GUI.EndClip();
            }

            // ── Control bar ──────────────────────────────────────────────────
            float ctrlY = previewRect.yMax - controlsH + 2f;
            float cx    = previewRect.x + 4f;

            if (GUI.Button(new Rect(cx, ctrlY, 68f, 18f),
                    _previewPlaying ? "⏸ Pause" : "▶ Play", EditorStyles.miniButton))
            {
                _previewPlaying = !_previewPlaying;
                _prevTime = EditorApplication.timeSinceStartup;
            }
            cx += 72f;

            if (GUI.Button(new Rect(cx, ctrlY, 48f, 18f), "Reset", EditorStyles.miniButton))
            {
                _previewOffsets = new float[Selected?.layers.Count ?? 0];
            }
            cx += 52f;

            // Master speed slider (also edits the config live)
            var cfg2 = Selected;
            if (cfg2 != null)
            {
                GUI.Label(new Rect(cx, ctrlY, 46f, 18f), "Speed:", EditorStyles.miniLabel);
                cx += 46f;
                float newSpeed = GUI.HorizontalSlider(new Rect(cx, ctrlY + 3f, 100f, 14f),
                    cfg2.masterSpeed, 0f, 0.3f);
                if (Mathf.Abs(newSpeed - cfg2.masterSpeed) > 0.0001f)
                {
                    cfg2.masterSpeed = newSpeed;
                    EditorUtility.SetDirty(cfg2);
                }
                cx += 106f;
                GUI.Label(new Rect(cx, ctrlY, 60f, 18f), cfg2.masterSpeed.ToString("F3"),
                    EditorStyles.miniLabel);
            }
        }

        // ════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════

        void RefreshConfigList()
        {
            _configs.Clear();
            var guids = AssetDatabase.FindAssets("t:ParallaxBiomeConfig");
            foreach (var g in guids)
            {
                var c = AssetDatabase.LoadAssetAtPath<ParallaxBiomeConfig>(
                    AssetDatabase.GUIDToAssetPath(g));
                if (c != null) _configs.Add(c);
            }
            _configs = _configs
                .OrderBy(c => string.IsNullOrEmpty(c.biomeName) ? c.name : c.biomeName)
                .ToList();
        }

        void BrowseFolder(string folder)
        {
            _browserFolder   = folder;
            _subFolders.Clear();
            _browserTextures.Clear();

            if (!AssetDatabase.IsValidFolder(folder)) return;

            // Immediate sub-folders
            foreach (var sub in AssetDatabase.GetSubFolders(folder))
                _subFolders.Add(sub);

            // Textures directly inside this folder (not in sub-folders)
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                // Skip if it lives deeper than one level
                var rel = path.Substring(folder.Length + 1);
                if (rel.Contains('/')) continue;
                // Skip example composites
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null && !tex.name.StartsWith("example"))
                    _browserTextures.Add(tex);
            }
            _browserTextures = _browserTextures.OrderBy(t => t.name).ToList();
        }

        void AssignToSelected(Texture2D tex)
        {
            var cfg = Selected;
            if (cfg == null || _selectedLayerIndex < 0 || _selectedLayerIndex >= cfg.layers.Count) return;
            cfg.layers[_selectedLayerIndex].texture = tex;
            EditorUtility.SetDirty(cfg);
        }

        void AutoPopulate(ParallaxBiomeConfig cfg)
        {
            if (_browserTextures.Count == 0)
            {
                EditorUtility.DisplayDialog("Auto Populate",
                    "Navigate the Sprite Browser to a variant folder first.\n" +
                    "Example: Caged Swamps / Greenish Blue\n\n" +
                    "Then click 'Auto Populate from Browser'.", "OK");
                return;
            }

            // Textures are sorted ascending: "1 close" → "6 sky".
            // Reverse so index 0 = sky (farthest, scrollFactor 0).
            var sorted = _browserTextures.OrderBy(t => t.name).Reverse().ToList();
            float[] factors = ComputeDefaultScrollFactors(sorted.Count);

            cfg.layers.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                cfg.layers.Add(new ParallaxLayerData
                {
                    texture      = sorted[i],
                    label        = ParseLabel(sorted[i].name),
                    scrollFactor = factors[i],
                    tint         = Color.white
                });
            }

            _selectedLayerIndex = -1;
            _previewOffsets     = new float[cfg.layers.Count];
            EditorUtility.SetDirty(cfg);
            Debug.Log($"[Parallax Editor] Auto-populated {cfg.layers.Count} layers for '{cfg.biomeName}'.");
        }

        // Produces scroll factors curved from 0.0 (index 0 = sky) to 0.40 (last = close)
        static float[] ComputeDefaultScrollFactors(int count)
        {
            if (count <= 0) return System.Array.Empty<float>();
            if (count == 1) return new[] { 0f };
            var f = new float[count];
            for (int i = 0; i < count; i++)
                f[i] = 0.40f * Mathf.Pow((float)i / (count - 1), 1.5f);
            return f;
        }

        // Extracts a human-readable label from sprite filenames like "6 sky1" → "sky"
        static string ParseLabel(string filename)
        {
            string noExt = Path.GetFileNameWithoutExtension(filename ?? "");
            int sp = noExt.IndexOf(' ');
            string lbl = sp >= 0 ? noExt.Substring(sp + 1).Trim() : noExt;
            while (lbl.Length > 0 && char.IsDigit(lbl[lbl.Length - 1]))
                lbl = lbl.Substring(0, lbl.Length - 1).Trim();
            return string.IsNullOrEmpty(lbl) ? noExt : lbl;
        }

        void CreateConfig(string name)
        {
            EnsureResourcesFolder();
            var cfg = CreateInstance<ParallaxBiomeConfig>();
            cfg.biomeName   = name;
            cfg.masterSpeed = 0.04f;
            cfg.dimColor    = new Color(0f, 0f, 0.05f, 0.45f);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{CONFIGS_PATH}/{name}.asset");
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshConfigList();
            _selectedConfigIndex = _configs.FindIndex(c => c == cfg);
        }

        void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(CONFIGS_PATH))
                AssetDatabase.CreateFolder("Assets/Resources", "ParallaxConfigs");
        }

        void DrawVDivider()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(1f, float.MaxValue, GUILayout.ExpandHeight(true)),
                new Color(0.1f, 0.1f, 0.1f, 0.5f));
            EditorGUILayout.EndVertical();
        }
    }
}
