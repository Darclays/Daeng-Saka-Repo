using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmPixelCrushersConversation : GmInteractable
    {
        [ConversationPopup]
        [SerializeField] private string conversationTitle;
        [SerializeField] private GmPixelCrushersDialogueBridge dialogueBridge;

        [Header("Demo Progression")]
        [SerializeField] private GmDemoProgress demoProgress;
        [ConversationPopup]
        [SerializeField] private string afterPoppo;
        [ConversationPopup]
        [SerializeField] private string afterPakande;

        [Header("Main Quest")]
        [SerializeField] private GmPixelCrushersMainQuestDialogue mainQuestDialogue;
        [Header("Ambient Conversation")]
        [SerializeField] private GmMainQuestProgress requiredQuest;
        [SerializeField] private GmMainQuestStep requiredCompletedStep;
        [SerializeField] private bool useAuthoredActors;
        private GmFishingQuestDialogue fishingQuest;

        private bool IsUnlocked => requiredQuest == null ||
            (requiredQuest.isActiveAndEnabled && requiredQuest.HasCompleted(requiredCompletedStep));

        private string QuestTitle => mainQuestDialogue != null ? mainQuestDialogue.CurrentTitleFor(transform) : null;

        private bool OffersFishingLoan => fishingQuest != null && fishingQuest.CanBorrow &&
            string.IsNullOrWhiteSpace(QuestTitle);
        private bool OffersMerchant => mainQuestDialogue != null && mainQuestDialogue.OffersMerchantTo(transform);
        private bool OffersBlacksmith => mainQuestDialogue != null && mainQuestDialogue.OffersBlacksmithTo(transform);

        public override string Prompt
        {
            get
            {
                if (OffersFishingLoan) return "[E] Pinjam alat pancing — Nelayan";
                if (OffersMerchant)
                    return string.IsNullOrWhiteSpace(QuestTitle) && !OffersBlacksmith
                        ? "[E] Belanja — Merchant"
                        : "[E] Bicara atau belanja — Pemandu Kerajaan";
                return OffersBlacksmith ? "[E] Belanja atau enchant — Penempa Besi" : base.Prompt;
            }
        }

        public string ConversationTitle => conversationTitle;
        public void SetConversation(string title) => conversationTitle = title;

        public void ConfigureAmbientDialogue(string title, GmPixelCrushersDialogueBridge bridge,
            GmMainQuestProgress quest, GmMainQuestStep completedStep)
        {
            SetConversation(title);
            dialogueBridge = bridge;
            requiredQuest = quest;
            requiredCompletedStep = completedStep;
            useAuthoredActors = true;
        }

        public void SetMainQuestDialogue(GmPixelCrushersMainQuestDialogue sequence) => mainQuestDialogue = sequence;

        public void SetDemoConversations(GmDemoProgress progress, string poppoTitle, string pakandeTitle)
        {
            demoProgress = progress;
            afterPoppo = poppoTitle;
            afterPakande = pakandeTitle;
        }

        private void Awake() => fishingQuest = GetComponent<GmFishingQuestDialogue>();

        public override bool CanInteract(GmInteractor requester) => IsUnlocked && base.CanInteract(requester)
            && (OffersFishingLoan || OffersMerchant || OffersBlacksmith || !string.IsNullOrWhiteSpace(ResolveConversationTitle()));

        private string ResolveConversationTitle()
        {
            if (mainQuestDialogue != null) return QuestTitle;
            if (demoProgress == null) return conversationTitle;
            string title = demoProgress.Stage switch
            {
                GmDemoStage.GuaLembayung => afterPoppo,
                GmDemoStage.Selesai => afterPakande,
                _ => conversationTitle
            };
            return string.IsNullOrWhiteSpace(title) ? conversationTitle : title;
        }

        public override bool TryInteract(GmInteractor requester)
        {
            if (!IsUnlocked || !base.CanInteract(requester)) return false;
            if (OffersFishingLoan) return fishingQuest.TryBorrow(requester);
            if (OffersMerchant)
            {
                var counter = GetComponent<GmMerchantCounter>();
                if (counter == null) counter = gameObject.AddComponent<GmMerchantCounter>();
                return counter.TryOpen(mainQuestDialogue, requester);
            }
            if (OffersBlacksmith)
            {
                var counter = GetComponent<GmBlacksmithCounter>();
                if (counter == null) counter = gameObject.AddComponent<GmBlacksmithCounter>();
                return counter.TryOpen(mainQuestDialogue, requester);
            }
            if (mainQuestDialogue != null) return mainQuestDialogue.TryStartCurrent(requester, transform);
            string title = ResolveConversationTitle();
            if (string.IsNullOrWhiteSpace(title)) return false;
            if (dialogueBridge == null && DialogueManager.hasInstance)
                dialogueBridge = DialogueManager.instance.GetComponent<GmPixelCrushersDialogueBridge>();
            if (dialogueBridge == null)
            {
                Debug.LogError("[Daeng Saka 1A.2B] Add Gm Pixel Crushers Dialogue Bridge to the Dialogue Manager.", this);
                return false;
            }
            return useAuthoredActors
                ? dialogueBridge.TryStartCutscene(title, requester)
                : dialogueBridge.TryStartConversation(title, requester, transform);
        }
    }
}
