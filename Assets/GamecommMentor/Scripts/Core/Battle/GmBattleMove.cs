using System;
using System.Collections.Generic;

namespace GamecommMentor.DaengSaka
{
    internal enum GmBattleCharacter { Unspecified, Saka, Pacca, Daung }
    internal enum GmBattleTarget { Enemy, AllEnemies, Ally, AllAllies, Self }
    internal enum GmBattleEffect { Damage, StunDamage, Heal, HealPercent, RestoreMp, Buff }
    internal enum GmBattleBuff { None, Attack, Defense }

    internal sealed class GmBattleMove
    {
        public static readonly GmBattleMove BasicAttack = new GmBattleMove(
            "Basic Attack", GmBattleTarget.Enemy, GmBattleEffect.Damage, 0);
        public static readonly GmBattleMove TideResonance = new GmBattleMove(
            "Tide Resonance", GmBattleTarget.Enemy, GmBattleEffect.Damage, 36, 7, 3,
            animation: "tideCast", impact: "tideImpact");
        public static readonly GmBattleMove SharkBite = new GmBattleMove(
            "Shark Bite", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, 18, 6, 2,
            animation: "sakaSkill", impact: "sharkImpact");
        public static readonly GmBattleMove LegacyOfBarani = new GmBattleMove(
            "Legacy of Barani", GmBattleTarget.Self, GmBattleEffect.Buff, 0, 4, 3,
            buff: GmBattleBuff.Attack, buffPercent: 35, buffRounds: 3, animation: "legacyCast", impact: "legacyImpact");
        public static readonly GmBattleMove ShieldSwing = new GmBattleMove(
            "Shield Swing", GmBattleTarget.Enemy, GmBattleEffect.StunDamage, 12, 5, 2,
            bossStunChance: 50, animation: "paccaSkill", impact: "shieldImpact");
        public static readonly GmBattleMove BattleAura = new GmBattleMove(
            "Battle Aura", GmBattleTarget.AllAllies, GmBattleEffect.Buff, 0, 5, 3,
            buff: GmBattleBuff.Defense, buffPercent: 30, buffRounds: 3, animation: "auraCast", impact: "shieldImpact");
        public static readonly GmBattleMove DominionStrike = new GmBattleMove(
            "Dominion Strike", GmBattleTarget.Enemy, GmBattleEffect.Damage, 26, 7, 5,
            buff: GmBattleBuff.Defense, buffPercent: 20, buffRounds: 3, animation: "dominionCast", impact: "dominionImpact");
        public static readonly GmBattleMove RayOfHealing = new GmBattleMove(
            "Ray of Healing", GmBattleTarget.Ally, GmBattleEffect.Heal, 20, 5,
            animation: "daungSkill", impact: "healingImpact");
        public static readonly GmBattleMove GrimoireInvocation = new GmBattleMove(
            "Grimoire Invocation", GmBattleTarget.AllAllies, GmBattleEffect.Buff, 0, 6, 5,
            buff: GmBattleBuff.Attack, buffPercent: 30, buffRounds: 3, animation: "grimoireCast", impact: "grimoireImpact");
        public static readonly GmBattleMove SanctuaryBloom = new GmBattleMove(
            "Sanctuary Bloom", GmBattleTarget.AllAllies, GmBattleEffect.HealPercent, 50, 12, 5,
            animation: "sanctuaryCast", impact: "sanctuaryImpact", impactScale: 0.43f);
        public static readonly GmBattleMove HpItem = new GmBattleMove(
            "Ramuan HP", GmBattleTarget.Ally, GmBattleEffect.Heal, 25, isItem: true, impact: "healingImpact");
        public static readonly GmBattleMove MpItem = new GmBattleMove(
            "Ramuan MP", GmBattleTarget.Ally, GmBattleEffect.RestoreMp, 8, isItem: true);

        private static readonly IReadOnlyList<GmBattleMove> SakaSkills = Array.AsReadOnly(new[] { TideResonance, SharkBite, LegacyOfBarani });
        private static readonly IReadOnlyList<GmBattleMove> PaccaSkills = Array.AsReadOnly(new[] { ShieldSwing, BattleAura, DominionStrike });
        private static readonly IReadOnlyList<GmBattleMove> DaungSkills = Array.AsReadOnly(new[] { RayOfHealing, GrimoireInvocation, SanctuaryBloom });

        private GmBattleMove(string name, GmBattleTarget target, GmBattleEffect effect,
            int amount, int mpCost = 0, int cooldown = 0, bool isItem = false,
            GmBattleBuff buff = GmBattleBuff.None, int buffPercent = 0, int buffRounds = 0, int bossStunChance = 100,
            string animation = null, string impact = null, float impactScale = 0.94f)
        {
            Name = name;
            Target = target;
            Effect = effect;
            Amount = amount;
            MpCost = mpCost;
            Cooldown = cooldown;
            IsItem = isItem;
            Buff = buff;
            BuffPercent = buffPercent;
            BuffRounds = buffRounds;
            BossStunChance = bossStunChance;
            AnimationKey = animation;
            ImpactKey = impact;
            ImpactScale = impactScale;
        }

        public string Name { get; }
        public GmBattleTarget Target { get; }
        public GmBattleEffect Effect { get; }
        public int Amount { get; }
        public int MpCost { get; }
        public int Cooldown { get; }
        public bool IsItem { get; }
        public GmBattleBuff Buff { get; }
        public int BuffPercent { get; }
        public int BuffRounds { get; }
        public int BossStunChance { get; }
        public string AnimationKey { get; }
        public string ImpactKey { get; }
        public float ImpactScale { get; }
        public bool TargetsAll => Target == GmBattleTarget.AllEnemies || Target == GmBattleTarget.AllAllies;
        public bool NeedsConfirm => TargetsAll || Target == GmBattleTarget.Self;
        public bool IsDamaging => Effect == GmBattleEffect.Damage || Effect == GmBattleEffect.StunDamage;

        public static IReadOnlyList<GmBattleMove> ForCharacter(GmBattleCharacter character) => character switch
        {
            GmBattleCharacter.Saka => SakaSkills,
            GmBattleCharacter.Pacca => PaccaSkills,
            GmBattleCharacter.Daung => DaungSkills,
            _ => Array.Empty<GmBattleMove>()
        };
    }

    internal readonly struct GmBattleHit
    {
        public readonly int Index;
        public readonly int Amount;
        public readonly bool Stunned;

        public GmBattleHit(int index, int amount, bool stunned)
        {
            Index = index;
            Amount = amount;
            Stunned = stunned;
        }
    }
}
