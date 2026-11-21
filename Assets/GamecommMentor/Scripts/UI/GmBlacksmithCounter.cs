using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmBlacksmithCounter : MonoBehaviour
    {
        internal sealed class Entry
        {
            internal readonly GmWeaponDefinition Definition;
            internal readonly GmBlacksmithOffer Offer;
            internal readonly GmOwnedWeapon Weapon;

            internal Entry(GmWeaponDefinition definition, GmBlacksmithOffer offer)
            {
                Definition = definition;
                Offer = offer;
            }

            internal Entry(GmOwnedWeapon weapon)
            {
                Definition = weapon.Definition;
                Weapon = weapon;
            }

            internal string Name => Weapon == null ? Definition.Name : $"{Definition.Name} +{Weapon.Rank}";
        }

        private sealed class Order
        {
            internal readonly Entry Entry;
            internal readonly int Price, Balance, Rank, Revision;

            internal Order(Entry entry, int price, GmPartyRun run)
            {
                Entry = entry;
                Price = price;
                Balance = run.Wallet.Coins;
                Rank = entry.Weapon?.Rank ?? 0;
                Revision = run.Equipment.Revision;
            }
        }

        private readonly List<Entry> entries = new List<Entry>();
        private GmBlacksmithView view;
        private GmPixelCrushersMainQuestDialogue sequence;
        private GmInteractor player;
        private GmPartyRun run;
        private AudioSource feedbackAudio;
        private Entry selected;
        private Order pending;
        private bool enchanting;
        private string notice;
        private int shownBalance, shownRevision, shownPrice;

        private bool CanUseCounter => isActiveAndEnabled && player != null && player.isActiveAndEnabled &&
            player.gameObject.scene == gameObject.scene && gameObject.scene == SceneManager.GetActiveScene() &&
            sequence != null && sequence.OffersBlacksmithTo(transform) && run == GmPartyRun.Current;

        internal bool TryOpen(GmPixelCrushersMainQuestDialogue dialogue, GmInteractor requester)
        {
            if (run != null || !isActiveAndEnabled || requester == null || !requester.isActiveAndEnabled ||
                dialogue == null || !dialogue.OffersBlacksmithTo(transform) || requester.gameObject.scene != gameObject.scene ||
                gameObject.scene != SceneManager.GetActiveScene() || !requester.ControlGate.CanExplore) return false;

            run = GmPartyRun.Current;
            player = requester;
            sequence = dialogue;
            player.ControlGate.Block(this);
            player.SuppressInteraction();
            view ??= new GmBlacksmithView(ChangeTab, Select, Request, BackOrClose);
            enchanting = false;
            notice = null;
            view.Show();
            RebuildEntries();
            return true;
        }

        private void ChangeTab(bool enchant)
        {
            if (pending != null || !CanUseCounter || enchanting == enchant) return;
            enchanting = enchant;
            selected = null;
            notice = null;
            RebuildEntries();
        }

        private void RebuildEntries()
        {
            var equipment = run.Equipment;
            Entry previous = selected;
            selected = null;
            entries.Clear();
            if (enchanting)
            {
                foreach (var weapon in equipment.OwnedWeapons) entries.Add(new Entry(weapon));
            }
            else if (run.Economy != null)
            {
                var ids = new HashSet<string>();
                foreach (var offer in run.Economy.Blacksmith)
                {
                    var definition = equipment.FindDefinition(offer?.WeaponId);
                    if (definition != null && !definition.IsStarter && offer.Price > 0 && ids.Add(definition.Id))
                        entries.Add(new Entry(definition, offer));
                }
            }

            if (previous != null)
                selected = entries.Find(entry => entry.Definition == previous.Definition && entry.Weapon == previous.Weapon);
            if (selected == null && entries.Count > 0) selected = entries[0];
            view.ShowEntries(entries, enchanting);
            Render();
            view.FocusSelection();
        }

        private void Select(Entry entry)
        {
            if (pending != null || !CanUseCounter || !entries.Contains(entry)) return;
            selected = entry;
            notice = null;
            Render();
        }

        private int Price(Entry entry) => entry == null ? -1 :
            entry.Offer != null ? entry.Offer.Price : run.PriceForEnchant(entry.Weapon);

        private string Badge(Entry entry)
        {
            if (entry.Offer != null && run.Equipment.OwnsDefinition(entry.Definition.Id)) return "Sudah dimiliki";
            int price = Price(entry);
            if (price < 0) return entry.Weapon.Rank >= GmEconomyCatalog.MaxEnchantRank ? "Maksimum" : "Tidak tersedia";
            return price == 0 ? "Gratis" : $"{price:N0} koin";
        }

        private string BlockReason(Entry entry, int price)
        {
            if (entry.Offer != null && run.Equipment.OwnsDefinition(entry.Definition.Id)) return "Senjata sudah ada di Tas.";
            if (price < 0) return entry.Weapon?.Rank >= GmEconomyCatalog.MaxEnchantRank
                ? "Tingkat enchant sudah maksimum." : "Layanan sedang tidak tersedia.";
            int lacking = price - run.Wallet.Coins;
            return lacking > 0 ? $"Kurang {lacking:N0} koin." : null;
        }

        private GmPartyVitals Wielder(Entry entry)
        {
            foreach (var member in run.Members)
                if (member.Character == entry.Definition.Wielder) return member;
            return null;
        }

        private void Render()
        {
            shownBalance = run.Wallet.Coins;
            shownRevision = run.Equipment.Revision;
            shownPrice = Price(selected);
            view.ShowSelection(selected, shownBalance, enchanting && run.FreeEnchantAvailable, Badge);
            if (selected == null) return;

            var member = Wielder(selected);
            var weapon = selected.Weapon;
            string wielder = member?.Name ?? selected.Definition.Wielder.ToString();
            string usage = weapon == null ? $"{wielder} · Pasang lewat Tas → Perlengkapan" :
                wielder + (run.Equipment.IsEquipped(weapon) ? " · Terpasang" : " · Tersimpan di Tas");
            int before = weapon?.Bonus ?? run.Equipment.Equipped(member)?.Bonus ?? 0;
            int rank = weapon?.Rank ?? 0;
            int nextRank = weapon == null ? 0 : Math.Min(rank + 1, GmEconomyCatalog.MaxEnchantRank);
            int after = selected.Definition.BonusAtRank(nextRank);
            string power = "Bonus daya   " + Change(before, after, "+");
            string attacks = member == null ? "" : "Serangan biasa   " + Change(
                GmBattleMove.BasicAttack.PowerWithWeapon(member.BaseAttack, before),
                GmBattleMove.BasicAttack.PowerWithWeapon(member.BaseAttack, after));
            string reason = BlockReason(selected, shownPrice);
            string action = enchanting ? "Enchant" : "Beli senjata";
            view.RenderDetail(usage, power, attacks, action, shownPrice, shownBalance, reason, notice);
        }

        private static string Change(int before, int after, string prefix = "") =>
            before == after ? $"{prefix}{after}" : $"{prefix}{before} → {prefix}{after}";

        private void Request()
        {
            if (!CanUseCounter || pending != null || selected == null) return;
            int price = Price(selected);
            if (BlockReason(selected, price) != null)
            {
                Render();
                return;
            }

            var order = new Order(selected, price, run);
            pending = order;
            string title = enchanting ? $"Enchant {selected.Name} → +{order.Rank + 1}?" : $"Beli {selected.Name}?";
            view.ShowConfirmation(title, enchanting, order.Price, order.Balance, () => Confirm(order));
        }

        private void Confirm(Order order)
        {
            if (pending == null || order != pending) return;
            if (!CanUseCounter)
            {
                Close();
                return;
            }

            pending = null;
            if (run.Wallet.Coins != order.Balance || run.Equipment.Revision != order.Revision || Price(order.Entry) != order.Price)
                notice = "Saldo atau isi Tas berubah. Periksa lagi sebelum konfirmasi.";
            else
            {
                var entry = order.Entry;
                bool success = entry.Offer != null
                    ? run.TryBuyWeapon(entry.Offer, order.Price)
                    : run.TryEnchant(entry.Weapon, order.Rank, order.Price);
                if (success)
                {
                    notice = entry.Offer != null ? "Senjata masuk ke Tas → Perlengkapan." : $"{entry.Name} berhasil ditempa.";
                    GmGameplayAudio.Play(gameObject, ref feedbackAudio, GmGameplayAudio.Purchase);
                }
                else notice = "Transaksi belum berhasil. Periksa saldo dan isi Tas.";
            }

            view.HideConfirmation();
            RebuildEntries();
        }

        private void BackOrClose()
        {
            if (pending == null || !CanUseCounter)
            {
                Close();
                return;
            }
            pending = null;
            view.HideConfirmation();
            RebuildEntries();
        }

        private void Update()
        {
            if (run == null) return;
            if (!CanUseCounter)
            {
                Close();
                return;
            }
#if ENABLE_INPUT_SYSTEM
            bool cancel = Keyboard.current?.escapeKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool cancel = Input.GetKeyDown(KeyCode.Escape);
#else
            bool cancel = false;
#endif
            if (cancel) BackOrClose();
            else if (pending == null)
            {
                if (shownRevision != run.Equipment.Revision) RebuildEntries();
                else if (shownBalance != run.Wallet.Coins || shownPrice != Price(selected)) Render();
            }
        }

        private void Close()
        {
            if (run == null) return;
            pending = null;
            selected = null;
            entries.Clear();
            view?.Hide();
            if (player != null)
            {
                player.ControlGate.Unblock(this);
                player.SuppressInteraction();
            }
            player = null;
            sequence = null;
            run = null;
        }

        private void OnDisable() => Close();

        private void OnDestroy() => view?.Dispose();
    }
}
