using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GmWorldPortal : GmInteractable
    {
        [SerializeField] private GmWorldScene worldScene;
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationEntry = GmWorldScene.DefaultEntry;
        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private bool requiresQuest;
        [SerializeField] private GmMainQuestStep completedStep;
        [SerializeField] private string lockedMessage;
        [SerializeField] private GmWorldBorder border;

        public bool IsOpen => !requiresQuest || (quest != null && quest.HasCompleted(completedStep));
        public override string Prompt => IsOpen ? base.Prompt : lockedMessage;

        public void SetBorder(GmWorldBorder value) => border = value;

        internal Vector3 InteractionPoint(Vector3 from) =>
            border != null && border.isActiveAndEnabled ? border.ClosestFocus(from) : FocusPosition;

        public void Configure(GmWorldScene world, string scenePath, string entryId, GmMainQuestProgress progress,
            GmMainQuestStep? prerequisite, string lockedPrompt)
        {
            worldScene = world;
            destinationScene = scenePath;
            destinationEntry = entryId;
            quest = progress;
            requiresQuest = prerequisite.HasValue;
            completedStep = prerequisite.GetValueOrDefault();
            lockedMessage = lockedPrompt;
        }

        public override bool TryInteract(GmInteractor requester)
        {
            if (!CanInteract(requester) || !IsOpen || worldScene == null ||
                !worldScene.TryTravel(destinationScene, destinationEntry, requester, this)) return false;
            GmGameplayAudio.World("Portal");
            return true;
        }
    }
}
