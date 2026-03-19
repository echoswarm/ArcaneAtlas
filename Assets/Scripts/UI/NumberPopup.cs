using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneAtlas.UI
{
    public enum NumberColor { Red, Green, Yellow, White }

    /// <summary>
    /// Manages sprite-based number popups using the Number Themes pixel digit sprites.
    /// Red = damage, Green = healing/buffs, Yellow = gold/HP changes.
    /// Call Spawn() from anywhere to show a floating number at a world position or UI transform.
    /// </summary>
    public static class NumberPopup
    {
        private static Dictionary<NumberColor, Sprite[]> digitSprites;
        private static bool loaded;

        /// <summary>
        /// Loads digit sprites from Resources/NumberThemes/. Call once or it auto-loads on first Spawn.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (loaded) return;
            digitSprites = new Dictionary<NumberColor, Sprite[]>();

            LoadDigitSet(NumberColor.Red, "NumberThemes/T_PixelRed");
            LoadDigitSet(NumberColor.Green, "NumberThemes/T_PixelGreen");
            LoadDigitSet(NumberColor.Yellow, "NumberThemes/T_PixelYellow");
            LoadDigitSet(NumberColor.White, "NumberThemes/T_PixelWhite");

            loaded = true;
        }

        private static void LoadDigitSet(NumberColor color, string resourcePath)
        {
            var allSprites = Resources.LoadAll<Sprite>(resourcePath);
            var digits = new Sprite[10];

            foreach (var sprite in allSprites)
            {
                // Sprite names are like T_PixelRed_0, T_PixelRed_1, etc.
                string name = sprite.name;
                int underscoreIdx = name.LastIndexOf('_');
                if (underscoreIdx >= 0)
                {
                    string digitStr = name.Substring(underscoreIdx + 1);
                    if (int.TryParse(digitStr, out int digit) && digit >= 0 && digit <= 9)
                        digits[digit] = sprite;
                }
            }

            digitSprites[color] = digits;
        }

        /// <summary>
        /// Spawns a floating sprite-based number at the given UI transform's position.
        /// </summary>
        /// <param name="parent">The Canvas or parent transform to spawn under</param>
        /// <param name="worldPos">World position to spawn at</param>
        /// <param name="value">The number to display (positive)</param>
        /// <param name="color">Red=damage, Green=heal, Yellow=gold</param>
        /// <param name="prefix">Optional prefix like "-" or "+"</param>
        /// <param name="scale">Size multiplier (1.0 = default)</param>
        /// <param name="duration">How long the popup lasts</param>
        public static void Spawn(Transform parent, Vector3 worldPos, int value, NumberColor color,
            string prefix = "", float scale = 1f, float duration = 0.8f)
        {
            Spawn(parent, worldPos, value, color, prefix, scale, duration, null);
        }

        /// <summary>
        /// Convenience: spawn at a UI element's position (e.g., a card slot).
        /// </summary>
        public static void SpawnAtTransform(Transform target, int value, NumberColor color,
            string prefix = "", float scale = 1f, float duration = 0.8f)
        {
            if (target == null) return;

            var canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Spawn(canvas.transform, target.position, value, color, prefix, scale, duration);
        }

        /// <summary>
        /// Spawn a tinted number popup using White sprites colored by Image.color.
        /// Use for custom colors: purple (XP), orange (crits), etc.
        /// Falls back to Yellow if White sprites aren't loaded yet.
        /// </summary>
        public static void SpawnTinted(Transform target, int value, Color tint,
            string prefix = "", float scale = 1f, float duration = 0.8f)
        {
            if (target == null) return;
            EnsureLoaded();

            // Use White sprites if available, otherwise fall back to Yellow
            NumberColor baseColor = digitSprites.ContainsKey(NumberColor.White) &&
                                    digitSprites[NumberColor.White][0] != null
                                    ? NumberColor.White : NumberColor.Yellow;

            var canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Spawn(canvas.transform, target.position, value, baseColor, prefix, scale, duration, tint);
        }

        /// <summary>
        /// Full spawn with optional tint override (applies to digit Images when using White base).
        /// </summary>
        public static void Spawn(Transform parent, Vector3 worldPos, int value, NumberColor color,
            string prefix, float scale, float duration, Color? tintOverride)
        {
            EnsureLoaded();

            if (!digitSprites.ContainsKey(color)) return;
            var digits = digitSprites[color];

            // Create container
            var container = new GameObject("NumberPopup", typeof(RectTransform), typeof(CanvasGroup));
            container.transform.SetParent(parent, false);
            container.transform.position = worldPos;

            var containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = containerRT.anchorMax = new Vector2(0.5f, 0.5f);

            // Resolve dash/tint color
            Color dashColor;
            if (tintOverride.HasValue)
                dashColor = tintOverride.Value;
            else if (color == NumberColor.Red) dashColor = new Color(0.9f, 0.2f, 0.2f);
            else if (color == NumberColor.Green) dashColor = new Color(0.2f, 0.8f, 0.2f);
            else dashColor = new Color(0.9f, 0.8f, 0.2f);

            // Build digit string
            string numStr = prefix + value.ToString();

            // Layout digits horizontally
            float digitHeight = 40f * scale;
            float xOffset = 0f;
            var digitImages = new List<RectTransform>();

            foreach (char c in numStr)
            {
                if (c == '-' || c == '+')
                {
                    var dashGO = new GameObject("Dash", typeof(RectTransform), typeof(Image));
                    dashGO.transform.SetParent(container.transform, false);
                    var dashRT = dashGO.GetComponent<RectTransform>();
                    var dashImg = dashGO.GetComponent<Image>();
                    dashImg.color = dashColor;
                    dashImg.raycastTarget = false;
                    float dashWidth = 12f * scale;
                    float dashHeight = 4f * scale;
                    dashRT.anchoredPosition = new Vector2(xOffset + dashWidth / 2f, 0f);
                    dashRT.sizeDelta = new Vector2(dashWidth, dashHeight);
                    xOffset += dashWidth + 2f * scale;
                    digitImages.Add(dashRT);
                    continue;
                }

                if (!char.IsDigit(c)) continue;
                int d = c - '0';
                Sprite sprite = digits[d];
                if (sprite == null) continue;

                var digitGO = new GameObject($"D{d}", typeof(RectTransform), typeof(Image));
                digitGO.transform.SetParent(container.transform, false);
                var rt = digitGO.GetComponent<RectTransform>();
                var img = digitGO.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;

                // Apply tint to white sprites
                if (tintOverride.HasValue)
                    img.color = tintOverride.Value;

                float aspect = sprite.rect.width / sprite.rect.height;
                float digitWidth = digitHeight * aspect;
                rt.sizeDelta = new Vector2(digitWidth, digitHeight);
                rt.anchoredPosition = new Vector2(xOffset + digitWidth / 2f, 0f);
                xOffset += digitWidth + 2f * scale;
                digitImages.Add(rt);
            }

            // Center the whole number group
            float totalWidth = xOffset;
            foreach (var drt in digitImages)
                drt.anchoredPosition -= new Vector2(totalWidth / 2f, 0f);

            // Animate
            var runner = container.AddComponent<NumberPopupRunner>();
            runner.StartCoroutine(runner.Animate(container, duration));
        }
    }

    /// <summary>
    /// MonoBehaviour that runs the float-up + fade animation on a number popup, then destroys it.
    /// </summary>
    public class NumberPopupRunner : MonoBehaviour
    {
        public IEnumerator Animate(GameObject container, float duration)
        {
            var cg = container.GetComponent<CanvasGroup>();
            Vector3 startPos = container.transform.localPosition;
            float floatDistance = 60f;
            float elapsed = 0f;

            // Start slightly scaled up
            container.transform.localScale = Vector3.one * 1.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float up
                container.transform.localPosition = startPos + new Vector3(0f, floatDistance * t, 0f);

                // Scale: pop in then shrink
                float s = t < 0.15f ? Mathf.Lerp(1.3f, 1.0f, t / 0.15f) : 1.0f;
                container.transform.localScale = Vector3.one * s;

                // Fade out in the last 40%
                if (cg != null)
                    cg.alpha = t > 0.6f ? Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f) : 1f;

                yield return null;
            }

            Destroy(container);
        }
    }
}
