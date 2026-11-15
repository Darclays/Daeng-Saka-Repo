using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    public enum GmMainQuestStep
    {
        PersiapanSebelumBertarung,
        KunjungiMerchant,
        KunjungiPenempaBesi,
        TemuiKeluargaNelayan,
        SelamatkanKeluargaNelayan,
        BicaraDenganNelayan,
        MasukGuaLembayung,
        SelidikiGua,
        TemuiPakande,
        LawanPakande,
        Penutup,
        Selesai
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmEncounterFlow))]
    public sealed class GmMainQuestProgress : MonoBehaviour
    {
        [SerializeField] private GmEnemyEncounter soreliaEncounter;
        [SerializeField] private GmEnemyEncounter caveEncounter;
        [SerializeField] private GmEnemyEncounter pakandeEncounter;

        public GmMainQuestStep Step { get; private set; }
        public GmEnemyEncounter PakandeEncounter => pakandeEncounter;
        public GmEnemyEncounter ActiveEncounter => Step switch
        {
            GmMainQuestStep.SelamatkanKeluargaNelayan => soreliaEncounter,
            GmMainQuestStep.SelidikiGua => caveEncounter,
            GmMainQuestStep.LawanPakande => pakandeEncounter,
            _ => null
        };

        public void Configure(GmEnemyEncounter sorelia, GmEnemyEncounter cave, GmEnemyEncounter pakande)
        {
            soreliaEncounter = sorelia;
            caveEncounter = cave;
            pakandeEncounter = pakande;
        }

        public bool TryCompleteDialogue(GmMainQuestStep completedStep)
        {
            if (Step != completedStep || Step == GmMainQuestStep.Selesai || IsEncounterStep(Step)) return false;

            Advance();
            return true;
        }

        public static bool IsEncounterStep(GmMainQuestStep step) =>
            step == GmMainQuestStep.SelamatkanKeluargaNelayan || step == GmMainQuestStep.SelidikiGua ||
            step == GmMainQuestStep.LawanPakande;

        internal bool CanStartEncounter(GmEnemyEncounter encounter)
        {
            if (!isActiveAndEnabled) return true;
            if (encounter == soreliaEncounter) return Step == GmMainQuestStep.SelamatkanKeluargaNelayan;
            if (encounter == caveEncounter) return Step == GmMainQuestStep.SelidikiGua;
            if (encounter == pakandeEncounter) return Step == GmMainQuestStep.LawanPakande;
            return true;
        }

        internal void OnEncounterWon(GmEnemyEncounter encounter)
        {
            if (!isActiveAndEnabled) return;
            GmEnemyEncounter required = ActiveEncounter;
            if (required != null && required == encounter && required.IsDefeated) Advance();
        }

        private void Advance() => Step = (GmMainQuestStep)((int)Step + 1);
    }
}
