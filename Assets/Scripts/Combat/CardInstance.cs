using UnityEngine;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Combat
{
    public class CardInstance
    {
        public CardData Data;
        public int CurrentAttack;
        public int CurrentHealth;
        public CardTier Tier;
        public int MergeCount; // Tracks copies absorbed (upgrades at 3)

        public CardInstance(CardData data)
        {
            Data = data;
            CurrentAttack = data.Attack;
            CurrentHealth = data.Health;
            Tier = CardTier.Bronze;
            MergeCount = 1; // The card itself counts as 1
        }

        public void Upgrade()
        {
            if (Tier == CardTier.Bronze)
            {
                Tier = CardTier.Silver;
                CurrentAttack = Data.Attack * 2;
                CurrentHealth = Data.Health * 2;
            }
            else if (Tier == CardTier.Silver)
            {
                Tier = CardTier.Gold;
                CurrentAttack = Data.Attack * 4;
                CurrentHealth = Data.Health * 4;
            }
        }
    }
}
