using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.UI;

namespace ArcaneAtlas.Combat
{
    /// <summary>
    /// Handles combat visual effects: arc attacks, damage popups, spinning destruction, shake defense.
    /// Attached to the CombatUI's GameObject at runtime.
    /// </summary>
    public class CombatAnimator : MonoBehaviour
    {
        /// <summary>
        /// Card arcs toward its target then returns.
        /// Player cards arc upward, opponent cards arc downward.
        /// </summary>
        public IEnumerator AnimateAttack(Transform slot, bool isPlayer)
        {
            if (slot == null) yield break;

            Vector3 original = slot.localPosition;
            float dir = isPlayer ? 1f : -1f;
            float arcHeight = 60f;
            float arcForward = 40f;
            float duration = 0.2f;

            // Arc toward target
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float parabola = 4f * t * (1f - t); // peaks at 0.5
                float forward = dir * arcForward * t;
                float up = parabola * arcHeight * dir;
                slot.localPosition = original + new Vector3(forward * 0.3f, forward + up * 0.5f, 0f);
                yield return null;
            }

            // Snap back quickly
            elapsed = 0f;
            float returnDuration = 0.1f;
            Vector3 peakPos = slot.localPosition;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnDuration;
                slot.localPosition = Vector3.Lerp(peakPos, original, t);
                yield return null;
            }

