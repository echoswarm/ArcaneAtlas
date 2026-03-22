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

        [Header("Parallax Background")]
        public ParallaxBackgroundController parallaxBackground;

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

        // Pause overlay
        private GameObject pausePanel;

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

            // Load parallax background for the current zone
            if (parallaxBackground != null)
            {
                var zone = ZoneData.GetByName(GameState.CurrentZone);
                parallaxBackground.LoadByBiomeName(zone?.BiomePalette ?? "");
                parallaxBackground.SetPlaying(true);
            }

            var cm = FindFirstObjectByType<CombatManager>();
            if (cm == null)
            {
                var go = new GameObject("CombatManager");
                cm = go.AddComponent<CombatManager>();
            }
            cm.StartMatch();
        }

        void OnDisable()
        {
            if (parallaxBackground != null)
                parallaxBackground.SetPlaying(false);

            if (pausePanel != null) { Destroy(pausePanel); pausePanel = null; }
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
                    // Show detail for any board card (shop phase enables sell button)
                    ShowCardDetail(cm.playerBoard[slot].Data, cm.playerBoard[slot], slot);

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
                    // Cost is already shown via TierNum in SetCardDisplay — no separate overlay needed
                    NumberRenderer.Clear(slot, "CostNum");
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
            var style = GetCardStyle();

            // 1. Rarity border via Outline
            var outline = slot.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = GetRarityColor(card.Rarity);
                float borderSize = card.Rarity == CardRarity.Legendary ? 3f :
                                   card.Rarity == CardRarity.Rare ? 2.5f : 2f;
                outline.effectDistance = new Vector2(borderSize, -borderSize);
            }

            // ── Modular Card Layers (if available) ──
            if (style != null && style.HasModularCards)
            {
                // Layer 1: Element background (full card)
                var bgImg = GetOrCreateChild<Image>(slot, "ModBg", Vector2.zero, Vector2.one);
                bgImg.sprite = style.GetModBg(card.Element);
                bgImg.color = Color.white;
                bgImg.raycastTarget = false;
                bgImg.transform.SetAsFirstSibling();

                // Layer 2: Glow for rare/legendary (behind frame)
                Sprite glow = style.GetModGlow(card.Rarity);
                var glowImg = GetOrCreateChild<Image>(slot, "ModGlow", new Vector2(-0.05f, -0.05f), new Vector2(1.05f, 1.05f));
                glowImg.sprite = glow;
                glowImg.color = glow != null ? Color.white : Color.clear;
                glowImg.raycastTarget = false;

                // Layer 3: Frame by rarity
                var frameImg = GetOrCreateChild<Image>(slot, "ModFrame", Vector2.zero, Vector2.one);
                frameImg.sprite = style.GetModFrame(card.Rarity);
                frameImg.color = Color.white;
                frameImg.raycastTarget = false;

                // Layer 4: Border by element
                var borderImg = GetOrCreateChild<Image>(slot, "ModBorder", Vector2.zero, Vector2.one);
                borderImg.sprite = style.GetModBorder(card.Element);
                borderImg.color = Color.white;
                borderImg.raycastTarget = false;
            }

            // 2. Background tint (used when no modular layers)
            var slotImg = slot.GetComponent<Image>();
            if (slotImg != null)
            {
                if (style != null && style.HasModularCards)
                    slotImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Dark base behind modular layers
                else
                {
                    Color elemColor = GetElementColor(card.Element);
                    if (isSelected) elemColor = Color.Lerp(elemColor, Color.yellow, 0.3f);
                    slotImg.color = elemColor;
                }
            }

            // 4. Creature art (fills center)
            SetSlotArt(slot, card);

            // 5. Caption bar at TOP for card name
            if (style != null && style.HasModularCards)
            {
                var captionImg = GetOrCreateChild<Image>(slot, "ModCaption",
                    new Vector2(0.0f, 0.80f), new Vector2(1.0f, 0.98f));
                captionImg.sprite = style.GetModCaption(card.Element);
                captionImg.color = Color.white;
                captionImg.raycastTarget = false;
            }

            if (mainText != null)
            {
                mainText.text = card.CardName;
                mainText.fontSize = 15f;
                mainText.fontStyle = TMPro.FontStyles.Normal;
                mainText.alignment = TextAlignmentOptions.Center;
                mainText.color = Color.white;
                // 3. Shifted left ~5px by adjusting margins (less right margin)
                mainText.margin = new Vector4(14f, 0f, 8f, 0f);

                // Reposition mainText to top of card, centered better
                var mtRT = mainText.GetComponent<RectTransform>();
                if (mtRT != null)
                {
                    mtRT.anchorMin = new Vector2(0.12f, 0.80f);
                    mtRT.anchorMax = new Vector2(0.92f, 0.98f);
                    mtRT.offsetMin = mtRT.offsetMax = Vector2.zero;
                }

                if (pixelFont == null)
                    pixelFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Pixeled SDF");
                if (pixelFont != null)
                    mainText.font = pixelFont;

                // 4. Drop shadow on card name
                var shadow = mainText.GetComponent<Shadow>();
                if (shadow == null) shadow = mainText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // 5b. Row indicator — 2. moved down ~10px
            string rowLetter = card.Row == RowPreference.Front ? "F" :
                               card.Row == RowPreference.Back ? "B" : "*";
            var rowTMP = GetOrCreateChild<TextMeshProUGUI>(slot, "RowTag",
                new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.32f));
            rowTMP.text = rowLetter;
            rowTMP.fontSize = 20f;
            rowTMP.alignment = TextAlignmentOptions.Center;
            rowTMP.color = new Color(1f, 1f, 1f, 0.6f);
            if (pixelFont != null) rowTMP.font = pixelFont;

            // 6. Cost — 1. dark circle instead of gold (gold-on-gold was unreadable)
            if (style != null && style.ModCostCircle != null)
            {
                var costCircle = GetOrCreateChild<Image>(slot, "CostBg",
                    new Vector2(-0.04f, 0.82f), new Vector2(0.14f, 1.04f));
                costCircle.sprite = style.ModCostCircle;
                costCircle.color = new Color(0.15f, 0.12f, 0.08f); // Dark brown tint for readability
                costCircle.raycastTarget = false;
                costCircle.preserveAspect = true;
            }
            else
            {
                var costBg = GetOrCreateChild<Image>(slot, "CostBg",
                    new Vector2(0.0f, 0.85f), new Vector2(0.15f, 1.0f));
                costBg.color = new Color(0f, 0f, 0f, 0.7f);
                costBg.raycastTarget = false;
            }

            NumberRenderer.Set(slot, "TierNum", card.Cost, NumberRenderer.Gold,
                22f, new Vector2(-0.02f, 0.84f), new Vector2(0.12f, 1.02f), 0.5f);

            // 7. Rank stars (top-right, only for board cards)
            var rankTMP = GetOrCreateChild<TextMeshProUGUI>(slot, "RankStars",
                new Vector2(0.70f, 0.87f), new Vector2(0.98f, 1f));
            if (instance != null)
            {
                string stars = instance.Tier == CardTier.Gold ? "<color=#FFD700>***</color>" :
                               instance.Tier == CardTier.Silver ? "<color=#C0C0C0>**</color>" :
                               "<color=#CD7F32>*</color>";
                string mergeInfo = instance.Tier < CardTier.Gold && instance.MergeCount > 1
                    ? $" <size=10>{instance.MergeCount}/3</size>" : "";
                rankTMP.text = stars + mergeInfo;
            }
            else
            {
                rankTMP.text = "";
            }
            rankTMP.fontSize = 14f;
            rankTMP.alignment = TextAlignmentOptions.TopRight;

            // 8. ATK — bottom-left with modular square or dark backing
            int atk = instance != null ? instance.CurrentAttack : card.Attack;
            int hp = instance != null ? instance.CurrentHealth : card.Health;

            var atkBg = GetOrCreateChild<Image>(slot, "AtkBg",
                new Vector2(-0.01f, -0.01f), new Vector2(0.19f, 0.19f));
            atkBg.sprite = (style != null && style.ModStatSquare != null) ? style.ModStatSquare : null;
            atkBg.color = (atkBg.sprite != null) ? Color.white : new Color(0f, 0f, 0f, 0.7f);
            atkBg.raycastTarget = false;
            atkBg.preserveAspect = true;

            // ATK number centered in the box
            NumberRenderer.Set(slot, "AtkNum", atk, NumberRenderer.Red,
                32f, new Vector2(0.0f, 0.0f), new Vector2(0.18f, 0.18f), 0.5f);

            // 9. HP — bottom-right with modular square or dark backing
            var hpBg = GetOrCreateChild<Image>(slot, "HpBg",
                new Vector2(0.81f, -0.01f), new Vector2(1.01f, 0.19f));
            hpBg.sprite = (style != null && style.ModStatSquare != null) ? style.ModStatSquare : null;
            hpBg.color = (hpBg.sprite != null) ? Color.white : new Color(0f, 0f, 0f, 0.7f);
            hpBg.raycastTarget = false;
            hpBg.preserveAspect = true;

            // HP number centered in the box
            NumberRenderer.Set(slot, "HpNum", hp, NumberRenderer.Green,
                32f, new Vector2(0.82f, 0.0f), new Vector2(1.0f, 0.18f), 0.5f);

            // 10. Enforce rendering order: bg → frame → border → creature → caption → text/numbers
            // Earlier siblings render behind, later siblings render in front.
            SetSiblingOrder(slot, "ModBg", 0);
            SetSiblingOrder(slot, "ModGlow", 1);
            SetSiblingOrder(slot, "ModFrame", 2);
            SetSiblingOrder(slot, "ModBorder", 3);
            BringToFront(slot, "CreatureArt");    // Creature on top of frame layers
            BringToFront(slot, "RowTag");         // Row letter on portrait
            BringToFront(slot, "ModCaption");     // Name bar on top of creature
            BringToFront(slot, "CostBg");
            BringToFront(slot, "TierNum");
            if (mainText != null) mainText.transform.SetAsLastSibling();
            BringToFront(slot, "RankStars");
            BringToFront(slot, "AtkBg");
            BringToFront(slot, "AtkNum");
            BringToFront(slot, "HpBg");
            BringToFront(slot, "HpNum");
        }

        private void BringToFront(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null) child.SetAsLastSibling();
        }

        private void SetSiblingOrder(Transform parent, string childName, int index)
        {
            var child = parent.Find(childName);
            if (child != null) child.SetSiblingIndex(index);
        }

        /// <summary>
        /// Clears all dynamic card display elements from a slot.
        /// </summary>
        private void ClearCardDisplay(Transform slot, TextMeshProUGUI mainText)
        {
            if (mainText != null)
            {
                mainText.text = "";
                mainText.fontSize = 14f;
            }

            // Destroy ALL dynamic children — guarantees clean slate
            // Keep only children that existed in the prefab (mainText, the slot's own Image/Outline)
            string[] dynamicChildren = {
                "CreatureArt", "CardFrame", "ElementIcon", "ElemBg",
                "ModBg", "ModGlow", "ModFrame", "ModBorder", "ModCaption",
                "CostBg", "AtkBg", "HpBg", "RankStars", "RowTag"
            };
            foreach (string childName in dynamicChildren)
            {
                var child = slot.Find(childName);
                if (child != null) Destroy(child.gameObject);
            }

            // Clear number renderers
            NumberRenderer.Clear(slot, "TierNum");
            NumberRenderer.Clear(slot, "AtkNum");
            NumberRenderer.Clear(slot, "HpNum");
            NumberRenderer.Clear(slot, "CostNum");

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

        // Cached card style and font — loaded once from Resources
        private static CardStyleDef cachedCardStyle;
        private static TMPro.TMP_FontAsset pixelFont;
        private static bool hasLoggedStyleLoad = false;
        private static CardStyleDef GetCardStyle()
        {
            if (cachedCardStyle == null)
            {
                cachedCardStyle = Resources.Load<CardStyleDef>("CardStyles/MiniFantasyDefault");
                if (cachedCardStyle == null)
                    cachedCardStyle = Resources.Load<CardStyleDef>("CardStyles/Default");

                if (!hasLoggedStyleLoad)
                {
                    hasLoggedStyleLoad = true;
                    if (cachedCardStyle != null)
                        Debug.Log($"[CombatUI] Loaded card style: '{cachedCardStyle.StyleName}' with {cachedCardStyle.CreatureOverrides?.Length ?? 0} creature overrides");
                    else
                        Debug.LogError("[CombatUI] FAILED to load any CardStyleDef from Resources/CardStyles/!");
                }
            }
            return cachedCardStyle;
        }

        private void SetSlotArt(Transform slot, CardData card)
        {
            if (card == null || card.SpriteIndex <= 0) return;

            var style = GetCardStyle();

            // Creature art
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
            }
            else
            {
                artImage = artTransform.GetComponent<Image>();
            }

            // Try style overrides first, then direct loading with Texture2D fallback
            Sprite sprite = null;
            if (style != null)
                sprite = style.GetCreatureArt(card);
            if (sprite == null)
            {
                string path = CardDatabase.GetSpritePath(card);
                sprite = Resources.Load<Sprite>(path);
                if (sprite == null)
                {
                    var tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }

            if (sprite != null)
            {
                artImage.sprite = sprite;
                artImage.color = Color.white;
            }
            else
            {
                // Debug: log the failed path so we can diagnose
                Debug.LogWarning($"[CombatUI] No creature sprite for {card.CardName}: path='{CardDatabase.GetSpritePath(card)}' SpriteIndex={card.SpriteIndex}");
                artImage.sprite = null;
                artImage.color = GetElementColor(card.Element);
            }

            // Card frame overlay — skip legacy frames when modular cards are active
            if (style != null && !style.HasModularCards)
            {
                Sprite frame = style.GetFrame(card.Rarity);
                if (frame != null)
                {
                    var frameTransform = slot.Find("CardFrame");
                    Image frameImage;
                    if (frameTransform == null)
                    {
                        var frameGO = new GameObject("CardFrame", typeof(RectTransform), typeof(Image));
                        frameGO.transform.SetParent(slot, false);
                        var frt = frameGO.GetComponent<RectTransform>();
                        frt.anchorMin = Vector2.zero;
                        frt.anchorMax = Vector2.one;
                        frt.offsetMin = frt.offsetMax = Vector2.zero;
                        frameImage = frameGO.GetComponent<Image>();
                        frameImage.raycastTarget = false;
                        frameGO.transform.SetAsFirstSibling();
                    }
                    else
                    {
                        frameImage = frameTransform.GetComponent<Image>();
                    }
                    frameImage.sprite = frame;
                    frameImage.color = Color.white;
                    frameImage.type = Image.Type.Sliced;
                }

                // Element icon
                Sprite icon = style.GetElementIcon(card.Element);
                if (icon != null)
                {
                    var iconTransform = slot.Find("ElementIcon");
                    Image iconImage;
                    if (iconTransform == null)
                    {
                        var iconGO = new GameObject("ElementIcon", typeof(RectTransform), typeof(Image));
                        iconGO.transform.SetParent(slot, false);
                        var irt = iconGO.GetComponent<RectTransform>();
                        irt.anchorMin = new Vector2(0.02f, 0.82f);
                        irt.anchorMax = new Vector2(0.18f, 0.98f);
                        irt.offsetMin = irt.offsetMax = Vector2.zero;
                        iconImage = iconGO.GetComponent<Image>();
                        iconImage.raycastTarget = false;
                        iconImage.preserveAspect = true;
                    }
                    else
                    {
                        iconImage = iconTransform.GetComponent<Image>();
                    }
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                }
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
                case ElementType.Fire:  return new Color(0.40f, 0.12f, 0.08f, 0.95f);
                case ElementType.Water: return new Color(0.08f, 0.18f, 0.40f, 0.95f);
                case ElementType.Earth: return new Color(0.12f, 0.32f, 0.10f, 0.95f);
                case ElementType.Wind:  return new Color(0.22f, 0.22f, 0.38f, 0.95f);
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
            detailOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
            detailOutline.effectDistance = new Vector2(3f, -3f);

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

            // Card name — top, large and bold
            var nameGO = new GameObject("DetailName", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Shadow));
            nameGO.transform.SetParent(detailPanel.transform, false);
            detailName = nameGO.GetComponent<TextMeshProUGUI>();
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.05f, 0.91f);
            nameRT.anchorMax = new Vector2(0.83f, 0.99f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
            detailName.fontSize = 22f;
            detailName.fontStyle = TMPro.FontStyles.Bold;
            detailName.alignment = TextAlignmentOptions.Left;
            detailName.margin = new Vector4(6f, 0f, 0f, 0f);
            detailName.color = Color.white;
            detailName.raycastTarget = false;
            var nameShadow = nameGO.GetComponent<Shadow>();
            nameShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            nameShadow.effectDistance = new Vector2(2f, -2f);

            // Tier + Rank info — compact line below name
            var tierGO = new GameObject("DetailTierRank", typeof(RectTransform), typeof(TextMeshProUGUI));
            tierGO.transform.SetParent(detailPanel.transform, false);
            detailTierRank = tierGO.GetComponent<TextMeshProUGUI>();
            var tierRT = tierGO.GetComponent<RectTransform>();
            tierRT.anchorMin = new Vector2(0.05f, 0.86f);
            tierRT.anchorMax = new Vector2(0.95f, 0.91f);
            tierRT.offsetMin = tierRT.offsetMax = Vector2.zero;
            detailTierRank.fontSize = 15f;
            detailTierRank.alignment = TextAlignmentOptions.Center;
            detailTierRank.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            detailTierRank.raycastTarget = false;
            detailTierRank.richText = true;

            // Art area — large portrait center
            var artGO = new GameObject("DetailArt", typeof(RectTransform), typeof(Image));
            artGO.transform.SetParent(detailPanel.transform, false);
            detailArt = artGO.GetComponent<Image>();
            var artRT = artGO.GetComponent<RectTransform>();
            artRT.anchorMin = new Vector2(0.10f, 0.44f);
            artRT.anchorMax = new Vector2(0.90f, 0.86f);
            artRT.offsetMin = artRT.offsetMax = Vector2.zero;
            detailArt.preserveAspect = true;
            detailArt.raycastTarget = false;

            // ── Info section below portrait ──

            // Stats row: ATK | Row | HP
            var statsGO = new GameObject("DetailStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGO.transform.SetParent(detailPanel.transform, false);
            detailStats = statsGO.GetComponent<TextMeshProUGUI>();
            var statsRT = statsGO.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.05f, 0.36f);
            statsRT.anchorMax = new Vector2(0.95f, 0.44f);
            statsRT.offsetMin = statsRT.offsetMax = Vector2.zero;
            detailStats.fontSize = 20f;
            detailStats.alignment = TextAlignmentOptions.Center;
            detailStats.color = Color.white;
            detailStats.raycastTarget = false;
            detailStats.richText = true;

            // Damage threat — warning line
            detailDamageThreat = CreateDetailTMP(detailPanel.transform, "DetailDmgThreat",
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.36f), 13f, TextAlignmentOptions.Center,
                new Color(1f, 0.7f, 0.3f, 0.9f));

            // Ability / description — main info area
            var abilityGO = new GameObject("DetailAbility", typeof(RectTransform), typeof(TextMeshProUGUI));
            abilityGO.transform.SetParent(detailPanel.transform, false);
            detailAbility = abilityGO.GetComponent<TextMeshProUGUI>();
            var abilityRT = abilityGO.GetComponent<RectTransform>();
            abilityRT.anchorMin = new Vector2(0.08f, 0.04f);
            abilityRT.anchorMax = new Vector2(0.92f, 0.28f);
            abilityRT.offsetMin = abilityRT.offsetMax = Vector2.zero;
            detailAbility.fontSize = 14f;
            detailAbility.alignment = TextAlignmentOptions.Center;
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

            // Element-tinted background
            detailBg.color = GetElementColor(card.Element);
            detailOutline.effectColor = GetRarityColor(card.Rarity);

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

            // Art — use CardStyleDef overrides (same as shop cards)
            var style = GetCardStyle();
            Sprite artSprite = null;
            if (style != null)
                artSprite = style.GetCreatureArt(card);
            if (artSprite == null)
            {
                string path = CardDatabase.GetSpritePath(card);
                artSprite = Resources.Load<Sprite>(path);
                if (artSprite == null)
                {
                    var tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                        artSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            if (artSprite != null)
            {
                detailArt.sprite = artSprite;
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

        // ── Pause Overlay ──

        public void ShowPauseOverlay()
        {
            if (pausePanel != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Full-screen dim
            pausePanel = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
            pausePanel.transform.SetParent(canvas.transform, false);
            var overlayRT = pausePanel.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
            pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);

            // Center dialog box
            var dialogGO = new GameObject("Dialog", typeof(RectTransform), typeof(Image), typeof(Outline));
            dialogGO.transform.SetParent(pausePanel.transform, false);
            var dialogRT = dialogGO.GetComponent<RectTransform>();
            dialogRT.anchorMin = new Vector2(0.38f, 0.38f);
            dialogRT.anchorMax = new Vector2(0.62f, 0.62f);
            dialogRT.offsetMin = dialogRT.offsetMax = Vector2.zero;
            dialogGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
            var dialogOutline = dialogGO.GetComponent<Outline>();
            dialogOutline.effectColor = new Color(0.6f, 0.5f, 0.3f);
            dialogOutline.effectDistance = new Vector2(3f, -3f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(dialogGO.transform, false);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "PAUSED";
            titleTMP.fontSize = 32f;
            titleTMP.color = ElementColors.Gold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.raycastTarget = false;
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.1f, 0.70f);
            titleRT.anchorMax = new Vector2(0.9f, 0.92f);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

            // Resume button
            var resumeGO = new GameObject("ResumeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            resumeGO.transform.SetParent(dialogGO.transform, false);
            var resumeRT = resumeGO.GetComponent<RectTransform>();
            resumeRT.anchorMin = new Vector2(0.15f, 0.50f);
            resumeRT.anchorMax = new Vector2(0.85f, 0.65f);
            resumeRT.offsetMin = resumeRT.offsetMax = Vector2.zero;
            resumeGO.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.15f, 0.9f);
            resumeGO.GetComponent<Button>().onClick.AddListener(() => CombatManager.Instance?.ResumeCombat());
            AddPauseButtonLabel(resumeGO.transform, "Resume");

            // Quit to Menu button
            var quitGO = new GameObject("QuitBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            quitGO.transform.SetParent(dialogGO.transform, false);
            var quitRT = quitGO.GetComponent<RectTransform>();
            quitRT.anchorMin = new Vector2(0.15f, 0.28f);
            quitRT.anchorMax = new Vector2(0.85f, 0.43f);
            quitRT.offsetMin = quitRT.offsetMax = Vector2.zero;
            quitGO.GetComponent<Image>().color = new Color(0.45f, 0.15f, 0.15f, 0.9f);
            quitGO.GetComponent<Button>().onClick.AddListener(() => CombatManager.Instance?.QuitToMenu());
            AddPauseButtonLabel(quitGO.transform, "Quit to Menu");
        }

        public void HidePauseOverlay()
        {
            if (pausePanel != null)
            {
                Destroy(pausePanel);
                pausePanel = null;
            }
        }

        private void AddPauseButtonLabel(Transform parent, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
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
