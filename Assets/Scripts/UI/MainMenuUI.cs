using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Button btnNewJourney;
        public Button btnContinue;
        public Button btnCollection;
        public Button btnOptions;

        void Start()
        {
            WireButtons();
        }

        void OnEnable()
        {
            // Update Continue button availability each time menu is shown
            if (btnContinue != null)
                btnContinue.interactable = SaveSystem.HasSave();
        }

        public void WireButtons()
        {
            var sm = ScreenManager.Instance;
            btnNewJourney.onClick.AddListener(OnNewJourney);
            btnContinue.onClick.AddListener(OnContinue);
            btnCollection.onClick.AddListener(() => sm.ShowScreen(sm.screenCollection));
            btnOptions.onClick.AddListener(() => sm.ShowScreen(sm.screenSettings));

            btnContinue.interactable = SaveSystem.HasSave();
        }

        private void OnNewJourney()
        {
            SaveSystem.DeleteSave();
            GameState.Gold = 40;
            GameState.Packs = 3;
            GameState.RerollTokens = 0;
            GameState.CurrentZone = "Town";
            GameState.ZonesUnlocked = new bool[] { true, false, false, false };
            GameState.ZoneCompleted.Clear();
            GameState.BossDefeatCounts.Clear();
            PlayerCollection.Cards.Clear();
            PlayerCollection.HasSeenCurationTip = false;
            PlayerCollection.FireActive = true;
            PlayerCollection.WaterActive = true;
            PlayerCollection.EarthActive = true;
            PlayerCollection.WindActive = true;
            PlayerCollection.InitializeStarterCollection();
            GameState.HasSeenIntro = false;
            QuestManager.Initialize();

            // Show intro sequence for new journeys
            ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenIntro, showHUD: false);
        }

        private void OnContinue()
        {
            if (SaveSystem.Load())
            {
                ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenTown);
            }
        }
    }
}
