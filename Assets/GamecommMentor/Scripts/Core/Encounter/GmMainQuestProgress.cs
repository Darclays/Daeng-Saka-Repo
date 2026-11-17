using System.Collections.Generic;
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
        [SerializeField] private bool shareAcrossWorldScenes;
        private static QuestRun worldRun = new QuestRun();
        private readonly QuestRun localRun = new QuestRun();

        private QuestRun Run => shareAcrossWorldScenes ? worldRun : localRun;
        public GmMainQuestStep Step => Run.Step;
        public GmEnemyEncounter PakandeEncounter => pakandeEncounter;
        public GmEnemyEncounter ActiveEncounter => Step switch
        {
            GmMainQuestStep.SelamatkanKeluargaNelayan => soreliaEncounter,
            GmMainQuestStep.SelidikiGua => caveEncounter,
            GmMainQuestStep.LawanPakande => pakandeEncounter,
            _ => null
        };

        public void Configure(GmEnemyEncounter sorelia, GmEnemyEncounter cave, GmEnemyEncounter pakande,
            bool shareAcrossScenes = false)
        {
            soreliaEncounter = sorelia;
            caveEncounter = cave;
            pakandeEncounter = pakande;
            shareAcrossWorldScenes = shareAcrossScenes;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWorldRun() => worldRun = new QuestRun();

        private void Awake()
        {
            RestoreDefeat(soreliaEncounter, GmMainQuestStep.SelamatkanKeluargaNelayan);
            RestoreDefeat(caveEncounter, GmMainQuestStep.SelidikiGua);
            RestoreDefeat(pakandeEncounter, GmMainQuestStep.LawanPakande);
            RefreshCaveEncounters();
        }

        private void RestoreDefeat(GmEnemyEncounter encounter, GmMainQuestStep step)
        {
            if (encounter != null && HasCompleted(step)) encounter.MarkDefeated();
        }

        public bool HasCompleted(GmMainQuestStep step) => Run.Completed.Contains(step);

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

        private void Advance()
        {
            Run.Advance();
            RefreshCaveEncounters();
        }

        private void RefreshCaveEncounters()
        {
            if (!shareAcrossWorldScenes) return;
            SetEncounterPresence(caveEncounter, Step == GmMainQuestStep.SelidikiGua);
            SetEncounterPresence(pakandeEncounter,
                Step == GmMainQuestStep.TemuiPakande || Step == GmMainQuestStep.LawanPakande);
        }

        private static void SetEncounterPresence(GmEnemyEncounter encounter, bool show)
        {
            if (encounter == null) return;
            bool active = show && !encounter.IsDefeated;
            if (encounter.gameObject.activeSelf != active) encounter.gameObject.SetActive(active);
        }

        private sealed class QuestRun
        {
            public GmMainQuestStep Step { get; private set; } = GmMainQuestStep.PersiapanSebelumBertarung;
            public readonly HashSet<GmMainQuestStep> Completed = new HashSet<GmMainQuestStep>();

            public void Advance()
            {
                Completed.Add(Step);
                Step = Step switch
                {
                    GmMainQuestStep.PersiapanSebelumBertarung => GmMainQuestStep.KunjungiMerchant,
                    GmMainQuestStep.KunjungiMerchant => GmMainQuestStep.KunjungiPenempaBesi,
                    GmMainQuestStep.KunjungiPenempaBesi => GmMainQuestStep.TemuiKeluargaNelayan,
                    GmMainQuestStep.TemuiKeluargaNelayan => GmMainQuestStep.SelamatkanKeluargaNelayan,
                    GmMainQuestStep.SelamatkanKeluargaNelayan => GmMainQuestStep.BicaraDenganNelayan,
                    GmMainQuestStep.BicaraDenganNelayan => GmMainQuestStep.MasukGuaLembayung,
                    GmMainQuestStep.MasukGuaLembayung => GmMainQuestStep.SelidikiGua,
                    GmMainQuestStep.SelidikiGua => GmMainQuestStep.TemuiPakande,
                    GmMainQuestStep.TemuiPakande => GmMainQuestStep.LawanPakande,
                    GmMainQuestStep.LawanPakande => GmMainQuestStep.Penutup,
                    _ => GmMainQuestStep.Selesai
                };
            }
        }
    }
}
