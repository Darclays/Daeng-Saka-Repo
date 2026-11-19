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

    public sealed class GmEconomyCatalog : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Economy/EconomyCatalog";
        internal const int DefaultAllowance = 100;
        internal const int DefaultPoppoReward = 45;

        [Min(1), SerializeField] private int puaAllowance = DefaultAllowance;
        [Min(1), SerializeField] private int poppoReward = DefaultPoppoReward;
        [SerializeField] private GmMerchantOffer[] merchant = Array.Empty<GmMerchantOffer>();

        internal int PuaAllowance => Mathf.Max(1, puaAllowance);
        internal int PoppoReward => Mathf.Max(1, poppoReward);
        internal IReadOnlyList<GmMerchantOffer> Merchant => merchant ?? Array.Empty<GmMerchantOffer>();
    }
}
