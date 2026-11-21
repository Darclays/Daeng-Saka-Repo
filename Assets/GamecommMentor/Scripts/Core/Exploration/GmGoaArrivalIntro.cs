using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmSoreliaRescueCutscene))]
    public sealed class GmGoaArrivalIntro : MonoBehaviour
    {
        public const float PreviewHeight = 2.6f;
        private const float ArrivalRadius = 4f;
        private static GmPartyRun shownForRun;

        [SerializeField] private GmSakaMotor player;
        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private Transform arrival;
        [SerializeField] private GmSoreliaRescueCutscene director;
        [SerializeField] private Transform poppoVisual;
        [SerializeField] private Transform mouthPoppoVisual;
        [SerializeField] private GmCavePatrol mouthPatrol;
        private Vector3 groundedPoppo;
        private Vector3 groundedMouthPoppo;

        public bool IsConfigured => player != null && quest != null && arrival != null &&
            director != null && poppoVisual != null;

        public void Configure(GmSakaMotor player, GmMainQuestProgress quest, Transform arrival,
            GmSoreliaRescueCutscene director, Transform poppoVisual)
        {
            this.player = player;
            this.quest = quest;
            this.arrival = arrival;
            this.director = director;
            this.poppoVisual = poppoVisual;
        }

        public void SetMouthPoppo(Transform visual) => mouthPoppoVisual = visual;
        public void SetMouthPatrol(GmCavePatrol patrol) => mouthPatrol = patrol;

        private void Awake()
        {
            if (poppoVisual != null) groundedPoppo = poppoVisual.localPosition;
            if (mouthPoppoVisual != null) groundedMouthPoppo = mouthPoppoVisual.localPosition;
        }

        private void OnEnable() => UpdatePoppo(snap: true);

        private void OnDisable()
        {
            if (poppoVisual != null) poppoVisual.localPosition = groundedPoppo;
            if (mouthPoppoVisual != null)
            {
                mouthPoppoVisual.localPosition = groundedMouthPoppo;
                mouthPoppoVisual.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            UpdatePoppo(snap: false);
            GmPartyRun run = GmPartyRun.Current;
            if (ReferenceEquals(shownForRun, run) || !IsConfigured || gameObject.scene != SceneManager.GetActiveScene() ||
                !quest.isActiveAndEnabled || quest.Step != GmMainQuestStep.MasukGuaLembayung || !player.isActiveAndEnabled)
                return;

            Vector3 offset = Vector3.ProjectOnPlane(player.transform.position - arrival.position, Vector3.up);
            if (offset.sqrMagnitude <= ArrivalRadius * ArrivalRadius && director.TryPlay(player))
                shownForRun = run;
        }

        private void UpdatePoppo(bool snap)
        {
            if (quest == null) return;
            GmMainQuestStep step = quest.Step;
            bool arriving = step == GmMainQuestStep.MasukGuaLembayung;
            Vector3 hover = Vector3.up * (PreviewHeight + Mathf.Sin(Time.unscaledTime * 3.25f) * .14f);

            if (mouthPoppoVisual != null && (mouthPatrol == null || !mouthPatrol.DrivesMouthVisual))
            {
                bool present = quest.isActiveAndEnabled && (arriving || step == GmMainQuestStep.SelidikiGua);
                if (present) mouthPoppoVisual.localPosition = groundedMouthPoppo + hover;
                if (mouthPoppoVisual.gameObject.activeSelf != present)
                    mouthPoppoVisual.gameObject.SetActive(present);
            }

            if (poppoVisual == null || !poppoVisual.gameObject.activeInHierarchy) return;
            Vector3 target = groundedPoppo;
            if (arriving) target += hover;
            poppoVisual.localPosition = snap ? target : Vector3.MoveTowards(poppoVisual.localPosition, target,
                Time.unscaledDeltaTime * 7f);
        }
    }
}
