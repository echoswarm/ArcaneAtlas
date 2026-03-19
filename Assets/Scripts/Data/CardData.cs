using UnityEngine;

namespace ArcaneAtlas.Data
{
    public enum CardRarity { Common, Uncommon, Rare, Legendary }
    public enum RowPreference { Front, Back, Either }
    public enum CardTier { Bronze, Silver, Gold }

    [CreateAssetMenu(fileName = "Card_", menuName = "Arcane Atlas/Card Data")]
    public class CardData : ScriptableObject
    {
        public string CardName;
        public ElementType Element;
        public CardRarity Rarity;
        public RowPreference Row;
        public int Tier;   // 1-6, determines gold cost and round availability
        public int Cost;   // Always equals Tier
        public int Attack;
        public int Health;
        public string AbilityText;
        public int SpriteIndex; // Maps to creature_{element}_{SpriteIndex:D2}.png (1-25)
    }
}
