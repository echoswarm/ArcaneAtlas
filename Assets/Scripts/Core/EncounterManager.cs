using UnityEngine;
using ArcaneAtlas.Data;
using ArcaneAtlas.UI;

namespace ArcaneAtlas.Core
{
    public class EncounterManager : MonoBehaviour
    {
        public static EncounterManager Instance { get; private set; }

        private NpcController currentNpc;
        private int currentDialogueLine;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartEncounter(NpcController npc)
        {
            currentNpc = npc;
            currentDialogueLine = 0;

            // Pause exploration
            ExplorationManager.Instance.SetPaused(true);

            // Show encounter UI
            var encounterUI = FindFirstObjectByType<EncounterUI>();
            if (encounterUI != null)
            {
                encounterUI.Show(npc.Data);
                encounterUI.ShowDialogueLine(npc.Data.DialogueLines[0]);
            }
        }

        public void AdvanceDialogue()
        {
            currentDialogueLine++;
            var encounterUI = FindFirstObjectByType<EncounterUI>();
            if (currentDialogueLine < currentNpc.Data.DialogueLines.Length)
            {
                encounterUI.ShowDialogueLine(currentNpc.Data.DialogueLines[currentDialogueLine]);
            }
            else
            {
                encounterUI.ShowChoices();
            }
        }

        public void OnChoiceDuel()
        {
            GameState.CurrentOpponent = currentNpc.Data;

            var encounterUI = FindFirstObjectByType<EncounterUI>();
            if (encounterUI != null)
                encounterUI.Hide();

            ScreenManager.Instance.ShowScreen(ScreenManager.Instance.screenCombat, showHUD: false);
        }

        public void OnChoiceTrade()
        {
            var encounterUI = FindFirstObjectByType<EncounterUI>();
            if (encounterUI != null)
                encounterUI.ShowDialogueLine("Trading is not yet available. Come back later!");
        }

        public void OnChoiceLeave()
        {
            var encounterUI = FindFirstObjectByType<EncounterUI>();
            if (encounterUI != null)
                encounterUI.Hide();
            ExplorationManager.Instance.SetPaused(false);
        }

        public void OnCombatComplete(bool playerWon)
        {
            if (playerWon && currentNpc != null)
            {
                currentNpc.Data.IsDefeated = true;
                if (currentNpc.spriteRenderer != null)
                    currentNpc.spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

                // Check if all NPCs in the room are defeated
                ExplorationManager.Instance.OnRoomNpcDefeated();

                // Quest tracking
                string zone = GameState.CurrentZone;
                QuestManager.OnNPCDefeated(zone, currentNpc.Data.Name);
                if (currentNpc.Data.IsBoss)
                    QuestManager.OnBossDefeated(zone);
            }

            ExplorationManager.Instance.SetPaused(false);
        }
    }
}
