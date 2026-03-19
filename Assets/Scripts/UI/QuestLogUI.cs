using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class QuestLogUI : MonoBehaviour
    {
        [Header("Layout")]
        public GameObject panel;
        public Transform contentContainer;
        public Button btnClose;
        public TextMeshProUGUI atlasProgress;

        void Start()
        {
            if (btnClose != null)
                btnClose.onClick.AddListener(Hide);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Refresh()
        {
            if (contentContainer == null) return;

            // Clear existing content
            foreach (Transform child in contentContainer)
                Object.Destroy(child.gameObject);

            string[] zones = { "Ancient Forest", "Volcanic Wastes", "Sky Peaks", "Coral Depths" };
            int zoneIndex = 0;

            foreach (string zone in zones)
            {
                bool isUnlocked = GameState.ZonesUnlocked[zoneIndex];
                BuildZoneSection(zone, isUnlocked, zoneIndex);
                zoneIndex++;
            }

            // Atlas progress at bottom
            if (atlasProgress != null)
            {
                int total = QuestManager.GetTotalOwnedCards();
                atlasProgress.text = $"Atlas Progress: {total}/100 cards";
            }
        }

        private void BuildZoneSection(string zoneName, bool isUnlocked, int zoneIndex)
        {
            var zoneElement = NpcData.GetZoneElement(zoneName);
            Color elementColor = GetElementColor(zoneElement);

            // Zone header
            var headerGO = new GameObject("Header_" + zoneName, typeof(RectTransform), typeof(LayoutElement));
            headerGO.transform.SetParent(contentContainer, false);
            headerGO.GetComponent<LayoutElement>().preferredHeight = 32f;

            var headerText = headerGO.AddComponent<TextMeshProUGUI>();
            if (!isUnlocked)
            {
                string unlockReq = GetUnlockRequirement(zoneIndex);
                headerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(elementColor)}>{zoneName}</color>  <color=#888888><size=14>LOCKED - {unlockReq}</size></color>";
            }
            else
            {
                int completed = QuestManager.GetCompletedQuestCount(zoneName);
                int total = QuestManager.GetTotalQuestCount(zoneName);
                headerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(elementColor)}>{zoneName}</color>  <color=#AAAAAA><size=14>{completed}/{total} Quests</size></color>";
            }
            headerText.fontSize = 22f;
            headerText.color = Color.white;
            headerText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            if (!isUnlocked) return;

            // Quest entries
            var quests = QuestManager.GetQuestsForZone(zoneName);
            foreach (var quest in quests)
            {
                BuildQuestEntry(quest, elementColor);
            }

            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(contentContainer, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = 12f;
        }

        private void BuildQuestEntry(ZoneQuest quest, Color elementColor)
        {
            var entryGO = new GameObject("Quest_" + quest.Title, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            entryGO.transform.SetParent(contentContainer, false);
            entryGO.GetComponent<LayoutElement>().preferredHeight = 70f;
            entryGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.8f);

            bool completed = quest.Status == QuestStatus.Completed;
            string icon = completed ? "<color=#FFD700>*</color>" : "<color=#888888>o</color>";
            float pct = quest.Target > 0 ? (float)quest.Progress / quest.Target : 0f;
            string progressStr = $"{quest.Progress}/{quest.Target}";

            // Build progress bar string
            int barLength = 20;
            int filled = Mathf.RoundToInt(pct * barLength);
            string bar = new string('|', filled) + new string('.', barLength - filled);
            string barColor = completed ? "#FFD700" : "#88AAFF";

            string statusLine = completed
                ? $"  {icon} {quest.Title}  <color=#FFD700>COMPLETE</color>"
                : $"  {icon} {quest.Title}  {progressStr}";

            string fullText = $"{statusLine}\n" +
                $"  <color={barColor}>[{bar}]</color> {Mathf.RoundToInt(pct * 100)}%\n" +
                $"  <color=#AAAAAA><size=12><i>\"{quest.NarrativeDescription}\"</i></size></color>";

            if (completed)
                fullText += $"\n  <color=#FFD700><size=12>Reward: {quest.RewardDescription}</size></color>";

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(entryGO.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = fullText;
            tmp.fontSize = 15f;
            tmp.color = completed ? new Color(0.8f, 0.8f, 0.7f) : Color.white;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.raycastTarget = false;
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.02f, 0f);
            trt.anchorMax = new Vector2(0.98f, 1f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        private string GetUnlockRequirement(int zoneIndex)
        {
            switch (zoneIndex)
            {
                case 1: return "Collect 10 Earth cards";
                case 2: return "Collect 10 Wind cards";
                case 3: return "Collect 10 Fire cards";
                default: return "";
            }
        }

        private Color GetElementColor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return ElementColors.Fire;
                case ElementType.Water: return ElementColors.Water;
                case ElementType.Earth: return ElementColors.Earth;
                case ElementType.Wind: return ElementColors.Wind;
                default: return Color.white;
            }
        }
    }
}
