using System;
using ProjectVN.Presentation.Actors;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [CreateAssetMenu(menuName = "Daeng Saka/Presentasi Fase Pakande")]
    public sealed class GmPakandePhasePresentation : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/BattleStage/PresentasiFasePakande";

        [Serializable]
        internal struct Line
        {
            public VNActorVisualProfile portrait;
            public string speaker;
            public string text;
        }

        [SerializeField] private Line[] opening = Array.Empty<Line>();
        [SerializeField] private Line[] afterSummon = Array.Empty<Line>();
        [SerializeField] private Line[] airborne = Array.Empty<Line>();
        [SerializeField] private AudioClip roar;

        internal Line[] Opening => opening;
        internal Line[] AfterSummon => afterSummon;
        internal Line[] Airborne => airborne;
        internal AudioClip Roar => roar;
    }
}
