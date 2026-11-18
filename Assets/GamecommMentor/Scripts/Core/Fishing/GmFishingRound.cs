using System;

namespace GamecommMentor.DaengSaka
{
    internal enum GmFishingPhase { Ready, Waiting, Bite, Reeling, Caught, Escaped }

    internal sealed class GmFishingSpecies
    {
        internal static readonly GmFishingSpecies Wader = new GmFishingSpecies(GmItemDefinition.Wader, "Umum", 1.75f, .29f, 1.12f);
        internal static readonly GmFishingSpecies Nila = new GmFishingSpecies(GmItemDefinition.Nila, "Tidak umum", 2.65f, .42f, .97f);
        internal static readonly GmFishingSpecies Gabus = new GmFishingSpecies(GmItemDefinition.Gabus, "Langka", 3.35f, .57f, .78f);
        internal readonly GmItemDefinition Item;
        internal readonly string Rarity;
        internal readonly float Response, Jump, Interval;

        private GmFishingSpecies(GmItemDefinition item, string rarity, float response, float jump, float interval)
        {
            Item = item;
            Rarity = rarity;
            Response = response;
            Jump = jump;
            Interval = interval;
        }

        internal static GmFishingSpecies Pick(bool firstCatch, int roll) =>
            firstCatch || roll < 70 ? Wader : roll < 95 ? Nila : Gabus;
    }

    internal sealed class GmFishingRound
    {
        internal const float BarHeight = .28f;
        private readonly Random random;
        private float remaining, velocity, target, untilMove, grace;
        private bool collected;
        internal GmFishingPhase Phase { get; private set; }
        internal GmFishingSpecies Species { get; private set; }
        internal float Elapsed { get; private set; }
        internal float Fish { get; private set; }
        internal float Bar { get; private set; }
        internal float Progress { get; private set; }
        internal bool InBar => Fish >= Bar && Fish <= Bar + BarHeight;
        internal bool MissedBite { get; private set; }
        internal bool EarlyPull { get; private set; }

        internal GmFishingRound(Random random) => this.random = random;

        internal void Press(bool firstCatch)
        {
            switch (Phase)
            {
                case GmFishingPhase.Waiting:
                    EarlyPull = true;
                    Enter(GmFishingPhase.Ready);
                    break;
                case GmFishingPhase.Bite:
                    Species = GmFishingSpecies.Pick(firstCatch, random.Next(100));
                    Bar = .02f;
                    Fish = .21f;
                    target = .45f;
                    velocity = 0f;
                    untilMove = 1.66f;
                    grace = .5f;
                    Progress = .30f;
                    Enter(GmFishingPhase.Reeling);
                    break;
                case GmFishingPhase.Ready:
                case GmFishingPhase.Escaped:
                case GmFishingPhase.Caught:
                    Species = null;
                    collected = EarlyPull = MissedBite = false;
                    remaining = 1.35f + Sample() * .75f;
                    Enter(GmFishingPhase.Waiting);
                    break;
            }
        }

        internal void Tick(float delta, bool lifting)
        {
            // Substeps keep bar acceleration comparable across Windows frame rates.
            float time = Math.Min(.20f, delta);
            while (time > 0f)
            {
                float dt = Math.Min(time, 1f / 60f);
                time -= dt;
                Elapsed += dt;
                if (Phase == GmFishingPhase.Reeling) Reel(dt, lifting);
                else if (Phase == GmFishingPhase.Waiting || Phase == GmFishingPhase.Bite)
                {
                    remaining -= dt;
                    if (remaining > 0f) continue;
                    if (Phase == GmFishingPhase.Waiting)
                    {
                        remaining = 2.75f;
                        Enter(GmFishingPhase.Bite);
                    }
                    else
                    {
                        MissedBite = true;
                        Enter(GmFishingPhase.Escaped);
                    }
                }
            }
        }

        private void Reel(float dt, bool lifting)
        {
            velocity += (lifting ? 3.25f : -3.05f) * dt;
            velocity = Clamp(velocity * (float)Math.Exp(-2.45f * dt), -.76f, .76f);
            float next = Bar + velocity * dt;
            Bar = Clamp(next, 0f, 1f - BarHeight);
            if (next < 0f) velocity = Math.Max(0f, -velocity * .18f);
            if (next > 1f - BarHeight) velocity = 0f;

            grace -= dt;
            if (grace <= 0f)
            {
                untilMove -= dt;
                if (untilMove <= 0f)
                {
                    bool up = Fish < .24f || (Fish <= .76f && Sample() > .5f);
                    float distance = Species.Jump * (.55f + Sample() * .50f);
                    target = Clamp(Fish + (up ? distance : -distance), .08f, .92f);
                    untilMove = Species.Interval + Sample() * .65f;
                }
                Fish += (target - Fish) * (1f - (float)Math.Exp(-Species.Response * dt));
            }

            Progress = Clamp(Progress + (InBar ? .265f : -.15f) * dt, 0f, 1f);
            if (Progress >= 1f) Enter(GmFishingPhase.Caught);
            else if (Progress <= 0f) Enter(GmFishingPhase.Escaped);
        }

        internal bool TryCollect(GmPartyInventory inventory)
        {
            if (Phase != GmFishingPhase.Caught || collected || !inventory.TryAdd(Species.Item)) return false;
            collected = true;
            return true;
        }

        private void Enter(GmFishingPhase phase) { Phase = phase; Elapsed = 0f; }
        private float Sample() => (float)random.NextDouble();
        private static float Clamp(float value, float min, float max) => Math.Min(max, Math.Max(value, min));
    }
}
