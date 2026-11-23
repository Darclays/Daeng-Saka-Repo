using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmSoreliaRescueCutscene))]
    public sealed class GmSoreliaInvasionIntro : MonoBehaviour
    {
        private const float ArrivalRadius = 4f;
        private static readonly (string Text, float Seconds)[] ArrivalDialogue =
        {
            ("Saka   ·   Terakhir kita melawan To Manurung... Bertarung melawan seseorang yang terbang sangat sulit.", 3.7f),
            ("Daung   ·   Hahaha, aku inget Pacca beberapa kali terhempas angin sampai hampir jatuh dari jurang.", 3.9f),
            ("Pacca   ·   Hei, kalian enak bisa menyerang jarak jauh. Aku cuma punya tameng dan belati.", 3.7f),
            ("Saka   ·   Kalian tau... Aku khawatir bagian Acci terakhir ini akan membuat kita melawan seseorang yang kuat lagi...", 4.6f),
            ("Tiba-tiba terdengar suara teriakan.", 1.6f),
            ("Anak Warga   ·   Tolooong!", 1.1f)
        };
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
            if (offset.sqrMagnitude <= ArrivalRadius * ArrivalRadius && director.TryPlay(player, openingCaptions: ArrivalDialogue))
                playedForRun = run;
        }
    }
}
