using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmPixelCrushersConversation))]
    public sealed class GmSoreliaSuppliesDialogue : MonoBehaviour
    {
        public const string FirstConversation = "DaengSaka/Dialog Warga Sorelia/Ibu Nelayan";
        public const string RepeatConversation = "DaengSaka/Dialog Warga Sorelia/Ibu Nelayan Setelah Bekal";
        public const string CompletionEntry = "DaengSaka Sorelia Supplies Complete";

        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        private GmPixelCrushersConversation interaction;
        private DialogueSystemController controller;

        public void Configure(GmMainQuestProgress progress, GmPixelCrushersDialogueBridge dialogueBridge)
        {
            quest = progress;
            bridge = dialogueBridge;
        }

        private void OnEnable()
        {
            interaction = GetComponent<GmPixelCrushersConversation>();
            if (interaction != null)
                interaction.SetConversation(GmPartyRun.Current.ReceivedSoreliaSupplies ? RepeatConversation : FirstConversation);
            controller = bridge != null ? bridge.GetComponent<DialogueSystemController>() : null;
            if (controller != null) controller.conversationLinePrepared += OnLinePrepared;
        }

        private void OnDisable()
        {
            if (controller != null) controller.conversationLinePrepared -= OnLinePrepared;
            controller = null;
        }

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != CompletionEntry || interaction == null || quest == null || controller == null ||
                !quest.isActiveAndEnabled || !quest.HasCompleted(GmMainQuestStep.SelamatkanKeluargaNelayan) ||
                controller.lastConversationStarted != FirstConversation ||
                controller.masterDatabase?.GetConversation(FirstConversation)?.id != entry.conversationID) return;

            if (GmPartyRun.Current.TryReceiveSoreliaSupplies()) interaction.SetConversation(RepeatConversation);
        }
    }
}
