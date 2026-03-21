using System.Collections.Generic;

namespace ArcaneAtlas.Data
{
    /// <summary>
    /// Test mode utilities. Unlocks all zones, cards, gold, and quest data
    /// for rapid testing without playing through the full game.
    /// </summary>
    public static class TestMode
    {
        public static bool IsActive { get; private set; } = false;

        /// <summary>
        /// Activates test mode: unlocks everything, maxes gold/packs, grants all cards.
        /// </summary>
        public static void Activate()
        {
            IsActive = true;

            // Max resources
            GameState.Gold = 9999;
            GameState.Packs = 99;
            GameState.RerollTokens = 99;
            GameState.CurrentZone = "Town";
            GameState.HasSeenIntro = true;

            // Unlock all zones
            GameState.ZonesUnlocked = new bool[] { true, true, true, true };
            GameState.ZoneCompleted.Clear();

            // Reset boss pity counters
            GameState.BossDefeatCounts.Clear();

            // Grant all cards (3 copies each for merge testing)
            PlayerCollection.Cards.Clear();
            var allCards = CardDatabase.GetAllCards();
            foreach (var card in allCards)
            {
                PlayerCollection.AddCard(card);
                PlayerCollection.AddCard(card);
                PlayerCollection.AddCard(card);
            }

            // All elements active
            PlayerCollection.FireActive = true;
            PlayerCollection.WaterActive = true;
            PlayerCollection.EarthActive = true;
            PlayerCollection.WindActive = true;
            PlayerCollection.HasSeenCurationTip = true;

            // Initialize quests
            QuestManager.Initialize();

            UnityEngine.Debug.Log("[TestMode] Activated — all zones, cards, and resources unlocked.");
        }

        /// <summary>
        /// Resets specific state for targeted testing.
        /// </summary>
        public static void ResetZoneProgress()
        {
            GameState.ZoneCompleted.Clear();
            GameState.BossDefeatCounts.Clear();
            QuestManager.Initialize();
            UnityEngine.Debug.Log("[TestMode] Zone progress reset.");
        }

        public static void SetGold(int amount)
        {
            GameState.Gold = amount;
            UnityEngine.Debug.Log($"[TestMode] Gold set to {amount}.");
        }

        public static void UnlockZone(int index)
        {
            if (index >= 0 && index < GameState.ZonesUnlocked.Length)
            {
                GameState.ZonesUnlocked[index] = true;
                UnityEngine.Debug.Log($"[TestMode] Zone {index} unlocked.");
            }
        }
    }
}
