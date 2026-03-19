using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class ExplorationHUD : MonoBehaviour
    {
        public Button btnBack;
        public Button btnQuests;
        public TextMeshProUGUI roomLabel;
        public TextMeshProUGUI zoneCardCount;
        public MinimapUI minimap;
        public QuestLogUI questLog;

        void Start()
        {
            btnBack.onClick.AddListener(() => ExplorationManager.Instance.ExitExploration());
            if (btnQuests != null)
                btnQuests.onClick.AddListener(ToggleQuestLog);
        }

        private void ToggleQuestLog()
        {
            if (questLog == null) return;
            if (questLog.panel != null && questLog.panel.activeSelf)
                questLog.Hide();
            else
                questLog.Show();
        }

        public void SetRoomLabel(string text)
        {
            if (roomLabel != null) roomLabel.text = text;
        }

        public void UpdateZoneCardCount()
        {
            if (zoneCardCount == null) return;

            string zone = GameState.CurrentZone;
            var zoneElement = NpcData.GetZoneElement(zone);
            int owned = CountOwnedByElement(zoneElement);
            int total = CountTotalByElement(zoneElement);

            zoneCardCount.text = $"{zone}\nCards: {owned}/{total}";
        }

        private int CountOwnedByElement(ElementType element)
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

        private int CountTotalByElement(ElementType element)
        {
            int count = 0;
            foreach (var card in CardDatabase.GetAllCards())
            {
                if (card.Element == element) count++;
            }
            return count;
        }
    }
}
