using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Core
{
    public class NpcController : MonoBehaviour
    {
        public NpcData Data { get; private set; }
        public SpriteRenderer spriteRenderer;

        private bool playerInRange = false;
        private GameObject interactPrompt;
        private bool hasCharacterDef = false;

        public void Initialize(NpcData data, Sprite sprite)
        {
            Data = data;

            // Check if a CharacterAnimator is handling the sprite (don't tint character art)
            hasCharacterDef = GetComponent<CharacterAnimator>() != null;

            if (spriteRenderer != null)
            {
                if (data.IsDefeated)
                {
                    spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                else if (!hasCharacterDef)
                {
                    // Only tint by element when using placeholder sprites
                    spriteRenderer.color = GetElementColor(data.Element);
                }
                // Character art NPCs keep Color.white (no tint)
            }
        }

        void Update()
        {
            if (Data == null || Data.IsDefeated) return;

            var player = ExplorationManager.Instance?.player;
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.transform.position);
            bool wasInRange = playerInRange;
            playerInRange = dist < 1.0f; // Tight range for pixel-art characters

            if (playerInRange && !wasInRange) ShowPrompt();
            if (!playerInRange && wasInRange) HidePrompt();

            if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TriggerEncounter();
            }
        }

        private void TriggerEncounter()
        {
            HidePrompt();
            if (EncounterManager.Instance != null)
                EncounterManager.Instance.StartEncounter(this);
        }

        private void ShowPrompt()
        {
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
                return;
            }

            interactPrompt = new GameObject("InteractPrompt");
            interactPrompt.transform.SetParent(transform, false);

            // Position above the character sprite
            // Pixel-art characters at 3x scale are ~1 unit tall
            float yOffset = hasCharacterDef ? 0.6f : 0.8f;
            interactPrompt.transform.localPosition = new Vector3(0f, yOffset, 0f);

            // Create a simple diamond/indicator shape using a generated sprite
            var sr = interactPrompt.AddComponent<SpriteRenderer>();
            sr.sprite = CreateIndicatorSprite();
            sr.color = new Color(0.83f, 0.66f, 0.26f, 0.9f); // Gold
            sr.sortingLayerName = "PropsAbove"; // Above characters
            sr.sortingOrder = 10;
            interactPrompt.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        private void HidePrompt()
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }

        /// <summary>
        /// Creates a small 8x8 diamond indicator sprite at runtime.
        /// </summary>
        private static Sprite indicatorCache;
        private static Sprite CreateIndicatorSprite()
        {
            if (indicatorCache != null) return indicatorCache;

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[64];

            // Draw a diamond / exclamation pattern
            Color c = Color.white;
            Color t = Color.clear;

            // Simple down-arrow / diamond shape
            pixels[3 + 7 * 8] = c; pixels[4 + 7 * 8] = c;                          // top
            pixels[2 + 6 * 8] = c; pixels[3 + 6 * 8] = c; pixels[4 + 6 * 8] = c; pixels[5 + 6 * 8] = c; // row 2
            pixels[3 + 5 * 8] = c; pixels[4 + 5 * 8] = c;                          // row 3
            pixels[3 + 4 * 8] = c; pixels[4 + 4 * 8] = c;                          // row 4
            pixels[3 + 3 * 8] = c; pixels[4 + 3 * 8] = c;                          // row 5
            // gap
            pixels[3 + 1 * 8] = c; pixels[4 + 1 * 8] = c;                          // dot

            for (int i = 0; i < 64; i++)
                if (pixels[i].a == 0) pixels[i] = t;

            tex.SetPixels(pixels);
            tex.Apply();

            indicatorCache = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 32f);
            return indicatorCache;
        }

        private Color GetElementColor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return ElementColors.Fire;
                case ElementType.Water: return ElementColors.Water;
                case ElementType.Earth: return ElementColors.Earth;
                case ElementType.Wind: return ElementColors.Wind;
                default: return Color.white;
            }
        }
    }
}
