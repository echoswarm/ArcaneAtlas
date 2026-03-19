using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Core;

namespace ArcaneAtlas.UI
{
    public class PlaceholderScreenUI : MonoBehaviour
    {
        public Button btnBack;
        public string screenLabel = "Unknown";

        void Start()
        {
            WireButtons();
        }

        void OnEnable()
        {
            var hud = FindFirstObjectByType<HUD>();
            if (hud != null) hud.SetZone(screenLabel);
        }

        public void WireButtons()
        {
            btnBack.onClick.AddListener(() => ScreenManager.Instance.GoBack());
        }
    }
}
