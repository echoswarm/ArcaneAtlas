using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ArcaneAtlas.Core
{
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Screens")]
        public GameObject screenMainMenu;
        public GameObject screenTown;
        public GameObject screenWorldMap;
        public GameObject screenExploration;
        public GameObject screenCombat;
        public GameObject screenCollection;
        public GameObject screenTownShop;
        public GameObject screenSettings;
        public GameObject screenPackOpening;
        public GameObject screenIntro;

        [Header("Overlays")]
        public GameObject hudPersistent;

        private GameObject currentScreen;
        private Stack<GameObject> screenHistory = new Stack<GameObject>();

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            DeactivateAll();
            ShowScreen(screenMainMenu, showHUD: false);
        }

        public void ShowScreen(GameObject screen, bool showHUD = true)
        {
            if (currentScreen != null)
            {
                screenHistory.Push(currentScreen);
                currentScreen.SetActive(false);
            }

            screen.SetActive(true);
            currentScreen = screen;
            hudPersistent.SetActive(showHUD);
        }

        public void GoBack()
        {
            if (screenHistory.Count == 0) return;

            currentScreen.SetActive(false);
            currentScreen = screenHistory.Pop();
            currentScreen.SetActive(true);

            hudPersistent.SetActive(currentScreen != screenMainMenu);
        }

        private void DeactivateAll()
        {
            screenMainMenu?.SetActive(false);
            screenTown?.SetActive(false);
            screenWorldMap?.SetActive(false);
            screenExploration?.SetActive(false);
            screenCombat?.SetActive(false);
            screenCollection?.SetActive(false);
            screenTownShop?.SetActive(false);
            screenSettings?.SetActive(false);
            screenPackOpening?.SetActive(false);
            screenIntro?.SetActive(false);
            hudPersistent?.SetActive(false);
        }
    }
}
