using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmMainQuestProgress))]
    public sealed class GmPixelCrushersMainQuestDialogue : MonoBehaviour
    {
        public const string CompletionEntryTitle = "DaengSaka Main Quest Complete";

        [Serializable]
        public struct StepDialogue
        {
            public GmMainQuestStep step;
            [ConversationPopup] public string conversationTitle;

            public StepDialogue(GmMainQuestStep step, string title)
            {
                this.step = step;
                conversationTitle = title;
            }
        }

        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        [SerializeField] private StepDialogue[] dialogues = Array.Empty<StepDialogue>();
        private GmMainQuestProgress quest;
        private GmEncounterFlow flow;
        private DialogueSystemController controller;

        public string CurrentTitle
        {
            get
            {
                if (quest == null || !quest.isActiveAndEnabled || !isActiveAndEnabled) return null;
                foreach (StepDialogue dialogue in dialogues)
                    if (dialogue.step == quest.Step) return dialogue.conversationTitle;
                return null;
            }
        }

        public void Configure(GmPixelCrushersDialogueBridge dialogueBridge, StepDialogue[] stepDialogues)
        {
            bridge = dialogueBridge;
            dialogues = stepDialogues ?? Array.Empty<StepDialogue>();
        }

        private void OnEnable()
        {
            quest = GetComponent<GmMainQuestProgress>();
            flow = GetComponent<GmEncounterFlow>();
            flow.VictoryReturned += OnVictoryReturned;
            controller = bridge != null ? bridge.GetComponent<DialogueSystemController>() : null;
            if (controller != null) controller.conversationLinePrepared += OnLinePrepared;
        }

        private void OnDisable()
        {
            if (flow != null) flow.VictoryReturned -= OnVictoryReturned;
            if (controller != null) controller.conversationLinePrepared -= OnLinePrepared;
        }

        public bool TryStartCurrent(GmInteractor requester)
        {
            string title = CurrentTitle;
            return bridge != null && !string.IsNullOrWhiteSpace(title) && bridge.TryStartCutscene(title, requester);
        }

        private void OnVictoryReturned(GmSakaMotor player, GmEnemyEncounter enemy)
        {
            if (quest.Step == GmMainQuestStep.Penutup && enemy == quest.PakandeEncounter)
                TryStartCurrent(player.GetComponent<GmInteractor>());
        }

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != CompletionEntryTitle) return;

            string title = CurrentTitle;
            if (string.IsNullOrWhiteSpace(title) || controller.lastConversationStarted != title ||
                controller.masterDatabase.GetConversation(title)?.id != entry.conversationID) return;

            // This terminal entry is reached only after advancing the final visible subtitle.
            quest.TryCompleteDialogue(quest.Step);
        }
    }
}
