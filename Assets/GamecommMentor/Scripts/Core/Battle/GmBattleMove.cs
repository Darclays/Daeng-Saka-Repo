using System;
using System.Collections.Generic;
using System.Linq;

namespace GamecommMentor.DaengSaka
{
    internal enum GmBattleCharacter { Unspecified, Saka, Pacca, Daung }
    internal enum GmBattleTarget { Enemy, AllEnemies, Ally, AllAllies, Self }
    internal enum GmBattleEffect { Damage, StunDamage, Heal, HealPercent, RestoreMp, Buff, Summon, Charge, Poison, RestoreVitals }
    internal enum GmBattleBuff { None, Attack, Defense, Weaken }
    internal enum GmBattleCleanse { None, DefenseDown, AllNegative }

    internal sealed class GmBattleMove
    {
        public static readonly GmBattleMove BasicAttack = new GmBattleMove(
            "Basic Attack", GmBattleTarget.Enemy, GmBattleEffect.Damage, 0);
        public static readonly GmBattleMove GBite = new GmBattleMove(
            "GBite", GmBattleTarget.Enemy, GmBattleEffect.Damage, 12, cooldown: 1,
            animation: "poppoBite", impact: "poppoBiteImpact", enemyOnly: true, poisonTurns: 2, poisonDamage: 4, audioKey: "GBite");
        public static readonly GmBattleMove HammerSweep = new GmBattleMove(
            "Hammer Sweep", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, 24, cooldown: 1,
            buff: GmBattleBuff.Defense, buffPercent: -20, buffRounds: 2, animation: "babiRusaHammerSweep", enemyOnly: true,
            audioKey: "HammerSweep");
        public static readonly GmBattleMove Uppercut = new GmBattleMove(
            "Uppercut", GmBattleTarget.Enemy, GmBattleEffect.StunDamage, 0, cooldown: 2,
            animation: "manguniUppercut", enemyOnly: true, audioKey: "Uppercut");
        public static readonly GmBattleMove TailSweep = new GmBattleMove(
            "Tail Sweep", GmBattleTarget.Enemy, GmBattleEffect.Damage, 20, cooldown: 1,
            animation: "laWunaTailSweep", enemyOnly: true, audioKey: "TailSweep");
        public static readonly GmBattleMove Glare = new GmBattleMove(
            "Glare", GmBattleTarget.Enemy, GmBattleEffect.Poison, 7, cooldown: 2,
            animation: "laWunaGlare", impact: "glareImpact", impactScale: 0.75f, enemyOnly: true, poisonTurns: 2, audioKey: "Glare");
        public static readonly GmBattleMove CaneStrike = new GmBattleMove(
            "Cane Strike", GmBattleTarget.Enemy, GmBattleEffect.Damage, 22, cooldown: 3,
            animation: "pakandeCane", impact: "shieldImpact", impactScale: 0.72f, enemyOnly: true, audioKey: "CaneStrike");
        public static readonly GmBattleMove TideResonance = new GmBattleMove(
            "Tide Resonance", GmBattleTarget.Enemy, GmBattleEffect.Damage, 36, 7, 3,
            animation: "tideCast", impact: "tideImpact", audioKey: "TideResonance");
        public static readonly GmBattleMove SharkBite = new GmBattleMove(
            "Shark Bite", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, 18, 6, 2,
            animation: "sakaSkill", impact: "sharkImpact", audioKey: "SharkBite");
        public static readonly GmBattleMove LegacyOfBarani = new GmBattleMove(
            "Legacy of Barani", GmBattleTarget.Self, GmBattleEffect.Buff, 0, 4, 3,
            buff: GmBattleBuff.Attack, buffPercent: 35, buffRounds: 3, animation: "legacyCast", impact: "legacyImpact",
            audioKey: "LegacyOfBarani");
        public static readonly GmBattleMove ShieldSwing = new GmBattleMove(
            "Shield Swing", GmBattleTarget.Enemy, GmBattleEffect.StunDamage, 12, 5, 2,
            bossStunChance: 50, animation: "paccaSkill", impact: "shieldImpact", audioKey: "ShieldSwing");
        public static readonly GmBattleMove BattleAura = new GmBattleMove(
            "Battle Aura", GmBattleTarget.AllAllies, GmBattleEffect.Buff, 0, 5, 3,
            buff: GmBattleBuff.Defense, buffPercent: 30, buffRounds: 3, animation: "auraCast", impact: "shieldImpact",
            cleanse: GmBattleCleanse.DefenseDown, audioKey: "BattleAura");
        public static readonly GmBattleMove DominionStrike = new GmBattleMove(
            "Dominion Strike", GmBattleTarget.Enemy, GmBattleEffect.Damage, 26, 5, 5,
            buff: GmBattleBuff.Defense, buffPercent: -25, buffRounds: 2, animation: "dominionCast", impact: "dominionImpact",
            audioKey: "DominionStrike");
        public static readonly GmBattleMove RayOfHealing = new GmBattleMove(
            "Ray of Healing", GmBattleTarget.Ally, GmBattleEffect.Heal, 40, 5,
            animation: "daungSkill", impact: "healingImpact", cleanse: GmBattleCleanse.AllNegative, audioKey: "RayOfHealing");
        public static readonly GmBattleMove GrimoireInvocation = new GmBattleMove(
            "Grimoire Invocation", GmBattleTarget.AllAllies, GmBattleEffect.Buff, 0, 6, 5,
            buff: GmBattleBuff.Attack, buffPercent: 30, buffRounds: 3, animation: "grimoireCast", impact: "grimoireImpact",
            audioKey: "GrimoireInvocation");
        public static readonly GmBattleMove SanctuaryBloom = new GmBattleMove(
            "Sanctuary Bloom", GmBattleTarget.AllAllies, GmBattleEffect.HealPercent, 50, 12, 5,
            animation: "sanctuaryCast", impact: "sanctuaryImpact", impactScale: 0.43f, cleanse: GmBattleCleanse.AllNegative,
            audioKey: "SanctuaryBloom");
        public static readonly GmBattleMove HpItem = new GmBattleMove(
            GmItemDefinition.HpPotion.Name, GmBattleTarget.Ally, GmBattleEffect.Heal, GmItemDefinition.HpPotion.HpRecovery,
            item: GmItemDefinition.HpPotion, impact: "healingImpact", audioKey: "PotionHp");
        public static readonly GmBattleMove MpItem = new GmBattleMove(
            GmItemDefinition.MpPotion.Name, GmBattleTarget.Ally, GmBattleEffect.RestoreMp, GmItemDefinition.MpPotion.MpRecovery,
            item: GmItemDefinition.MpPotion, audioKey: "PotionMp");
        public static readonly GmBattleMove GrilledWader = new GmBattleMove(
            GmItemDefinition.GrilledWader.Name, GmBattleTarget.Ally, GmBattleEffect.Heal, GmItemDefinition.GrilledWader.HpRecovery,
            item: GmItemDefinition.GrilledWader, impact: "healingImpact", audioKey: "FoodWader");
        public static readonly GmBattleMove GrilledNila = new GmBattleMove(
            GmItemDefinition.GrilledNila.Name, GmBattleTarget.Ally, GmBattleEffect.RestoreMp, GmItemDefinition.GrilledNila.MpRecovery,
            item: GmItemDefinition.GrilledNila, audioKey: "FoodNila");
        public static readonly GmBattleMove GrilledGabus = new GmBattleMove(
            GmItemDefinition.GrilledGabus.Name, GmBattleTarget.AllAllies, GmBattleEffect.RestoreVitals, 0,
            item: GmItemDefinition.GrilledGabus, impact: "healingImpact", cleanse: GmBattleCleanse.AllNegative,
            audioKey: "FoodGabus");
        public static IReadOnlyList<GmBattleMove> Items { get; } =
            Array.AsReadOnly(new[] { HpItem, MpItem, GrilledWader, GrilledNila, GrilledGabus });
        internal static IReadOnlyList<GmBattleMove> Potions { get; } =
            Array.AsReadOnly(Items.Where(move => !move.IsFood).ToArray());
        internal static IReadOnlyList<GmBattleMove> Food { get; } =
            Array.AsReadOnly(Items.Where(move => move.IsFood).ToArray());

