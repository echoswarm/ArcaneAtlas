using System.Collections;
using UnityEngine;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Cycles each title-screen parallax quadrant to full-screen and back in an endless loop.
    /// Attach to Screen_MainMenu. Assign showcaseOrder (the 4 quadrant controllers in cycle order)
    /// and showcaseParent (the Screen_MainMenu RectTransform) via CanvasBuilderTool or Inspector.
    ///
    /// When the screen is hidden (OnDisable), any expanded quadrant is synchronously restored
    /// so navigation away always leaves the hierarchy in a clean state.
    /// </summary>
    public class TitleScreenBiomeShowcase : MonoBehaviour
    {
        [Header("Quadrants in cycle order")]
        public ParallaxBackgroundController[] showcaseOrder;

        [Header("Parent that owns Logo + ButtonGroup (Screen_MainMenu root)")]
        public RectTransform showcaseParent;

        [Header("Timing (seconds)")]
        public float initialPause   = 3f;
        public float expandDuration = 0.9f;
        public float holdDuration   = 8f;
        public float shrinkDuration = 0.9f;
        public float pauseBetween   = 1f;

        // Saved state for the currently-expanded quadrant
        private ParallaxBackgroundController _expanded;
        private Transform _expandedOrigParent;
        private int       _expandedOrigSiblingIndex;
        private Vector2   _expandedOrigAnchorMin;
        private Vector2   _expandedOrigAnchorMax;

        void OnEnable()
        {
            if (showcaseOrder == null || showcaseOrder.Length == 0 || showcaseParent == null) return;
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
                Vector2 fromMin = rt.anchorMin;
                Vector2 fromMax = rt.anchorMax;

                // Save original placement
                _expandedOrigParent       = rt.parent;
                _expandedOrigSiblingIndex = rt.GetSiblingIndex();
                _expandedOrigAnchorMin    = fromMin;
                _expandedOrigAnchorMax    = fromMax;

                // Reparent into Screen_MainMenu at index 1:
                //   [0] BG_Title  [1] this quadrant  [2] Logo  [3] ButtonGroup
                // so it renders above the bg but below the title text and buttons.
                rt.SetParent(showcaseParent, false);
                rt.SetSiblingIndex(1);
                _expanded = ctrl;

                // Expand to full screen
                yield return TweenAnchors(rt, fromMin, fromMax, Vector2.zero, Vector2.one, expandDuration);

                // Hold full screen
                yield return new WaitForSeconds(holdDuration);

                // Shrink back to quadrant
                yield return TweenAnchors(rt, Vector2.zero, Vector2.one, fromMin, fromMax, shrinkDuration);

                // Restore to BG_Title
                rt.SetParent(_expandedOrigParent, false);
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

        private void RestoreExpanded()
        {
            if (_expanded == null) return;
            var rt = _expanded.GetComponent<RectTransform>();
            rt.SetParent(_expandedOrigParent, false);
            rt.SetSiblingIndex(_expandedOrigSiblingIndex);
            rt.anchorMin = _expandedOrigAnchorMin;
            rt.anchorMax = _expandedOrigAnchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _expanded = null;
        }
    }
}
