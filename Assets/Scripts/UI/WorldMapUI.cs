using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class WorldMapUI : MonoBehaviour
    {
        [Header("Zone Buttons")]
        public Button btnAncientForest;
        public Button btnVolcanicWastes;
        public Button btnCoralDepths;
        public Button btnSkyPeaks;

        [Header("Info Panel")]
        public TextMeshProUGUI infoName;
        public TextMeshProUGUI infoDescription;
        public TextMeshProUGUI infoDifficulty;
        public TextMeshProUGUI infoElement;
        public TextMeshProUGUI infoCards;
        public TextMeshProUGUI infoQuests;
        public GameObject infoPanel;

        [Header("Navigation")]
        public Button btnBack;
        public Button btnEnterZone;

        [Header("Atlas Progress")]
        public TextMeshProUGUI atlasProgressText;

        [Header("Zone Labels")]
        public TextMeshProUGUI[] zoneLabels;

        private ZoneData selectedZone;
        private ZoneData[] allZones;

        void Start()
        {
            WireButtons();
        }

        void OnEnable()
        {
            var hud = FindFirstObjectByType<HUD>();
            if (hud != null) hud.SetZone("World Map");

            allZones = ZoneData.GetAllZones();

            // Sync zone unlock state from GameState
            for (int i = 0; i < allZones.Length && i < GameState.ZonesUnlocked.Length; i++)
                allZones[i].IsUnlocked = GameState.ZonesUnlocked[i];

            RefreshZoneButtons();
            UpdateAtlasProgress();

            if (infoPanel != null) infoPanel.SetActive(false);
            if (btnEnterZone != null) btnEnterZone.gameObject.SetActive(false);
        }

        public void WireButtons()
        {
            btnBack.onClick.AddListener(() => ScreenManager.Instance.GoBack());

            btnAncientForest.onClick.AddListener(() => SelectZone(0));
            btnVolcanicWastes.onClick.AddListener(() => SelectZone(1));
            btnCoralDepths.onClick.AddListener(() => SelectZone(2));
            btnSkyPeaks.onClick.AddListener(() => SelectZone(3));

            btnEnterZone.onClick.AddListener(() => EnterSelectedZone());
        }

        private void RefreshZoneButtons()
        {
            Button[] buttons = { btnAncientForest, btnVolcanicWastes, btnCoralDepths, btnSkyPeaks };
            for (int i = 0; i < buttons.Length && i < allZones.Length; i++)
            {
                buttons[i].interactable = allZones[i].IsUnlocked;
            }

            // Update zone labels with card/quest counts
            if (zoneLabels != null)
            {
                for (int i = 0; i < zoneLabels.Length && i < allZones.Length; i++)
                {
                    if (zoneLabels[i] == null) continue;
                    var zone = allZones[i];

                    if (zone.IsUnlocked)
                    {
                        int cards = CountOwnedByElement(zone.Element);
                        int totalCards = CountTotalByElement(zone.Element);
                        int quests = QuestManager.GetCompletedQuestCount(zone.Name);
                        zoneLabels[i].text = $"{zone.Name}\n<size=11>Cards: {cards}/{totalCards}  Quests: {quests}/3</size>";
                    }
                    else
                    {
                        string req = GetUnlockRequirement(i);
                        zoneLabels[i].text = $"{zone.Name}\n<size=11><color=#888>{req}</color></size>";
                    }
                }
            }
        }

        private void SelectZone(int index)
        {
            if (index < 0 || index >= allZones.Length) return;

            selectedZone = allZones[index];

            if (infoPanel != null) infoPanel.SetActive(true);
            if (infoName != null) infoName.text = selectedZone.Name;
            if (infoDescription != null) infoDescription.text = selectedZone.Description;
            if (infoDifficulty != null)
            {
                string filled = new string('>', selectedZone.Difficulty);
                string empty = new string('-', 4 - selectedZone.Difficulty);
                infoDifficulty.text = "Difficulty: " + filled + empty;
            }
            if (infoElement != null) infoElement.text = "Element: " + selectedZone.Element.ToString();

            if (infoCards != null)
            {
                int cards = CountOwnedByElement(selectedZone.Element);
                int total = CountTotalByElement(selectedZone.Element);
                infoCards.text = $"Cards: {cards}/{total}";
            }
            if (infoQuests != null)
            {
                int completed = QuestManager.GetCompletedQuestCount(selectedZone.Name);
                infoQuests.text = $"Quests: {completed}/3";
            }

            if (btnEnterZone != null)
            {
                btnEnterZone.gameObject.SetActive(selectedZone.IsUnlocked);
            }
        }

        private void EnterSelectedZone()
        {
            if (selectedZone == null || !selectedZone.IsUnlocked) return;

            GameState.CurrentZone = selectedZone.Name;

            ScreenManager.Instance.ShowScreen(
                ScreenManager.Instance.screenExploration);

            if (ExplorationManager.Instance != null)
                ExplorationManager.Instance.EnterExploration();
        }

        private void UpdateAtlasProgress()
        {
            if (atlasProgressText != null)
            {
                int total = QuestManager.GetTotalOwnedCards();
                atlasProgressText.text = $"Atlas: {total}/100";
            }
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
                if (card.Element == element) count++;
            return count;
        }
    }
}
