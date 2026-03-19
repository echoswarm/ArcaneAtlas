using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    public static class QuestManager
    {
        public static List<ZoneQuest> AllQuests = new List<ZoneQuest>();

        // Celebration callback — set by UI systems to show zone unlock banners
        public static System.Action<string> OnZoneUnlocked;
        public static System.Action<ZoneQuest> OnQuestCompleted;

        public static void Initialize()
        {
            AllQuests = new List<ZoneQuest>();

            // Ancient Forest (Earth) — unlocks Volcanic Wastes
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Ancient Forest", Type = QuestType.CollectCards, Status = QuestStatus.Active,
                Title = "Collect 10 Earth Cards", Target = 10, Progress = 0,
                NarrativeDescription = "The Atlas demands tribute \u2014 10 guardians of the Forest must be recorded in its pages before the next chapter reveals itself.",
                RewardDescription = "Unlocks Volcanic Wastes",
                RewardCardName = null, RewardGold = 0
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Ancient Forest", Type = QuestType.DefeatAllNPCs, Status = QuestStatus.Active,
                Title = "Defeat All Forest NPCs", Target = 6, Progress = 0,
                NarrativeDescription = "The Forest's challengers stand between you and its deepest secrets. Defeat them all in a single expedition to prove your worth.",
                RewardDescription = "Unique Earth card",
                RewardCardName = null, RewardGold = 25
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Ancient Forest", Type = QuestType.DefeatBoss, Status = QuestStatus.Active,
                Title = "Defeat Ancient Treant", Target = 1, Progress = 0,
                NarrativeDescription = "The Ancient Treant guards the cornerstone of the Forest chapter. Only by defeating it will the Atlas surrender its most powerful secret.",
                RewardDescription = "Heart of the Forest (Legendary)",
                RewardCardName = "Heart of the Forest", RewardGold = 0
            });

            // Volcanic Wastes (Fire) — unlocks Sky Peaks
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Volcanic Wastes", Type = QuestType.CollectCards, Status = QuestStatus.Active,
                Title = "Collect 10 Fire Cards", Target = 10, Progress = 0,
                NarrativeDescription = "The Atlas burns warm. Its ember-script demands proof \u2014 10 creatures of flame catalogued before the winds reveal themselves.",
                RewardDescription = "Unlocks Sky Peaks",
                RewardCardName = null, RewardGold = 0
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Volcanic Wastes", Type = QuestType.DefeatAllNPCs, Status = QuestStatus.Active,
                Title = "Defeat All Volcanic NPCs", Target = 6, Progress = 0,
                NarrativeDescription = "The Wastes are unforgiving. Only those who conquer every challenger in a single expedition earn the right to claim its hidden blade.",
                RewardDescription = "Unique Fire card",
                RewardCardName = null, RewardGold = 25
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Volcanic Wastes", Type = QuestType.DefeatBoss, Status = QuestStatus.Active,
                Title = "Defeat Inferno Drake", Target = 1, Progress = 0,
                NarrativeDescription = "The Inferno Drake's fire has burned for a thousand years. Extinguish it, and claim the core that powers the Wastes.",
                RewardDescription = "Volcanic Core (Legendary)",
                RewardCardName = "Volcanic Core", RewardGold = 0
            });

            // Sky Peaks (Wind) — unlocks Coral Depths
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Sky Peaks", Type = QuestType.CollectCards, Status = QuestStatus.Active,
                Title = "Collect 10 Wind Cards", Target = 10, Progress = 0,
                NarrativeDescription = "The Atlas's pages flutter restlessly. 10 spirits of wind must be bound to its chapters before the depths below reveal themselves.",
                RewardDescription = "Unlocks Coral Depths",
                RewardCardName = null, RewardGold = 0
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Sky Peaks", Type = QuestType.DefeatAllNPCs, Status = QuestStatus.Active,
                Title = "Defeat All Sky Peaks NPCs", Target = 6, Progress = 0,
                NarrativeDescription = "The peaks test endurance above all else. Clear every challenger in one expedition to earn what the thin air conceals.",
                RewardDescription = "Unique Wind card",
                RewardCardName = null, RewardGold = 25
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Sky Peaks", Type = QuestType.DefeatBoss, Status = QuestStatus.Active,
                Title = "Defeat Tempest Dragon", Target = 1, Progress = 0,
                NarrativeDescription = "The Tempest Dragon sees all from above. Bring it down, and the storm's eye becomes yours to command.",
                RewardDescription = "Eye of the Storm (Legendary)",
                RewardCardName = "Eye of the Storm", RewardGold = 0
            });

            // Coral Depths (Water) — final zone
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Coral Depths", Type = QuestType.CollectCards, Status = QuestStatus.Active,
                Title = "Collect 10 Water Cards", Target = 10, Progress = 0,
                NarrativeDescription = "The final chapter. 10 creatures of the deep complete the Atlas's map of the known world. But the journey is not yet over.",
                RewardDescription = "All zones complete",
                RewardCardName = null, RewardGold = 0
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Coral Depths", Type = QuestType.DefeatAllNPCs, Status = QuestStatus.Active,
                Title = "Defeat All Coral NPCs", Target = 6, Progress = 0,
                NarrativeDescription = "The depths hide their treasures well. Only a complete expedition through every chamber will reveal the pearl at the bottom.",
                RewardDescription = "Unique Water card",
                RewardCardName = null, RewardGold = 25
            });
            AllQuests.Add(new ZoneQuest
            {
                ZoneName = "Coral Depths", Type = QuestType.DefeatBoss, Status = QuestStatus.Active,
                Title = "Defeat Leviathan", Target = 1, Progress = 0,
                NarrativeDescription = "The Leviathan has guarded these waters since before the Atlas was first imagined. Its trident is the key to the deepest truths.",
                RewardDescription = "Abyssal Trident (Legendary)",
                RewardCardName = "Abyssal Trident", RewardGold = 0
            });

            // Sync collection progress from existing cards
            RefreshCollectionProgress();
        }

        public static List<ZoneQuest> GetQuestsForZone(string zone)
        {
            return AllQuests.Where(q => q.ZoneName == zone).ToList();
        }

        public static int GetCompletedQuestCount(string zone)
        {
            return AllQuests.Count(q => q.ZoneName == zone && q.Status == QuestStatus.Completed);
        }

        public static int GetTotalQuestCount(string zone)
        {
            return AllQuests.Count(q => q.ZoneName == zone);
        }

        /// <summary>
        /// Called when a card is added to the collection (pack opening, treasure, reward).
        /// Updates collection quests and checks zone unlock conditions.
        /// </summary>
        public static void OnCardAdded(CardData card)
        {
            if (card == null) return;
            RefreshCollectionProgress();
        }

        /// <summary>
        /// Recalculates all CollectCards quest progress from PlayerCollection.
        /// </summary>
        public static void RefreshCollectionProgress()
        {
            foreach (var quest in AllQuests)
            {
                if (quest.Type != QuestType.CollectCards || quest.Status == QuestStatus.Completed) continue;

                var zoneElement = NpcData.GetZoneElement(quest.ZoneName);
                int count = CountOwnedByElement(zoneElement);
                quest.Progress = count;

                if (quest.Progress >= quest.Target)
                    CompleteQuest(quest);
            }
        }

        /// <summary>
        /// Called when an NPC is defeated in exploration.
        /// Tracks run-based NPC defeat quests.
        /// </summary>
        public static void OnNPCDefeated(string zone, string npcName)
        {
            foreach (var quest in AllQuests)
            {
                if (quest.ZoneName != zone || quest.Type != QuestType.DefeatAllNPCs) continue;
                if (quest.Status == QuestStatus.Completed) continue;

                quest.RunNPCsDefeated++;
                quest.Progress = Mathf.Max(quest.Progress, quest.RunNPCsDefeated);

                if (quest.RunNPCsDefeated >= quest.Target)
                    CompleteQuest(quest);
            }
        }

        /// <summary>
        /// Called when a boss is defeated.
        /// </summary>
        public static void OnBossDefeated(string zone)
        {
            foreach (var quest in AllQuests)
            {
                if (quest.ZoneName != zone || quest.Type != QuestType.DefeatBoss) continue;
                if (quest.Status == QuestStatus.Completed) continue;

                quest.Progress = 1;
                CompleteQuest(quest);
            }
        }

        /// <summary>
        /// Resets run-based NPC defeat tracking when entering a zone.
        /// </summary>
        public static void OnZoneEntered(string zone)
        {
            foreach (var quest in AllQuests)
            {
                if (quest.ZoneName != zone || quest.Type != QuestType.DefeatAllNPCs) continue;
                quest.RunNPCsDefeated = 0;
            }
        }

        /// <summary>
        /// Completes a quest and grants its rewards.
        /// </summary>
        public static void CompleteQuest(ZoneQuest quest)
        {
            if (quest.Status == QuestStatus.Completed) return;
            quest.Status = QuestStatus.Completed;

            // Grant reward card
            if (!string.IsNullOrEmpty(quest.RewardCardName))
            {
                var allCards = CardDatabase.GetAllCards();
                var card = allCards.Find(c => c.CardName == quest.RewardCardName);
                if (card != null)
                    PlayerCollection.AddCard(card);
            }

            // Grant gold
            if (quest.RewardGold > 0)
                GameState.Gold += quest.RewardGold;

            // Check zone unlock (collection quests unlock next zones)
            if (quest.Type == QuestType.CollectCards)
                CheckZoneUnlocks(quest);

            OnQuestCompleted?.Invoke(quest);
            Debug.Log($"[QuestManager] Quest completed: {quest.Title}");
        }

        /// <summary>
        /// Zone unlock chain:
        /// 10 Earth cards → Volcanic Wastes
        /// 10 Fire cards → Sky Peaks
        /// 10 Wind cards → Coral Depths
        /// </summary>
        private static void CheckZoneUnlocks(ZoneQuest quest)
        {
            string unlockedZone = null;
            int zoneIndex = -1;

            switch (quest.ZoneName)
            {
                case "Ancient Forest":
                    unlockedZone = "Volcanic Wastes";
                    zoneIndex = 1;
                    break;
                case "Volcanic Wastes":
                    unlockedZone = "Sky Peaks";
                    zoneIndex = 3;
                    break;
                case "Sky Peaks":
                    unlockedZone = "Coral Depths";
                    zoneIndex = 2;
                    break;
            }

            if (unlockedZone != null && zoneIndex >= 0 && !GameState.ZonesUnlocked[zoneIndex])
            {
                GameState.ZonesUnlocked[zoneIndex] = true;
                OnZoneUnlocked?.Invoke(unlockedZone);
                Debug.Log($"[QuestManager] Zone unlocked: {unlockedZone}");
            }
        }

        /// <summary>
        /// Total unique cards owned across all elements.
        /// </summary>
        public static int GetTotalOwnedCards()
        {
            return PlayerCollection.Cards.Count;
        }

        private static int CountOwnedByElement(ElementType element)
        {
            int count = 0;
            var all = CardDatabase.GetAllCards();
            foreach (var owned in PlayerCollection.Cards)
            {
                var data = all.Find(c => c.CardName == owned.CardName);
                if (data != null && data.Element == element)
                    count++;
            }
            return count;
        }
    }
}
