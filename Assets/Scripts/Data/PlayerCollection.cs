using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    [System.Serializable]
    public class OwnedCard
    {
        public string CardName;
        public int Count;
        public bool IsActive;
        public bool IsStarter;

        public OwnedCard(string name, bool isStarter = false)
        {
            CardName = name;
            Count = 1;
            IsActive = true;
            IsStarter = isStarter;
        }
    }

    public static class PlayerCollection
    {
        public static List<OwnedCard> Cards = new List<OwnedCard>();

        public static bool FireActive = true;
        public static bool WaterActive = true;
        public static bool EarthActive = true;
        public static bool WindActive = true;

        // One-time tutorial flag — set true after first display
        public static bool HasSeenCurationTip = false;

        public static void AddCard(CardData card, bool isStarter = false)
        {
            var existing = Cards.Find(c => c.CardName == card.CardName);
            if (existing != null)
                existing.Count++;
            else
                Cards.Add(new OwnedCard(card.CardName, isStarter));
        }

        public static int TotalCards()
        {
            int total = 0;
            foreach (var c in Cards) total += c.Count;
            return total;
        }

        /// <summary>
        /// Count of unique cards that are active and whose element is enabled.
        /// </summary>
        public static int ActiveCount()
        {
            int count = 0;
            foreach (var c in Cards)
            {
                if (!c.IsActive) continue;
                var data = CardDatabase.GetAllCards().Find(d => d.CardName == c.CardName);
                if (data == null) continue;
                if (!IsElementActive(data.Element)) continue;
                count += c.Count;
            }
            return count;
        }

        /// <summary>
        /// Count of non-starter cards that are active.
        /// </summary>
        public static int NonStarterActiveCount()
        {
            int count = 0;
            foreach (var c in Cards)
            {
                if (!c.IsActive || c.IsStarter) continue;
                var data = CardDatabase.GetAllCards().Find(d => d.CardName == c.CardName);
                if (data == null) continue;
                if (!IsElementActive(data.Element)) continue;
                count += c.Count;
            }
            return count;
        }

        /// <summary>
        /// Whether a card can be unchecked right now.
        /// Rules: 1) Cannot drop below 10 active cards
        ///        2) Starter cards locked until player has 10+ non-starter active cards
        /// </summary>
        public static bool CanDeactivate(OwnedCard card)
        {
            if (!card.IsActive) return false;
            if (ActiveCount() <= 10) return false;
            if (card.IsStarter && NonStarterActiveCount() < 10) return false;
            return true;
        }

        public static bool IsElementActive(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return FireActive;
                case ElementType.Water: return WaterActive;
                case ElementType.Earth: return EarthActive;
                case ElementType.Wind: return WindActive;
                default: return true;
            }
        }

        public static void ToggleElement(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: FireActive = !FireActive; break;
                case ElementType.Water: WaterActive = !WaterActive; break;
                case ElementType.Earth: EarthActive = !EarthActive; break;
                case ElementType.Wind: WindActive = !WindActive; break;
            }
        }

        /// <summary>
        /// Fixed 10-card starter set: weak T1-T2 cards with duplicates to teach merging.
        /// Intentionally underpowered so winning is hard until the player expands their collection.
        /// </summary>
        public static void InitializeStarterCollection()
        {
            if (Cards.Count > 0) return;

            var all = CardDatabase.GetAllCards();

            // 10 starter cards: 2x of four T1 commons (one per element) + 1x of two T2 commons
            string[] starterNames = new string[]
            {
                "Ember Sprite",   // Fire T1 — 3/2
                "Ember Sprite",   // duplicate for merge potential
                "Tide Pool",      // Water T1 — 2/3
                "Tide Pool",      // duplicate
                "Pebble Imp",     // Earth T1 — 1/3
                "Pebble Imp",     // duplicate
                "Breeze Sprite",  // Wind T1 — 2/2
                "Breeze Sprite",  // duplicate
                "Blaze Hound",    // Fire T2 — 4/3
                "Bark Shield",    // Earth T2 — 1/5 Taunt
            };

            foreach (string name in starterNames)
            {
                var data = all.Find(c => c.CardName == name);
                if (data != null)
                    AddCard(data, isStarter: true);
            }
        }
    }
}
