using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmPartyVitals
    {
        public string Name { get; private set; }
        internal GmBattleCharacter Character { get; private set; }
        internal int BaseAttack { get; private set; }
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
            Character = party ? stats.ResolvedCharacter : GmBattleCharacter.Unspecified;
            BaseAttack = Math.Max(1, stats.attackDamage);
            MaxHp = Math.Max(1, stats.maxHp);
            MaxMp = Math.Max(0, stats.maxMp);
        }
    }

    public sealed class GmPartyRun
    {
        internal const int WipeRecoveryPercent = 50;
        internal const float ItemNoticeDuration = 10f;
        public static GmPartyRun Current { get; private set; } = new GmPartyRun();
        public GmPartyInventory Inventory { get; } = new GmPartyInventory();
        public GmPartyWallet Wallet { get; } = new GmPartyWallet();
        internal bool ReceivedSoreliaSupplies { get; private set; }
        internal bool FreeEnchantAvailable { get; private set; } = true;
        private GmEconomyCatalog economy;
        private GmPartyEquipment equipment;
        private readonly HashSet<string> defeatedOptionalEncounters = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> receivedItemNames = new List<string>();
        internal string NewItemSummary { get; private set; }
        internal float ItemNoticeRemaining { get; private set; }

        internal bool HasDefeatedEncounter(string id) => defeatedOptionalEncounters.Contains(id);
        internal void RememberDefeatedEncounter(string id) => defeatedOptionalEncounters.Add(id);

        internal GmEconomyCatalog Economy
        {
            get
            {
                if (economy == null) economy = Resources.Load<GmEconomyCatalog>(GmEconomyCatalog.ResourcePath);
                return economy;
            }
        }

        public GmPartyEquipment Equipment
        {
            get
            {
                if (equipment == null)
                {
                    // Load Unity assets on first gameplay/UI access, outside static run initialization.
                    equipment = new GmPartyEquipment(Resources.Load<GmWeaponCatalog>(GmWeaponCatalog.ResourcePath));
                    foreach (var member in Members) equipment.BindMember(member);
                    equipment.WeaponReceived += RecordNewItem;
                }
                return equipment;
            }
        }

        private readonly Dictionary<(string id, int copy), GmPartyVitals> membersById =
            new Dictionary<(string, int), GmPartyVitals>();
        internal IReadOnlyList<GmPartyVitals> Members { get; private set; }

        internal GmPartyRun()
        {
            Inventory.TryAdd(GmItemDefinition.HpPotion);
            Inventory.TryAdd(GmItemDefinition.MpPotion);
            Inventory.ItemReceived += RecordNewItem;
            BindRoster(GmBattleStats.CreateDefaultParty(), true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginPlaySession() => Current = new GmPartyRun();

        private void RecordNewItem(string name)
        {
            if (!receivedItemNames.Contains(name)) receivedItemNames.Add(name);
            NewItemSummary = receivedItemNames.Count <= 2
                ? string.Join(" + ", receivedItemNames)
                : receivedItemNames.Count + " jenis item";
            ItemNoticeRemaining = ItemNoticeDuration;
        }

        internal void TickItemNotice(float seconds)
        {
            if (ItemNoticeRemaining <= 0f) return;
            ItemNoticeRemaining -= seconds;
            if (ItemNoticeRemaining <= 0f) DismissItemNotice();
        }

        internal void DismissItemNotice()
        {
            receivedItemNames.Clear();
            NewItemSummary = null;
            ItemNoticeRemaining = 0f;
        }

        internal bool TryBuyWeapon(GmBlacksmithOffer offer, int expectedPrice)
        {
            if (offer == null || expectedPrice <= 0 || offer.Price != expectedPrice || Economy == null) return false;
            foreach (var available in Economy.Blacksmith)
            {
                if (available != offer) continue;
                return Wallet.TrySpend(expectedPrice, () => Equipment.TryGrant(offer.WeaponId));
            }
            return false;
        }

        internal int PriceForEnchant(GmOwnedWeapon weapon)
        {
            if (Economy == null || !Equipment.Owns(weapon) || weapon.Rank >= GmEconomyCatalog.MaxEnchantRank)
                return -1;
            return weapon.Rank == 0 && FreeEnchantAvailable ? 0 : Economy.EnchantPrice(weapon.Rank);
        }

        internal bool TryEnchant(GmOwnedWeapon weapon, int expectedRank, int expectedPrice)
        {
            if (expectedPrice < 0 || PriceForEnchant(weapon) != expectedPrice || weapon.Rank != expectedRank)
                return false;

            if (expectedPrice > 0)
                return Wallet.TrySpend(expectedPrice, () => Equipment.TryUpgrade(weapon, expectedRank, GmEconomyCatalog.MaxEnchantRank));
            if (!Equipment.TryUpgrade(weapon, expectedRank, GmEconomyCatalog.MaxEnchantRank)) return false;
            FreeEnchantAvailable = false;
            return true;
        }

        internal bool TryReceiveSoreliaSupplies()
        {
            const int quantity = 5;
            var hp = GmItemDefinition.HpPotion;
            var mp = GmItemDefinition.MpPotion;
            if (ReceivedSoreliaSupplies || Inventory.Count(hp) > int.MaxValue - quantity ||
                Inventory.Count(mp) > int.MaxValue - quantity) return false;

            Inventory.TryAdd(hp, quantity);
            Inventory.TryAdd(mp, quantity);
            ReceivedSoreliaSupplies = true;
            return true;
        }

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
                equipment?.BindMember(existing);
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
