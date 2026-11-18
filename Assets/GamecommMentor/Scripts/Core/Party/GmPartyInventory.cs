using System;
using System.Collections.Generic;

namespace GamecommMentor.DaengSaka
{
    public sealed class GmItemDefinition
    {
        public static readonly GmItemDefinition HpPotion = new GmItemDefinition("potion.hp", "Ramuan HP");
        public static readonly GmItemDefinition MpPotion = new GmItemDefinition("potion.mp", "Ramuan MP");
        public static readonly GmItemDefinition Wader = new GmItemDefinition("fish.wader", "Ikan Wader");
        public static IReadOnlyList<GmItemDefinition> All { get; } =
            Array.AsReadOnly(new[] { HpPotion, MpPotion, Wader });

        public string Id { get; }
        public string Name { get; }

        private GmItemDefinition(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public sealed class GmPartyInventory
    {
        private readonly Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        internal int Revision { get; private set; }

        public int Count(GmItemDefinition item) =>
            item != null && quantities.TryGetValue(item.Id, out int count) ? count : 0;

        public bool TryAdd(GmItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            int current = Count(item);
            if (quantity > int.MaxValue - current) return false;
            quantities[item.Id] = current + quantity;
            Revision++;
            return true;
        }

        public bool TryRemove(GmItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            int current = Count(item);
            if (quantity > current) return false;
            if (quantity == current) quantities.Remove(item.Id);
            else quantities[item.Id] = current - quantity;
            Revision++;
            return true;
        }
    }
}
