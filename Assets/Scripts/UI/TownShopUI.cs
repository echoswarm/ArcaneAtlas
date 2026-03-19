using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class TownShopUI : MonoBehaviour
    {
        [Header("Display")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI feedbackText;

        [Header("Buttons")]
        public Button btnBuyPack;
        public Button btnBuyReroll;
        public Button btnSellDuplicates;
        public Button btnBack;

        private const int PACK_COST = 10;
        private const int REROLL_COST = 5;

        void Start()
        {
            btnBuyPack.onClick.AddListener(BuyPack);
            btnBuyReroll.onClick.AddListener(BuyReroll);
            btnSellDuplicates.onClick.AddListener(SellDuplicates);
            btnBack.onClick.AddListener(() => { SaveSystem.Save(); ScreenManager.Instance.GoBack(); });
        }

        void OnEnable()
        {
            RefreshDisplay();
            if (feedbackText != null) feedbackText.text = "";
        }

        private void BuyPack()
        {
            if (GameState.Gold >= PACK_COST)
            {
                GameState.Gold -= PACK_COST;
                if (goldText != null)
                    NumberPopup.SpawnAtTransform(goldText.transform, PACK_COST, NumberColor.Red, "-", 1.2f);
                SetFeedback("Opening booster pack!");
                RefreshDisplay();
                RefreshHUD();

                // Open the pack immediately instead of stockpiling
                if (ScreenManager.Instance != null && ScreenManager.Instance.screenPackOpening != null)
                    ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenPackOpening);
            }
            else
            {
                SetFeedback("Not enough gold!");
                RefreshDisplay();
            }
        }

        private void BuyReroll()
        {
            if (GameState.Gold >= REROLL_COST)
            {
                GameState.Gold -= REROLL_COST;
                GameState.RerollTokens++;
                if (goldText != null)
                    NumberPopup.SpawnAtTransform(goldText.transform, REROLL_COST, NumberColor.Red, "-", 1.2f);
                SetFeedback("Bought a reroll token!");
            }
            else
            {
                SetFeedback("Not enough gold!");
            }
            RefreshDisplay();
            RefreshHUD();
        }

        private void SellDuplicates()
        {
            int sold = 0;
            int goldEarned = 0;
            foreach (var card in PlayerCollection.Cards)
            {
                if (card.Count > 1)
                {
                    int extras = card.Count - 1;
                    sold += extras;
                    goldEarned += extras * 1; // 1g per duplicate (economy sink — can't break even)
                    card.Count = 1;
                }
            }

            if (sold > 0)
            {
                GameState.Gold += goldEarned;
                if (goldText != null)
                    NumberPopup.SpawnAtTransform(goldText.transform, goldEarned, NumberColor.Yellow, "+", 1.2f);
                SetFeedback($"Sold {sold} duplicates for {goldEarned}g!");
            }
            else
            {
                SetFeedback("No duplicates to sell.");
            }
            RefreshDisplay();
            RefreshHUD();
        }

        private void RefreshDisplay()
        {
            if (goldText != null)
            {
                goldText.text = "Gold";
                NumberRenderer.Set(goldText.transform, "GoldNum", GameState.Gold, NumberRenderer.Gold,
                    22f, new Vector2(0.45f, 0.05f), new Vector2(1f, 0.7f), 0f);
            }
            if (btnBuyPack != null)
                btnBuyPack.interactable = GameState.Gold >= PACK_COST;
            if (btnBuyReroll != null)
                btnBuyReroll.interactable = GameState.Gold >= REROLL_COST;
        }

        private void RefreshHUD()
        {
            var hud = FindFirstObjectByType<HUD>();
            if (hud != null) hud.Refresh();
        }

        private void SetFeedback(string msg)
        {
            if (feedbackText != null)
                feedbackText.text = msg;
        }
    }
}
