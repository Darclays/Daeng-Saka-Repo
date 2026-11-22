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
        public const string HerbalOfferTitle = "DaengSaka/Side Quest/Jamu untuk Si Kecil/Titipan Pak Yusuf";
        public const string HerbalPickupTitle = "DaengSaka/Side Quest/Jamu untuk Si Kecil/Ambil Jamu";
        public const string HerbalDropoffTitle = "DaengSaka/Side Quest/Jamu untuk Si Kecil/Antar Jamu";
        public const string AcoGreeting = "DaengSaka/Dialog Ibu Kota/Daeng Aco";
        public const string CompletionEntry = "DaengSaka City Delivery Complete";

        private enum Stop { None, OfferSword, InspectSword, DeliverSword, SwordPayment, OfferJamu, PickUpJamu, DeliverJamu }

        [SerializeField] private GmQuestJournal journal;
        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        [SerializeField] private Transform blacksmith, aco, guard;
        [SerializeField] private Transform herbalist, mother;
        private GmMainQuestProgress mainQuest;
        private DialogueSystemController controller;
        private Transform speaker;
        private Stop started;

        public void Configure(GmQuestJournal questJournal, GmPixelCrushersDialogueBridge dialogueBridge,
            Transform smith, Transform guide, Transform castleGuard, Transform jamu = null, Transform family = null)
        {
            journal = questJournal;
            bridge = dialogueBridge;
            blacksmith = smith;
            aco = guide;
            guard = castleGuard;
            herbalist = jamu;
            mother = family;
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

        private Stop StepAt(Transform npc)
        {
            if (npc == null || !isActiveAndEnabled || journal == null || !journal.isActiveAndEnabled ||
                bridge == null || !bridge.isActiveAndEnabled || controller == null || !controller.isActiveAndEnabled ||
                mainQuest == null || !mainQuest.isActiveAndEnabled ||
                !mainQuest.HasCompleted(GmMainQuestStep.KunjungiPenempaBesi)) return Stop.None;

            int swordStep = journal.CompletedSideSteps(GmQuestCatalog.CityDelivery.Id);
            return swordStep switch
            {
                -1 when npc == blacksmith => Stop.OfferSword,
                 0 when npc == aco => Stop.InspectSword,
                 1 when npc == guard => Stop.DeliverSword,
                 2 when npc == blacksmith => Stop.SwordPayment,
                 >= 2 => HerbalStopAt(npc),
                 _ => Stop.None
            };
        }

        private Stop HerbalStopAt(Transform npc) => journal.CompletedSideSteps(GmQuestCatalog.HerbalDelivery.Id) switch
        {
            -1 when npc == guard && herbalist != null && mother != null => Stop.OfferJamu,
             0 when npc == herbalist => Stop.PickUpJamu,
             1 when npc == mother => Stop.DeliverJamu,
             _ => Stop.None
        };

        private static string TitleAt(Stop stop) => stop switch
        {
            Stop.OfferSword => OfferTitle,
            Stop.InspectSword => InspectionTitle,
            Stop.DeliverSword => DeliveryTitle,
            Stop.SwordPayment => PaymentTitle,
            Stop.OfferJamu => HerbalOfferTitle,
            Stop.PickUpJamu => HerbalPickupTitle,
            Stop.DeliverJamu => HerbalDropoffTitle,
            _ => null
        };

        internal bool CanStartAt(Transform npc) => StepAt(npc) != Stop.None;
        internal GmSideQuestDefinition QuestOfferedAt(Transform npc) => StepAt(npc) switch
        {
            Stop.OfferSword => GmQuestCatalog.CityDelivery,
            Stop.OfferJamu => GmQuestCatalog.HerbalDelivery,
            _ => null
        };

        internal bool SmithHasPayment => StepAt(blacksmith) == Stop.SwordPayment;

        internal bool TryStartAt(Transform npc, GmInteractor requester)
        {
            Stop step = StepAt(npc);
            if (step == Stop.None || !bridge.TryStartFramedDialogue(TitleAt(step), requester, npc)) return false;
            speaker = npc;
            started = step;
            return true;
        }

        private void OnConversationEnded(Transform actor) => ClearDialogue();

        private void ClearDialogue()
        {
            speaker = null;
            started = Stop.None;
        }

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != CompletionEntry || started == Stop.None ||
                StepAt(speaker) != started) return;
            string title = TitleAt(started);
            if (controller.lastConversationStarted != title ||
                controller.masterDatabase?.GetConversation(title)?.id != entry.conversationID) return;

            Stop finished = started;
            ClearDialogue();
            var sword = GmQuestCatalog.CityDelivery;
            var jamu = GmQuestCatalog.HerbalDelivery;
            switch (finished)
            {
                case Stop.OfferSword:
                    journal.TryStartSideQuest(sword);
                    break;
                case Stop.InspectSword:
                    journal.TryCompleteSideStep(sword.Id, 0);
                    break;
                case Stop.DeliverSword:
                    journal.TryCompleteSideStep(sword.Id, 1);
                    break;
                case Stop.SwordPayment:
                    CompleteWithPayment(sword.Id, 2, GmQuestCatalog.CityDeliveryReward);
                    break;
                case Stop.OfferJamu:
                    journal.TryStartSideQuest(jamu);
                    break;
                case Stop.PickUpJamu:
                    journal.TryCompleteSideStep(jamu.Id, 0);
                    break;
                case Stop.DeliverJamu:
                    CompleteWithPayment(jamu.Id, 1, GmQuestCatalog.HerbalDeliveryReward);
                    break;
            }
        }

        private void CompleteWithPayment(string questId, int step, int amount)
        {
            if (GmPartyRun.Current.Wallet.TryGrantOnce(questId, amount))
                journal.TryCompleteSideStep(questId, step);
        }
    }
}
