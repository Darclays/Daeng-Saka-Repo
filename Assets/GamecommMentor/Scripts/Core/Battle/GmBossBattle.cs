using System;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBossBattle
    {
        private static readonly GmBattleMove Summon = new GmBattleMove(
            "Panggil Poppo", GmBattleTarget.Self, GmBattleEffect.Summon, 0,
            animation: "pakandeSummon", enemyOnly: true);
        private static readonly GmBattleMove Rally = new GmBattleMove(
            "Perkuat Poppo", GmBattleTarget.AllAllies, GmBattleEffect.Buff, 0,
            buff: GmBattleBuff.Attack, buffPercent: 25, buffRounds: 1,
            animation: "pakandeSummon", impact: "grimoireImpact", enemyOnly: true);
        private static readonly GmBattleMove Weaken = new GmBattleMove(
            "Kutukan Pakande", GmBattleTarget.AllEnemies, GmBattleEffect.Buff, 0,
            buff: GmBattleBuff.Weaken, buffPercent: 20, buffRounds: 2, impact: "pakandeCurse",
            enemyOnly: true, freeAction: true);
        private static readonly GmBattleMove Fury = new GmBattleMove(
            "Amarah Pakande", GmBattleTarget.Self, GmBattleEffect.Buff, 0,
            buff: GmBattleBuff.Attack, buffPercent: 20, buffRounds: 2,
            animation: "pakandeCharge", impact: "legacyImpact", enemyOnly: true, freeAction: true);
        private static readonly GmBattleMove Charge = new GmBattleMove(
            "Charge Hujan Api", GmBattleTarget.Self, GmBattleEffect.Charge, 0,
            animation: "pakandeCharge", enemyOnly: true);

        private readonly GmBossBattleProfile profile;
        private readonly GmBattleUnit[] units;
        private readonly GmBattleStats[] roster;
        private readonly GmBattleMove bolt;
        private readonly GmBattleMove wave;
        private readonly GmBattleMove shadow;
        private readonly GmBattleMove fireRain;
        private bool firstPhaseThreeAction = true;
        private bool chargeNext;

        public GmBossBattle(GmBossBattleProfile profile, GmBattleStats[] roster, GmBattleUnit[] units, int index)
        {
            this.profile = profile;
            this.roster = roster;
            this.units = units;
            Index = index;
            bolt = new GmBattleMove("Bola Bayangan", GmBattleTarget.Enemy, GmBattleEffect.Damage, profile.BoltDamage,
                animation: "pakandeBolt", impact: "pakandeBoltImpact", enemyOnly: true);
            wave = new GmBattleMove("Gelombang Bayangan", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, profile.WaveDamage,
                animation: "pakandeBolt", impact: "pakandeBoltImpact", enemyOnly: true);
            shadow = new GmBattleMove("Goresan Bayangan", GmBattleTarget.Enemy, GmBattleEffect.Damage, profile.ShadowDamage,
                animation: "pakandeShadow", impact: "pakandeShadowImpact", enemyOnly: true);
            fireRain = new GmBattleMove("Hujan Api", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, profile.FireRainDamage,
                animation: "pakandeFireRain", impact: "pakandeFireImpact", impactScale: 0.82f, enemyOnly: true);
            DespawnMinions();
        }

        public int Index { get; }
        public int Phase { get; private set; } = 1;
        public bool IsCharging { get; private set; }
        public bool WasAirborne { get; private set; }
        public GmBattleMove FireRain => fireRain;

        public void UpdatePhase()
        {
            if (Phase == 3) return;
            GmBattleUnit boss = units[Index];
            long health = (long)boss.Hp * 100;
            int phase = health <= (long)boss.MaxHp * profile.PhaseThreeAt ? 3
                : health <= (long)boss.MaxHp * profile.PhaseTwoAt ? 2 : 1;
            if (phase <= Phase) return;
            WasAirborne = Phase == 2;
            Phase = phase;
            DespawnMinions();
        }

        public GmBattleMove ChooseMove(Random random, out GmBattleMove support)
        {
            support = null;
            if (Phase == 1)
            {
                for (int index = Index + 1; index < units.Length; index++)
                    if (!units[index].IsAlive) return Summon;
                return Rally;
            }
            if (Phase == 2)
            {
                support = Weaken;
                return random.Next(2) == 0 ? bolt : wave;
            }
            if (IsCharging) return fireRain;
            support = firstPhaseThreeAction || random.Next(2) == 0 ? Fury : Weaken;
            return chargeNext ? Charge : shadow;
        }

        public void ResolveMove(GmBattleMove move)
        {
            if (move == Summon)
            {
                for (int index = Index + 1; index < units.Length; index++)
                    if (!units[index].IsAlive) units[index] = new GmBattleUnit(roster[index - Index], false);
            }
            if (Phase != 3 || move.IsFreeAction) return;
            firstPhaseThreeAction = false;
            IsCharging = move == Charge;
            chargeNext = move == shadow;
        }

        public void CancelCharge() => IsCharging = false;

        public void DespawnMinions()
        {
            for (int index = Index + 1; index < units.Length; index++) units[index].IsPresent = false;
        }
    }
}
