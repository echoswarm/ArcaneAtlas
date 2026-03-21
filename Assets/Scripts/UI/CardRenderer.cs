using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Composites a card visual from CardData + CardStyleDef.
    /// Attach to a card UI prefab root. Creates/updates child elements for
    /// frame, creature art, element icon, name, stats, and tier badge.
    /// </summary>
    public class CardRenderer : MonoBehaviour
    {
        // Child references (auto-created if missing)
        private Image frameImage;
        private Image detailImage;
        private Image creatureImage;
        private Image elementIcon;
        private Image tierBadge;
        private Image bgTint;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI costText;
        private TextMeshProUGUI abilityText;

        private bool isInitialized = false;

        /// <summary>
        /// Renders a card with the given data and style. Creates child UI elements on first call.
        /// </summary>
        public void Render(CardData card, CardStyleDef style)
        {
            if (card == null) return;
            if (!isInitialized) Initialize();

            // Background tint by element
            if (bgTint != null && style != null)
                bgTint.color = style.GetElementTint(card.Element) * 0.3f + new Color(0, 0, 0, 0.7f);

            // Frame by rarity
            if (frameImage != null && style != null)
            {
                frameImage.sprite = style.GetFrame(card.Rarity);
                frameImage.color = Color.white;
                frameImage.enabled = frameImage.sprite != null;
            }

            // Detail overlay by rarity
            if (detailImage != null && style != null)
            {
                detailImage.sprite = style.GetDetail(card.Rarity);
                detailImage.enabled = detailImage.sprite != null;
            }

            // Creature art
            if (creatureImage != null)
            {
                Sprite creatureSprite = style != null ? style.GetCreatureArt(card) : null;
                if (creatureSprite == null)
                {
                    string path = CardDatabase.GetSpritePath(card);
                    creatureSprite = Resources.Load<Sprite>(path);
                }
                creatureImage.sprite = creatureSprite;
                creatureImage.color = Color.white;
                creatureImage.enabled = creatureSprite != null;
            }

            // Element icon
            if (elementIcon != null && style != null)
            {
                elementIcon.sprite = style.GetElementIcon(card.Element);
                elementIcon.enabled = elementIcon.sprite != null;
            }

            // Name
            if (nameText != null)
                nameText.text = card.CardName;

            // Stats (ATK / HP)
            if (statsText != null)
                statsText.text = $"{card.Attack}/{card.Health}";

            // Cost
            if (costText != null)
                costText.text = card.Cost.ToString();

            // Ability
            if (abilityText != null)
                abilityText.text = card.AbilityText ?? "";
        }

        /// <summary>
        /// Renders card back (face-down).
        /// </summary>
        public void RenderBack(CardStyleDef style)
        {
            if (!isInitialized) Initialize();

            if (frameImage != null && style != null && style.CardBack != null)
            {
                frameImage.sprite = style.CardBack;
                frameImage.enabled = true;
            }

            // Hide all other elements
            if (detailImage != null) detailImage.enabled = false;
            if (creatureImage != null) creatureImage.enabled = false;
            if (elementIcon != null) elementIcon.enabled = false;
            if (tierBadge != null) tierBadge.enabled = false;
            if (bgTint != null) bgTint.color = new Color(0.2f, 0.2f, 0.3f);
            if (nameText != null) nameText.text = "";
            if (statsText != null) statsText.text = "";
            if (costText != null) costText.text = "";
            if (abilityText != null) abilityText.text = "";
        }

        /// <summary>
        /// Creates the child UI element hierarchy for card rendering.
        /// </summary>
        private void Initialize()
        {
            isInitialized = true;
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            // Background tint (full card area)
            bgTint = FindOrCreateImage("BgTint", Vector2.zero, Vector2.one);
            bgTint.raycastTarget = false;
            bgTint.color = new Color(0.2f, 0.2f, 0.3f);

            // Creature art (center area)
            creatureImage = FindOrCreateImage("CreatureArt",
                new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.78f));
            creatureImage.preserveAspect = true;
            creatureImage.raycastTarget = false;

            // Frame (full card area, on top of creature)
            frameImage = FindOrCreateImage("Frame", Vector2.zero, Vector2.one);
            frameImage.raycastTarget = false;

            // Detail overlay (on top of frame)
            detailImage = FindOrCreateImage("Detail", Vector2.zero, Vector2.one);
            detailImage.raycastTarget = false;

            // Element icon (top-left corner)
            elementIcon = FindOrCreateImage("ElementIcon",
                new Vector2(0.02f, 0.82f), new Vector2(0.22f, 0.98f));
            elementIcon.preserveAspect = true;
            elementIcon.raycastTarget = false;

            // Tier badge (top-right corner)
            tierBadge = FindOrCreateImage("TierBadge",
                new Vector2(0.78f, 0.82f), new Vector2(0.98f, 0.98f));
            tierBadge.preserveAspect = true;
            tierBadge.raycastTarget = false;

            // Cost text (top-left, over element icon)
            costText = FindOrCreateText("CostText",
                new Vector2(0.02f, 0.82f), new Vector2(0.22f, 0.98f), 14f);
            costText.alignment = TextAlignmentOptions.Center;

            // Name text (bottom area)
            nameText = FindOrCreateText("NameText",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.28f), 10f);
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.textWrappingMode = TextWrappingModes.Normal;

            // Stats text (bottom-left: ATK/HP)
            statsText = FindOrCreateText("StatsText",
                new Vector2(0.05f, 0.02f), new Vector2(0.50f, 0.15f), 12f);
            statsText.alignment = TextAlignmentOptions.Left;

            // Ability text (bottom-right)
            abilityText = FindOrCreateText("AbilityText",
                new Vector2(0.35f, 0.02f), new Vector2(0.95f, 0.15f), 8f);
            abilityText.alignment = TextAlignmentOptions.Right;
            abilityText.textWrappingMode = TextWrappingModes.Normal;
        }

        private Image FindOrCreateImage(string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var existing = transform.Find(name);
            if (existing != null) return existing.GetComponent<Image>();

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var childRT = go.GetComponent<RectTransform>();
            childRT.anchorMin = anchorMin;
            childRT.anchorMax = anchorMax;
            childRT.offsetMin = childRT.offsetMax = Vector2.zero;
            return go.GetComponent<Image>();
        }

        private TextMeshProUGUI FindOrCreateText(string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
        {
            var existing = transform.Find(name);
            if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var childRT = go.GetComponent<RectTransform>();
            childRT.anchorMin = anchorMin;
            childRT.anchorMax = anchorMax;
            childRT.offsetMin = childRT.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
