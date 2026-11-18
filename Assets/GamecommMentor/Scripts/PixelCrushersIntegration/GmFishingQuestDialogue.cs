using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmPixelCrushersConversation))]
    public sealed class GmFishingQuestDialogue : MonoBehaviour
    {
        public const string LoanConversation = "DaengSaka/Side Quest/Pinjam Alat Pancing";
        public const string LoanCompletionEntry = "DaengSaka Fishing Loan Complete";
        private const int BorrowEquipmentStep = 0;

        [SerializeField] private GmMainQuestProgress mainQuest;
        [SerializeField] private GmQuestJournal journal;
        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        private DialogueSystemController controller;

        private bool QuestUnlocked => mainQuest != null && mainQuest.isActiveAndEnabled &&
            journal != null && journal.isActiveAndEnabled &&
            mainQuest.HasCompleted(GmMainQuestStep.BicaraDenganNelayan);

        internal bool CanBorrow => isActiveAndEnabled && QuestUnlocked &&
            bridge != null && bridge.isActiveAndEnabled &&
            controller != null && controller.isActiveAndEnabled &&
            journal.CompletedSideSteps(GmQuestCatalog.Fishing.Id) == BorrowEquipmentStep;

        public void Configure(GmMainQuestProgress progress, GmQuestJournal questJournal,
            GmPixelCrushersDialogueBridge dialogueBridge)
        {
            mainQuest = progress;
            journal = questJournal;
            bridge = dialogueBridge;
        }

        private void OnEnable()
        {
            controller = bridge != null ? bridge.GetComponent<DialogueSystemController>() : null;
            if (controller == null) return;
            controller.conversationEnded += OnConversationEnded;
            controller.conversationLinePrepared += OnLinePrepared;
            OfferQuest();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.conversationEnded -= OnConversationEnded;
                controller.conversationLinePrepared -= OnLinePrepared;
            }
            controller = null;
        }

        private void OnConversationEnded(Transform actor) => OfferQuest();

        private void OfferQuest()
        {
            if (QuestUnlocked) journal.TryStartSideQuest(GmQuestCatalog.Fishing);
        }

        internal bool TryBorrow(GmInteractor requester) =>
            CanBorrow && bridge.TryStartCutscene(LoanConversation, requester);

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != LoanCompletionEntry || !CanBorrow ||
                controller.lastConversationStarted != LoanConversation ||
                controller.masterDatabase?.GetConversation(LoanConversation)?.id != entry.conversationID) return;

            journal.TryCompleteSideStep(GmQuestCatalog.Fishing.Id, BorrowEquipmentStep);
        }
    }
}
