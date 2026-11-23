using System;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [CreateAssetMenu(menuName = "Daeng Saka/Nenek Pakande Battle Profile")]
    public sealed class GmBossBattleProfile : ScriptableObject
    {
        private const int DefaultGreaterShadowDamage = 42;

        [SerializeField, Min(1)] private int bossHp = 260;
        [SerializeField, Range(2, 99)] private int phaseTwoAt = 70;
        [SerializeField, Range(1, 98)] private int phaseThreeAt = 45;
        [Tooltip("Use three equal health segments. Damage stops at each phase boundary.")]
        [SerializeField] private bool splitHealthIntoThirds;
        [SerializeField, Min(1)] private int minionHp = 22;
        [SerializeField, Min(1)] private int minionDamage = 13;
        [SerializeField, Min(1)] private int basicAttackDamage = 24;
        [SerializeField, Min(1)] private int boltDamage = 20;
        [SerializeField, Min(1)] private int waveDamage = 14;
        [SerializeField, Min(1)] private int airbornePoisonDamage = 4;
        [SerializeField, Min(1)] private int shadowDamage = 24;
        [SerializeField, Min(1)] private int fireRainDamage = 30;
        [SerializeField, Min(1)] private int greaterShadowDamage = DefaultGreaterShadowDamage;
        [SerializeField, Range(0, 90)] private int airborneBasicReduction;

        internal int PhaseTwoAt => Math.Max(2, Math.Min(99, phaseTwoAt));
        internal int PhaseThreeAt => Math.Max(1, Math.Min(PhaseTwoAt - 1, phaseThreeAt));
        internal bool HasHealthSegments => splitHealthIntoThirds && bossHp >= 3;
        internal int BoltDamage => Math.Max(1, boltDamage);
        internal int WaveDamage => Math.Max(1, waveDamage);
        internal int AirbornePoisonDamage => Math.Max(1, airbornePoisonDamage);
        internal int ShadowDamage => Math.Max(1, shadowDamage);
        internal int FireRainDamage => Math.Max(1, fireRainDamage);
        internal int GreaterShadowDamage => greaterShadowDamage > 0 ? greaterShadowDamage : DefaultGreaterShadowDamage;
        internal int AirborneBasicReduction => Math.Max(0, Math.Min(90, airborneBasicReduction));

        internal GmBattleStats[] CreateRoster() => new[]
        {
            new GmBattleStats("Nenek Pakande", bossHp, 0, 0, 25, Math.Max(1, basicAttackDamage), isBoss: true),
            new GmBattleStats("Poppo A", minionHp, 0, 0, 16, minionDamage, enemyKind: GmBattleEnemyKind.Poppo),
            new GmBattleStats("Poppo B", minionHp, 0, 0, 15, minionDamage, enemyKind: GmBattleEnemyKind.Poppo)
        };
    }
}
