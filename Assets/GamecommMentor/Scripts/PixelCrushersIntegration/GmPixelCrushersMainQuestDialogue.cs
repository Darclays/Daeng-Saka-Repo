using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmMainQuestProgress))]
    public sealed class GmPixelCrushersMainQuestDialogue : MonoBehaviour
    {
        public const string CompletionEntryTitle = "DaengSaka Main Quest Complete";

        [Serializable]
        public struct StepDialogue
        {
            public GmMainQuestStep step;
            [ConversationPopup] public string conversationTitle;

            public StepDialogue(GmMainQuestStep step, string title)
            {
                this.step = step;
                conversationTitle = title;
            }
        }

        [Serializable]
        public struct StepSpeaker
        {
            public GmMainQuestStep step;
            public Transform speaker;

            public StepSpeaker(GmMainQuestStep step, Transform speaker)
            {
                this.step = step;
                this.speaker = speaker;
            }
        }

        [SerializeField] private GmPixelCrushersDialogueBridge bridge;
        [SerializeField] private StepDialogue[] dialogues = Array.Empty<StepDialogue>();
        [SerializeField] private StepSpeaker[] speakers = Array.Empty<StepSpeaker>();
        [SerializeField] private GmEnemyEncounter revealAfterEncounter;
        [SerializeField] private string caveRouteTitle;
        [SerializeField] private string pakandeRevealTitle;
        private GmMainQuestProgress quest;
        private GmEncounterFlow flow;
        private DialogueSystemController controller;

        private bool HasActiveQuest => isActiveAndEnabled && quest != null && quest.isActiveAndEnabled;

        internal bool OffersMerchantTo(Transform partner) => HasActiveQuest &&
            quest.HasCompleted(GmMainQuestStep.KunjungiMerchant) &&
            IsSpeaker(GmMainQuestStep.KunjungiMerchant, partner) &&
            !string.IsNullOrWhiteSpace(TitleFor(GmMainQuestStep.KunjungiMerchant));

        public string CurrentTitle => HasActiveQuest ? TitleFor(quest.Step) : null;

        internal string CurrentTitleFor(Transform partner) =>
            HasActiveQuest && IsSpeaker(quest.Step, partner) ? TitleFor(quest.Step) : null;

        private bool IsSpeaker(GmMainQuestStep step, Transform partner)
        {
            foreach (var binding in speakers)
                if (binding.step == step) return binding.speaker != null && binding.speaker == partner;
            return true;
        }

        public void SetSpeakers(params StepSpeaker[] bindings) => speakers = bindings ?? Array.Empty<StepSpeaker>();

        private string TitleFor(GmMainQuestStep step)
        {
            if (step == GmMainQuestStep.SelidikiGua && !string.IsNullOrWhiteSpace(caveRouteTitle)) return caveRouteTitle;
            if (step == GmMainQuestStep.TemuiPakande && !string.IsNullOrWhiteSpace(pakandeRevealTitle)) return pakandeRevealTitle;
            foreach (StepDialogue dialogue in dialogues)
                if (dialogue.step == step) return dialogue.conversationTitle;
            return null;
        }

        public void Configure(GmPixelCrushersDialogueBridge dialogueBridge, StepDialogue[] stepDialogues,
            GmEnemyEncounter revealAfter = null)
        {
            bridge = dialogueBridge;
            dialogues = stepDialogues ?? Array.Empty<StepDialogue>();
            revealAfterEncounter = revealAfter;
        }

        public void SetCaveFinaleDialogues(string route, string reveal)
        {
            caveRouteTitle = route;
            pakandeRevealTitle = reveal;
        }

        private void OnEnable()
        {
            quest = GetComponent<GmMainQuestProgress>();
            flow = GetComponent<GmEncounterFlow>();
            flow.VictoryReturned += OnVictoryReturned;
            controller = bridge != null ? bridge.GetComponent<DialogueSystemController>() : null;
            if (controller != null) controller.conversationLinePrepared += OnLinePrepared;
        }

        private void OnDisable()
        {
            if (flow != null) flow.VictoryReturned -= OnVictoryReturned;
            if (controller != null) controller.conversationLinePrepared -= OnLinePrepared;
        }

        public bool TryStartCurrent(GmInteractor requester) => TryStartCurrent(requester, null);

        internal bool TryStartCurrent(GmInteractor requester, Transform partner)
        {
            string title = CurrentTitleFor(partner);
            if (bridge == null || string.IsNullOrWhiteSpace(title)) return false;

            if (partner != null)
            {
                GmVnDialogueCatalog.Resolve(title, out bool frameWorldPair);
                if (frameWorldPair) return bridge.TryStartFramedDialogue(title, requester, partner);
            }
            return bridge.TryStartCutscene(title, requester);
        }

        private void OnVictoryReturned(GmSakaMotor player, GmEnemyEncounter enemy)
        {
            bool reveal = quest.Step == GmMainQuestStep.TemuiPakande && enemy == revealAfterEncounter;
            bool ending = quest.Step == GmMainQuestStep.Penutup && enemy == quest.PakandeEncounter;
            if (reveal || ending)
                TryStartCurrent(player.GetComponent<GmInteractor>());
        }

        private void OnLinePrepared(Subtitle subtitle)
        {
            DialogueEntry entry = subtitle?.dialogueEntry;
            if (entry == null || entry.Title != CompletionEntryTitle) return;

            string title = CurrentTitle;
            if (string.IsNullOrWhiteSpace(title) || controller.lastConversationStarted != title ||
                controller.masterDatabase?.GetConversation(title)?.id != entry.conversationID) return;

            // This terminal entry is reached only after advancing the final visible subtitle.
            quest.TryCompleteDialogue(quest.Step);
        }
    }
}
