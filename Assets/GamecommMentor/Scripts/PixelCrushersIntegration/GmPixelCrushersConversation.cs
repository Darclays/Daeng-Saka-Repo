using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmPixelCrushersConversation : GmInteractable
    {
        private static readonly HashSet<GmPixelCrushersConversation> activeConversations = new HashSet<GmPixelCrushersConversation>();

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
        [SerializeField] private GmCapitalDeliveryDialogue cityDelivery;
        [Header("Ambient Conversation")]
        [SerializeField] private GmMainQuestProgress requiredQuest;
        [SerializeField] private GmMainQuestStep requiredCompletedStep;
        [SerializeField] private bool useAuthoredActors;
        private GmFishingQuestDialogue fishingQuest;
        private GmSoreliaSuppliesDialogue supplies;
        private SpriteRenderer guidanceSprite;
        private CapsuleCollider guidanceBody;

        internal static IEnumerable<GmPixelCrushersConversation> ActiveConversations => activeConversations;

        private bool IsUnlocked => requiredQuest == null ||
            (requiredQuest.isActiveAndEnabled && requiredQuest.HasCompleted(requiredCompletedStep));

        private string QuestTitle => mainQuestDialogue != null ? mainQuestDialogue.CurrentTitleFor(transform) : null;

        private bool OffersFishingLoan => fishingQuest != null && fishingQuest.CanBorrow &&
            string.IsNullOrWhiteSpace(QuestTitle);
        private bool OffersMerchant => mainQuestDialogue != null && mainQuestDialogue.OffersMerchantTo(transform);
        private bool OffersBlacksmith => mainQuestDialogue != null && mainQuestDialogue.OffersBlacksmithTo(transform);
        private bool OffersCityDelivery => string.IsNullOrWhiteSpace(QuestTitle) &&
            cityDelivery != null && cityDelivery.CanStartAt(transform);

        public override string Prompt
        {
            get
            {
                if (OffersFishingLoan) return "[E] Pinjam alat pancing — Nelayan";
                if (OffersCityDelivery)
                    return OffersBlacksmith ? "[E] Layanan dan tugas — Penempa Besi" : base.Prompt;
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
        public void SetCityDelivery(GmCapitalDeliveryDialogue delivery) => cityDelivery = delivery;

        public void SetDemoConversations(GmDemoProgress progress, string poppoTitle, string pakandeTitle)
        {
            demoProgress = progress;
            afterPoppo = poppoTitle;
            afterPakande = pakandeTitle;
        }

        private void Awake() => fishingQuest = GetComponent<GmFishingQuestDialogue>();

        private void OnEnable()
        {
            supplies = GetComponent<GmSoreliaSuppliesDialogue>();
            guidanceSprite = GetComponentInChildren<SpriteRenderer>(true);
            guidanceBody = GetComponent<CapsuleCollider>();
            activeConversations.Add(this);
        }

        private void OnDisable() => activeConversations.Remove(this);

        internal GmNpcGuideIcon GuidanceIcon(GmInteractor requester)
        {
            if (!CanInteract(requester)) return GmNpcGuideIcon.None;
            if (OffersFishingLoan || OffersCityDelivery || !string.IsNullOrWhiteSpace(QuestTitle) ||
                (supplies != null && supplies.HasSuppliesToOffer))
                return GmNpcGuideIcon.Quest;
            if (OffersMerchant || OffersBlacksmith) return GmNpcGuideIcon.Shop;
            return GmNpcGuideIcon.Dialogue;
        }

        internal bool TryGetGuidanceHead(out Vector3 head)
        {
            if (guidanceSprite != null && guidanceSprite.enabled &&
                guidanceSprite.gameObject.activeInHierarchy && guidanceSprite.sprite != null)
            {
                Bounds bounds = guidanceSprite.bounds;
                head = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                // The imported NPC cells can include transparent padding above the body.
                if (guidanceBody != null && guidanceBody.enabled)
                {
                    Bounds body = guidanceBody.bounds;
                    if (body.size.y > 0f && body.max.y < head.y)
                        head = new Vector3(body.center.x, body.max.y, body.center.z);
                }
                return true;
            }
            head = transform.position + Vector3.up * 1.55f;
            return guidanceSprite == null;
        }

        public override bool CanInteract(GmInteractor requester) => IsUnlocked && base.CanInteract(requester)
            && (OffersFishingLoan || OffersCityDelivery || OffersMerchant || OffersBlacksmith ||
                !string.IsNullOrWhiteSpace(ResolveConversationTitle()));

        private string ResolveConversationTitle()
        {
            if (mainQuestDialogue != null && (!useAuthoredActors || !string.IsNullOrWhiteSpace(QuestTitle)))
                return QuestTitle;
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
                return counter.TryOpen(mainQuestDialogue, requester, cityDelivery);
            }
            if (OffersCityDelivery) return cityDelivery.TryStartAt(transform, requester);
            if (!string.IsNullOrWhiteSpace(QuestTitle)) return mainQuestDialogue.TryStartCurrent(requester, transform);
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
