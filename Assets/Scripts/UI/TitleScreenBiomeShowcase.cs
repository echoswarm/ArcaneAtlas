using System.Collections;
using UnityEngine;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Cycles each title-screen parallax quadrant to full-screen and back in an endless loop.
    /// Quadrants stay inside BG_Title at all times — only their anchors are animated.
    /// SetAsLastSibling within BG_Title brings the active one in front of the other three;
    /// Logo and ButtonGroup live one level up in Screen_MainMenu so they naturally render on top.
    ///
    /// OnDisable only resets anchors (no hierarchy changes) so it is safe to call even while
    /// Unity is deactivating the parent Screen_MainMenu.
    /// </summary>
    public class TitleScreenBiomeShowcase : MonoBehaviour
    {
        [Header("Quadrants in cycle order")]
        public ParallaxBackgroundController[] showcaseOrder;

        [Header("Timing (seconds)")]
        public float initialPause   = 3f;
        public float expandDuration = 0.9f;
        public float holdDuration   = 8f;
        public float shrinkDuration = 0.9f;
        public float pauseBetween   = 1f;

        // Saved state for the currently-expanded quadrant
        private ParallaxBackgroundController _expanded;
        private int     _expandedOrigSiblingIndex;
        private Vector2 _expandedOrigAnchorMin;
        private Vector2 _expandedOrigAnchorMax;

        void OnEnable()
        {
            if (showcaseOrder == null || showcaseOrder.Length == 0) return;
            StartCoroutine(ShowcaseLoop());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            RestoreExpanded();
        }

        private IEnumerator ShowcaseLoop()
        {
            yield return new WaitForSeconds(initialPause);

            int index = 0;
            while (true)
            {
                var ctrl = showcaseOrder[index];
                if (ctrl == null)
                {
                    index = (index + 1) % showcaseOrder.Length;
                    continue;
                }

                var rt = ctrl.GetComponent<RectTransform>();

                // Save original state (quadrant stays in BG_Title — no reparenting)
                _expandedOrigSiblingIndex = rt.GetSiblingIndex();
                _expandedOrigAnchorMin    = rt.anchorMin;
                _expandedOrigAnchorMax    = rt.anchorMax;
                _expanded = ctrl;

                // Bring in front of the other three quadrants within BG_Title.
                // Logo and ButtonGroup are siblings of BG_Title in Screen_MainMenu,
                // so they always render above everything inside BG_Title.
                rt.SetAsLastSibling();

                // Expand to fill BG_Title (= fill screen, since BG_Title stretches full)
                yield return TweenAnchors(rt,
                    _expandedOrigAnchorMin, _expandedOrigAnchorMax,
                    Vector2.zero, Vector2.one,
                    expandDuration);

                yield return new WaitForSeconds(holdDuration);

                // Shrink back to original quadrant position
                yield return TweenAnchors(rt,
                    Vector2.zero, Vector2.one,
                    _expandedOrigAnchorMin, _expandedOrigAnchorMax,
                    shrinkDuration);

                // Restore sibling index (screen is still active here — safe to call)
                rt.SetSiblingIndex(_expandedOrigSiblingIndex);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                _expanded = null;

                yield return new WaitForSeconds(pauseBetween);

                index = (index + 1) % showcaseOrder.Length;
            }
        }

        private IEnumerator TweenAnchors(RectTransform rt,
            Vector2 fromMin, Vector2 fromMax,
            Vector2 toMin,   Vector2 toMax,
            float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                rt.anchorMin = Vector2.LerpUnclamped(fromMin, toMin, s);
                rt.anchorMax = Vector2.LerpUnclamped(fromMax, toMax, s);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                yield return null;
            }
            rt.anchorMin = toMin;
            rt.anchorMax = toMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Called from OnDisable — must NOT touch the transform hierarchy.
        // Only anchor resets are safe while the parent is being deactivated.
        private void RestoreExpanded()
        {
            if (_expanded == null) return;
            var rt = _expanded.GetComponent<RectTransform>();
            rt.anchorMin = _expandedOrigAnchorMin;
            rt.anchorMax = _expandedOrigAnchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _expanded = null;
            // Note: sibling index is intentionally NOT restored here.
            // The quadrants don't overlap in their normal positions so order doesn't matter,
            // and SetSiblingIndex throws during parent deactivation.
        }
    }
}
