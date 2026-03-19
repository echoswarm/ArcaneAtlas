using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Combat;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class CombatUI : MonoBehaviour
    {
        [Header("HUD")]
        public TextMeshProUGUI playerHPText;
        public TextMeshProUGUI opponentHPText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI goldText;

        [Header("Board Slots - Player")]
        public Button[] playerSlots;
        public TextMeshProUGUI[] playerSlotTexts;

        [Header("Board Slots - Opponent")]
        public TextMeshProUGUI[] opponentSlotTexts;

        [Header("Phase Panels")]
        public GameObject shopArea;
        public GameObject opponentBoard;

        [Header("Shop (3 cards)")]
        public Button[] shopButtons;
        public TextMeshProUGUI[] shopTexts;

        [Header("Actions")]
        public Button btnBattle;
        public Button btnReroll;
        public Button btnPool;
        public TextMeshProUGUI rerollCostText;

        [Header("Result")]
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;
        public Button btnContinue;

        private int selectedShopIndex = -1;

        // Card detail tooltip
        private GameObject detailPanel;
        private TextMeshProUGUI detailName;
        private TextMeshProUGUI detailStats;
        private TextMeshProUGUI detailAbility;
        private TextMeshProUGUI detailTierRank;
        private Image detailArt;
        private Image detailBg;
        private Outline detailOutline;
        private Button detailSellBtn;
        private TextMeshProUGUI detailDamageThreat;
        private int detailBoardSlot = -1; // Which board slot is being inspected (-1 = shop card)

        // Pool viewer overlay
        private GameObject poolPanel;

        void Start()
        {
            btnBattle.onClick.AddListener(() => CombatManager.Instance?.StartBattle());
            btnReroll.onClick.AddListener(() => CombatManager.Instance?.Reroll());
            if (btnPool != null)
                btnPool.onClick.AddListener(TogglePoolViewer);
            btnContinue.onClick.AddListener(() => CombatManager.Instance?.ReturnToExploration());

            for (int i = 0; i < shopButtons.Length; i++)
            {
                int index = i;
                shopButtons[i].onClick.AddListener(() => OnShopCardClicked(index));
            }

            for (int i = 0; i < playerSlots.Length; i++)
            {
                int slot = i;
                playerSlots[i].onClick.AddListener(() => OnPlayerSlotClicked(slot));
            }

            if (resultPanel != null)
                resultPanel.SetActive(false);
        }

        void OnEnable()
        {
            selectedShopIndex = -1;
            if (resultPanel != null)
                resultPanel.SetActive(false);

            SetPhaseLayout(CombatPhase.Shop);

            var cm = FindFirstObjectByType<CombatManager>();
            if (cm == null)
            {
                var go = new GameObject("CombatManager");
                cm = go.AddComponent<CombatManager>();
            }
            cm.StartMatch();
        }

        public void SetPhaseLayout(CombatPhase phase)
        {
            bool isShop = phase == CombatPhase.Shop;
            if (shopArea != null) shopArea.SetActive(isShop);
            if (opponentBoard != null) opponentBoard.SetActive(!isShop);
            if (btnBattle != null) btnBattle.gameObject.SetActive(isShop);

            // Hide overlays when leaving shop phase
            if (!isShop)
            {
                HideCardDetail();
                if (poolPanel != null) { Destroy(poolPanel); poolPanel = null; }
            }
        }

        private void OnShopCardClicked(int index)
        {
            var cm = CombatManager.Instance;
            if (cm == null) return;

            if (index >= 0 && index < cm.shopOffers.Count && cm.shopOffers[index] != null)
            {
                var card = cm.shopOffers[index];

                // Show detail panel for this card
                ShowCardDetail(card, null);

                if (cm.playerGold >= card.Cost)
                {
                    bool merged = cm.BuyCard(index, -1);
                    if (merged)
                    {
                        selectedShopIndex = -1;
                        HideCardDetail();
                        Refresh();
                        return;
                    }
                }
            }
            else
            {
                HideCardDetail();
            }

            selectedShopIndex = index;
            Refresh();
        }

        private void OnPlayerSlotClicked(int slot)
        {
            if (CombatManager.Instance == null) return;

            if (selectedShopIndex >= 0)
            {
                bool success = CombatManager.Instance.BuyCard(selectedShopIndex, slot);
                if (success)
                {
                    selectedShopIndex = -1;
                    HideCardDetail();
                }
            }
            else
            {
                var cm = CombatManager.Instance;
                if (cm.playerBoard[slot] != null)
                {
                    // During shop phase: show detail + sell button
                    if (cm.currentPhase == CombatPhase.Shop)
                        ShowCardDetail(cm.playerBoard[slot].Data, cm.playerBoard[slot], slot);
                    else
                        HideCardDetail();

                    // Only sell if no detail panel was just opened (sell via long-press or double-tap later)
                    // For now: clicking a board card during shop shows info; clicking empty slot does nothing
                }
                else
                {
                    HideCardDetail();
                }
            }
            Refresh();
        }

        public void Refresh()
        {
            var cm = CombatManager.Instance;
            if (cm == null) return;

            SetPhaseLayout(cm.currentPhase);

            // HUD — labels stay as TMP, numbers rendered as sprites (+30% size, baseline-aligned)
            if (playerHPText != null)
            {
                playerHPText.text = "HP";
                NumberRenderer.Set(playerHPText.transform, "ValNum", cm.playerHP, NumberRenderer.Green,
                    26f, new Vector2(0.45f, 0.0f), new Vector2(1f, 0.75f), 0f);
            }
            if (opponentHPText != null)
            {
                opponentHPText.text = "HP";
                NumberRenderer.Set(opponentHPText.transform, "ValNum", cm.opponentHP, NumberRenderer.Red,
                    26f, new Vector2(0.45f, 0.0f), new Vector2(1f, 0.75f), 0f);
            }
            if (roundText != null)
            {
                roundText.text = "Round";
                NumberRenderer.Set(roundText.transform, "ValNum", cm.currentRound, NumberRenderer.White,
                    24f, new Vector2(0.55f, 0.0f), new Vector2(1f, 0.75f), 0f);
            }
            if (goldText != null)
            {
                goldText.text = "Gold";
                NumberRenderer.Set(goldText.transform, "ValNum", cm.playerGold, NumberRenderer.Gold,
                    26f, new Vector2(0.45f, 0.0f), new Vector2(1f, 0.75f), 0f);
            }
            if (rerollCostText != null) rerollCostText.text = $"Reroll ({cm.rerollCost}g)";

            // Shop (3 cards)
            for (int i = 0; i < shopButtons.Length; i++)
            {
                Transform slot = shopButtons[i].transform;
                if (i < cm.shopOffers.Count && cm.shopOffers[i] != null)
                {
                    var card = cm.shopOffers[i];
                    SetCardDisplay(slot, card, null, shopTexts[i], i == selectedShopIndex);
                    // Show cost as gold sprite number (bottom-center overlay)
                    NumberRenderer.Set(slot, "CostNum", card.Cost, NumberRenderer.Gold,
                        16f, new Vector2(0.30f, 0.28f), new Vector2(0.70f, 0.38f), 0.5f);
                    shopButtons[i].interactable = cm.currentPhase == CombatPhase.Shop;
                }
                else
                {
                    ClearCardDisplay(slot, shopTexts[i]);
                    NumberRenderer.Clear(slot, "CostNum");
                    shopButtons[i].interactable = false;
                }
            }

            // Player board
            for (int i = 0; i < 5; i++)
            {
                if (i >= playerSlots.Length || playerSlots[i] == null) continue;
                Transform slot = playerSlots[i].transform;

                if (cm.playerBoard[i] != null)
                {
                    var c = cm.playerBoard[i];
                    SetCardDisplay(slot, c.Data, c, playerSlotTexts[i], false);
                }
                else
                {
                    ClearCardDisplay(slot, playerSlotTexts[i]);
                    if (playerSlotTexts[i] != null)
                        playerSlotTexts[i].text = i < 3 ? "[Front]" : "[Back]";

                    // Reset outline to default
                    var outline = slot.GetComponent<Outline>();
                    if (outline != null) outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
            }

            // Opponent board
            for (int i = 0; i < 5; i++)
            {
                if (i >= opponentSlotTexts.Length || opponentSlotTexts[i] == null) continue;
                Transform slot = opponentSlotTexts[i].transform.parent;

                if (cm.opponentBoard[i] != null)
                {
                    var c = cm.opponentBoard[i];
                    SetCardDisplay(slot, c.Data, c, opponentSlotTexts[i], false);
                }
                else
                {
                    ClearCardDisplay(slot, opponentSlotTexts[i]);
                }
            }

            // Action buttons
            if (btnBattle != null) btnBattle.interactable = cm.currentPhase == CombatPhase.Shop;
            if (btnReroll != null) btnReroll.interactable = cm.currentPhase == CombatPhase.Shop && cm.playerGold >= cm.rerollCost;
        }

        // ── Card Display System ──

        /// <summary>
        /// Sets all visual elements on a card slot: art, rarity border, tier badge, rank stars, ATK/HP, name.
        /// </summary>
        private void SetCardDisplay(Transform slot, CardData card, CardInstance instance, TextMeshProUGUI mainText, bool isSelected)
        {
            if (card == null) return;

            // 1. Rarity border via Outline
            var outline = slot.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = GetRarityColor(card.Rarity);

            // 2. Background tint based on element + rank
            var slotImg = slot.GetComponent<Image>();
            if (slotImg != null)
            {
                if (isSelected)
                    slotImg.color = new Color(0.3f, 0.3f, 0.15f, 0.95f);
                else if (instance != null && instance.Tier == CardTier.Gold)
                    slotImg.color = new Color(0.2f, 0.17f, 0.05f, 0.95f);
                else if (instance != null && instance.Tier == CardTier.Silver)
                    slotImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
                else
                    slotImg.color = GetElementColor(card.Element);
            }

            // 3. Creature art (upper area)
            SetSlotArt(slot, card);

            // 4. Tier badge (top-left) — sprite number showing cost/tier
            NumberRenderer.Set(slot, "TierNum", card.Tier, NumberRenderer.Gold,
                14f, new Vector2(0.02f, 0.85f), new Vector2(0.25f, 1f), 0f);

            // 5. Rank stars (top-right)
            var rankTMP = GetOrCreateChild<TextMeshProUGUI>(slot, "RankStars",
                new Vector2(0.60f, 0.82f), new Vector2(1f, 1f));
            if (instance != null)
            {
                string stars = instance.Tier == CardTier.Gold ? "<color=#FFD700>***</color>" :
                               instance.Tier == CardTier.Silver ? "<color=#C0C0C0>**</color>" :
                               "<color=#CD7F32>*</color>";
                string mergeInfo = instance.Tier < CardTier.Gold && instance.MergeCount > 1
                    ? $" <size=9>{instance.MergeCount}/3</size>" : "";
                rankTMP.text = stars + mergeInfo;
            }
            else
            {
                rankTMP.text = ""; // Shop cards have no rank
            }
            rankTMP.fontSize = 12f;
            rankTMP.alignment = TextAlignmentOptions.TopRight;
            rankTMP.margin = new Vector4(0f, 0f, 3f, 0f);

            // 6. Card name (center)
            if (mainText != null)
            {
                mainText.text = card.CardName;
                mainText.fontSize = 11f;
                mainText.alignment = TextAlignmentOptions.Center;
            }

            // 7. ATK/HP (bottom — sprite-rendered, large and prominent)
            int atk = instance != null ? instance.CurrentAttack : card.Attack;
            int hp = instance != null ? instance.CurrentHealth : card.Health;

            // ATK on left side (red sprites, +40% size)
            NumberRenderer.Set(slot, "AtkNum", atk, NumberRenderer.Red,
                31f, new Vector2(0.02f, 0.01f), new Vector2(0.45f, 0.30f), 0.5f);

            // HP on right side (green sprites, +40% size)
            NumberRenderer.Set(slot, "HpNum", hp, NumberRenderer.Green,
                31f, new Vector2(0.55f, 0.01f), new Vector2(0.98f, 0.30f), 0.5f);

            // Row indicator stays as small TMP (not a number)
            string rowTag = card.Row == RowPreference.Front ? "F" :
                            card.Row == RowPreference.Back ? "B" : "*";
            var rowTMP = GetOrCreateChild<TextMeshProUGUI>(slot, "RowTag",
                new Vector2(0.40f, 0.02f), new Vector2(0.60f, 0.15f));
            rowTMP.text = rowTag;
            rowTMP.fontSize = 10f;
            rowTMP.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            rowTMP.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Clears all dynamic card display elements from a slot.
        /// </summary>
        private void ClearCardDisplay(Transform slot, TextMeshProUGUI mainText)
        {
            ClearSlotArt(slot);

            if (mainText != null)
            {
                mainText.text = "";
                mainText.fontSize = 14f;
            }

            // Clear dynamic children
            SetChildText(slot, "RankStars", "");
            SetChildText(slot, "RowTag", "");
            NumberRenderer.Clear(slot, "TierNum");
            NumberRenderer.Clear(slot, "AtkNum");
            NumberRenderer.Clear(slot, "HpNum");

            // Reset outline
            var outline = slot.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // Reset background
            var img = slot.GetComponent<Image>();
            if (img != null) img.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        }

        private void SetChildText(Transform slot, string childName, string text)
        {
            var child = slot.Find(childName);
            if (child != null)
            {
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = text;
            }
        }

        // ── Rarity Colors ──

        private static Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:    return new Color(0.53f, 0.53f, 0.53f, 1f); // Gray #888
                case CardRarity.Uncommon:  return new Color(0.15f, 0.68f, 0.38f, 1f); // Green #27AE60
                case CardRarity.Rare:      return new Color(0.16f, 0.50f, 0.73f, 1f); // Blue #2980B9
                case CardRarity.Legendary: return new Color(0.83f, 0.66f, 0.26f, 1f); // Gold #D4A843
                default:                   return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        // ── Dynamic Child Helpers ──

        /// <summary>
        /// Gets or creates a child TMP element at the given anchor position.
        /// </summary>
        private T GetOrCreateChild<T>(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax) where T : Component
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<T>();

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();

            // Configure TMP defaults
            if (component is TextMeshProUGUI tmp)
            {
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Truncate;
                tmp.richText = true;
            }

            return component;
        }

        // ── Card Art Helpers ──

        private void SetSlotArt(Transform slot, CardData card)
        {
            if (card == null || card.SpriteIndex <= 0) return;

            var artTransform = slot.Find("CreatureArt");
            Image artImage;

            if (artTransform == null)
            {
                var artGO = new GameObject("CreatureArt", typeof(RectTransform), typeof(Image));
                artGO.transform.SetParent(slot, false);
                var rt = artGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.10f, 0.30f);
                rt.anchorMax = new Vector2(0.90f, 0.82f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                artImage = artGO.GetComponent<Image>();
                artImage.raycastTarget = false;
                artImage.preserveAspect = true;
                artGO.transform.SetAsFirstSibling();
            }
            else
            {
                artImage = artTransform.GetComponent<Image>();
            }

            string path = CardDatabase.GetSpritePath(card);
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                artImage.sprite = sprite;
                artImage.color = Color.white;
            }
            else
            {
                artImage.sprite = null;
                artImage.color = GetElementColor(card.Element);
            }
        }

        private void ClearSlotArt(Transform slot)
        {
            var artTransform = slot.Find("CreatureArt");
            if (artTransform != null)
            {
                var img = artTransform.GetComponent<Image>();
                img.sprite = null;
                img.color = new Color(0, 0, 0, 0);
            }
        }

        private Color GetElementColor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire:  return new Color(0.25f, 0.08f, 0.08f, 0.95f);
                case ElementType.Water: return new Color(0.08f, 0.12f, 0.25f, 0.95f);
                case ElementType.Earth: return new Color(0.08f, 0.20f, 0.08f, 0.95f);
                case ElementType.Wind:  return new Color(0.15f, 0.08f, 0.22f, 0.95f);
                default:                return new Color(0.15f, 0.15f, 0.2f, 0.95f);
            }
        }

        // ── Card Detail Tooltip ──

        /// <summary>
        /// Creates the detail panel lazily on first use. Right-aligned, 220px wide tooltip.
        /// </summary>
        private void EnsureDetailPanel()
        {
            if (detailPanel != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Panel container — right sidebar area, between Reroll and Battle
            detailPanel = new GameObject("CardDetailPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            detailPanel.transform.SetParent(canvas.transform, false);
            var panelRT = detailPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.745f, 0.20f);
            panelRT.anchorMax = new Vector2(0.975f, 0.80f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

            detailBg = detailPanel.GetComponent<Image>();
            detailBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            detailOutline = detailPanel.GetComponent<Outline>();
            detailOutline.effectColor = Color.gray;
            detailOutline.effectDistance = new Vector2(2f, -2f);

            // X close button (top-right corner)
            var closeGO = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(detailPanel.transform, false);
            var closeRT = closeGO.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.85f, 0.93f);
            closeRT.anchorMax = new Vector2(0.98f, 0.99f);
            closeRT.offsetMin = closeRT.offsetMax = Vector2.zero;
            closeGO.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f, 0.8f);
            closeGO.GetComponent<Button>().onClick.AddListener(HideCardDetail);
            var closeTxtGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxtGO.transform.SetParent(closeGO.transform, false);
            var closeTxt = closeTxtGO.GetComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 12f;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            var closeTxtRT = closeTxtGO.GetComponent<RectTransform>();
            closeTxtRT.anchorMin = Vector2.zero;
            closeTxtRT.anchorMax = Vector2.one;
            closeTxtRT.offsetMin = closeTxtRT.offsetMax = Vector2.zero;

            // Card name — top
            var nameGO = new GameObject("DetailName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(detailPanel.transform, false);
            detailName = nameGO.GetComponent<TextMeshProUGUI>();
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.05f, 0.88f);
            nameRT.anchorMax = new Vector2(0.83f, 0.97f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
            detailName.fontSize = 18f;
            detailName.alignment = TextAlignmentOptions.Center;
            detailName.color = Color.white;
            detailName.raycastTarget = false;

            // Tier + Rank info
            var tierGO = new GameObject("DetailTierRank", typeof(RectTransform), typeof(TextMeshProUGUI));
            tierGO.transform.SetParent(detailPanel.transform, false);
            detailTierRank = tierGO.GetComponent<TextMeshProUGUI>();
            var tierRT = tierGO.GetComponent<RectTransform>();
            tierRT.anchorMin = new Vector2(0.05f, 0.81f);
            tierRT.anchorMax = new Vector2(0.95f, 0.88f);
            tierRT.offsetMin = tierRT.offsetMax = Vector2.zero;
            detailTierRank.fontSize = 12f;
            detailTierRank.alignment = TextAlignmentOptions.Center;
            detailTierRank.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            detailTierRank.raycastTarget = false;
            detailTierRank.richText = true;

            // Art area — compact center
            var artGO = new GameObject("DetailArt", typeof(RectTransform), typeof(Image));
            artGO.transform.SetParent(detailPanel.transform, false);
            detailArt = artGO.GetComponent<Image>();
            var artRT = artGO.GetComponent<RectTransform>();
            artRT.anchorMin = new Vector2(0.20f, 0.52f);
            artRT.anchorMax = new Vector2(0.80f, 0.80f);
            artRT.offsetMin = artRT.offsetMax = Vector2.zero;
            detailArt.preserveAspect = true;
            detailArt.raycastTarget = false;

            // Stats — ATK (left) | Row (center) | HP (right)
            var statsGO = new GameObject("DetailStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGO.transform.SetParent(detailPanel.transform, false);
            detailStats = statsGO.GetComponent<TextMeshProUGUI>();
            var statsRT = statsGO.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.05f, 0.42f);
            statsRT.anchorMax = new Vector2(0.95f, 0.52f);
            statsRT.offsetMin = statsRT.offsetMax = Vector2.zero;
            detailStats.fontSize = 16f;
            detailStats.alignment = TextAlignmentOptions.Center;
            detailStats.color = Color.white;
            detailStats.raycastTarget = false;
            detailStats.richText = true;

            // Damage threat line
            detailDamageThreat = CreateDetailTMP(detailPanel.transform, "DetailDmgThreat",
                new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.42f), 12f, TextAlignmentOptions.Center,
                new Color(1f, 0.7f, 0.3f, 0.9f));

            // Ability text / cost info
            var abilityGO = new GameObject("DetailAbility", typeof(RectTransform), typeof(TextMeshProUGUI));
            abilityGO.transform.SetParent(detailPanel.transform, false);
            detailAbility = abilityGO.GetComponent<TextMeshProUGUI>();
            var abilityRT = abilityGO.GetComponent<RectTransform>();
            abilityRT.anchorMin = new Vector2(0.08f, 0.12f);
            abilityRT.anchorMax = new Vector2(0.92f, 0.34f);
            abilityRT.offsetMin = abilityRT.offsetMax = Vector2.zero;
            detailAbility.fontSize = 12f;
            detailAbility.alignment = TextAlignmentOptions.TopLeft;
            detailAbility.color = new Color(0.9f, 0.85f, 0.7f, 1f);
            detailAbility.raycastTarget = false;
            detailAbility.richText = true;
            detailAbility.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Sell button (only visible for board cards)
            var sellGO = new GameObject("DetailSellBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            sellGO.transform.SetParent(detailPanel.transform, false);
            var sellRT = sellGO.GetComponent<RectTransform>();
            sellRT.anchorMin = new Vector2(0.15f, 0.02f);
            sellRT.anchorMax = new Vector2(0.85f, 0.09f);
            sellRT.offsetMin = sellRT.offsetMax = Vector2.zero;
            var sellImg = sellGO.GetComponent<Image>();
            sellImg.color = new Color(0.6f, 0.15f, 0.15f, 0.9f);
            detailSellBtn = sellGO.GetComponent<Button>();
            detailSellBtn.onClick.AddListener(OnDetailSellClicked);

            var sellTextGO = new GameObject("SellText", typeof(RectTransform), typeof(TextMeshProUGUI));
            sellTextGO.transform.SetParent(sellGO.transform, false);
            var sellTextRT = sellTextGO.GetComponent<RectTransform>();
            sellTextRT.anchorMin = Vector2.zero;
            sellTextRT.anchorMax = Vector2.one;
            sellTextRT.offsetMin = sellTextRT.offsetMax = Vector2.zero;
            var sellTMP = sellTextGO.GetComponent<TextMeshProUGUI>();
            sellTMP.text = "Sell (1g)";
            sellTMP.fontSize = 12f;
            sellTMP.alignment = TextAlignmentOptions.Center;
            sellTMP.color = Color.white;
            sellTMP.raycastTarget = false;

            detailPanel.SetActive(false);
        }

        private TextMeshProUGUI CreateDetailTMP(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.richText = true;
            return tmp;
        }

        private void OnDetailSellClicked()
        {
            if (detailBoardSlot < 0) return;
            var cm = CombatManager.Instance;
            if (cm == null || cm.currentPhase != CombatPhase.Shop) return;

            cm.SellCard(detailBoardSlot);
            detailBoardSlot = -1;
            HideCardDetail();
            Refresh();
        }

        /// <summary>
        /// Shows the detail panel for a given card. Called on card click during shop phase.
        /// </summary>
        private void ShowCardDetail(CardData card, CardInstance instance, int boardSlot = -1)
        {
            if (card == null) { HideCardDetail(); return; }

            EnsureDetailPanel();
            detailPanel.SetActive(true);

            // Name
            detailName.text = card.CardName;

            // Tier + rank + rarity + element
            string rankStr = "";
            if (instance != null)
            {
                rankStr = instance.Tier == CardTier.Gold ? " <color=#FFD700>*** Gold</color>" :
                          instance.Tier == CardTier.Silver ? " <color=#C0C0C0>** Silver</color>" :
                          " <color=#CD7F32>* Bronze</color>";
                if (instance.Tier < CardTier.Gold && instance.MergeCount > 1)
                    rankStr += $" ({instance.MergeCount}/3)";
            }
            detailTierRank.text = $"T{card.Tier} {card.Rarity} · {card.Element}{rankStr}";

            // Rarity border
            detailOutline.effectColor = GetRarityColor(card.Rarity);

            // Art
            string path = CardDatabase.GetSpritePath(card);
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                detailArt.sprite = sprite;
                detailArt.color = Color.white;
            }
            else
            {
                detailArt.sprite = null;
                detailArt.color = GetElementColor(card.Element);
            }

            // Stats — ATK (left), row text (center), HP (right)
            int atk = instance != null ? instance.CurrentAttack : card.Attack;
            int hp = instance != null ? instance.CurrentHealth : card.Health;
            string row = card.Row == RowPreference.Front ? "Front Row" :
                         card.Row == RowPreference.Back ? "Back Row" : "Any Row";
            detailStats.text = row;
            NumberRenderer.Set(detailStats.transform, "AtkNum", atk, NumberRenderer.Red,
                24f, new Vector2(0f, 0f), new Vector2(0.22f, 1f), 0.5f);
            NumberRenderer.Set(detailStats.transform, "HpNum", hp, NumberRenderer.Green,
                24f, new Vector2(0.78f, 0f), new Vector2(1f, 1f), 0.5f);

            // Damage threat: tier + rank bonus (Bronze=0, Silver=1, Gold=2)
            int rankBonus = instance != null ? (int)instance.Tier : 0;
            int dmgThreat = card.Tier + rankBonus;
            if (detailDamageThreat != null)
                detailDamageThreat.text = $"HP Damage if survives: {dmgThreat}";

            // Ability text
            string ability = !string.IsNullOrEmpty(card.AbilityText) ? card.AbilityText :
                             $"Cost: {card.Cost}g\n{card.Element} creature.";
            detailAbility.text = ability;

            // Sell button — only for player board cards during shop
            detailBoardSlot = boardSlot;
            if (detailSellBtn != null)
                detailSellBtn.gameObject.SetActive(boardSlot >= 0);
        }

        private void HideCardDetail()
        {
            if (detailPanel != null)
                detailPanel.SetActive(false);
        }

        // ── Pool Viewer (Tier List) ──

        private void TogglePoolViewer()
        {
            if (poolPanel != null)
            {
                Destroy(poolPanel);
                poolPanel = null;
                return;
            }
            BuildPoolViewer();
        }

        private void BuildPoolViewer()
        {
            HideCardDetail();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Full-screen overlay
            poolPanel = new GameObject("PoolViewer", typeof(RectTransform), typeof(Image));
            poolPanel.transform.SetParent(canvas.transform, false);
            var panelRT = poolPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            poolPanel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.96f);

            // Title bar
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(poolPanel.transform, false);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "MY POOL";
            titleTMP.fontSize = 28f;
            titleTMP.color = ElementColors.Gold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.raycastTarget = false;
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.1f, 0.92f);
            titleRT.anchorMax = new Vector2(0.9f, 0.99f);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

            // Close button (top-right)
            var closeGO = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(poolPanel.transform, false);
            var closeRT = closeGO.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.92f, 0.93f);
            closeRT.anchorMax = new Vector2(0.98f, 0.99f);
            closeRT.offsetMin = closeRT.offsetMax = Vector2.zero;
            closeGO.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f, 0.8f);
            closeGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (poolPanel != null) { Destroy(poolPanel); poolPanel = null; }
            });
            var closeTxtGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxtGO.transform.SetParent(closeGO.transform, false);
            var closeTxt = closeTxtGO.GetComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 16f;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            var closeTxtRT = closeTxtGO.GetComponent<RectTransform>();
            closeTxtRT.anchorMin = Vector2.zero;
            closeTxtRT.anchorMax = Vector2.one;
            closeTxtRT.offsetMin = closeTxtRT.offsetMax = Vector2.zero;

            // Get max tier for current round
            int maxTier = CombatManager.Instance != null
                ? CombatManager.Instance.GetMaxTierForRound(CombatManager.Instance.currentRound)
                : 6;

            // Get the full active pool (all tiers, not just current round)
            var fullPool = CardDatabase.GetActivePool(6);

            // Group by tier
            var byTier = new Dictionary<int, List<CardData>>();
            for (int t = 1; t <= 6; t++)
                byTier[t] = new List<CardData>();
            foreach (var card in fullPool)
            {
                if (card.Tier >= 1 && card.Tier <= 6)
                    byTier[card.Tier].Add(card);
            }

            // Tier label colors
            Color[] tierColors = new Color[]
            {
                Color.white, // unused index 0
                new Color(0.6f, 0.6f, 0.6f),    // T1 — gray
                new Color(0.4f, 0.8f, 0.4f),    // T2 — green
                new Color(0.3f, 0.5f, 1f),      // T3 — blue
                new Color(0.7f, 0.3f, 0.9f),    // T4 — purple
                new Color(1f, 0.6f, 0.1f),      // T5 — orange
                new Color(1f, 0.84f, 0f),        // T6 — gold
            };

            // Build tier rows — each row has a label + scrollable card strip
            // Grid area: y 0.05 to 0.91 (86% of height), 6 tiers
            float rowHeight = 0.86f / 6f; // ~14.3% each
            float gridTop = 0.91f;

            for (int tier = 1; tier <= 6; tier++)
            {
                float rowTop = gridTop - (tier - 1) * rowHeight;
                float rowBot = rowTop - rowHeight + 0.005f;
                bool isAvailable = tier <= maxTier;

                // Row background — subtle stripe
                var rowBG = new GameObject($"TierRow_{tier}", typeof(RectTransform), typeof(Image));
                rowBG.transform.SetParent(poolPanel.transform, false);
                var rowBGRT = rowBG.GetComponent<RectTransform>();
                rowBGRT.anchorMin = new Vector2(0.02f, rowBot);
                rowBGRT.anchorMax = new Vector2(0.98f, rowTop);
                rowBGRT.offsetMin = rowBGRT.offsetMax = Vector2.zero;
                rowBG.GetComponent<Image>().color = tier % 2 == 0
                    ? new Color(0.06f, 0.06f, 0.10f, 0.6f)
                    : new Color(0.04f, 0.04f, 0.08f, 0.4f);

                // Tier label (left column)
                var labelGO = new GameObject($"TierLabel_{tier}", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(rowBG.transform, false);
                var labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
                labelTMP.text = $"T{tier}";
                labelTMP.fontSize = 20f;
                labelTMP.color = isAvailable ? tierColors[tier] : new Color(0.3f, 0.3f, 0.3f);
                labelTMP.alignment = TextAlignmentOptions.Center;
                labelTMP.fontStyle = FontStyles.Bold;
                labelTMP.raycastTarget = false;
                var labelRT = labelGO.GetComponent<RectTransform>();
                labelRT.anchorMin = new Vector2(0f, 0f);
                labelRT.anchorMax = new Vector2(0.06f, 1f);
                labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

                // "Locked" indicator for unavailable tiers
                if (!isAvailable)
                {
                    var lockGO = new GameObject("Locked", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lockGO.transform.SetParent(rowBG.transform, false);
                    var lockTMP = lockGO.GetComponent<TextMeshProUGUI>();
                    lockTMP.text = $"(unlocks round {tier})";
                    lockTMP.fontSize = 12f;
                    lockTMP.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
                    lockTMP.alignment = TextAlignmentOptions.Left;
                    lockTMP.raycastTarget = false;
                    var lockRT = lockGO.GetComponent<RectTransform>();
                    lockRT.anchorMin = new Vector2(0.07f, 0.1f);
                    lockRT.anchorMax = new Vector2(0.4f, 0.9f);
                    lockRT.offsetMin = lockRT.offsetMax = Vector2.zero;
                }

                // Card strip (horizontal layout)
                var cards = byTier[tier];
                if (cards.Count == 0) continue;

                var strip = new GameObject($"CardStrip_{tier}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                strip.transform.SetParent(rowBG.transform, false);
                var stripRT = strip.GetComponent<RectTransform>();
                stripRT.anchorMin = new Vector2(0.07f, 0.04f);
                stripRT.anchorMax = new Vector2(0.99f, 0.96f);
                stripRT.offsetMin = stripRT.offsetMax = Vector2.zero;
                var hlg = strip.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;

                foreach (var card in cards.OrderBy(c => c.Element).ThenBy(c => c.Rarity))
                {
                    float cardWidth = 70f;
                    var cardGO = new GameObject(card.CardName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    cardGO.transform.SetParent(strip.transform, false);
                    cardGO.GetComponent<LayoutElement>().preferredWidth = cardWidth;

                    Color bgColor = isAvailable
                        ? GetElementColor(card.Element)
                        : new Color(0.15f, 0.15f, 0.15f, 0.5f);
                    cardGO.GetComponent<Image>().color = bgColor;

                    // Rarity border
                    var cardOutline = cardGO.AddComponent<Outline>();
                    cardOutline.effectColor = isAvailable ? GetRarityColor(card.Rarity) : new Color(0.2f, 0.2f, 0.2f);
                    cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

                    // Card art (upper portion)
                    string spritePath = CardDatabase.GetSpritePath(card);
                    Sprite sprite = Resources.Load<Sprite>(spritePath);
                    if (sprite != null)
                    {
                        var artGO = new GameObject("Art", typeof(RectTransform), typeof(Image));
                        artGO.transform.SetParent(cardGO.transform, false);
                        var artImg = artGO.GetComponent<Image>();
                        artImg.sprite = sprite;
                        artImg.preserveAspect = true;
                        artImg.raycastTarget = false;
                        artImg.color = isAvailable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
                        var artRT = artGO.GetComponent<RectTransform>();
                        artRT.anchorMin = new Vector2(0.05f, 0.35f);
                        artRT.anchorMax = new Vector2(0.95f, 0.95f);
                        artRT.offsetMin = artRT.offsetMax = Vector2.zero;
                    }

                    // Card name (bottom)
                    var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                    nameGO.transform.SetParent(cardGO.transform, false);
                    var nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
                    nameTMP.text = card.CardName;
                    nameTMP.fontSize = 8f;
                    nameTMP.color = isAvailable ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                    nameTMP.alignment = TextAlignmentOptions.Bottom;
                    nameTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;
                    nameTMP.raycastTarget = false;
                    var nameRT = nameGO.GetComponent<RectTransform>();
                    nameRT.anchorMin = new Vector2(0.02f, 0f);
                    nameRT.anchorMax = new Vector2(0.98f, 0.38f);
                    nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

                    // ATK/HP tiny indicators
                    var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
                    statsGO.transform.SetParent(cardGO.transform, false);
                    var statsTMP = statsGO.GetComponent<TextMeshProUGUI>();
                    statsTMP.text = $"<color=#FF4444>{card.Attack}</color>/<color=#44FF44>{card.Health}</color>";
                    statsTMP.fontSize = 9f;
                    statsTMP.color = Color.white;
                    statsTMP.alignment = TextAlignmentOptions.TopLeft;
                    statsTMP.richText = true;
                    statsTMP.raycastTarget = false;
                    var statsRT = statsGO.GetComponent<RectTransform>();
                    statsRT.anchorMin = new Vector2(0.05f, 0.88f);
                    statsRT.anchorMax = new Vector2(0.95f, 1f);
                    statsRT.offsetMin = statsRT.offsetMax = Vector2.zero;
                }
            }

            // Stats summary at bottom
            int totalActive = fullPool.Count;
            int availableNow = fullPool.Count(c => c.Tier <= maxTier);
            var summaryGO = new GameObject("Summary", typeof(RectTransform), typeof(TextMeshProUGUI));
            summaryGO.transform.SetParent(poolPanel.transform, false);
            var summaryTMP = summaryGO.GetComponent<TextMeshProUGUI>();
            summaryTMP.text = $"Pool: {totalActive} cards total | {availableNow} available this round (T1-T{maxTier})";
            summaryTMP.fontSize = 13f;
            summaryTMP.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            summaryTMP.alignment = TextAlignmentOptions.Center;
            summaryTMP.raycastTarget = false;
            var summaryRT = summaryGO.GetComponent<RectTransform>();
            summaryRT.anchorMin = new Vector2(0.1f, 0.01f);
            summaryRT.anchorMax = new Vector2(0.9f, 0.05f);
            summaryRT.offsetMin = summaryRT.offsetMax = Vector2.zero;
        }

        // ── Result Screen ──

        public void ShowResult(bool playerWon)
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText != null)
            {
                string rewardMsg = "";
                if (playerWon)
                {
                    var cm = CombatManager.Instance;
                    if (cm != null)
                    {
                        switch (cm.lastReward)
                        {
                            case RewardType.Pack:
                                rewardMsg = "\nReward: Booster Pack!";
                                break;
                            case RewardType.SingleCard:
                                string cardName = cm.lastRewardCard != null ? cm.lastRewardCard.CardName : "a card";
                                rewardMsg = $"\nReward: {cardName}";
                                break;
                            case RewardType.Gold:
                                rewardMsg = "\nReward: Gold";
                                NumberRenderer.Set(resultText.transform, "GoldReward", cm.lastRewardGold,
                                    NumberRenderer.Gold, 30f, new Vector2(0.3f, -0.5f), new Vector2(0.7f, -0.1f), 0.5f);
                                break;
                            case RewardType.Nothing:
                                rewardMsg = "\nThe opponent had nothing to offer.";
                                break;
                        }
                    }
                }
                resultText.text = (playerWon ? "VICTORY!" : "DEFEAT") + rewardMsg;
                resultText.color = playerWon ? ElementColors.Gold : ElementColors.Fire;
            }
        }
    }
}
