using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;
using ArcaneAtlas.UI;

namespace ArcaneAtlas.Combat
{
    public enum CombatPhase { Shop, Battle, RoundEnd, MatchEnd }

    /// <summary>
    /// Reward given after winning a duel.
    /// </summary>
    public enum RewardType { Pack, SingleCard, Gold, Nothing }

    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Match State")]
        public int playerHP = 20;
        public int opponentHP = 20;
        public int currentRound = 1;
        public int playerGold = 0;
        public CombatPhase currentPhase = CombatPhase.Shop;

        [Header("Board State")]
        public CardInstance[] playerBoard = new CardInstance[5]; // 0-2 front, 3-4 back
        public CardInstance[] opponentBoard = new CardInstance[5];

        [Header("Shop")]
        public List<CardData> shopOffers;
        public int rerollCost = 1;

        // Last match reward (read by ReturnToExploration)
        public RewardType lastReward = RewardType.Nothing;
        public CardData lastRewardCard;
        public int lastRewardGold;

        private NpcData opponent;
        private int baseGoldPerRound = 3;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartMatch()
        {
            opponent = GameState.CurrentOpponent;
            playerHP = 20;
            opponentHP = 20;
            currentRound = 1;
            playerBoard = new CardInstance[5];
            opponentBoard = new CardInstance[5];

            StartRound();
        }

        private void StartRound()
        {
            currentPhase = CombatPhase.Shop;
            playerGold = baseGoldPerRound + currentRound - 1; // 3, 4, 5...
            RefreshShop();
            AIBuyCards();
            UpdateUI();
        }

        // --- Shop Phase ---

        /// <summary>
        /// Max tier available = current round, capped at 6.
        /// Round 1 = Tier 1 only, Round 2 = Tier 1-2, ... Round 6+ = all tiers.
        /// </summary>
        public int GetMaxTierForRound(int round)
        {
            return Mathf.Min(round, 6);
        }

