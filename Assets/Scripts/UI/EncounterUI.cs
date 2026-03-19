using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class EncounterUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject dialoguePanel;
        public GameObject dimOverlay;

        [Header("Text")]
        public TextMeshProUGUI npcName;
        public TextMeshProUGUI npcElement;
        public TextMeshProUGUI dialogueText;

        [Header("Portrait")]
        public Image npcPortrait;
        public Image portraitFrame;
        public Image accentBar;

        [Header("Buttons")]
        public Button btnNext;
        public Button btnDuel;
        public Button btnTrade;
        public Button btnLeave;
        public GameObject choicePanel;

        private CanvasGroup canvasGroup;

        void Start()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (btnNext != null)
                btnNext.onClick.AddListener(() => EncounterManager.Instance?.AdvanceDialogue());
            if (btnDuel != null)
                btnDuel.onClick.AddListener(() => EncounterManager.Instance?.OnChoiceDuel());
            if (btnTrade != null)
                btnTrade.onClick.AddListener(() => EncounterManager.Instance?.OnChoiceTrade());
            if (btnLeave != null)
                btnLeave.onClick.AddListener(() => EncounterManager.Instance?.OnChoiceLeave());
        }

        public void Show(NpcData data)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (dimOverlay != null) dimOverlay.SetActive(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (choicePanel != null) choicePanel.SetActive(false);
            if (btnNext != null) btnNext.gameObject.SetActive(true);

            Color elemColor = GetElementColor(data.Element);

            if (npcName != null) npcName.text = data.Name;
            if (npcElement != null) npcElement.text = data.Element.ToString();
            if (npcPortrait != null) npcPortrait.color = elemColor;
            if (portraitFrame != null) portraitFrame.color = elemColor;
            if (accentBar != null) accentBar.color = elemColor;
        }

        public void ShowDialogueLine(string line)
        {
            if (dialogueText != null) dialogueText.text = line;
            if (btnNext != null) btnNext.gameObject.SetActive(true);
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        public void ShowChoices()
        {
            if (btnNext != null) btnNext.gameObject.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(true);
            // Re-enable all choice buttons (ShowRoomMessage may have hidden some)
            if (btnDuel != null) btnDuel.gameObject.SetActive(true);
            if (btnTrade != null) btnTrade.gameObject.SetActive(true);
            if (btnLeave != null) btnLeave.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
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
