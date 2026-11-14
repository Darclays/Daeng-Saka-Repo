namespace GamecommMentor.DaengSaka
{
    internal enum GmBattleCharacter { Unspecified, Saka, Pacca, Daung }
    internal enum GmBattleTarget { Enemy, AllEnemies, Ally }
    internal enum GmBattleEffect { Damage, StunDamage, Heal, RestoreMp }

    internal sealed class GmBattleMove
    {
        public static readonly GmBattleMove BasicAttack = new GmBattleMove(
            "Basic Attack", GmBattleTarget.Enemy, GmBattleEffect.Damage, 0);
        public static readonly GmBattleMove SharkBite = new GmBattleMove(
            "Shark Bite", GmBattleTarget.AllEnemies, GmBattleEffect.Damage, 18, 6, 2);
        public static readonly GmBattleMove ShieldSwing = new GmBattleMove(
            "Shield Swing", GmBattleTarget.Enemy, GmBattleEffect.StunDamage, 12, 5, 2);
        public static readonly GmBattleMove RayOfHealing = new GmBattleMove(
            "Ray of Healing", GmBattleTarget.Ally, GmBattleEffect.Heal, 20, 5);
        public static readonly GmBattleMove HpItem = new GmBattleMove(
            "Ramuan HP", GmBattleTarget.Ally, GmBattleEffect.Heal, 25, isItem: true);
        public static readonly GmBattleMove MpItem = new GmBattleMove(
            "Ramuan MP", GmBattleTarget.Ally, GmBattleEffect.RestoreMp, 8, isItem: true);

        private GmBattleMove(string name, GmBattleTarget target, GmBattleEffect effect,
            int amount, int mpCost = 0, int cooldown = 0, bool isItem = false)
        {
            Name = name;
            Target = target;
            Effect = effect;
            Amount = amount;
            MpCost = mpCost;
            Cooldown = cooldown;
            IsItem = isItem;
        }

        public string Name { get; }
        public GmBattleTarget Target { get; }
        public GmBattleEffect Effect { get; }
        public int Amount { get; }
        public int MpCost { get; }
        public int Cooldown { get; }
        public bool IsItem { get; }

        public static GmBattleMove ForCharacter(GmBattleCharacter character) => character switch
        {
            GmBattleCharacter.Saka => SharkBite,
            GmBattleCharacter.Pacca => ShieldSwing,
            GmBattleCharacter.Daung => RayOfHealing,
            _ => null
        };
    }

    internal readonly struct GmBattleHit
    {
        public readonly int Index;
        public readonly int Amount;

        public GmBattleHit(int index, int amount)
        {
            Index = index;
            Amount = amount;
        }
    }
}
