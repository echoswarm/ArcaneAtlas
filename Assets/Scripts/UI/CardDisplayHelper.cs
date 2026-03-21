using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Data;
using ArcaneAtlas.Combat;

namespace ArcaneAtlas.UI
{
    /// <summary>
    /// Shared utility for rendering modular card visuals on any UI slot.
    /// Used by CombatUI (shop/board) and PackOpeningUI (pack cards).
    /// </summary>
    public static class CardDisplayHelper
    {
        private static CardStyleDef cachedStyle;
        private static TMPro.TMP_FontAsset pixelFont;

        public static CardStyleDef GetStyle()
        {
            if (cachedStyle == null)
            {
                cachedStyle = Resources.Load<CardStyleDef>("CardStyles/MiniFantasyDefault");
                if (cachedStyle == null)
                    cachedStyle = Resources.Load<CardStyleDef>("CardStyles/Default");
            }
            return cachedStyle;
        }

        /// <summary>
        /// Renders a full modular card on the given transform (must have Image + Outline).
        /// Creates child GameObjects for all layers. Call ClearCard first if reusing.
        /// </summary>
        public static void RenderCard(Transform slot, CardData card, CardInstance instance = null)
        {
            if (card == null) return;
            var style = GetStyle();

            // Rarity border
            var outline = slot.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = GetRarityColor(card.Rarity);
                float borderSize = card.Rarity == CardRarity.Legendary ? 3f :
                                   card.Rarity == CardRarity.Rare ? 2.5f : 2f;
                outline.effectDistance = new Vector2(borderSize, -borderSize);
            }

            // Background
            var slotImg = slot.GetComponent<Image>();
            if (slotImg != null)
                slotImg.color = (style != null && style.HasModularCards)
                    ? new Color(0.1f, 0.1f, 0.1f, 0.95f)
                    : GetElementColor(card.Element);

            // Modular layers
            if (style != null && style.HasModularCards)
            {
                var bgImg = GetOrCreate<Image>(slot, "ModBg", Vector2.zero, Vector2.one);
                bgImg.sprite = style.GetModBg(card.Element);
                bgImg.color = Color.white;
                bgImg.raycastTarget = false;
                bgImg.transform.SetAsFirstSibling();

                Sprite glow = style.GetModGlow(card.Rarity);
                var glowImg = GetOrCreate<Image>(slot, "ModGlow", new Vector2(-0.05f, -0.05f), new Vector2(1.05f, 1.05f));
                glowImg.sprite = glow;
                glowImg.color = glow != null ? Color.white : Color.clear;
                glowImg.raycastTarget = false;

                var frameImg = GetOrCreate<Image>(slot, "ModFrame", Vector2.zero, Vector2.one);
                frameImg.sprite = style.GetModFrame(card.Rarity);
                frameImg.color = Color.white;
                frameImg.raycastTarget = false;

                var borderImg = GetOrCreate<Image>(slot, "ModBorder", Vector2.zero, Vector2.one);
                borderImg.sprite = style.GetModBorder(card.Element);
                borderImg.color = Color.white;
                borderImg.raycastTarget = false;
            }

            // Creature art
            Sprite creatureSprite = null;
            if (style != null) creatureSprite = style.GetCreatureArt(card);
            if (creatureSprite != null)
            {
                var artImg = GetOrCreate<Image>(slot, "CreatureArt",
                    new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.78f));
                artImg.sprite = creatureSprite;
                artImg.color = Color.white;
                artImg.preserveAspect = true;
                artImg.raycastTarget = false;
            }

            // Caption bar + name at top
            if (style != null && style.HasModularCards)
            {
                var captionImg = GetOrCreate<Image>(slot, "ModCaption",
                    new Vector2(0.0f, 0.80f), new Vector2(1.0f, 0.98f));
                captionImg.sprite = style.GetModCaption(card.Element);
                captionImg.color = Color.white;
                captionImg.raycastTarget = false;
            }

            var nameTMP = GetOrCreate<TextMeshProUGUI>(slot, "CardName",
                new Vector2(0.12f, 0.80f), new Vector2(0.92f, 0.98f));
            nameTMP.text = card.CardName;
            nameTMP.fontSize = 15f;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.color = Color.white;
            nameTMP.margin = new Vector4(14f, 0f, 8f, 0f);
            if (pixelFont == null)
                pixelFont = Resources.Load<TMP_FontAsset>("Fonts/Pixeled SDF");
            if (pixelFont != null) nameTMP.font = pixelFont;

            var shadow = nameTMP.GetComponent<Shadow>();
            if (shadow == null) shadow = nameTMP.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Cost circle
            if (style != null && style.ModCostCircle != null)
            {
                var costCircle = GetOrCreate<Image>(slot, "CostBg",
                    new Vector2(-0.04f, 0.82f), new Vector2(0.14f, 1.04f));
                costCircle.sprite = style.ModCostCircle;
                costCircle.color = new Color(0.15f, 0.12f, 0.08f);
                costCircle.raycastTarget = false;
                costCircle.preserveAspect = true;
            }

