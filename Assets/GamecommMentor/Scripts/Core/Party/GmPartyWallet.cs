using System;
using System.Collections.Generic;

namespace GamecommMentor.DaengSaka
{
    public sealed class GmPartyWallet
    {
        private readonly HashSet<string> claimed = new HashSet<string>(StringComparer.Ordinal);
        public int Coins { get; private set; }

        internal bool TryGrantOnce(string source, int amount)
        {
            if (string.IsNullOrWhiteSpace(source) || amount <= 0 ||
                amount > int.MaxValue - Coins || !claimed.Add(source)) return false;
            Coins += amount;
            return true;
        }

        internal bool TrySpend(int price, Func<bool> receive)
        {
            // Purchases resolve synchronously on the gameplay thread. Debit only after receiving the item.
            if (price <= 0 || Coins < price || !receive()) return false;
            Coins -= price;
            return true;
        }
    }
}
