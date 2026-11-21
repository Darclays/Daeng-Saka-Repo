using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmSoreliaRescueCutscene))]
    public sealed class GmCapitalIntro : MonoBehaviour
    {
        private static GmPartyRun playedForRun;

        [SerializeField] private GmSakaMotor player;
        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private GmSoreliaRescueCutscene director;
        [SerializeField, HideInInspector] private int layoutVersion;

        public int LayoutVersion => layoutVersion;

        public void Configure(GmSakaMotor player, GmMainQuestProgress quest, GmSoreliaRescueCutscene director,
            int layoutVersion)
        {
            this.player = player;
            this.quest = quest;
            this.director = director;
            this.layoutVersion = layoutVersion;
        }

        // The orbit camera initializes at execution order -200, before the first input update.
        private void Start()
        {
            if (gameObject.scene != SceneManager.GetActiveScene() || player == null || !player.isActiveAndEnabled ||
                director == null || quest == null || !quest.isActiveAndEnabled ||
                quest.Step != GmMainQuestStep.PersiapanSebelumBertarung)
                return;

            GmPartyRun run = GmPartyRun.Current;
            if (!ReferenceEquals(playedForRun, run) && director.TryPlay(player, limitFrameDelta: true))
                playedForRun = run;
        }
    }
}
