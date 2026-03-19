using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Core
{
    [System.Serializable]
    public class SaveData
    {
        public int Gold;
        public int Packs;
        public int RerollTokens;
        public string CurrentZone;
        public bool[] ZonesUnlocked;
        public List<string> ZonesCompleted;
        public List<SerializedCard> Collection;
        public List<BossDefeatEntry> BossDefeats;
        public List<SerializedQuest> Quests;
        public bool HasSeenCurationTip;
        public bool HasSeenIntro;
        public bool FireActive;
        public bool WaterActive;
        public bool EarthActive;
        public bool WindActive;

        [System.Serializable]
        public class SerializedCard
        {
            public string CardName;
            public int Count;
            public bool IsActive;
            public bool IsStarter;
        }

        [System.Serializable]
        public class BossDefeatEntry
        {
            public string BossName;
            public int Count;
        }

        [System.Serializable]
        public class SerializedQuest
        {
            public string ZoneName;
            public int QuestType; // Cast from QuestType enum
            public int Status;    // Cast from QuestStatus enum
            public int Progress;
        }
    }

    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "arcane_atlas_save.json");

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static void Save()
        {
            var data = new SaveData
            {
                Gold = GameState.Gold,
                Packs = GameState.Packs,
                RerollTokens = GameState.RerollTokens,
                CurrentZone = GameState.CurrentZone,
                ZonesUnlocked = (bool[])GameState.ZonesUnlocked.Clone(),
                ZonesCompleted = new List<string>(),
                Collection = new List<SaveData.SerializedCard>()
            };

            foreach (var kvp in GameState.ZoneCompleted)
                if (kvp.Value) data.ZonesCompleted.Add(kvp.Key);

            foreach (var card in PlayerCollection.Cards)
            {
                data.Collection.Add(new SaveData.SerializedCard
                {
                    CardName = card.CardName,
                    Count = card.Count,
                    IsActive = card.IsActive,
                    IsStarter = card.IsStarter
                });
            }

            data.HasSeenCurationTip = PlayerCollection.HasSeenCurationTip;
            data.HasSeenIntro = GameState.HasSeenIntro;
            data.FireActive = PlayerCollection.FireActive;
            data.WaterActive = PlayerCollection.WaterActive;
            data.EarthActive = PlayerCollection.EarthActive;
            data.WindActive = PlayerCollection.WindActive;

            data.BossDefeats = new List<SaveData.BossDefeatEntry>();
            foreach (var kvp in GameState.BossDefeatCounts)
            {
                data.BossDefeats.Add(new SaveData.BossDefeatEntry
                {
                    BossName = kvp.Key,
                    Count = kvp.Value
                });
            }

            // Save quest progress
            data.Quests = new List<SaveData.SerializedQuest>();
            foreach (var quest in QuestManager.AllQuests)
            {
                data.Quests.Add(new SaveData.SerializedQuest
                {
                    ZoneName = quest.ZoneName,
                    QuestType = (int)quest.Type,
                    Status = (int)quest.Status,
                    Progress = quest.Progress
                });
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] Saved to {SavePath}");
        }

        public static bool Load()
        {
            if (!HasSave()) return false;

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);

            GameState.Gold = data.Gold;
            GameState.Packs = data.Packs;
            GameState.RerollTokens = data.RerollTokens;
            GameState.CurrentZone = data.CurrentZone;

            if (data.ZonesUnlocked != null)
                GameState.ZonesUnlocked = data.ZonesUnlocked;

            GameState.ZoneCompleted.Clear();
            if (data.ZonesCompleted != null)
                foreach (var zone in data.ZonesCompleted)
                    GameState.ZoneCompleted[zone] = true;

            PlayerCollection.Cards.Clear();
            if (data.Collection != null)
            {
                foreach (var card in data.Collection)
                {
                    PlayerCollection.Cards.Add(new OwnedCard(card.CardName, card.IsStarter)
                    {
                        Count = card.Count,
                        IsActive = card.IsActive
                    });
                }
            }

            PlayerCollection.HasSeenCurationTip = data.HasSeenCurationTip;
            GameState.HasSeenIntro = data.HasSeenIntro;

            // Element filters: old saves lack these fields, so all-false means "legacy save, default to all-true"
            if (!data.FireActive && !data.WaterActive && !data.EarthActive && !data.WindActive)
            {
                PlayerCollection.FireActive = true;
                PlayerCollection.WaterActive = true;
                PlayerCollection.EarthActive = true;
                PlayerCollection.WindActive = true;
            }
            else
            {
                PlayerCollection.FireActive = data.FireActive;
                PlayerCollection.WaterActive = data.WaterActive;
                PlayerCollection.EarthActive = data.EarthActive;
                PlayerCollection.WindActive = data.WindActive;
            }

            GameState.BossDefeatCounts.Clear();
            if (data.BossDefeats != null)
            {
                foreach (var entry in data.BossDefeats)
                    GameState.BossDefeatCounts[entry.BossName] = entry.Count;
            }

            // Restore quest progress
            QuestManager.Initialize(); // Create fresh quests first
            if (data.Quests != null)
            {
                foreach (var sq in data.Quests)
                {
                    var quest = QuestManager.AllQuests.Find(q =>
                        q.ZoneName == sq.ZoneName && (int)q.Type == sq.QuestType);
                    if (quest != null)
                    {
                        quest.Status = (QuestStatus)sq.Status;
                        quest.Progress = sq.Progress;
                    }
                }
            }

            Debug.Log("[SaveSystem] Game loaded");
            return true;
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