            slot.localPosition = original;
        }

        /// <summary>
        /// Shows floating sprite-based damage number above a card slot.
        /// Uses NumberPopup system with red pixel digits.
        /// </summary>
        public IEnumerator ShowDamagePopup(Transform slot, int damage)
        {
            if (slot == null) yield break;
            NumberPopup.SpawnAtTransform(slot, damage, NumberColor.Red, "-", 1.2f, 0.8f);
            yield return null; // Yield once so coroutine is valid
        }

        /// <summary>
        /// Shows floating healing number (green digits).
        /// </summary>
        public void ShowHealPopup(Transform slot, int amount)
        {
            if (slot == null) return;
            NumberPopup.SpawnAtTransform(slot, amount, NumberColor.Green, "+", 1.0f, 0.8f);
        }

        /// <summary>
        /// Shows floating gold/HP number (yellow digits).
        /// </summary>
        public void ShowGoldPopup(Transform slot, int amount)
        {
            if (slot == null) return;
            NumberPopup.SpawnAtTransform(slot, amount, NumberColor.Yellow, "+", 1.0f, 0.8f);
        }

        /// <summary>
        /// Defeated card spins and shrinks away.
        /// </summary>
        public IEnumerator AnimateDefeat(Transform slot)
        {
            if (slot == null) yield break;

            Vector3 originalScale = slot.localScale;
            Quaternion originalRotation = slot.localRotation;
            var image = slot.GetComponent<Image>();
            Color startColor = image != null ? image.color : Color.white;

            float duration = 0.5f;
            float elapsed = 0f;
            float spinSpeed = 720f; // 2 full rotations

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Shrink
                float scale = Mathf.Lerp(1f, 0f, t * t);
                slot.localScale = originalScale * scale;

                // Spin
                float angle = spinSpeed * t;
                slot.localRotation = Quaternion.Euler(0f, 0f, angle);

                // Fade
                if (image != null)
                    image.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);

                yield return null;
            }

            // Restore for reuse next round
            slot.localScale = originalScale;
            slot.localRotation = originalRotation;
            if (image != null)
                image.color = startColor;
        }

        /// <summary>
        /// Card shakes left-right when taking damage but surviving (standing its ground).
        /// </summary>
        public IEnumerator AnimateHit(Transform slot)
        {
            if (slot == null) yield break;

            Vector3 original = slot.localPosition;
            var image = slot.GetComponent<Image>();
            Color originalColor = image != null ? image.color : Color.white;

            // Flash red
            if (image != null)
                image.color = new Color(1f, 0.3f, 0.3f, originalColor.a);

            // Shake 3 times
            float shakeIntensity = 8f;
            float shakeInterval = 0.04f;
            for (int i = 0; i < 3; i++)
            {
                slot.localPosition = original + new Vector3(-shakeIntensity, 0f, 0f);
                yield return new WaitForSeconds(shakeInterval);
                slot.localPosition = original + new Vector3(shakeIntensity, 0f, 0f);
                yield return new WaitForSeconds(shakeInterval);
            }
            slot.localPosition = original;

            // Fade red back to normal
            float elapsed = 0f;
            float fadeDuration = 0.15f;
            Color redTint = new Color(1f, 0.3f, 0.3f, originalColor.a);
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (image != null)
                    image.color = Color.Lerp(redTint, originalColor, elapsed / fadeDuration);
                yield return null;
            }
            if (image != null)
                image.color = originalColor;
        }

        /// <summary>
        /// Celebration burst when a card upgrades via triple merge.
        /// Scale pulse (1.0→1.2→1.0) + gold flash + particle sparkle burst. 0.5s total.
        /// </summary>
        public IEnumerator AnimateMerge(Transform slot, bool isGold = false)
        {
            if (slot == null) yield break;

            Vector3 originalScale = slot.localScale;
            Color sparkleColor = isGold ? new Color(1f, 0.84f, 0f, 1f) : new Color(0.75f, 0.75f, 0.85f, 1f);

            // Spawn particle burst
            SpawnSparkles(slot, sparkleColor, isGold ? 12 : 8);

            // Scale pulse: grow to 1.2x over 0.15s
            float elapsed = 0f;
            float growDuration = 0.15f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growDuration;
                slot.localScale = originalScale * Mathf.Lerp(1f, 1.2f, t);
                yield return null;
            }

            // Flash gold/silver
            var image = slot.GetComponent<Image>();
            Color originalColor = image != null ? image.color : Color.white;
            if (image != null)
                image.color = sparkleColor;

            yield return new WaitForSeconds(0.05f);

            // Shrink back to 1.0x over 0.15s + fade flash
            elapsed = 0f;
            float shrinkDuration = 0.15f;
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shrinkDuration;
                slot.localScale = originalScale * Mathf.Lerp(1.2f, 1f, t);
                if (image != null)
                    image.color = Color.Lerp(sparkleColor, originalColor, t);
                yield return null;
            }

            slot.localScale = originalScale;
            if (image != null)
                image.color = originalColor;
        }

        /// <summary>
        /// Spawns small sparkle particles radiating outward from a card slot.
        /// Uses simple UI Images that scale down and fade as they fly out.
        /// </summary>
        private void SpawnSparkles(Transform slot, Color color, int count)
        {
            var canvas = slot.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            for (int i = 0; i < count; i++)
            {
                var sparkle = new GameObject("Sparkle", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                sparkle.transform.SetParent(canvas.transform, false);
                sparkle.transform.position = slot.position;

                var rt = sparkle.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(6f, 6f);

                var img = sparkle.GetComponent<Image>();
                img.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.4f));
                img.raycastTarget = false;

                // Random radial direction
                float angle = (360f / count) * i + Random.Range(-20f, 20f);
                float distance = Random.Range(50f, 100f);
                float duration = Random.Range(0.3f, 0.5f);

                StartCoroutine(AnimateSparkle(sparkle, angle, distance, duration));
            }
        }

        private IEnumerator AnimateSparkle(GameObject sparkle, float angle, float distance, float duration)
        {
            var rt = sparkle.GetComponent<RectTransform>();
            var cg = sparkle.GetComponent<CanvasGroup>();
            Vector3 startPos = sparkle.transform.localPosition;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Move outward with deceleration
                float dist = distance * (1f - (1f - t) * (1f - t));
                sparkle.transform.localPosition = startPos + new Vector3(dir.x * dist, dir.y * dist, 0f);

                // Shrink and fade
                float scale = Mathf.Lerp(1.5f, 0f, t);
                rt.localScale = Vector3.one * scale;
                if (cg != null)
                    cg.alpha = Mathf.Lerp(1f, 0f, t * t);

                yield return null;
            }

            Destroy(sparkle);
        }
    }
}
