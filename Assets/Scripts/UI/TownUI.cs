using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Core;

namespace ArcaneAtlas.UI
{
    public class TownUI : MonoBehaviour
    {
        public Button btnWorldMap;
        public Button btnCollection;
        public Button btnShop;
        public Button btnSettings;

        void Start()
        {
            WireButtons();
        }

        void OnEnable()
        {
            var hud = FindFirstObjectByType<HUD>();
            if (hud != null) hud.SetZone("Town");
        }

        public void WireButtons()
        {
            var sm = ScreenManager.Instance;
            btnWorldMap.onClick.AddListener(() => sm.ShowScreen(sm.screenWorldMap));
            btnCollection.onClick.AddListener(() => sm.ShowScreen(sm.screenCollection));
            btnShop.onClick.AddListener(() => sm.ShowScreen(sm.screenTownShop));
            btnSettings.onClick.AddListener(() => sm.ShowScreen(sm.screenSettings));
        }
    }
}
