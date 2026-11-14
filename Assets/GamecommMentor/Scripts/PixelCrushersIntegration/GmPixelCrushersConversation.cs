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
        public void SetConversation(string title) => conversationTitle = title;

        public override bool CanInteract(GmInteractor requester) => base.CanInteract(requester)
            && !string.IsNullOrWhiteSpace(conversationTitle);

        public override bool TryInteract(GmInteractor requester)
        {
            if (!CanInteract(requester)) return false;
            if (dialogueBridge == null && DialogueManager.hasInstance)
                dialogueBridge = DialogueManager.instance.GetComponent<GmPixelCrushersDialogueBridge>();
            if (dialogueBridge == null)
            {
                Debug.LogError("[Daeng Saka 1A.2B] Add Gm Pixel Crushers Dialogue Bridge to the Dialogue Manager.", this);
                return false;
            }
            return dialogueBridge.TryStartConversation(conversationTitle, requester, transform);
        }
    }
}
