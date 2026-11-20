using System;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmMainQuestProgress), typeof(GmEncounterFlow))]
    public sealed class GmSoreliaRescue : MonoBehaviour
    {
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
        private GmMainQuestProgress progress;
        private GmEncounterFlow flow;

        public void Configure(GmEnemyEncounter encounter, Resident[] placements)
        {
            poppo = encounter;
            residents = placements ?? Array.Empty<Resident>();
        }

        public void SetPostRescueResidents(GameObject group) => postRescueResidents = group;
        public void SetVictoryCutscene(GmSoreliaRescueCutscene cutscene) => victoryCutscene = cutscene;

        private void OnEnable()
        {
            progress = GetComponent<GmMainQuestProgress>();
            flow = GetComponent<GmEncounterFlow>();
            flow.VictoryReturned += OnVictoryReturned;
            PlaceResidents(progress.HasCompleted(GmMainQuestStep.SelamatkanKeluargaNelayan));
        }

        private void OnDisable()
        {
            if (flow != null) flow.VictoryReturned -= OnVictoryReturned;
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
