using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmMainQuestProgress), typeof(GmEncounterFlow))]
    public sealed class GmSoreliaRescue : MonoBehaviour
    {
        private const float HoverHeight = 2.65f;

        [Serializable]
        public struct Resident
        {
            [SerializeField] private Transform actor;
            [SerializeField] private Transform beforeRescue;
            [SerializeField] private Transform afterRescue;

            public Resident(Transform actor, Transform beforeRescue, Transform afterRescue)
            {
                this.actor = actor;
                this.beforeRescue = beforeRescue;
                this.afterRescue = afterRescue;
            }

            internal void Place(bool rescued)
            {
                Transform point = rescued ? afterRescue : beforeRescue;
                if (actor != null && point != null)
                    actor.SetPositionAndRotation(point.position, point.rotation);
            }
        }

        [SerializeField] private GmEnemyEncounter poppo;
        [SerializeField] private Resident[] residents = Array.Empty<Resident>();
        [SerializeField] private GameObject postRescueResidents;
        [SerializeField] private GmSoreliaRescueCutscene victoryCutscene;
        [SerializeField] private GmSakaMotor encounterPlayer;
        [SerializeField] private Transform poppoVisual;
        private GmMainQuestProgress progress;
        private GmEncounterFlow flow;
        private GmControlGate encounterControls;
        private GmMainQuestStep previousStep;
        private Vector3 groundedVisual;
        private bool battleQueued;

        public void Configure(GmEnemyEncounter encounter, Resident[] placements)
        {
            poppo = encounter;
            residents = placements ?? Array.Empty<Resident>();
        }

        public void SetPostRescueResidents(GameObject group) => postRescueResidents = group;
        public void SetVictoryCutscene(GmSoreliaRescueCutscene cutscene) => victoryCutscene = cutscene;

        public void SetEncounterHandoff(GmSakaMotor player, Transform visual)
        {
            encounterPlayer = player;
            poppoVisual = visual;
        }

        private void OnEnable()
        {
            progress = GetComponent<GmMainQuestProgress>();
            flow = GetComponent<GmEncounterFlow>();
            flow.VictoryReturned += OnVictoryReturned;
            previousStep = progress.Step;
            battleQueued = false;
            encounterControls = encounterPlayer != null ? encounterPlayer.GetComponent<GmControlGate>() : null;
            if (poppoVisual != null)
            {
                groundedVisual = poppoVisual.localPosition;
                if (!progress.HasCompleted(GmMainQuestStep.TemuiKeluargaNelayan))
                    poppoVisual.localPosition = groundedVisual + Vector3.up * HoverHeight;
            }
            PlaceResidents(progress.HasCompleted(GmMainQuestStep.SelamatkanKeluargaNelayan));
        }

        private void OnDisable()
        {
            if (flow != null) flow.VictoryReturned -= OnVictoryReturned;
            if (poppoVisual != null) poppoVisual.localPosition = groundedVisual;
        }

        private void LateUpdate()
        {
            if (encounterPlayer == null || poppoVisual == null || poppo == null || poppo.IsDefeated) return;

            GmMainQuestStep step = progress.Step;
            bool ready = step == GmMainQuestStep.SelamatkanKeluargaNelayan && progress.ActiveEncounter == poppo;
            if (ready && previousStep == GmMainQuestStep.TemuiKeluargaNelayan) battleQueued = true;
            if (!ready) battleQueued = false;
            previousStep = step;

            // The dialogue bridge releases controls after restoring its camera and closing the VN.
            if (battleQueued && encounterControls != null && encounterControls.CanExplore &&
                encounterPlayer.isActiveAndEnabled && gameObject.scene == SceneManager.GetActiveScene())
            {
                battleQueued = false;
                flow.TryBegin(encounterPlayer, poppo);
            }

            bool hovering = !progress.HasCompleted(GmMainQuestStep.TemuiKeluargaNelayan) ||
                battleQueued || flow.CurrentEncounter == poppo;
            Vector3 target = groundedVisual;
            if (hovering) target += Vector3.up * (HoverHeight + Mathf.Sin(Time.unscaledTime * 3.5f) * .15f);
            poppoVisual.localPosition = Vector3.MoveTowards(poppoVisual.localPosition, target,
                Time.unscaledDeltaTime * (hovering ? 2f : 8f));
        }

        private void OnVictoryReturned(GmSakaMotor player, GmEnemyEncounter enemy)
        {
            if (enemy != poppo || !progress.HasCompleted(GmMainQuestStep.SelamatkanKeluargaNelayan)) return;
            PlaceResidents(true);
            if (victoryCutscene != null && progress.Step == GmMainQuestStep.BicaraDenganNelayan)
                victoryCutscene.TryPlay(player);
        }

        private void PlaceResidents(bool rescued)
        {
            foreach (Resident resident in residents) resident.Place(rescued);
            if (postRescueResidents != null) postRescueResidents.SetActive(rescued);
            Physics.SyncTransforms();
        }
    }
}
