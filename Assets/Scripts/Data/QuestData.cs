namespace ArcaneAtlas.Data
{
    public enum QuestType { CollectCards, DefeatAllNPCs, DefeatBoss }
    public enum QuestStatus { Active, Completed }

    public class ZoneQuest
    {
        public string ZoneName;
        public QuestType Type;
        public QuestStatus Status;
        public string Title;
        public string NarrativeDescription;
        public string RewardDescription;
        public string RewardCardName;   // null if reward is not a card
        public int RewardGold;          // 0 if no gold reward
        public int Progress;
        public int Target;

        // For DefeatAllNPCs: tracks within a single run
        public int RunNPCsDefeated;
    }
}
