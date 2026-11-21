using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmMainQuestProgress), typeof(GmEncounterFlow))]
    public sealed class GmGoaPakandeFinale : MonoBehaviour
    {
        public const string AmbushTitle = "DaengSaka/Main Quest/Rombongan di Goa";
        public const string RevealTitle = "DaengSaka/Main Quest/Amarah Nenek Pakande";
        public const string RouteTitle = "DaengSaka/Main Quest/Jalur ke Ruang Dalam";

        private const float ApproachRadius = 1.85f;
        private static GmPartyRun watchedAmbushForRun;

        [SerializeField] private GmSakaMotor player;
        [SerializeField] private GmEnemyEncounter guards;
        [SerializeField] private GmSoreliaRescueCutscene ambush;
        [SerializeField] private GmSoreliaRescueCutscene reveal;
        [SerializeField] private GmPixelCrushersDialogueBridge dialogue;

        private GmMainQuestProgress quest;
        private GmEncounterFlow flow;
        private GmInteractor interactor;
        private GmSoreliaRescueCutscene playing;
        private bool requestedDialogue;
        private bool enteredApproach;

        public void Configure(GmSakaMotor hero, GmEnemyEncounter encounter, GmSoreliaRescueCutscene beforeBattle,
            GmSoreliaRescueCutscene afterBattle, GmPixelCrushersDialogueBridge bridge)
        {
            player = hero;
            guards = encounter;
            ambush = beforeBattle;
            reveal = afterBattle;
            dialogue = bridge;
        }

        private void OnEnable()
        {
            quest = GetComponent<GmMainQuestProgress>();
            flow = GetComponent<GmEncounterFlow>();
            interactor = player != null ? player.GetComponent<GmInteractor>() : null;
            flow.VictoryReturned += OnVictoryReturned;
        }

        private void OnDisable()
        {
            if (flow != null) flow.VictoryReturned -= OnVictoryReturned;
            if (playing != null)
            {
                if (dialogue != null) dialogue.StopStagedDialogue(TitleFor(playing));
                playing.CancelPlayback();
            }
            playing = null;
            requestedDialogue = false;
            enteredApproach = false;
        }

        private void LateUpdate()
        {
            if (gameObject.scene != SceneManager.GetActiveScene() || player == null || !player.isActiveAndEnabled ||
                quest == null || !quest.isActiveAndEnabled || interactor == null) return;

            if (playing != null)
            {
                UpdateSequence();
                return;
            }
            if (guards == null || quest.ActiveEncounter != guards || !guards.isActiveAndEnabled ||
                guards.IsDefeated) return;

            Vector3 offset = player.transform.position - guards.transform.position;
            bool near = Mathf.Abs(offset.y) < 2f &&
                Vector3.ProjectOnPlane(offset, Vector3.up).sqrMagnitude <= ApproachRadius * ApproachRadius;
            if (!near)
            {
                enteredApproach = false;
                return;
            }
            if (enteredApproach || !interactor.ControlGate.CanExplore) return;

            if (ambush == null || !ambush.IsConfigured || ReferenceEquals(watchedAmbushForRun, GmPartyRun.Current))
            {
                enteredApproach = true;
                flow.TryBegin(player, guards);
            }
            else if (TryPlay(ambush))
            {
                enteredApproach = true;
            }
        }

        private void OnVictoryReturned(GmSakaMotor hero, GmEnemyEncounter enemy)
        {
            if (hero == player && enemy == guards && quest.Step == GmMainQuestStep.TemuiPakande)
                TryPlay(reveal);
        }

        private bool TryPlay(GmSoreliaRescueCutscene director)
        {
            if (director == null || !director.TryPlay(player, holdFinalShot: true)) return false;
            playing = director;
            requestedDialogue = false;
            return true;
        }

        private string TitleFor(GmSoreliaRescueCutscene director) => director == ambush ? AmbushTitle : RevealTitle;

        private void UpdateSequence()
        {
            if (playing.IsHoldingFinalShot)
            {
                if (!requestedDialogue)
                {
                    requestedDialogue = true;
                    if (dialogue != null && dialogue.TryStartStagedDialogue(TitleFor(playing), interactor, playing))
                        return;
                }
                if (dialogue == null || !dialogue.StagedDialogueBusy) playing.FinishHeldShot();
                return;
            }

            if (playing.IsPlaying) return;
            if (requestedDialogue && dialogue != null)
            {
                if (playing.WasSkipped) dialogue.StopStagedDialogue(TitleFor(playing));
                if (dialogue.StagedDialogueBusy) return;
            }

            var finished = playing;
            playing = null;
            if (!finished.Completed) return;

            if (finished == ambush && quest.ActiveEncounter == guards)
            {
                watchedAmbushForRun = GmPartyRun.Current;
                flow.TryBegin(player, guards);
                return;
            }
            if (finished == reveal && finished.WasSkipped && quest.Step == GmMainQuestStep.TemuiPakande)
                quest.TryCompleteDialogue(GmMainQuestStep.TemuiPakande);
            if (finished == reveal && quest.Step == GmMainQuestStep.LawanPakande && quest.ActiveEncounter != null)
                flow.TryBegin(player, quest.ActiveEncounter);
        }
    }
}
