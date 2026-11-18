using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmPartyVitals
    {
        public string Name { get; private set; }
        public int MaxHp { get; private set; }
        public int MaxMp { get; private set; }
        private int hp, mp;
        private bool useAuthoredInitialHp, useAuthoredInitialMp;

        public int Hp
        {
            get => hp;
            set { hp = Clamp(value, MaxHp); useAuthoredInitialHp = false; }
        }

        public int Mp
        {
            get => mp;
            set { mp = Clamp(value, MaxMp); useAuthoredInitialMp = false; }
        }

        internal GmPartyVitals(GmBattleStats stats, bool party, bool awaitingSceneStats = false)
        {
            SetLimits(stats, party);
            hp = MaxHp;
            mp = Clamp(stats.startingMp, MaxMp);
            useAuthoredInitialHp = useAuthoredInitialMp = awaitingSceneStats;
        }

        internal void Bind(GmBattleStats stats)
        {
            SetLimits(stats, true);
            // Only untouched starter values may take the first scene's authored overrides.
            Hp = useAuthoredInitialHp ? MaxHp : hp;
            Mp = useAuthoredInitialMp ? stats.startingMp : mp;
        }

        private static int Clamp(int value, int max) => Math.Max(0, Math.Min(value, max));

        private void SetLimits(GmBattleStats stats, bool party)
        {
            Name = string.IsNullOrWhiteSpace(stats.displayName) ? (party ? "Anggota Party" : "Musuh") : stats.displayName;
            MaxHp = Math.Max(1, stats.maxHp);
            MaxMp = Math.Max(0, stats.maxMp);
        }
    }

    public sealed class GmPartyRun
    {
        internal const int WipeRecoveryPercent = 50;
        public static GmPartyRun Current { get; private set; } = new GmPartyRun();
        public GmPartyInventory Inventory { get; } = new GmPartyInventory();
        private readonly Dictionary<(string id, int copy), GmPartyVitals> membersById =
            new Dictionary<(string, int), GmPartyVitals>();
        internal IReadOnlyList<GmPartyVitals> Members { get; private set; }

        internal GmPartyRun()
        {
            Inventory.TryAdd(GmItemDefinition.HpPotion);
            Inventory.TryAdd(GmItemDefinition.MpPotion);
            BindRoster(GmBattleStats.CreateDefaultParty(), true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginPlaySession() => Current = new GmPartyRun();

        internal GmPartyVitals[] BindRoster(GmBattleStats[] roster, bool awaitingSceneStats = false)
        {
            var active = new GmPartyVitals[roster.Length];
            var copies = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < roster.Length; i++)
            {
                GmBattleStats stats = roster[i];
                GmBattleCharacter character = stats.ResolvedCharacter;
                string id = character == GmBattleCharacter.Unspecified
                    ? "legacy:" + (stats.displayName ?? "").Trim().ToLowerInvariant()
                    : "hero:" + (int)character;
                copies.TryGetValue(id, out int copy);
                copies[id] = copy + 1;
                var key = (id, copy);
                if (membersById.TryGetValue(key, out GmPartyVitals existing))
                    existing.Bind(stats);
                else
                {
                    existing = new GmPartyVitals(stats, true, awaitingSceneStats);
                    membersById.Add(key, existing);
                }
                active[i] = existing;
            }
            Members = active;
            return active;
        }

        internal bool PartyWiped
        {
            get
            {
                foreach (GmPartyVitals member in Members)
                    if (member.Hp > 0) return false;
                return Members.Count > 0;
            }
        }

        // Recovery requires a player action after the entire active party has fallen.
        internal bool TryRecoverWipedParty()
        {
            if (!PartyWiped) return false;
            foreach (GmPartyVitals member in Members)
                member.Hp = Math.Max(1, (int)((long)member.MaxHp * WipeRecoveryPercent / 100));
            return true;
        }
    }
}
