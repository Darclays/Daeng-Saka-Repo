using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmMainQuestProgress))]
    public sealed class GmCapitalDeliveryDialogue : MonoBehaviour
    {
        private const string Prefix = "DaengSaka/Side Quest/Titipan Penempa Besi/";
        public const string OfferTitle = Prefix + "Titipan";
        public const string InspectionTitle = Prefix + "Periksa Pedang";
        public const string DeliveryTitle = Prefix + "Pedang Pak Yusuf";
        public const string PaymentTitle = Prefix + "Upah";
        public const string AcoGreeting = "DaengSaka/Dialog Ibu Kota/Daeng Aco";
        public const string CompletionEntry = "DaengSaka City Delivery Complete";

        private const int Unavailable = -2, Offer = -1, Inspect = 0, Deliver = 1, Payment = 2;
        [SerializeField] private GmQuestJournal journal;
        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        [SerializeField] private Transform blacksmith, aco, guard;
        private GmMainQuestProgress mainQuest;
        private DialogueSystemController controller;
        private Transform speaker;
        private int started = Unavailable;

        public void Configure(GmQuestJournal questJournal, GmPixelCrushersDialogueBridge dialogueBridge,
            Transform smith, Transform guide, Transform castleGuard)
        {
            journal = questJournal;
            bridge = dialogueBridge;
            blacksmith = smith;
            aco = guide;
            guard = castleGuard;
        }

        private void OnEnable()
        {
            mainQuest = GetComponent<GmMainQuestProgress>();
            controller = bridge != null ? bridge.GetComponent<DialogueSystemController>() : null;
            if (controller == null) return;
            controller.conversationLinePrepared += OnLinePrepared;
            controller.conversationEnded += OnConversationEnded;
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.conversationLinePrepared -= OnLinePrepared;
                controller.conversationEnded -= OnConversationEnded;
            }
            ClearDialogue();
        }

        private int StepAt(Transform npc)
        {
            if (npc == null || !isActiveAndEnabled || journal == null || !journal.isActiveAndEnabled ||
                bridge == null || !bridge.isActiveAndEnabled || controller == null || !controller.isActiveAndEnabled ||
                mainQuest == null || !mainQuest.isActiveAndEnabled ||
                !mainQuest.HasCompleted(GmMainQuestStep.KunjungiPenempaBesi)) return Unavailable;

            return journal.CompletedSideSteps(GmQuestCatalog.CityDelivery.Id) switch
            {
                Offer when npc == blacksmith => Offer,
                Inspect when npc == aco => Inspect,
                Deliver when npc == guard => Deliver,
                Payment when npc == blacksmith => Payment,
                _ => Unavailable
            };
        }

        private static string TitleAt(int step) => step switch
        {
            Offer => OfferTitle,
            Inspect => InspectionTitle,
            Deliver => DeliveryTitle,
            Payment => PaymentTitle,
            _ => null
        };

        internal bool CanStartAt(Transform npc) => StepAt(npc) != Unavailable;

        internal bool TryStartAt(Transform npc, GmInteractor requester)
        {
            int step = StepAt(npc);
            if (step == Unavailable || !bridge.TryStartFramedDialogue(TitleAt(step), requester, npc)) return false;
            speaker = npc;
            started = step;
            return true;
        }

        private void OnConversationEnded(Transform actor) => ClearDialogue();

        private void ClearDialogue()
        {
            speaker = null;
            started = Unavailable;
        }

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != CompletionEntry || started == Unavailable ||
                StepAt(speaker) != started) return;
            string title = TitleAt(started);
            if (controller.lastConversationStarted != title ||
                controller.masterDatabase?.GetConversation(title)?.id != entry.conversationID) return;

            var quest = GmQuestCatalog.CityDelivery;
            int step = started;
            ClearDialogue();
            if (step == Offer)
                journal.TryStartSideQuest(quest);
            else if (step != Payment || GmPartyRun.Current.Wallet.TryGrantOnce(quest.Id, GmQuestCatalog.CityDeliveryReward))
                journal.TryCompleteSideStep(quest.Id, step);
        }
    }
}
