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
        public const string DeliveryConversation = "DaengSaka/Side Quest/Titipan Ibu Nelayan/Serahkan Bahan";
        public const string CompletionEntry = "DaengSaka Sorelia Supplies Complete";

        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        [SerializeField] private GmQuestJournal journal;
        [SerializeField] private bool atMerchant;
        private GmPixelCrushersConversation interaction;
        private DialogueSystemController controller;
        private string started;

        private static GmSideQuestDefinition Delivery => GmQuestCatalog.SoreliaDelivery;
        private static GmPartyInventory Bag => GmPartyRun.Current.Inventory;
        private bool IsReady => isActiveAndEnabled && controller != null && controller.isActiveAndEnabled &&
            bridge != null && bridge.isActiveAndEnabled && journal != null && journal.isActiveAndEnabled;

        internal bool HasQuestToOffer => IsReady && !atMerchant &&
            quest != null && quest.isActiveAndEnabled && quest.HasCompleted(GmMainQuestStep.SelamatkanKeluargaNelayan) &&
            journal.CompletedSideSteps(Delivery.Id) < 0;
        internal bool HasDelivery => IsReady && atMerchant &&
            journal.CompletedSideSteps(Delivery.Id) == 0 && Bag.Count(GmItemDefinition.SoreliaHerbs) > 0;
        internal bool CanDeliver => HasDelivery && Bag.CanExchangeSoreliaHerbs;

        public void Configure(GmMainQuestProgress progress, GmPixelCrushersDialogueBridge dialogueBridge,
            GmQuestJournal questJournal, bool merchant = false)
        {
            quest = progress;
            bridge = dialogueBridge;
            journal = questJournal;
            atMerchant = merchant;
        }

        private void OnEnable()
        {
            interaction = GetComponent<GmPixelCrushersConversation>();
            if (!atMerchant && interaction != null && journal != null)
                interaction.SetConversation(journal.CompletedSideSteps(Delivery.Id) < 0 ? FirstConversation : RepeatConversation);
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
            started = null;
            controller = null;
        }

        internal bool TryStartOffer(GmInteractor requester) =>
            HasQuestToOffer && TryStart(FirstConversation, requester);

        internal bool TryStartDelivery(GmInteractor requester) =>
            CanDeliver && TryStart(DeliveryConversation, requester);

        private bool TryStart(string title, GmInteractor requester)
        {
            if (!bridge.TryStartFramedDialogue(title, requester, transform)) return false;
            started = title;
            return true;
        }

        private void OnConversationEnded(Transform actor) => started = null;

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != CompletionEntry || started == null ||
                controller.lastConversationStarted != started ||
                controller.masterDatabase?.GetConversation(started)?.id != entry.conversationID) return;

            string finished = started;
            started = null;
            if (finished == DeliveryConversation)
            {
                if (CanDeliver && Bag.TryExchangeSoreliaHerbs())
                    journal.TryCompleteSideStep(Delivery.Id, 0);
                return;
            }

            if (!HasQuestToOffer || Bag.Count(GmItemDefinition.SoreliaHerbs) == int.MaxValue ||
                !journal.TryStartSideQuest(Delivery)) return;
            Bag.TryAdd(GmItemDefinition.SoreliaHerbs);
            journal.TrackSideQuest(Delivery.Id);
            if (interaction != null) interaction.SetConversation(RepeatConversation);
        }
    }
}
