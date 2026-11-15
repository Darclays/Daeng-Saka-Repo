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

        public string ConversationTitle => conversationTitle;
        public void SetConversation(string title) => conversationTitle = title;

        public void SetDemoConversations(GmDemoProgress progress, string poppoTitle, string pakandeTitle)
        {
            demoProgress = progress;
            afterPoppo = poppoTitle;
            afterPakande = pakandeTitle;
        }

        public override bool CanInteract(GmInteractor requester) => base.CanInteract(requester)
            && !string.IsNullOrWhiteSpace(ResolveConversationTitle());

        private string ResolveConversationTitle()
        {
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
            if (!base.CanInteract(requester)) return false;
            string title = ResolveConversationTitle();
            if (string.IsNullOrWhiteSpace(title)) return false;
            if (dialogueBridge == null && DialogueManager.hasInstance)
                dialogueBridge = DialogueManager.instance.GetComponent<GmPixelCrushersDialogueBridge>();
            if (dialogueBridge == null)
            {
                Debug.LogError("[Daeng Saka 1A.2B] Add Gm Pixel Crushers Dialogue Bridge to the Dialogue Manager.", this);
                return false;
            }
            return dialogueBridge.TryStartConversation(title, requester, transform);
        }
    }
}