        private static readonly IReadOnlyList<GmBattleMove> SakaSkills = Array.AsReadOnly(new[] { TideResonance, SharkBite, LegacyOfBarani });
        private static readonly IReadOnlyList<GmBattleMove> PaccaSkills = Array.AsReadOnly(new[] { ShieldSwing, BattleAura, DominionStrike });
        private static readonly IReadOnlyList<GmBattleMove> DaungSkills = Array.AsReadOnly(new[] { RayOfHealing, GrimoireInvocation, SanctuaryBloom });

        public GmBattleMove(string name, GmBattleTarget target, GmBattleEffect effect,
            int amount, int mpCost = 0, int cooldown = 0, GmItemDefinition item = null,
            GmBattleBuff buff = GmBattleBuff.None, int buffPercent = 0, int buffRounds = 0, int bossStunChance = 100,
            string animation = null, string impact = null, float impactScale = 0.94f,
            bool enemyOnly = false, bool freeAction = false, int poisonTurns = 0,
            GmBattleCleanse cleanse = GmBattleCleanse.None, int poisonDamage = 0, string audioKey = null)
        {
            Name = name;
            Target = target;
            Effect = effect;
            Amount = amount;
            MpCost = mpCost;
            Cooldown = cooldown;
            Item = item;
            Buff = buff;
            BuffPercent = buffPercent;
            BuffRounds = buffRounds;
            BossStunChance = bossStunChance;
            AnimationKey = animation;
            ImpactKey = impact;
            AudioKey = audioKey;
            ImpactScale = impactScale;
            IsEnemyMove = enemyOnly;
            IsFreeAction = freeAction;
            PoisonTurns = poisonTurns;
            PoisonDamage = effect == GmBattleEffect.Poison ? amount : poisonDamage;
            Cleanse = cleanse;
        }

