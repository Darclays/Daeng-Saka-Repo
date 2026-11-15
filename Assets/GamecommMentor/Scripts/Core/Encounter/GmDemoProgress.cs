using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    public enum GmDemoStage { Sorelia, GuaLembayung, Selesai }

    [DisallowMultipleComponent]
    public sealed class GmDemoProgress : MonoBehaviour
    {
        [SerializeField] private GmEnemyEncounter poppoEncounter;
        [SerializeField] private GmEnemyEncounter pakandeEncounter;

        public GmDemoStage Stage
        {
            get
            {
                if (pakandeEncounter != null && pakandeEncounter.IsDefeated) return GmDemoStage.Selesai;
                if (poppoEncounter != null && poppoEncounter.IsDefeated) return GmDemoStage.GuaLembayung;
                return GmDemoStage.Sorelia;
            }
        }

        public void Configure(GmEnemyEncounter poppo, GmEnemyEncounter pakande)
        {
            poppoEncounter = poppo;
            pakandeEncounter = pakande;
        }
    }
}