        public void RefreshShop()
        {
            int maxTier = GetMaxTierForRound(currentRound);
            var pool = CardDatabase.GetActivePool(maxTier, 10);
            shopOffers = new List<CardData>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                shopOffers.Add(pool[Random.Range(0, pool.Count)]);
            }
        }

        public bool BuyCard(int shopIndex, int boardSlot)
        {
            if (shopIndex < 0 || shopIndex >= shopOffers.Count) return false;
            if (shopOffers[shopIndex] == null) return false;
            var card = shopOffers[shopIndex];
            if (playerGold < card.Cost) return false;

            // Check if we can auto-merge with an existing board card
            int mergeSlot = FindMergeSlot(card);
            if (mergeSlot >= 0)
            {
                // Auto-merge: buying a copy of a card already on the board
                playerGold -= card.Cost;
                shopOffers[shopIndex] = null;
                CheckTripleMerge(playerBoard, mergeSlot, card);
                UpdateUI();
                return true;
            }

            // Normal placement
            if (boardSlot < 0 || boardSlot >= 5) return false;
            if (playerBoard[boardSlot] != null) return false;

            bool isFrontRow = boardSlot < 3;
            if (card.Row == RowPreference.Front && !isFrontRow) return false;
            if (card.Row == RowPreference.Back && isFrontRow) return false;

            playerGold -= card.Cost;
            playerBoard[boardSlot] = new CardInstance(card);
            shopOffers[shopIndex] = null;

            UpdateUI();
            return true;
        }

        /// <summary>
        /// Finds a board slot with a matching card (same name, same tier) for auto-merge.
        /// Returns -1 if no match found.
        /// </summary>
        private int FindMergeSlot(CardData card)
        {
            for (int i = 0; i < 5; i++)
            {
                if (playerBoard[i] != null &&
                    playerBoard[i].Data.CardName == card.CardName &&
                    playerBoard[i].Tier < CardTier.Gold)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Handles triple merge. The target slot's card tracks how many copies it has absorbed.
        /// When it reaches 3 total (itself + 2 bought), it upgrades.
        /// </summary>
        private void CheckTripleMerge(CardInstance[] board, int targetSlot, CardData boughtCard)
        {
            var existing = board[targetSlot];
            if (existing == null) return;

            existing.MergeCount++;
            if (existing.MergeCount >= 3)
            {
                bool isGold = existing.Tier == CardTier.Silver; // Silver→Gold
                existing.MergeCount = 1; // Reset for next upgrade level
                existing.Upgrade();

                // Trigger merge animation
                var combatUI = FindFirstObjectByType<CombatUI>();
                Transform slotT = GetPlayerSlotTransform(combatUI, targetSlot);
                if (slotT != null)
                {
                    var animator = GetOrCreateAnimator(combatUI);
                    if (animator != null)
                        animator.StartCoroutine(animator.AnimateMerge(slotT, isGold));
                }
            }
        }

        public void SellCard(int boardSlot)
        {
            if (boardSlot < 0 || boardSlot >= 5) return;
            if (playerBoard[boardSlot] == null) return;

            // Show gold popup at the slot being sold
            var combatUI = FindFirstObjectByType<CombatUI>();
            Transform slotT = GetPlayerSlotTransform(combatUI, boardSlot);
            if (slotT != null)
                NumberPopup.SpawnAtTransform(slotT, 1, NumberColor.Yellow, "+", 1f);

            // Sell for 1 gold regardless of tier (economy sink — don't break even)
            playerGold += 1;
            playerBoard[boardSlot] = null;
            UpdateUI();
        }

        public void Reroll()
        {
            if (playerGold < rerollCost) return;
            playerGold -= rerollCost;
            RefreshShop();
            UpdateUI();
        }

        // --- Battle Phase ---

        public void StartBattle()
        {
            currentPhase = CombatPhase.Battle;
            nextAttackerIndex = 0;
            StartCoroutine(ResolveBattle());
        }

        private IEnumerator ResolveBattle()
        {
            var combatUI = FindFirstObjectByType<CombatUI>();
            var animator = GetOrCreateAnimator(combatUI);

            // Intro delay — let the player see the opponent's board
            if (combatUI != null)
                combatUI.SetPhaseLayout(CombatPhase.Battle);
            UpdateUI();
            yield return new WaitForSeconds(1.0f);

            // === Main combat loop: fight until one side is wiped ===
            bool playerTurn = true;
            int maxIterations = 50; // Safety cap
            int iterations = 0;

            while (HasLivingUnits(playerBoard) && HasLivingUnits(opponentBoard) && iterations < maxIterations)
            {
                iterations++;

                if (playerTurn)
                {
                    // Find next living player unit to attack
                    int attackerSlot = FindNextAttacker(playerBoard);
                    if (attackerSlot >= 0)
                    {
                        int targetSlot = FindTargetSlot(opponentBoard);
                        if (targetSlot >= 0)
                        {
                            yield return DoAttack(combatUI, animator,
                                playerBoard, attackerSlot, true,
                                opponentBoard, targetSlot);
                        }
                    }
                }
                else
                {
                    // Find next living opponent unit to attack
                    int attackerSlot = FindNextAttacker(opponentBoard);
                    if (attackerSlot >= 0)
                    {
                        int targetSlot = FindTargetSlot(playerBoard);
                        if (targetSlot >= 0)
                        {
                            yield return DoAttack(combatUI, animator,
                                opponentBoard, attackerSlot, false,
                                playerBoard, targetSlot);
                        }
                    }
                }

                playerTurn = !playerTurn;
            }

            // === Post-combat damage phase ===
            // Surviving units deal HP damage = tier + rank bonus (Bronze=0, Silver=1, Gold=2)
            bool playerWonRound = HasLivingUnits(playerBoard);
            CardInstance[] winnerBoard = playerWonRound ? playerBoard : opponentBoard;
            Transform hpTarget = playerWonRound
                ? (combatUI != null ? combatUI.opponentHPText?.transform : null)
                : (combatUI != null ? combatUI.playerHPText?.transform : null);

            // 1-second pause to show the aftermath
            yield return new WaitForSeconds(1.0f);

            // Each surviving unit steps forward and deals HP damage
            int totalHpDamage = 0;
            for (int i = 0; i < 5; i++)
            {
                if (winnerBoard[i] == null || winnerBoard[i].CurrentHealth <= 0) continue;

                int unitDmg = GetUnitHPDamage(winnerBoard[i]);
                totalHpDamage += unitDmg;

                // Highlight the unit
                Transform unitT = playerWonRound
                    ? GetPlayerSlotTransform(combatUI, i)
                    : GetOpponentSlotTransform(combatUI, i);

                if (unitT != null)
                {
                    // Flash the unit gold briefly
                    var img = unitT.GetComponent<Image>();
                    Color origColor = img != null ? img.color : Color.white;
                    if (img != null) img.color = new Color(1f, 0.84f, 0f, 1f);

                    // Show damage number floating up from the unit
                    NumberPopup.SpawnAtTransform(unitT, unitDmg, NumberColor.Yellow, "", 1.5f, 1.0f);
                    yield return new WaitForSeconds(0.3f);

                    // Apply HP damage
                    if (playerWonRound)
                        opponentHP -= unitDmg;
                    else
                        playerHP -= unitDmg;
                    UpdateUI();

                    // Flash HP display — red for player taking damage, orange for opponent
                    if (hpTarget != null)
                        NumberPopup.SpawnAtTransform(hpTarget, unitDmg,
                            playerWonRound ? NumberColor.Yellow : NumberColor.Red, "-", 1.2f, 0.8f);

                    yield return new WaitForSeconds(0.2f);

                    // Restore unit color
                    if (img != null) img.color = origColor;
                }
            }

            UpdateUI();
            yield return new WaitForSeconds(0.5f);

            // Check match end
            if (playerHP <= 0 || opponentHP <= 0)
            {
                EndMatch(playerHP > 0);
            }
            else
            {
                currentRound++;
                CleanBoard(playerBoard);
                CleanBoard(opponentBoard);
                StartRound();
            }
        }

        /// <summary>
        /// Executes a single attack: attacker hits target, damage popup, death check.
        /// </summary>
        private IEnumerator DoAttack(CombatUI combatUI, CombatAnimator animator,
            CardInstance[] attackerBoard, int attackerSlot, bool attackerIsPlayer,
            CardInstance[] targetBoard, int targetSlot)
        {
            int dmg = attackerBoard[attackerSlot].CurrentAttack;

            Transform attackerT = attackerIsPlayer
                ? GetPlayerSlotTransform(combatUI, attackerSlot)
                : GetOpponentSlotTransform(combatUI, attackerSlot);
            Transform targetT = attackerIsPlayer
                ? GetOpponentSlotTransform(combatUI, targetSlot)
                : GetPlayerSlotTransform(combatUI, targetSlot);

            // Animate attack arc
            if (animator != null && attackerT != null)
                yield return animator.AnimateAttack(attackerT, attackerIsPlayer);

            targetBoard[targetSlot].CurrentHealth -= dmg;
            UpdateUI();

            // Hit effect + damage popup
            if (animator != null && targetT != null)
            {
                animator.StartCoroutine(animator.AnimateHit(targetT));
                animator.StartCoroutine(animator.ShowDamagePopup(targetT, dmg));
            }
            yield return new WaitForSeconds(0.3f);

            // Death check
            if (targetBoard[targetSlot].CurrentHealth <= 0)
            {
                if (animator != null && targetT != null)
                    yield return animator.AnimateDefeat(targetT);
                ClearDeadCard(targetBoard, targetBoard[targetSlot]);
                UpdateUI();
            }
        }

        /// <summary>
        /// HP damage a surviving unit deals = tier + rank bonus.
        /// Bronze=0, Silver=1, Gold=2. T1 Bronze=1, T6 Gold=8.
        /// </summary>
        public static int GetUnitHPDamage(CardInstance card)
        {
            if (card == null) return 0;
            int rankBonus = (int)card.Tier; // Bronze=0, Silver=1, Gold=2
            return card.Data.Tier + rankBonus;
        }

        private bool HasLivingUnits(CardInstance[] board)
        {
            for (int i = 0; i < 5; i++)
            {
                if (board[i] != null && board[i].CurrentHealth > 0) return true;
            }
            return false;
        }

        private int nextAttackerIndex = 0;

        /// <summary>
        /// Round-robin attacker selection. Each call picks the next living unit.
        /// </summary>
        private int FindNextAttacker(CardInstance[] board)
        {
            for (int tries = 0; tries < 5; tries++)
            {
                int slot = nextAttackerIndex % 5;
                nextAttackerIndex++;
                if (board[slot] != null && board[slot].CurrentHealth > 0)
                    return slot;
            }
            return -1;
        }

        // --- Animation Helpers ---

        private CombatAnimator GetOrCreateAnimator(CombatUI ui)
        {
            if (ui == null) return null;
            var animator = ui.GetComponent<CombatAnimator>();
            if (animator == null)
                animator = ui.gameObject.AddComponent<CombatAnimator>();
            return animator;
        }

        private Transform GetPlayerSlotTransform(CombatUI ui, int slot)
        {
            if (ui == null || ui.playerSlots == null || slot >= ui.playerSlots.Length) return null;
            return ui.playerSlots[slot]?.transform;
        }

        private Transform GetOpponentSlotTransform(CombatUI ui, int slot)
        {
            if (ui == null || ui.opponentSlotTexts == null || slot >= ui.opponentSlotTexts.Length) return null;
            return ui.opponentSlotTexts[slot]?.transform.parent;
        }

        private int FindTargetSlot(CardInstance[] board)
        {
            // Prioritize front row (0-2), then back row (3-4)
            for (int i = 0; i < 3; i++)
            {
                if (board[i] != null && board[i].CurrentHealth > 0)
                    return i;
            }
            for (int i = 3; i < 5; i++)
            {
                if (board[i] != null && board[i].CurrentHealth > 0)
                    return i;
            }
            return -1;
        }

        // --- AI ---

        private void AIBuyCards()
        {
            var aiPool = GetAICardPool();
            int aiGold = baseGoldPerRound + currentRound - 1;
            int maxTier = GetMaxTierForRound(currentRound);

            for (int slot = 0; slot < 5 && aiGold > 0; slot++)
            {
                if (opponentBoard[slot] != null) continue;

                var affordable = aiPool.Where(c => c.Tier <= maxTier && c.Cost <= aiGold).ToList();
                if (affordable.Count == 0) break;

                var pick = affordable[Random.Range(0, affordable.Count)];
                bool isFrontRow = slot < 3;
                if (pick.Row == RowPreference.Front && !isFrontRow) continue;
                if (pick.Row == RowPreference.Back && isFrontRow) continue;

                opponentBoard[slot] = new CardInstance(pick);
                aiGold -= pick.Cost;
            }
        }

        private List<CardData> GetAICardPool()
        {
            // Use opponent's curated pool if available (generated at NPC spawn time)
            if (opponent != null && opponent.CardPool != null && opponent.CardPool.Count > 0)
                return opponent.CardPool;

            // Fallback to full element database (legacy behavior)
            if (opponent != null)
                return CardDatabase.GetCardsByElement(opponent.Element);
            return CardDatabase.GetAllCards();
        }

        // --- Match End with Reward Variety ---

        private void EndMatch(bool playerWon)
        {
            currentPhase = CombatPhase.MatchEnd;

            if (playerWon)
                RollReward();

            var combatUI = FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
                combatUI.ShowResult(playerWon);
        }

        /// <summary>
        /// Rolls reward for winning. Bosses always give a pack.
        /// Regular opponents: 50% pack, 25% single card, 15% gold, 10% nothing.
        /// </summary>
        private void RollReward()
        {
            bool isBoss = opponent != null && opponent.IsBoss;

            if (isBoss)
            {
                // Boss always drops a pack
                lastReward = RewardType.Pack;
                GameState.Packs++;
                return;
            }

            float roll = Random.value;
            if (roll < 0.50f)
            {
                // 50% — booster pack
                lastReward = RewardType.Pack;
                GameState.Packs++;
            }
            else if (roll < 0.75f)
            {
                // 25% — single random card from active pool
                lastReward = RewardType.SingleCard;
                int maxTier = GetMaxTierForRound(currentRound);
                var pool = CardDatabase.GetActivePool(maxTier, 10);
                if (pool.Count > 0)
                {
                    lastRewardCard = pool[Random.Range(0, pool.Count)];
                    PlayerCollection.AddCard(lastRewardCard);
                }
            }
            else if (roll < 0.90f)
            {
                // 15% — gold reward (3-10)
                lastReward = RewardType.Gold;
                lastRewardGold = Random.Range(3, 11);
                GameState.Gold += lastRewardGold;
            }
            else
            {
                // 10% — nothing
                lastReward = RewardType.Nothing;
            }
        }

        public void ReturnToExploration()
        {
            bool playerWon = playerHP > 0;

            var em = FindFirstObjectByType<EncounterManager>();
            if (em != null) em.OnCombatComplete(playerWon);

            if (playerWon && lastReward == RewardType.Pack && ScreenManager.Instance.screenPackOpening != null)
            {
                // Got a pack — go to pack opening
                ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenPackOpening);
            }
            else
            {
                // No pack reward or lost — return to exploration
                ScreenManager.Instance.GoBack();
            }

            // Auto-save after combat
            SaveSystem.Save();

            Instance = null;
            Destroy(gameObject);
        }

        // --- Helpers ---

        // GetBoardPower and FindTarget removed — damage is now per-surviving-unit based on tier+rank

        private void ClearDeadCard(CardInstance[] board, CardInstance dead)
        {
            for (int i = 0; i < 5; i++)
            {
                if (board[i] == dead) { board[i] = null; break; }
            }
        }

        private void CleanBoard(CardInstance[] board)
        {
            for (int i = 0; i < 5; i++)
            {
                if (board[i] != null && board[i].CurrentHealth <= 0)
                    board[i] = null;
            }
        }

        private void UpdateUI()
        {
            var combatUI = FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
                combatUI.Refresh();
        }
    }
}
