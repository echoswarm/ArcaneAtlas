using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Persistent sprite-based number renderer using Number Themes white digit sprites, tinted per-context.
    /// Use this everywhere numbers appear in the game for a unified pixel-art visual style.
    ///
    /// Color conventions:
    ///   Red    (#FF4444) = ATK, damage dealt
    ///   Green  (#44FF44) = HP, healing
    ///   Gold   (#FFD700) = Gold amounts, costs, tier badges
    ///   White  (#FFFFFF) = Round counter, neutral info
    ///   Purple (#AA66FF) = Packs, XP
    ///   Orange (#FF8800) = Urgent/critical
    /// </summary>
    public static class NumberRenderer
    {
        // ── Preset Colors ──
        public static readonly Color Red    = new Color(1.00f, 0.27f, 0.27f, 1f); // #FF4444
        public static readonly Color Green  = new Color(0.27f, 1.00f, 0.27f, 1f); // #44FF44
        public static readonly Color Gold   = new Color(1.00f, 0.84f, 0.00f, 1f); // #FFD700
        public static readonly Color White  = new Color(1.00f, 1.00f, 1.00f, 1f); // #FFFFFF
        public static readonly Color Purple = new Color(0.67f, 0.40f, 1.00f, 1f); // #AA66FF
        public static readonly Color Orange = new Color(1.00f, 0.53f, 0.00f, 1f); // #FF8800

        private static Sprite[] whiteDigits;
        private static bool loaded;

        /// <summary>
        /// Loads the white digit sprite sheet from Resources/NumberThemes/T_PixelWhite.
        /// Auto-called on first Render(). Also loads colored sets into NumberPopup for popups.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (loaded) return;

            whiteDigits = new Sprite[10];
            var allSprites = Resources.LoadAll<Sprite>("NumberThemes/T_PixelWhite");
            foreach (var sprite in allSprites)
            {
                string name = sprite.name;
                int idx = name.LastIndexOf('_');
                if (idx >= 0)
                {
                    string digitStr = name.Substring(idx + 1);
                    if (int.TryParse(digitStr, out int d) && d >= 0 && d <= 9)
                        whiteDigits[d] = sprite;
                }
            }

            // Fallback: if white sprites aren't available, try red as base
            if (whiteDigits[0] == null)
            {
                allSprites = Resources.LoadAll<Sprite>("NumberThemes/T_PixelRed");
                foreach (var sprite in allSprites)
                {
                    string name = sprite.name;
                    int idx = name.LastIndexOf('_');
                    if (idx >= 0)
                    {
                        string digitStr = name.Substring(idx + 1);
                        if (int.TryParse(digitStr, out int d) && d >= 0 && d <= 9)
                            whiteDigits[d] = sprite;
                    }
                }
            }

            loaded = true;
        }

        /// <summary>
        /// Renders a sprite-based number into a named child container of the parent transform.
        /// Creates the container if it doesn't exist. Updates digits in place if it does.
        ///
        /// The container is positioned using anchorMin/anchorMax relative to the parent RectTransform.
        /// Digits are laid out horizontally, centered within the container.
        /// </summary>
        /// <param name="parent">Parent RectTransform to attach to</param>
        /// <param name="childName">Unique name for this number display (e.g., "AtkNum", "GoldNum")</param>
        /// <param name="value">The integer to display</param>
        /// <param name="tint">Color to tint the white sprites</param>
        /// <param name="digitHeight">Height of each digit in pixels</param>
        /// <param name="anchorMin">Bottom-left anchor (0-1 relative to parent)</param>
        /// <param name="anchorMax">Top-right anchor (0-1 relative to parent)</param>
        /// <param name="alignment">Horizontal alignment: 0=left, 0.5=center, 1=right</param>
        /// <param name="prefix">Optional prefix string rendered as tinted icon sprites or skipped</param>
        public static void Set(Transform parent, string childName, int value, Color tint,
            float digitHeight, Vector2 anchorMin, Vector2 anchorMax, float alignment = 0.5f)
        {
            EnsureLoaded();
            if (parent == null) return;

            // Find or create container
            var containerT = parent.Find(childName);
            RectTransform containerRT;
            if (containerT == null)
            {
                var go = new GameObject(childName, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                containerRT = go.GetComponent<RectTransform>();
                containerRT.anchorMin = anchorMin;
                containerRT.anchorMax = anchorMax;
                containerRT.offsetMin = containerRT.offsetMax = Vector2.zero;
            }
            else
            {
                containerRT = containerT.GetComponent<RectTransform>();
                // Update anchors in case they changed
                containerRT.anchorMin = anchorMin;
                containerRT.anchorMax = anchorMax;
                containerRT.offsetMin = containerRT.offsetMax = Vector2.zero;
            }

            // Build digit string
            string numStr = value.ToString();

            // Count existing digit children vs needed
            int existingCount = containerRT.childCount;
            int neededCount = numStr.Length;

            // Recycle or create digit Image objects
            float spacing = 2f;
            float xOffset = 0f;
            var widths = new float[neededCount];

            for (int i = 0; i < neededCount; i++)
            {
                int d = numStr[i] - '0';
                if (d < 0 || d > 9) continue;

                Sprite sprite = whiteDigits[d];
                if (sprite == null) continue;

                // Reuse or create
                RectTransform digitRT;
                Image digitImg;
                if (i < existingCount)
                {
                    digitRT = containerRT.GetChild(i).GetComponent<RectTransform>();
                    digitImg = digitRT.GetComponent<Image>();
                }
                else
                {
                    var digitGO = new GameObject($"D{i}", typeof(RectTransform), typeof(Image));
                    digitGO.transform.SetParent(containerRT, false);
                    digitRT = digitGO.GetComponent<RectTransform>();
                    digitImg = digitGO.GetComponent<Image>();
                    digitImg.raycastTarget = false;
                    digitImg.preserveAspect = true;
                }

                digitImg.sprite = sprite;
                digitImg.color = tint;

                float aspect = sprite.rect.width / sprite.rect.height;
                float w = digitHeight * aspect;
                digitRT.sizeDelta = new Vector2(w, digitHeight);
                digitRT.anchoredPosition = new Vector2(xOffset + w / 2f, 0f);
                widths[i] = w;
                xOffset += w + spacing;

                digitRT.gameObject.SetActive(true);
            }

            // Hide excess digit children
            for (int i = neededCount; i < existingCount; i++)
            {
                containerRT.GetChild(i).gameObject.SetActive(false);
            }

            // Apply alignment offset (center, left, or right)
            float totalWidth = xOffset > 0 ? xOffset - spacing : 0f;
            float offsetX = -totalWidth * alignment;
            for (int i = 0; i < neededCount; i++)
            {
                if (i >= containerRT.childCount) break;
                var drt = containerRT.GetChild(i).GetComponent<RectTransform>();
                var pos = drt.anchoredPosition;
                pos.x += offsetX;
                drt.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Overload: render into an existing child area, auto-filling the full parent.
        /// Good for overlaying onto TMP text areas.
        /// </summary>
        public static void Set(Transform parent, string childName, int value, Color tint, float digitHeight)
        {
            Set(parent, childName, value, tint, digitHeight,
                Vector2.zero, Vector2.one, 0.5f);
        }

        /// <summary>
        /// Clears/hides a named number display.
        /// </summary>
        public static void Clear(Transform parent, string childName)
        {
            if (parent == null) return;
            var child = parent.Find(childName);
            if (child != null)
            {
                for (int i = 0; i < child.childCount; i++)
                    child.GetChild(i).gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys a named number display entirely.
        /// </summary>
        public static void Destroy(Transform parent, string childName)
        {
            if (parent == null) return;
            var child = parent.Find(childName);
            if (child != null)
                Object.Destroy(child.gameObject);
        }
    }
}
