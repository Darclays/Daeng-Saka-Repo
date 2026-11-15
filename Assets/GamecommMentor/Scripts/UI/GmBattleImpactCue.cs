namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBattleImpactCue
    {
        // These are zero-based frames in the existing battle sprite strips.
        private static readonly GmBattleImpactCue SakaBasic = new GmBattleImpactCue(5, 2, 0, "basicHit", 12f);
        private static readonly GmBattleImpactCue PaccaBasic = new GmBattleImpactCue(6, 4, 0, "basicHit", 14f);
        private static readonly GmBattleImpactCue DaungBasic = new GmBattleImpactCue(5, 3, 0, "basicHit", 10f);
        private static readonly GmBattleImpactCue Shield = new GmBattleImpactCue(6, 4, 0, "shieldHit", 22f, 4f, 0.05f);
        private static readonly GmBattleImpactCue Shark = new GmBattleImpactCue(9, 6, 2, "sharkHit", 17f, 3f, 0.04f);
        private static readonly GmBattleImpactCue Bite = new GmBattleImpactCue(7, 5, 2, "basicHit", 16f);
        private static readonly GmBattleImpactCue Cane = new GmBattleImpactCue(5, 3, 0, "basicHit", 16f);

        private GmBattleImpactCue(int contact, int windup, int effectLead, string sound, float recoil,
            float shake = 0f, float hitStop = 0f)
        {
            Contact = contact;
            Windup = windup;
            EffectLead = effectLead;
            Sound = sound;
            Recoil = recoil;
            Shake = shake;
            HitStop = hitStop;
        }

        public int Contact { get; }
        public int Windup { get; }
        public int EffectLead { get; }
        public string Sound { get; }
        public float Recoil { get; }
        public float Shake { get; }
        public float HitStop { get; }

        public static GmBattleImpactCue For(GmBattleMove move, GmBattleCharacter character)
        {
            if (move == GmBattleMove.ShieldSwing) return Shield;
            if (move == GmBattleMove.SharkBite) return Shark;
            if (move == GmBattleMove.GBite) return Bite;
            if (move == GmBattleMove.CaneStrike) return Cane;
            if (move != GmBattleMove.BasicAttack) return null;
            return character switch
            {
                GmBattleCharacter.Saka => SakaBasic,
                GmBattleCharacter.Pacca => PaccaBasic,
                GmBattleCharacter.Daung => DaungBasic,
                _ => null
            };
        }
    }

    internal readonly struct GmBattleActionFrame
    {
        public GmBattleActionFrame(int index, int count, int contact, float position, float hitAge,
            GmBattleImpactCue cue = null, bool enhanced = false)
        {
            Index = index;
            Count = count;
            Contact = contact;
            Position = position;
            HitAge = hitAge;
            Cue = cue;
            Enhanced = enhanced;
        }

        public int Index { get; }
        public int Count { get; }
        public int Contact { get; }
        public float Position { get; }
        public float HitAge { get; }
        public GmBattleImpactCue Cue { get; }
        public bool Enhanced { get; }
    }
}
