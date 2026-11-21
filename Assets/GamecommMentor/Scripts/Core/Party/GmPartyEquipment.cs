using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmOwnedWeapon
    {
        internal readonly string InstanceId = Guid.NewGuid().ToString("N");
        internal GmWeaponDefinition Definition { get; }
        internal int Rank { get; private set; }
        internal int Bonus => Definition.BonusAtRank(Rank);

        internal GmOwnedWeapon(GmWeaponDefinition definition, int rank = 0)
        {
            Definition = definition;
            Rank = Math.Max(0, rank);
        }

        internal void IncreaseRank() => Rank++;
    }

    public sealed class GmPartyEquipment
    {
        private readonly Dictionary<string, GmWeaponDefinition> catalog =
            new Dictionary<string, GmWeaponDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<GmBattleCharacter, GmWeaponDefinition> starters =
            new Dictionary<GmBattleCharacter, GmWeaponDefinition>();
        private readonly List<GmOwnedWeapon> owned = new List<GmOwnedWeapon>();
        // The run reuses its member objects by resolved hero ID and copy, even when the battle roster is reordered.
        private readonly Dictionary<GmPartyVitals, GmOwnedWeapon> equipped =
            new Dictionary<GmPartyVitals, GmOwnedWeapon>();

        public IEnumerable<GmWeaponDefinition> Definitions => catalog.Values;
        internal IReadOnlyList<GmOwnedWeapon> OwnedWeapons => owned;
        internal int Revision { get; private set; }

        internal GmWeaponDefinition FindDefinition(string id) =>
            !string.IsNullOrEmpty(id) && catalog.TryGetValue(id, out var definition) ? definition : null;

        internal bool OwnsDefinition(string id) => owned.Exists(weapon => weapon.Definition.Id == id);
        internal bool Owns(GmOwnedWeapon weapon) => weapon != null && owned.Contains(weapon);
        internal bool IsEquipped(GmOwnedWeapon weapon) => weapon != null && equipped.ContainsValue(weapon);

        internal GmPartyEquipment(GmWeaponCatalog source)
        {
            if (source == null)
            {
                Debug.LogError("[Daeng Saka] Katalog senjata tidak ditemukan di Resources/" + GmWeaponCatalog.ResourcePath);
                return;
            }
            foreach (var definition in source.Weapons)
            {
                if (definition == null || !definition.IsValid || catalog.ContainsKey(definition.Id) ||
                    (definition.IsStarter && starters.ContainsKey(definition.Wielder)))
                {
                    Debug.LogWarning("[Daeng Saka] Entri katalog senjata kosong atau duplikat. Periksa WeaponCatalog.");
                    continue;
                }
                catalog.Add(definition.Id, definition);
                if (definition.IsStarter) starters.Add(definition.Wielder, definition);
            }
        }

        internal void BindMember(GmPartyVitals member)
        {
            if (equipped.ContainsKey(member)) return;
            GmOwnedWeapon first = null;
            if (starters.TryGetValue(member.Character, out var starter))
            {
                first = new GmOwnedWeapon(starter);
                owned.Add(first);
            }
            equipped.Add(member, first);
            Revision++;
        }

        // A purchased or rewarded model is unique across the party. Joining members receive their own starters.
        public bool TryGrant(string definitionId)
        {
            var definition = FindDefinition(definitionId);
            if (definition == null || definition.IsStarter || OwnsDefinition(definitionId)) return false;
            owned.Add(new GmOwnedWeapon(definition));
            Revision++;
            return true;
        }

        internal bool TryUpgrade(GmOwnedWeapon weapon, int expectedRank, int maxRank)
        {
            if (!Owns(weapon) || weapon.Rank != expectedRank || weapon.Rank >= maxRank) return false;
            weapon.IncreaseRank();
            Revision++;
            return true;
        }

        internal GmOwnedWeapon Equipped(GmPartyVitals member) =>
            member != null && equipped.TryGetValue(member, out var weapon) ? weapon : null;

        internal IEnumerable<GmOwnedWeapon> Options(GmPartyVitals member)
        {
            if (member == null || !equipped.TryGetValue(member, out var current)) yield break;
            foreach (var weapon in owned)
                if (weapon.Definition.Wielder == member.Character && (weapon == current || !equipped.ContainsValue(weapon)))
                    yield return weapon;
        }

        internal bool TryEquip(GmPartyVitals member, string instanceId)
        {
            if (member == null || string.IsNullOrEmpty(instanceId) || !equipped.ContainsKey(member)) return false;
            GmOwnedWeapon weapon = owned.Find(item => item.InstanceId == instanceId);
            if (weapon == null || weapon.Definition.Wielder != member.Character || equipped.ContainsValue(weapon)) return false;
            equipped[member] = weapon;
            Revision++;
            return true;
        }
    }
}
