using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class CollectionUI : MonoBehaviour
    {
        [Header("Filters")]
        public Button btnFilterFire;
        public Button btnFilterWater;
        public Button btnFilterEarth;
        public Button btnFilterWind;

        [Header("Grid")]
        public Transform cardGridContainer;

        [Header("Detail")]
        public GameObject detailPanel;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailStats;
        public TextMeshProUGUI detailAbility;
        public Image detailImage;

        [Header("Counter")]
        public TextMeshProUGUI activeCountText;

        [Header("Navigation")]
        public Button btnBack;

        // Tutorial tip overlay (created at runtime if needed)
        private GameObject tutorialTip;

        void Start()
        {
            btnBack.onClick.AddListener(() => { SaveSystem.Save(); ScreenManager.Instance.GoBack(); });
            btnFilterFire.onClick.AddListener(() => { PlayerCollection.ToggleElement(ElementType.Fire); RefreshGrid(); });
            btnFilterWater.onClick.AddListener(() => { PlayerCollection.ToggleElement(ElementType.Water); RefreshGrid(); });
            btnFilterEarth.onClick.AddListener(() => { PlayerCollection.ToggleElement(ElementType.Earth); RefreshGrid(); });
            btnFilterWind.onClick.AddListener(() => { PlayerCollection.ToggleElement(ElementType.Wind); RefreshGrid(); });
        }

        void OnEnable()
        {
            PlayerCollection.InitializeStarterCollection();
            RefreshGrid();
            if (detailPanel != null) detailPanel.SetActive(false);
            CheckTutorialPrompt();
        }

        private void RefreshGrid()
        {
            foreach (Transform child in cardGridContainer)
                Object.Destroy(child.gameObject);

            UpdateFilterButton(btnFilterFire, PlayerCollection.FireActive, ElementColors.Fire);
            UpdateFilterButton(btnFilterWater, PlayerCollection.WaterActive, ElementColors.Water);
            UpdateFilterButton(btnFilterEarth, PlayerCollection.EarthActive, ElementColors.Earth);
            UpdateFilterButton(btnFilterWind, PlayerCollection.WindActive, ElementColors.Wind);

            var allCards = CardDatabase.GetAllCards();
            foreach (var owned in PlayerCollection.Cards)
            {
                var data = allCards.Find(c => c.CardName == owned.CardName);
                if (data == null) continue;

                var cell = new GameObject(data.CardName, typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(cardGridContainer, false);
                var rt = cell.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100f, 140f);

                bool isActive = owned.IsActive && PlayerCollection.IsElementActive(data.Element);
                bool canDeactivate = PlayerCollection.CanDeactivate(owned);
                bool isLocked = owned.IsStarter && PlayerCollection.NonStarterActiveCount() < 10;

                cell.GetComponent<Image>().color = isActive
                    ? GetElementColor(data.Element)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);

                // Creature art (upper portion of cell)
                string spritePath = CardDatabase.GetSpritePath(data);
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite != null)
                {
                    var artGO = new GameObject("Art", typeof(RectTransform), typeof(Image));
                    artGO.transform.SetParent(cell.transform, false);
                    var artImg = artGO.GetComponent<Image>();
                    artImg.sprite = sprite;
                    artImg.preserveAspect = true;
                    artImg.raycastTarget = false;
                    artImg.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    var artRT = artGO.GetComponent<RectTransform>();
                    artRT.anchorMin = new Vector2(0.05f, 0.35f);
                    artRT.anchorMax = new Vector2(0.95f, 0.95f);
                    artRT.offsetMin = artRT.offsetMax = Vector2.zero;
                }

                // Lock icon for starter cards that can't be unchecked yet
                if (isLocked && isActive)
                {
                    var lockGO = new GameObject("Lock", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lockGO.transform.SetParent(cell.transform, false);
                    var lockTMP = lockGO.GetComponent<TextMeshProUGUI>();
                    lockTMP.text = "L";
                    lockTMP.fontSize = 10f;
                    lockTMP.color = new Color(1f, 0.8f, 0.2f, 0.9f);
                    lockTMP.alignment = TextAlignmentOptions.Center;
                    lockTMP.raycastTarget = false;
                    var lockRT = lockGO.GetComponent<RectTransform>();
                    lockRT.anchorMin = new Vector2(0.75f, 0.85f);
                    lockRT.anchorMax = new Vector2(0.98f, 0.98f);
                    lockRT.offsetMin = lockRT.offsetMax = Vector2.zero;
                }

                // Text (lower portion — name + count)
                var textGO = new GameObject("Text", typeof(RectTransform));
                textGO.transform.SetParent(cell.transform, false);
                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{data.CardName}\nx{owned.Count}";
                tmp.fontSize = 11f;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Bottom;
                tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
                var trt = textGO.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.02f, 0f);
                trt.anchorMax = new Vector2(0.98f, 0.38f);
                trt.offsetMin = trt.offsetMax = Vector2.zero;

                // Click handler — toggle active state or show detail
                var cardData = data;
                var ownedRef = owned;
                cell.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(cardData, ownedRef));
            }

            if (activeCountText != null)
                activeCountText.text = $"Active: {PlayerCollection.ActiveCount()} / {PlayerCollection.TotalCards()}";
        }

        private void OnCardClicked(CardData data, OwnedCard owned)
        {
            // Toggle active state
            if (owned.IsActive)
            {
                if (!PlayerCollection.CanDeactivate(owned))
                {
                    // Show why they can't uncheck
                    if (owned.IsStarter && PlayerCollection.NonStarterActiveCount() < 10)
                        ShowTemporaryMessage("Unlock new cards to replace starter cards.");
                    else
                        ShowTemporaryMessage("Minimum 10 active cards required.");
                    return;
                }
                owned.IsActive = false;
            }
            else
            {
                owned.IsActive = true;
            }

            RefreshGrid();
            ShowDetail(data);
        }

        private void ShowTemporaryMessage(string msg)
        {
            if (activeCountText != null)
            {
                activeCountText.text = msg;
                // Reset after 2 seconds
                CancelInvoke(nameof(RestoreCountText));
                Invoke(nameof(RestoreCountText), 2f);
            }
        }

        private void RestoreCountText()
        {
            if (activeCountText != null)
                activeCountText.text = $"Active: {PlayerCollection.ActiveCount()} / {PlayerCollection.TotalCards()}";
        }

        private void ShowDetail(CardData card)
        {
            if (detailPanel != null) detailPanel.SetActive(true);
            if (detailName != null) detailName.text = card.CardName;
            if (detailStats != null)
            {
                detailStats.text = $"{card.Element} | {card.Rarity} | {card.Row}";
                // Sprite numbers for ATK, HP, Cost
                NumberRenderer.Set(detailStats.transform, "AtkNum", card.Attack, NumberRenderer.Red,
                    18f, new Vector2(0f, 1.2f), new Vector2(0.30f, 2.2f), 0.5f);
                NumberRenderer.Set(detailStats.transform, "HpNum", card.Health, NumberRenderer.Green,
                    18f, new Vector2(0.35f, 1.2f), new Vector2(0.65f, 2.2f), 0.5f);
                NumberRenderer.Set(detailStats.transform, "CostNum", card.Cost, NumberRenderer.Gold,
                    18f, new Vector2(0.70f, 1.2f), new Vector2(1f, 2.2f), 0.5f);
            }
            if (detailAbility != null)
                detailAbility.text = string.IsNullOrEmpty(card.AbilityText) ? "No ability" : card.AbilityText;
            if (detailImage != null)
            {
                Sprite sprite = Resources.Load<Sprite>(CardDatabase.GetSpritePath(card));
                if (sprite != null)
                {
                    detailImage.sprite = sprite;
                    detailImage.color = Color.white;
                }
                else
                {
                    detailImage.sprite = null;
                    detailImage.color = GetElementColor(card.Element);
                }
            }
        }

        /// <summary>
        /// Shows a one-time tutorial tip when collection exceeds 15 cards.
        /// Teaches the player to curate their shop pool.
        /// </summary>
        private void CheckTutorialPrompt()
        {
            if (PlayerCollection.HasSeenCurationTip) return;
            if (PlayerCollection.TotalCards() <= 15) return;

            PlayerCollection.HasSeenCurationTip = true;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            tutorialTip = new GameObject("CurationTip", typeof(RectTransform), typeof(Image));
            tutorialTip.transform.SetParent(canvas.transform, false);
            var tipRT = tutorialTip.GetComponent<RectTransform>();
            tipRT.anchorMin = new Vector2(0.25f, 0.35f);
            tipRT.anchorMax = new Vector2(0.75f, 0.65f);
            tipRT.offsetMin = tipRT.offsetMax = Vector2.zero;
            tutorialTip.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

            var outline = tutorialTip.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.84f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            var textGO = new GameObject("TipText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(tutorialTip.transform, false);
            var tipText = textGO.GetComponent<TextMeshProUGUI>();
            tipText.text = "Tip: Your shop pool is getting crowded!\n\n" +
                "Click cards to uncheck them and focus your drafts.\n" +
                "Fewer active cards = more triples = more wins.\n\n" +
                "<color=#FFD700>Click anywhere to dismiss</color>";
            tipText.fontSize = 18f;
            tipText.color = Color.white;
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            tipText.raycastTarget = false;
            var tipTxtRT = textGO.GetComponent<RectTransform>();
            tipTxtRT.anchorMin = new Vector2(0.05f, 0.05f);
            tipTxtRT.anchorMax = new Vector2(0.95f, 0.95f);
            tipTxtRT.offsetMin = tipTxtRT.offsetMax = Vector2.zero;

            // Dismiss button covers the whole tip
            var dismissBtn = tutorialTip.AddComponent<Button>();
            dismissBtn.onClick.AddListener(() =>
            {
                if (tutorialTip != null) Destroy(tutorialTip);
            });
        }

        private void UpdateFilterButton(Button btn, bool active, Color elementColor)
        {
            var colors = btn.colors;
            colors.normalColor = active ? elementColor : new Color(0.2f, 0.2f, 0.2f);
            btn.colors = colors;
        }

        private Color GetElementColor(ElementType e)
        {
            switch (e)
            {
                case ElementType.Fire: return ElementColors.Fire;
                case ElementType.Water: return ElementColors.Water;
                case ElementType.Earth: return ElementColors.Earth;
                case ElementType.Wind: return ElementColors.Wind;
                default: return Color.gray;
            }
        }
    }
}
