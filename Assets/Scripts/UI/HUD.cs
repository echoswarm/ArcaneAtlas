using UnityEngine;
using TMPro;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class HUD : MonoBehaviour
    {
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI packText;
        public TextMeshProUGUI zoneText;

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (goldText != null)
            {
                goldText.text = ""; // Clear TMP — sprites take over
                NumberRenderer.Set(goldText.transform, "GoldNum", GameState.Gold, NumberRenderer.Gold, 22f);
            }
            if (packText != null)
            {
                packText.text = "";
                NumberRenderer.Set(packText.transform, "PackNum", GameState.Packs, NumberRenderer.Purple, 22f);
            }
            if (zoneText != null) zoneText.text = GameState.CurrentZone;
        }

        public void SetZone(string zoneName)
        {
            GameState.CurrentZone = zoneName;
            if (zoneText != null) zoneText.text = zoneName;
        }
    }
}
