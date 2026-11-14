using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmEncounterBattleRoot : MonoBehaviour
    {
        private GmEncounterFlow flow;

        public GmEnemyEncounter Encounter => flow != null ? flow.CurrentEncounter : null;
        public GmSakaMotor Player => flow != null ? flow.CurrentPlayer : null;

        internal void Bind(GmEncounterFlow encounterFlow) => flow = encounterFlow;

        public bool Resolve(GmEncounterOutcome outcome) => flow != null && flow.TryResolve(this, outcome);
    }
}
