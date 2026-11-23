using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmEncounterBattleRoot : MonoBehaviour
    {
        private GmEncounterFlow flow;

        public GmEnemyEncounter Encounter => flow != null ? flow.CurrentEncounter : null;
        internal string OriginSceneName => flow != null ? flow.OriginSceneName : null;
        public GmSakaMotor Player => flow != null ? flow.CurrentPlayer : null;

        internal void Bind(GmEncounterFlow encounterFlow) => flow = encounterFlow;

        internal GmBattleStats[] WithNearbyEnemies(GmBattleStats[] roster) =>
            flow != null ? flow.WithNearbyEnemies(roster) : roster;

        public bool Resolve(GmEncounterOutcome outcome) => flow != null && flow.TryResolve(this, outcome);
    }
}
