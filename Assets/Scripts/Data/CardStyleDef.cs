using System;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    /// <summary>
    /// Defines the visual style for rendering cards. Maps card properties (element, rarity)
    /// to specific sprites for frames, backgrounds, icons, and creature art.
    /// Like TilePaletteDef for tiles — swap the CardStyleDef and all cards get new visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardStyle", menuName = "Arcane Atlas/Card Style Def")]
    public class CardStyleDef : ScriptableObject
    {
        public string StyleName;

        [Header("Card Frames (by rarity)")]
        public Sprite FrameCommon;
        public Sprite FrameUncommon;
        public Sprite FrameRare;
        public Sprite FrameLegendary;

        [Header("Card Frame Details/Overlays (by rarity)")]
        public Sprite DetailCommon;
        public Sprite DetailUncommon;
        public Sprite DetailRare;
        public Sprite DetailLegendary;

        [Header("Shop Card Frames (by rarity)")]
        public Sprite ShopFrameCommon;
        public Sprite ShopFrameUncommon;
        public Sprite ShopFrameRare;
        public Sprite ShopFrameLegendary;

        [Header("Element Icons")]
        public Sprite IconFire;
        public Sprite IconWater;
        public Sprite IconEarth;
        public Sprite IconWind;

        [Header("Element Background Tints")]
        public Color TintFire = new Color(0.95f, 0.30f, 0.20f);
        public Color TintWater = new Color(0.25f, 0.50f, 0.90f);
        public Color TintEarth = new Color(0.35f, 0.70f, 0.30f);
        public Color TintWind = new Color(0.80f, 0.80f, 0.90f);

        [Header("Tier Badge Sprites")]
        public Sprite BadgeBronze;
        public Sprite BadgeSilver;
        public Sprite BadgeGold;

        [Header("Modular Card Layers — element-colored components")]
        public Sprite ModBgFire;
        public Sprite ModBgWater;
        public Sprite ModBgEarth;
        public Sprite ModBgWind;
        public Sprite ModBorderFire;
        public Sprite ModBorderWater;
        public Sprite ModBorderEarth;
        public Sprite ModBorderWind;
        public Sprite ModFrameCommon;
        public Sprite ModFrameRare;
        public Sprite ModFrameLegendary;
        public Sprite ModCaptionFire;
        public Sprite ModCaptionWater;
        public Sprite ModCaptionEarth;
        public Sprite ModCaptionWind;
        public Sprite ModTitleBar;
        public Sprite ModDescriptionBar;
        public Sprite ModCostCircle;
        public Sprite ModStatSquare;
        public Sprite ModGlowRare;
        public Sprite ModGlowLegendary;

        [Header("Card Back")]
        public Sprite CardBack;
        public Sprite CardBackShop;

        [Header("Creature Art Override")]
        [Tooltip("If set, overrides Resources-based creature loading. Map element+index to sprites.")]
        public CreatureArtMapping[] CreatureOverrides;

        /// <summary>
        /// Returns the frame sprite for a given rarity.
        /// </summary>
        public Sprite GetFrame(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return FrameCommon;
                case CardRarity.Uncommon: return FrameUncommon;
                case CardRarity.Rare: return FrameRare;
                case CardRarity.Legendary: return FrameLegendary;
                default: return FrameCommon;
            }
        }

        /// <summary>
        /// Returns the detail/overlay sprite for a given rarity.
        /// </summary>
        public Sprite GetDetail(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return DetailCommon;
                case CardRarity.Uncommon: return DetailUncommon;
                case CardRarity.Rare: return DetailRare;
                case CardRarity.Legendary: return DetailLegendary;
                default: return null;
            }
        }

        /// <summary>
        /// Returns the shop frame for a given rarity.
        /// </summary>
        public Sprite GetShopFrame(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return ShopFrameCommon;
                case CardRarity.Uncommon: return ShopFrameUncommon;
                case CardRarity.Rare: return ShopFrameRare;
                case CardRarity.Legendary: return ShopFrameLegendary;
                default: return ShopFrameCommon;
            }
        }

        /// <summary>
        /// Returns the element icon sprite.
        /// </summary>
        public Sprite GetElementIcon(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return IconFire;
                case ElementType.Water: return IconWater;
                case ElementType.Earth: return IconEarth;
                case ElementType.Wind: return IconWind;
                default: return null;
            }
        }

        /// <summary>
        /// Returns the element background tint.
        /// </summary>
        public Color GetElementTint(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return TintFire;
                case ElementType.Water: return TintWater;
                case ElementType.Earth: return TintEarth;
                case ElementType.Wind: return TintWind;
                default: return Color.white;
            }
        }

        // ── Modular Card Layer Getters ──

        public Sprite GetModBg(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return ModBgFire;
                case ElementType.Water: return ModBgWater;
                case ElementType.Earth: return ModBgEarth;
                case ElementType.Wind: return ModBgWind;
                default: return null;
            }
        }

        public Sprite GetModBorder(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return ModBorderFire;
                case ElementType.Water: return ModBorderWater;
                case ElementType.Earth: return ModBorderEarth;
                case ElementType.Wind: return ModBorderWind;
                default: return null;
            }
        }

        public Sprite GetModCaption(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return ModCaptionFire;
                case ElementType.Water: return ModCaptionWater;
                case ElementType.Earth: return ModCaptionEarth;
                case ElementType.Wind: return ModCaptionWind;
                default: return null;
            }
        }

        public Sprite GetModFrame(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Legendary: return ModFrameLegendary ?? ModFrameRare;
                case CardRarity.Rare: return ModFrameRare;
                default: return ModFrameCommon;
            }
        }

        public Sprite GetModGlow(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Legendary: return ModGlowLegendary;
                case CardRarity.Rare: return ModGlowRare;
                default: return null;
            }
        }

        public bool HasModularCards => ModBgFire != null || ModBorderFire != null;

        /// <summary>
        /// Returns the tier badge sprite.
        /// </summary>
        public Sprite GetBadge(CardTier tier)
        {
            switch (tier)
            {
                case CardTier.Bronze: return BadgeBronze;
                case CardTier.Silver: return BadgeSilver;
                case CardTier.Gold: return BadgeGold;
                default: return null;
            }
        }

        /// <summary>
        /// Gets creature art for a card. Checks overrides first, then falls back to Resources.
        /// </summary>
        public Sprite GetCreatureArt(CardData card)
        {
            // Check overrides first
            if (CreatureOverrides != null)
            {
                foreach (var ov in CreatureOverrides)
                {
                    if (ov.Element == card.Element && ov.SpriteIndex == card.SpriteIndex && ov.Sprite != null)
                        return ov.Sprite;
                }
            }

            // Fall back to Resources path — try Sprite first, then Texture2D
            string path = CardDatabase.GetSpritePath(card);
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

            // Sprite load failed — load as Texture2D and create sprite at runtime
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);

            return null;
        }
    }

    [Serializable]
    public struct CreatureArtMapping
    {
        public ElementType Element;
        public int SpriteIndex;
        public Sprite Sprite;
    }
}
