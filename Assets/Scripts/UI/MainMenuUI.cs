using System.Linq;
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
        public Button btnTestMode;
        public Button btnOpenPack;
        public Button btnQuickDuel;

        [Header("Parallax Quadrants")]
        public ParallaxBackgroundController parallaxFire;
        public ParallaxBackgroundController parallaxWind;
        public ParallaxBackgroundController parallaxWater;
        public ParallaxBackgroundController parallaxEarth;

        void Start()
        {
            WireButtons();
        }

        void OnEnable()
        {
            if (btnContinue != null)
                btnContinue.interactable = SaveSystem.HasSave();

            // Start parallax backgrounds — no dim overlay on the title screen
            LoadParallax(parallaxFire,  "VolcanicWastes");
            LoadParallax(parallaxWind,  "SkyPeaks");
            LoadParallax(parallaxWater, "CoralDepths");
            LoadParallax(parallaxEarth, "AncientForest");
        }

        void OnDisable()
        {
            parallaxFire?.SetPlaying(false);
            parallaxWind?.SetPlaying(false);
            parallaxWater?.SetPlaying(false);
            parallaxEarth?.SetPlaying(false);
        }

        private void LoadParallax(ParallaxBackgroundController ctrl, string biomeName)
        {
            if (ctrl == null) return;
            ctrl.LoadByBiomeName(biomeName);
            ctrl.SetDim(0f);
            ctrl.SetPlaying(true);
        }

        public void WireButtons()
        {
            var sm = ScreenManager.Instance;
            btnNewJourney.onClick.AddListener(OnNewJourney);
            btnContinue.onClick.AddListener(OnContinue);
            btnCollection.onClick.AddListener(() => sm.ShowScreen(sm.screenCollection));
            btnOptions.onClick.AddListener(() => sm.ShowScreen(sm.screenSettings));

            if (btnTestMode != null)
                btnTestMode.onClick.AddListener(OnTestMode);
            if (btnOpenPack != null)
                btnOpenPack.onClick.AddListener(OnOpenPack);
            if (btnQuickDuel != null)
                btnQuickDuel.onClick.AddListener(OnQuickDuel);

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

            ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenIntro, showHUD: false);
        }

        private void OnTestMode()
        {
            SaveSystem.DeleteSave();
            TestMode.Activate();
            ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenTown);
        }

        private void OnContinue()
        {
            if (SaveSystem.Load())
            {
                ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenTown);
            }
        }

        /// <summary>
        /// Opens a booster pack from the title screen.
        /// If a save exists, loads it first so cards go to that collection.
        /// If no save, initializes a starter collection so the pack cards have a home.
        /// After pack opening, saves the game.
        /// </summary>
        private void OnOpenPack()
        {
            // Load existing save if available, otherwise create minimal state
            if (SaveSystem.HasSave())
            {
                SaveSystem.Load();
            }
            else
            {
                // No save — set up minimal state for pack cards
                if (PlayerCollection.Cards.Count == 0)
                    PlayerCollection.InitializeStarterCollection();
                QuestManager.Initialize();
            }

            // Add a free pack and open the pack screen
            GameState.Packs = Mathf.Max(GameState.Packs, 1);

            var sm = ScreenManager.Instance;
            if (sm.screenPackOpening != null)
            {
                sm.ShowScreen(sm.screenPackOpening, showHUD: false);
            }
            else
            {
                Debug.LogWarning("[MainMenu] Pack opening screen not found!");
            }
        }

        /// <summary>
        /// Starts a quick duel against a random NPC from unlocked zones.
        /// Loads the save if available, picks a random zone and NPC,
        /// sets up the encounter, and jumps to combat.
        /// </summary>
        private void OnQuickDuel()
        {
            // Load existing save if available
            if (SaveSystem.HasSave())
            {
                SaveSystem.Load();
            }
            else
            {
                // No save — initialize minimal state
                if (PlayerCollection.Cards.Count == 0)
                    PlayerCollection.InitializeStarterCollection();
                GameState.Gold = Mathf.Max(GameState.Gold, 40);
                QuestManager.Initialize();
            }

            // Pick a random unlocked zone
            var zones = ZoneData.GetAllZones();
            var unlocked = zones.Where((z, i) => i < GameState.ZonesUnlocked.Length && GameState.ZonesUnlocked[i]).ToArray();
            if (unlocked.Length == 0)
            {
                Debug.LogWarning("[MainMenu] No zones unlocked for quick duel!");
                return;
            }

            var zone = unlocked[Random.Range(0, unlocked.Length)];
            GameState.CurrentZone = zone.Name;

            // Get NPCs for this zone and pick a random non-boss
            var zoneNpcs = NpcData.GetNpcsForZone(zone.Name);
            var eligible = zoneNpcs.Where(n => !n.IsBoss).ToArray();
            if (eligible.Length == 0)
            {
                Debug.LogWarning($"[MainMenu] No NPCs found for zone '{zone.Name}'!");
                return;
            }

            var npc = eligible[Random.Range(0, eligible.Length)];

            // Clone and set up the opponent
            var opponent = new NpcData
            {
                Name = npc.Name,
                Element = npc.Element,
                Difficulty = npc.Difficulty,
                DialogueLines = npc.DialogueLines,
                IsBoss = npc.IsBoss,
                BossCardName = npc.BossCardName,
            };
            opponent.GenerateOpponentPool(zone.Difficulty);

            // Set as current opponent and jump directly to combat
            GameState.CurrentOpponent = opponent;

            var sm = ScreenManager.Instance;
            sm.ShowScreen(sm.screenCombat, showHUD: false);
            Debug.Log($"[MainMenu] Quick Duel: {opponent.Name} ({zone.Name}, {zone.Element})");
        }
    }
}
