using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Core;

namespace ArcaneAtlas.UI
{
    public class SettingsUI : MonoBehaviour
    {
        public Slider volumeSlider;
        public Toggle fullscreenToggle;
        public Button btnApply;
        public Button btnBack;

        void Start()
        {
            btnBack.onClick.AddListener(() => ScreenManager.Instance.GoBack());
            btnApply.onClick.AddListener(ApplySettings);
            if (volumeSlider != null)
                volumeSlider.value = AudioListener.volume;
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;
        }

        private void ApplySettings()
        {
            if (volumeSlider != null)
                AudioListener.volume = volumeSlider.value;
            if (fullscreenToggle != null)
                Screen.fullScreen = fullscreenToggle.isOn;
        }
    }
}
