using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmSoreliaRescueCutscene))]
    public sealed class GmSoreliaInvasionIntro : MonoBehaviour
    {
        private const float ArrivalRadius = 4f;
        private static GmPartyRun playedForRun;

        [SerializeField] private GmSakaMotor player;
        [SerializeField] private Transform arrival;
        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private GmSoreliaRescueCutscene director;

        public bool IsConfigured => player != null && arrival != null && quest != null && director != null;

        public void Configure(GmSakaMotor player, Transform arrival, GmMainQuestProgress quest,
            GmSoreliaRescueCutscene director)
        {
            this.player = player;
            this.arrival = arrival;
            this.quest = quest;
            this.director = director;
        }

        private void LateUpdate()
        {
            GmPartyRun run = GmPartyRun.Current;
            if (ReferenceEquals(playedForRun, run) || !IsConfigured || gameObject.scene != SceneManager.GetActiveScene() ||
                !quest.isActiveAndEnabled || quest.Step != GmMainQuestStep.TemuiKeluargaNelayan || !player.isActiveAndEnabled)
                return;

            Vector3 offset = Vector3.ProjectOnPlane(player.transform.position - arrival.position, Vector3.up);
            if (offset.sqrMagnitude <= ArrivalRadius * ArrivalRadius && director.TryPlay(player))
                playedForRun = run;
        }
    }
}
