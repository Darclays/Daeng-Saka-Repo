using System;
using System.Collections.Generic;

namespace GamecommMentor.DaengSaka
{
    public sealed class GmItemDefinition
    {
        public static readonly GmItemDefinition HpPotion = new GmItemDefinition("potion.hp", "Ramuan HP", hpRecovery: 25);
        public static readonly GmItemDefinition MpPotion = new GmItemDefinition("potion.mp", "Ramuan MP", mpRecovery: 8);
        public static readonly GmItemDefinition Wader = new GmItemDefinition("fish.wader", "Ikan Wader");
        public static readonly GmItemDefinition Nila = new GmItemDefinition("fish.nila", "Ikan Nila");
        public static readonly GmItemDefinition Gabus = new GmItemDefinition("fish.gabus", "Ikan Gabus");
        public static readonly GmItemDefinition SoreliaHerbs = new GmItemDefinition("quest.sorelia.herbs", "Bahan Ramuan Sorelia");
        public static readonly GmItemDefinition GrilledWader = new GmItemDefinition(
            "food.wader.grilled", "Wader Bakar", hpRecovery: 15, ingredient: Wader);
        public static readonly GmItemDefinition GrilledNila = new GmItemDefinition(
            "food.nila.grilled", "Nila Bakar", mpRecovery: 10, ingredient: Nila);
        public static readonly GmItemDefinition GrilledGabus = new GmItemDefinition(
            "food.gabus.grilled", "Gabus Bakar", ingredient: Gabus, restoresParty: true);
        internal static IReadOnlyList<GmItemDefinition> FishRecipes { get; } =
            Array.AsReadOnly(new[] { GrilledWader, GrilledNila, GrilledGabus });
        public static IReadOnlyList<GmItemDefinition> All { get; } =
            Array.AsReadOnly(new[] { HpPotion, MpPotion, Wader, Nila, Gabus, GrilledWader, GrilledNila, GrilledGabus, SoreliaHerbs });

        public string Id { get; }
        public string Name { get; }
        internal int HpRecovery { get; }
        internal int MpRecovery { get; }
        internal GmItemDefinition Ingredient { get; }
        internal bool RestoresParty { get; }
        internal string RecoveryLabel => RestoresParty ? "HP/MP penuh · hapus debuff" :
            MpRecovery > 0 ? $"+{MpRecovery} MP" : $"+{HpRecovery} HP";

        private GmItemDefinition(string id, string name, int hpRecovery = 0, int mpRecovery = 0,
            GmItemDefinition ingredient = null, bool restoresParty = false)
        {
            Id = id;
            Name = name;
            HpRecovery = hpRecovery;
            MpRecovery = mpRecovery;
            Ingredient = ingredient;
            RestoresParty = restoresParty;
        }
    }

    public sealed class GmPartyInventory
    {
        private readonly Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        internal int Revision { get; private set; }
        internal event Action<string> ItemReceived;

        public int Count(GmItemDefinition item) =>
            item != null && quantities.TryGetValue(item.Id, out int count) ? count : 0;

        public bool TryAdd(GmItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            int current = Count(item);
            if (quantity > int.MaxValue - current) return false;
            quantities[item.Id] = current + quantity;
            Revision++;
            ItemReceived?.Invoke(item.Name);
            return true;
        }

        public bool TryRemove(GmItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            int current = Count(item);
            if (quantity > current) return false;
            SetCount(item, current - quantity);
            Revision++;
            return true;
        }

        internal bool CanCook(GmItemDefinition dish) =>
            dish?.Ingredient != null && Count(dish.Ingredient) > 0 && Count(dish) < int.MaxValue;

        // The bag publishes one revision after both stacks have been updated.
        internal bool TryCook(GmItemDefinition dish)
        {
            if (!CanCook(dish)) return false;
            SetCount(dish.Ingredient, Count(dish.Ingredient) - 1);
            quantities[dish.Id] = Count(dish) + 1;
            Revision++;
            return true;
        }

        internal bool CanExchangeSoreliaHerbs =>
            Count(GmItemDefinition.SoreliaHerbs) > 0 &&
            Count(GmItemDefinition.HpPotion) <= int.MaxValue - GmQuestCatalog.SoreliaSupplyReward &&
            Count(GmItemDefinition.MpPotion) <= int.MaxValue - GmQuestCatalog.SoreliaSupplyReward;

        internal bool TryExchangeSoreliaHerbs()
        {
            if (!CanExchangeSoreliaHerbs) return false;
            var herbs = GmItemDefinition.SoreliaHerbs;
            var hp = GmItemDefinition.HpPotion;
            var mp = GmItemDefinition.MpPotion;
            SetCount(herbs, Count(herbs) - 1);
            SetCount(hp, Count(hp) + GmQuestCatalog.SoreliaSupplyReward);
            SetCount(mp, Count(mp) + GmQuestCatalog.SoreliaSupplyReward);
            Revision++;
            ItemReceived?.Invoke(hp.Name);
            ItemReceived?.Invoke(mp.Name);
            return true;
        }

        private void SetCount(GmItemDefinition item, int count)
        {
            if (count == 0) quantities.Remove(item.Id);
            else quantities[item.Id] = count;
        }
    }
}