            NumberRenderer.Set(slot, "TierNum", card.Cost, NumberRenderer.Gold,
                22f, new Vector2(-0.02f, 0.84f), new Vector2(0.12f, 1.02f), 0.5f);

            // Row letter
            string rowLetter = card.Row == RowPreference.Front ? "F" :
                               card.Row == RowPreference.Back ? "B" : "*";
            var rowTMP = GetOrCreate<TextMeshProUGUI>(slot, "RowTag",
                new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.32f));
            rowTMP.text = rowLetter;
            rowTMP.fontSize = 20f;
            rowTMP.alignment = TextAlignmentOptions.Center;
            rowTMP.color = new Color(1f, 1f, 1f, 0.6f);
            if (pixelFont != null) rowTMP.font = pixelFont;

            // ATK / HP
            int atk = instance != null ? instance.CurrentAttack : card.Attack;
            int hp = instance != null ? instance.CurrentHealth : card.Health;

            var atkBg = GetOrCreate<Image>(slot, "AtkBg",
                new Vector2(-0.01f, -0.01f), new Vector2(0.19f, 0.19f));
            atkBg.sprite = (style != null && style.ModStatSquare != null) ? style.ModStatSquare : null;
            atkBg.color = (atkBg.sprite != null) ? Color.white : new Color(0f, 0f, 0f, 0.7f);
            atkBg.raycastTarget = false;
            atkBg.preserveAspect = true;

            NumberRenderer.Set(slot, "AtkNum", atk, NumberRenderer.Red,
                32f, new Vector2(0.0f, 0.0f), new Vector2(0.18f, 0.18f), 0.5f);

            var hpBg = GetOrCreate<Image>(slot, "HpBg",
                new Vector2(0.81f, -0.01f), new Vector2(1.01f, 0.19f));
            hpBg.sprite = (style != null && style.ModStatSquare != null) ? style.ModStatSquare : null;
            hpBg.color = (hpBg.sprite != null) ? Color.white : new Color(0f, 0f, 0f, 0.7f);
            hpBg.raycastTarget = false;
            hpBg.preserveAspect = true;

            NumberRenderer.Set(slot, "HpNum", hp, NumberRenderer.Green,
                32f, new Vector2(0.82f, 0.0f), new Vector2(1.0f, 0.18f), 0.5f);

            // Enforce sibling order
            SetOrder(slot, "ModBg", 0);
            SetOrder(slot, "ModGlow", 1);
            SetOrder(slot, "ModFrame", 2);
            SetOrder(slot, "ModBorder", 3);
            BringFront(slot, "CreatureArt");
            BringFront(slot, "RowTag");
            BringFront(slot, "ModCaption");
            BringFront(slot, "CostBg");
            BringFront(slot, "TierNum");
            BringFront(slot, "CardName");
            BringFront(slot, "AtkBg");
            BringFront(slot, "AtkNum");
            BringFront(slot, "HpBg");
            BringFront(slot, "HpNum");
        }

        /// <summary>
        /// Clears all dynamic children from a card slot.
        /// </summary>
        public static void ClearCard(Transform slot)
        {
            string[] children = {
                "CreatureArt", "CardFrame", "ElementIcon", "ElemBg",
                "ModBg", "ModGlow", "ModFrame", "ModBorder", "ModCaption",
                "CostBg", "AtkBg", "HpBg", "RankStars", "RowTag", "CardName"
            };
            foreach (string name in children)
            {
                var child = slot.Find(name);
                if (child != null) Object.Destroy(child.gameObject);
            }
            NumberRenderer.Clear(slot, "TierNum");
            NumberRenderer.Clear(slot, "AtkNum");
            NumberRenderer.Clear(slot, "HpNum");

            var outline = slot.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            var img = slot.GetComponent<Image>();
            if (img != null) img.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        }

        // ── Helpers ──

        private static T GetOrCreate<T>(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax) where T : Component
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<T>();

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void BringFront(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) child.SetAsLastSibling();
        }

        private static void SetOrder(Transform parent, string name, int idx)
        {
            var child = parent.Find(name);
            if (child != null) child.SetSiblingIndex(idx);
        }

        public static Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.5f, 0.5f, 0.5f);
                case CardRarity.Uncommon: return new Color(0.3f, 0.7f, 0.3f);
                case CardRarity.Rare: return new Color(0.3f, 0.5f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.7f, 0.2f);
                default: return Color.gray;
            }
        }

        public static Color GetElementColor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return new Color(0.40f, 0.12f, 0.08f, 0.95f);
                case ElementType.Water: return new Color(0.08f, 0.18f, 0.40f, 0.95f);
                case ElementType.Earth: return new Color(0.12f, 0.32f, 0.10f, 0.95f);
                case ElementType.Wind: return new Color(0.22f, 0.22f, 0.38f, 0.95f);
                default: return new Color(0.15f, 0.15f, 0.2f, 0.95f);
            }
        }
    }
}
