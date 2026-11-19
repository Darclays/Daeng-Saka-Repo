using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [Serializable]
    public sealed class GmWeaponDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GmBattleCharacter wielder;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool starter;
        [Min(0), SerializeField] private int baseBonus;
        [Min(0), SerializeField] private int bonusPerRank = 2;

        public string Id => id;
        public string Name => displayName;
        public bool IsStarter => starter;
        internal GmBattleCharacter Wielder => wielder;
        internal Sprite Icon => icon;
        internal bool IsValid => !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(displayName) &&
            wielder != GmBattleCharacter.Unspecified;

        internal int BonusAtRank(int rank) => (int)Math.Min(int.MaxValue,
            Math.Max(0, baseBonus) + (long)Math.Max(0, rank) * Math.Max(0, bonusPerRank));
    }

    public sealed class GmWeaponCatalog : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Equipment/WeaponCatalog";
        [SerializeField] private GmWeaponDefinition[] weapons = Array.Empty<GmWeaponDefinition>();
        internal IReadOnlyList<GmWeaponDefinition> Weapons => weapons ?? Array.Empty<GmWeaponDefinition>();
    }
}
