using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class PackOpeningUI : MonoBehaviour
    {
        [Header("Cards")]
        public Image[] cardImages;
        public TextMeshProUGUI[] cardTexts;
        public Image[] cardBacks;
        public TextMeshProUGUI[] newBadges;

        [Header("UI")]
        public TextMeshProUGUI bannerText;
        public Button btnContinue;

        private List<CardData> revealedCards;

        void Start()
        {
            btnContinue.onClick.AddListener(OnContinue);
        }

        void OnEnable()
        {
            if (btnContinue != null)
                btnContinue.gameObject.SetActive(false);
            StartCoroutine(RevealCards());
        }

        private IEnumerator RevealCards()
        {
            revealedCards = GeneratePackCards();
            if (bannerText != null)
                bannerText.text = "BOOSTER PACK";

            for (int i = 0; i < 5; i++)
            {
                if (i < cardBacks.Length && cardBacks[i] != null)
                    cardBacks[i].gameObject.SetActive(true);
                if (i < cardTexts.Length && cardTexts[i] != null)
                    cardTexts[i].text = "";
                if (i < newBadges.Length && newBadges[i] != null)
                    newBadges[i].gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < 5 && i < revealedCards.Count; i++)
            {
                var card = revealedCards[i];

                if (i < cardBacks.Length && cardBacks[i] != null)
                    cardBacks[i].gameObject.SetActive(false);

                if (i < cardImages.Length && cardImages[i] != null)
                {
                    Sprite sprite = Resources.Load<Sprite>(CardDatabase.GetSpritePath(card));
                    if (sprite != null)
                    {
                        cardImages[i].sprite = sprite;
                        cardImages[i].color = Color.white;
                        cardImages[i].preserveAspect = true;
                    }
                    else
                    {
                        cardImages[i].color = GetElementColor(card.Element);
                    }
                }
                if (i < cardTexts.Length && cardTexts[i] != null)
                    cardTexts[i].text = $"{card.CardName}\n{card.Attack}/{card.Health}";

                bool isNew = !PlayerCollection.Cards.Any(c => c.CardName == card.CardName);
                if (i < newBadges.Length && newBadges[i] != null)
                    newBadges[i].gameObject.SetActive(isNew);

                PlayerCollection.AddCard(card);
                QuestManager.OnCardAdded(card);

                yield return new WaitForSeconds(0.4f);
            }

            if (bannerText != null)
                bannerText.text = "VICTORY!";
            if (btnContinue != null)
                btnContinue.gameObject.SetActive(true);

            // Auto-save after pack cards are added to collection
            SaveSystem.Save();
        }

        private List<CardData> GeneratePackCards()
        {
            var cards = new List<CardData>();
            var allCards = CardDatabase.GetAllCards();
            if (allCards.Count == 0) return cards;

            // Pull from all biomes: one card per element, then one wild card
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Earth, ElementType.Wind };
            foreach (var element in elements)
            {
                var pool = allCards.Where(c => c.Element == element).ToList();
                if (pool.Count > 0)
                    cards.Add(pool[Random.Range(0, pool.Count)]);
                else
                    cards.Add(allCards[Random.Range(0, allCards.Count)]);
            }

            // 5th card: pity-roll for boss legendary, otherwise random
            CardData bossCard = TryPityRoll();
            if (bossCard != null)
                cards.Add(bossCard);
            else
                cards.Add(allCards[Random.Range(0, allCards.Count)]);

            return cards;
        }

        private CardData TryPityRoll()
        {
            var opponent = GameState.CurrentOpponent;
            if (opponent == null || !opponent.IsBoss || string.IsNullOrEmpty(opponent.BossCardName))
                return null;

            string bossName = opponent.Name;
            int defeats = 0;
            GameState.BossDefeatCounts.TryGetValue(bossName, out defeats);
            defeats++;

            // Calculate pity chance
            float chance = GameState.PITY_BASE_CHANCE;
            if (defeats > GameState.PITY_THRESHOLD)
                chance += (defeats - GameState.PITY_THRESHOLD) * GameState.PITY_INCREMENT;
            chance = Mathf.Min(chance, GameState.PITY_MAX_CHANCE);

            float roll = Random.value;
            if (roll <= chance)
            {
                // Success! Find the boss card and reset counter
                GameState.BossDefeatCounts[bossName] = 0;
                var allCards = CardDatabase.GetAllCards();
                var bossCard = allCards.Find(c => c.CardName == opponent.BossCardName);
                if (bossCard != null)
                {
                    Debug.Log($"[PityRoll] BOSS CARD DROP! {opponent.BossCardName} (chance was {chance:P2} after {defeats} defeats)");
                    return bossCard;
                }
            }

            // No drop — increment counter
            GameState.BossDefeatCounts[bossName] = defeats;
            Debug.Log($"[PityRoll] No drop for {bossName} (chance was {chance:P2}, defeats: {defeats})");
            return null;
        }

        private void OnContinue()
        {
            ScreenManager.Instance.GoBack();
            ScreenManager.Instance.GoBack();
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
