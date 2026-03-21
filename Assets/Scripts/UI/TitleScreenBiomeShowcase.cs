using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Cycles each biome as a full-screen showcase on the title screen.
    ///
    /// HOW IT WORKS (no squishing):
    ///   A dedicated full-screen overlay ParallaxBackgroundController ('overlay') is always
    ///   sized at the full canvas resolution. Only its RectMask2D padding is animated —
    ///   padding clips the visible window from the quadrant's area to full screen (expand)
    ///   or back (shrink). The parallax image itself never changes size, so there is zero
    ///   distortion at any point in the animation.
    ///
    ///   The original quadrant panels are never touched during the showcase.
    ///
    /// OnDisable only hides the overlay (no transform hierarchy changes), so it is safe
    /// to call even while Unity is deactivating the parent Screen_MainMenu.
    /// </summary>
    public class TitleScreenBiomeShowcase : MonoBehaviour
    {
        [Header("Quadrants in cycle order (source of biome configs)")]
        public ParallaxBackgroundController[] showcaseOrder;

        [Header("Full-screen overlay — created by CanvasBuilderTool")]
        public ParallaxBackgroundController overlay;

        [Header("Timing (seconds)")]
        public float initialPause   = 3f;
        public float expandDuration = 0.9f;
        public float holdDuration   = 8f;
        public float shrinkDuration = 0.9f;
        public float pauseBetween   = 1f;

        private bool _showing;

        void OnEnable()
        {
            if (showcaseOrder == null || showcaseOrder.Length == 0 || overlay == null) return;
            StartCoroutine(ShowcaseLoop());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            RestoreOverlay();
        }

        private IEnumerator ShowcaseLoop()
        {
            yield return new WaitForSeconds(initialPause);

            var mask     = overlay.GetComponent<RectMask2D>();
            var parentRt = (RectTransform)overlay.transform.parent;

            int index = 0;
            while (true)
            {
                var ctrl = showcaseOrder[index];
                if (ctrl == null || ctrl.ActiveConfig == null)
                {
                    index = (index + 1) % showcaseOrder.Length;
                    continue;
                }

                // Compute padding before activating anything.
                // Formula: padding = (left, bottom, right, top) in canvas units.
                // Since BG_Title fills Screen_MainMenu identically, the quadrant's
                // anchor values describe the same area in both coordinate spaces.
                var quadRt  = ctrl.GetComponent<RectTransform>();
                float fullW = Mathf.Max(parentRt.rect.width,  1920f);
                float fullH = Mathf.Max(parentRt.rect.height, 1080f);

                var quadPadding = new Vector4(
                          quadRt.anchorMin.x         * fullW,   // left
                          quadRt.anchorMin.y         * fullH,   // bottom
                    (1f - quadRt.anchorMax.x)        * fullW,   // right
                    (1f - quadRt.anchorMax.y)        * fullH);  // top

                // Activate the overlay BEFORE LoadConfig so that rt.rect.width is
                // guaranteed to be 1920 (from Canvas layout) when BuildLayers runs.
                // No layers exist yet so there is nothing to render — no flash.
                mask.padding = quadPadding;
                overlay.gameObject.SetActive(true);
                yield return null; // Canvas layout settles → rt.rect.width = 1920

                // BuildLayers now sees the correct full-screen dimensions.
                // Foreground layers get sizeDelta.y = tex.h/tex.w * 1920 (native aspect
                // ratio at full width), sky layer stretches to fill — identical logic
                // to the quadrant controllers, just at full-screen resolution.
                overlay.LoadConfig(ctrl.ActiveConfig);
                overlay.SetDim(0f);
                overlay.SetPlaying(true);
                _showing = true;

                yield return null; // UV lazy-init runs in ParallaxBackgroundController.Update()

                // Expand: grow the visible window from the quadrant area to full screen.
                yield return TweenPadding(mask, quadPadding, Vector4.zero, expandDuration);

                yield return new WaitForSeconds(holdDuration);

                // Shrink: window clips back to the quadrant area.
                // Content stays at full 1920×1080 — only the viewport changes.
                yield return TweenPadding(mask, Vector4.zero, quadPadding, shrinkDuration);

                overlay.SetPlaying(false);
                overlay.gameObject.SetActive(false);
                mask.padding = Vector4.zero;
                _showing = false;

                yield return new WaitForSeconds(pauseBetween);

                index = (index + 1) % showcaseOrder.Length;
            }
        }

        private IEnumerator TweenPadding(RectMask2D mask, Vector4 from, Vector4 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                mask.padding = Vector4.LerpUnclamped(from, to, s);
                yield return null;
            }
            mask.padding = to;
        }

        // Safe to call from OnDisable — only deactivates the overlay, no hierarchy changes.
        private void RestoreOverlay()
        {
            if (!_showing || overlay == null) return;
            overlay.SetPlaying(false);
            overlay.gameObject.SetActive(false);
            var mask = overlay.GetComponent<RectMask2D>();
            if (mask != null) mask.padding = Vector4.zero;
            _showing = false;
        }
    }
}
