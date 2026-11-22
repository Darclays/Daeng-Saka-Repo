using System;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBossBattle
    {
        public const int AirbornePoisonTurns = 2;
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
            "Charge Greater Shadow", GmBattleTarget.Self, GmBattleEffect.Charge, 0,
            animation: "pakandeCharge", enemyOnly: true);

        private readonly GmBossBattleProfile profile;
        private readonly GmBattleUnit[] units;
        private readonly GmBattleStats[] roster;
        private readonly GmBattleMove bolt;
        private readonly GmBattleMove magicCharge;
        private readonly GmBattleMove shadow;
        private readonly GmBattleMove fireRain;
        private readonly GmBattleMove greaterShadow;
        private bool firstPhaseThreeAction = true;
        private bool chargeNext;

        public GmBossBattle(GmBossBattleProfile profile, GmBattleStats[] roster, GmBattleUnit[] units, int index)
        {
            this.profile = profile;
            this.roster = roster;
            this.units = units;
            Index = index;
            bolt = new GmBattleMove("Bola Bayangan", GmBattleTarget.Enemy, GmBattleEffect.Damage, profile.BoltDamage,
                animation: "pakandeBolt", impact: "pakandeBoltImpact", enemyOnly: true,
                poisonTurns: AirbornePoisonTurns, poisonDamage: profile.AirbornePoisonDamage);
            magicCharge = new GmBattleMove("Magic Charge", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, profile.WaveDamage,
                cooldown: 2, animation: "pakandeBolt", impact: "pakandeMagicImpact", enemyOnly: true,
                poisonTurns: AirbornePoisonTurns, poisonDamage: profile.AirbornePoisonDamage);
            shadow = new GmBattleMove("Serangan Bayangan", GmBattleTarget.Enemy, GmBattleEffect.Damage, profile.ShadowDamage,
                animation: "pakandeCharge", impact: "pakandeBoltImpact", enemyOnly: true);
            fireRain = new GmBattleMove("Hujan Api", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, profile.FireRainDamage,
                cooldown: 3, animation: "pakandeFireRain", impact: "pakandeFireImpact", impactScale: 0.82f, enemyOnly: true);
            greaterShadow = new GmBattleMove("Greater Shadow", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, profile.GreaterShadowDamage,
                cooldown: 4, animation: "pakandeShadow", impact: "pakandeShadowImpact", impactScale: 0.54f, enemyOnly: true);
            DespawnMinions();
        }

        public int Index { get; }
        public int Phase { get; private set; } = 1;
        public bool IsCharging { get; private set; }
        public bool WasAirborne { get; private set; }
        public GmBattleMove FireRain => fireRain;
        public GmBattleMove ChargedMove => IsCharging ? greaterShadow : null;
        public int BasicAttackReduction => Phase == 2 ? profile.AirborneBasicReduction : 0;
        public GmBattleMove AirborneMove => units[Index].CooldownRemaining(magicCharge) == 0 ? magicCharge : bolt;

        public int ChargeStunTarget
        {
            get
            {
                for (int index = 0; index < Index; index++)
                    if (units[index].IsAlive && units[index].Character == GmBattleCharacter.Saka) return index;
                return -1;
            }
        }

        public bool SupportsMove(GmBattleMove move) => Phase switch
        {
            1 => move == Summon || move == Rally || move == GmBattleMove.CaneStrike,
            2 => move == Weaken || move == bolt || move == magicCharge,
            _ when IsCharging => move == greaterShadow,
            _ => move == Fury || move == Weaken || move == shadow || move == fireRain ||
                (move == Charge && units[Index].CooldownRemaining(greaterShadow) == 0)
        };

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
            GmBattleUnit boss = units[Index];
            if (Phase == 1)
            {
                for (int index = Index + 1; index < units.Length; index++)
                    if (!units[index].IsAlive) return Summon;
                // Keep the first Rally after the minions have attacked in the opening.
                return boss.CompletedActions > 1 && boss.CooldownRemaining(GmBattleMove.CaneStrike) == 0
                    ? GmBattleMove.CaneStrike : Rally;
            }
            if (Phase == 2)
            {
                GmBattleMove move = AirborneMove;
                if (move == magicCharge) support = Weaken;
                return move;
            }
            if (IsCharging) return greaterShadow;
            if ((firstPhaseThreeAction || chargeNext) && boss.CooldownRemaining(greaterShadow) == 0)
            {
                support = Fury;
                return Charge;
            }
            support = random.Next(2) == 0 ? Fury : Weaken;
            return boss.CooldownRemaining(fireRain) == 0 ? fireRain : shadow;
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
            chargeNext = move == shadow || move == fireRain;
        }

        public void CancelCharge() => IsCharging = false;

        public void DespawnMinions()
        {
            for (int index = Index + 1; index < units.Length; index++) units[index].IsPresent = false;
        }
    }
}
