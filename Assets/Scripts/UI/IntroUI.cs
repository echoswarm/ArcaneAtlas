using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class IntroUI : MonoBehaviour
    {
        [Header("Intro Sequence")]
        public Image sceneImage;
        public TextMeshProUGUI sceneText;
        public TextMeshProUGUI imageDescText;
        public Button btnContinue;
        public Button btnSkip;

        [Header("Tutorial Choice")]
        public GameObject tutorialChoicePanel;
        public Button btnTutorialYes;
        public Button btnTutorialNo;

        [Header("Tutorial")]
        public GameObject tutorialPanel;
        public Image scholarPortrait;
        public TextMeshProUGUI scholarName;
        public TextMeshProUGUI scholarText;
        public Button btnTutorialNext;

        private int currentScene = 0;
        private int currentTutorialLine = 0;
        private bool inTutorial = false;

        void Start()
        {
            btnContinue.onClick.AddListener(AdvanceScene);
            btnSkip.onClick.AddListener(FinishIntro);
            btnTutorialYes.onClick.AddListener(StartTutorial);
            btnTutorialNo.onClick.AddListener(GoToTown);
            btnTutorialNext.onClick.AddListener(AdvanceTutorial);

            tutorialChoicePanel.SetActive(false);
            tutorialPanel.SetActive(false);
        }

        void OnEnable()
        {
            currentScene = 0;
            inTutorial = false;
            tutorialChoicePanel.SetActive(false);
            tutorialPanel.SetActive(false);

            // Show first scene
            ShowIntroElements(true);
            ShowScene(0);
        }

        private void ShowIntroElements(bool show)
        {
            sceneImage.gameObject.SetActive(show);
            sceneText.gameObject.SetActive(show);
            imageDescText.gameObject.SetActive(show);
            btnContinue.gameObject.SetActive(show);
            btnSkip.gameObject.SetActive(show);
        }

        private void ShowScene(int index)
        {
            if (index >= IntroData.Scenes.Length)
            {
                FinishIntro();
                return;
            }

            currentScene = index;
            var scene = IntroData.Scenes[index];

            // Placeholder: tint the scene image based on scene mood
            sceneImage.color = GetSceneColor(index);
            imageDescText.text = scene.ImageDescription;
            sceneText.text = scene.Text;
        }

        private void AdvanceScene()
        {
            ShowScene(currentScene + 1);
        }

        private void FinishIntro()
        {
            ShowIntroElements(false);
            tutorialChoicePanel.SetActive(true);
        }

        private void StartTutorial()
        {
            tutorialChoicePanel.SetActive(false);
            tutorialPanel.SetActive(true);
            inTutorial = true;
            currentTutorialLine = 0;
            ShowTutorialLine(0);
        }

        private void ShowTutorialLine(int index)
        {
            if (index >= IntroData.TutorialLines.Length)
            {
                GoToTown();
                return;
            }

            currentTutorialLine = index;
            scholarText.text = IntroData.TutorialLines[index];
        }

        private void AdvanceTutorial()
        {
            ShowTutorialLine(currentTutorialLine + 1);
        }

        private void GoToTown()
        {
            GameState.HasSeenIntro = true;
            ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenTown);
        }

        /// <summary>
        /// Returns a placeholder mood color for each intro scene.
        /// Will be replaced with actual art later.
        /// </summary>
        private Color GetSceneColor(int index)
        {
            switch (index)
            {
                case 0: return new Color(0.15f, 0.12f, 0.08f); // Warm candlelight
                case 1: return new Color(0.2f, 0.25f, 0.15f);  // Path, greenery
                case 2: return new Color(0.12f, 0.10f, 0.15f);  // Lonely window, cool
                case 3: return new Color(0.10f, 0.15f, 0.20f);  // Stream, blue
                case 4: return new Color(0.20f, 0.08f, 0.08f);  // Heartbreak, red
                case 5: return new Color(0.12f, 0.12f, 0.15f);  // Walking away, gray
                case 6: return new Color(0.18f, 0.15f, 0.10f);  // Doorway, neutral
                case 7: return new Color(0.20f, 0.18f, 0.10f);  // Dusty room, golden
                case 8: return new Color(0.10f, 0.10f, 0.20f);  // Atlas glowing, purple
                case 9: return new Color(0.15f, 0.20f, 0.25f);  // Stepping out, bright
                default: return new Color(0.1f, 0.1f, 0.1f);
            }
        }
    }
}