        public string Name { get; }
        public GmBattleTarget Target { get; }
        public GmBattleEffect Effect { get; }
        public int Amount { get; }
        public int PoisonTurns { get; }
        public int PoisonDamage { get; }
        public bool InflictsPoison => PoisonDamage > 0 && PoisonTurns > 0;
        public int MpCost { get; }
        public int Cooldown { get; }
        public GmItemDefinition Item { get; }
        public bool IsItem => Item != null;
        internal bool IsFood => Item?.Ingredient != null;
        public GmBattleBuff Buff { get; }
        public int BuffPercent { get; }
        public int BuffRounds { get; }
        public GmBattleCleanse Cleanse { get; }
        public int BossStunChance { get; }
        public string AnimationKey { get; }
        public string ImpactKey { get; }
        internal string AudioKey { get; }
        public float ImpactScale { get; }
        public bool IsEnemyMove { get; }
        public bool IsFreeAction { get; }
        public bool TargetsAll => Target == GmBattleTarget.AllEnemies || Target == GmBattleTarget.AllAllies;
        public bool NeedsConfirm => TargetsAll || Target == GmBattleTarget.Self;
        public bool IsDamaging => Effect == GmBattleEffect.Damage || Effect == GmBattleEffect.StunDamage;
        public bool IsNegativeBuff => Buff == GmBattleBuff.Weaken ? BuffPercent > 0
            : Buff != GmBattleBuff.None && BuffPercent < 0;
        public bool AppliesDebuffOnHit => IsDamaging && IsNegativeBuff;

        // Raw power before attack buffs, weaken, guard, or the target's remaining HP.
        internal int PowerWithWeapon(int baseAttack, int weaponBonus)
        {
            if (!IsDamaging || IsItem) return Amount;
            int power = this == BasicAttack ? baseAttack : Amount;
            return (int)Math.Min(int.MaxValue, (long)power + Math.Max(0, weaponBonus));
        }

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
        public readonly bool DebuffApplied;
        public readonly int CleansedCount;
        public readonly bool Poisoned;
        public readonly int MpRestored;

        public GmBattleHit(int index, int amount, bool stunned, bool debuffApplied = false, int cleansedCount = 0,
            bool poisoned = false, int mpRestored = 0)
        {
            Index = index;
            Amount = amount;
            Stunned = stunned;
            DebuffApplied = debuffApplied;
            CleansedCount = cleansedCount;
            Poisoned = poisoned;
            MpRestored = mpRestored;
        }
    }
}
