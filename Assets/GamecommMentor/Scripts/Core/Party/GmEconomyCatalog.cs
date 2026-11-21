using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [Serializable]
    internal sealed class GmMerchantOffer
    {
        [SerializeField] private string itemId;
        [Min(1), SerializeField] private int price;
        [SerializeField] private Sprite icon;

        internal int Price => price;
        internal Sprite Icon => icon;

        internal GmItemDefinition Item
        {
            get
            {
                foreach (GmItemDefinition item in GmItemDefinition.All)
                    if (item.Id == itemId) return item;
                return null;
            }
        }
    }

    [Serializable]
    internal sealed class GmBlacksmithOffer
    {
        [SerializeField] private string weaponId;
        [Min(1), SerializeField] private int price;

        internal string WeaponId => weaponId;
        internal int Price => price;
    }

    public sealed class GmEconomyCatalog : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Economy/EconomyCatalog";
        internal const int DefaultAllowance = 100;
        internal const int DefaultPoppoReward = 45;
        internal const int MaxEnchantRank = 2;

        [Min(1), SerializeField] private int puaAllowance = DefaultAllowance;
        [Min(1), SerializeField] private int poppoReward = DefaultPoppoReward;
        [SerializeField] private GmMerchantOffer[] merchant = Array.Empty<GmMerchantOffer>();
        [SerializeField] private GmBlacksmithOffer[] blacksmith = Array.Empty<GmBlacksmithOffer>();
        [Min(1), SerializeField] private int firstEnchantPrice = 15;
        [Min(1), SerializeField] private int secondEnchantPrice = 25;

        internal int PuaAllowance => Mathf.Max(1, puaAllowance);
        internal int PoppoReward => Mathf.Max(1, poppoReward);
        internal IReadOnlyList<GmMerchantOffer> Merchant => merchant ?? Array.Empty<GmMerchantOffer>();
        internal IReadOnlyList<GmBlacksmithOffer> Blacksmith => blacksmith ?? Array.Empty<GmBlacksmithOffer>();

        internal int EnchantPrice(int rank) => rank switch
        {
            0 => Mathf.Max(1, firstEnchantPrice),
            1 => Mathf.Max(1, secondEnchantPrice),
            _ => -1
        };
    }
}
