using System;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [CreateAssetMenu(menuName = "GamecommMentor/Daeng Saka/Battle/Encounter Roster")]
    public sealed class GmEncounterRoster : ScriptableObject
    {
        [SerializeField] private GmBattleStats[] enemies = Array.Empty<GmBattleStats>();

        public bool HasEnemies => enemies != null && enemies.Length != 0;
        internal GmBattleStats[] Enemies => enemies;
    }
}
