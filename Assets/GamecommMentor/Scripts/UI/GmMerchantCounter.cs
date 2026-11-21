using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmMerchantCounter : MonoBehaviour
    {
        private sealed class Order
        {
            internal readonly GmMerchantOffer Offer;
            internal readonly GmItemDefinition Item;
            internal readonly int Price, Balance, Owned;

            internal Order(GmPartyRun run, GmMerchantOffer offer)
            {
                Offer = offer;
                Item = offer.Item;
                Price = offer.Price;
                Balance = run.Wallet.Coins;
                Owned = run.Inventory.Count(Item);
            }
        }

        private enum Page { Closed, Services, Shop, Confirm }

        private readonly List<GmMerchantOffer> offers = new List<GmMerchantOffer>();
        private GmMerchantView view;
        private AudioSource feedbackAudio;
        private GmPixelCrushersMainQuestDialogue sequence;
        private GmInteractor player;
        private GmPartyRun run;
        private GmMerchantOffer selected;
        private Order pending;
        private Page page;
        private string notice;
        private int shownBalance, shownOwned, shownPrice;

        private bool CanUseCounter => isActiveAndEnabled && player != null && player.isActiveAndEnabled &&
            player.gameObject.scene == gameObject.scene && gameObject.scene == SceneManager.GetActiveScene() &&
            sequence != null && sequence.OffersMerchantTo(transform) && run == GmPartyRun.Current;

        private bool CanTalk => sequence != null && !string.IsNullOrWhiteSpace(sequence.CurrentTitleFor(transform));
        private bool CanOpenBlacksmith => sequence != null && sequence.OffersBlacksmithTo(transform);
        private bool HasServices => CanTalk || CanOpenBlacksmith;

        internal bool TryOpen(GmPixelCrushersMainQuestDialogue dialogue, GmInteractor requester)
        {
            if (page != Page.Closed || !isActiveAndEnabled || requester == null || !requester.isActiveAndEnabled ||
                dialogue == null || !dialogue.OffersMerchantTo(transform) || requester.gameObject.scene != gameObject.scene ||
                gameObject.scene != SceneManager.GetActiveScene() || !requester.ControlGate.CanExplore) return false;

            run = GmPartyRun.Current;
            player = requester;
            sequence = dialogue;
            player.ControlGate.Block(this);
            player.SuppressInteraction();
            view ??= new GmMerchantView(Talk, OpenShop, OpenBlacksmith, Select, Request, BackOrClose);
            notice = null;
            if (HasServices) OpenServices();
            else OpenShop();
            return true;
        }

        private void OpenServices()
        {
            page = Page.Services;
            view.ShowServices(CanTalk, CanOpenBlacksmith);
        }

        private void Talk()
        {
            if (page != Page.Services || !CanUseCounter || !CanTalk) return;
            var current = sequence;
            var requester = player;
            Close();
            current.TryStartCurrent(requester, transform);
        }

        private void OpenBlacksmith()
        {
            if (page != Page.Services || !CanUseCounter || !CanOpenBlacksmith) return;
            var current = sequence;
            var requester = player;
            var counter = GetComponent<GmBlacksmithCounter>();
            if (counter == null) counter = gameObject.AddComponent<GmBlacksmithCounter>();
            Close();
            counter.TryOpen(current, requester);
        }

        private void OpenShop()
        {
            if (!CanUseCounter || (page != Page.Services && page != Page.Closed)) return;
            notice = null;
            offers.Clear();
            var catalog = run.Economy;
            if (catalog != null)
            {
                var offered = new HashSet<string>();
                foreach (var offer in catalog.Merchant)
                {
                    var item = offer?.Item;
                    if (item != null && offer.Price > 0 && offered.Add(item.Id)) offers.Add(offer);
                }
            }
            selected = offers.Count > 0 ? offers[0] : null;
            page = Page.Shop;
            view.ShowShop(offers, HasServices);
            Render();
            view.HideConfirmation();
        }

        private void Select(GmMerchantOffer offer)
        {
            if (page != Page.Shop || !CanUseCounter || !offers.Contains(offer)) return;
            selected = offer;
            notice = null;
            Render();
        }

        private string BlockReason(GmItemDefinition item, int price, int owned)
        {
            if (item == null || price <= 0) return "Barang sedang tidak tersedia.";
            if (owned == int.MaxValue) return "Tumpukan item ini sudah penuh.";
            int lacking = price - run.Wallet.Coins;
            return lacking > 0 ? $"Kurang {lacking:N0} koin." : null;
        }

        private void Render()
        {
            GmItemDefinition item = selected?.Item;
            shownOwned = run.Inventory.Count(item);
            shownPrice = selected?.Price ?? 0;
            shownBalance = run.Wallet.Coins;
            string reason = BlockReason(item, shownPrice, shownOwned);
            view.RenderOffer(selected, item, shownBalance, shownOwned, reason, notice);
        }

        private void Request()
        {
            if (page != Page.Shop || !CanUseCounter || selected == null) return;
            var order = new Order(run, selected);
            if (BlockReason(order.Item, order.Price, order.Owned) != null)
            {
                Render();
                return;
            }
            pending = order;
            page = Page.Confirm;
            view.ShowConfirmation(order.Item, order.Price, order.Balance, () => Confirm(order));
        }

        private void Confirm(Order order)
        {
            if (page != Page.Confirm || order != pending) return;
            if (!CanUseCounter)
            {
                Close();
                return;
            }
            // Consume this confirmation before changing the wallet. Old button events cannot place another order.
            pending = null;
            page = Page.Shop;
            if (run.Wallet.Coins != order.Balance || run.Inventory.Count(order.Item) != order.Owned ||
                order.Offer.Item != order.Item || order.Offer.Price != order.Price)
                notice = "Saldo, stok, atau harga berubah. Periksa lagi sebelum membeli.";
            else if (run.Wallet.TrySpend(order.Price, () => run.Inventory.TryAdd(order.Item)))
            {
                notice = $"{order.Item.Name} masuk ke Tas.";
                GmGameplayAudio.Play(gameObject, ref feedbackAudio, GmGameplayAudio.Purchase);
            }
            else
                notice = "Pembelian belum berhasil. Periksa saldo dan stok Tas.";
            Render();
            view.HideConfirmation();
        }

        private void BackOrClose()
        {
            if (!CanUseCounter)
            {
                Close();
                return;
            }
            if (page == Page.Confirm)
            {
                pending = null;
                page = Page.Shop;
                Render();
                view.HideConfirmation();
            }
            else if (page == Page.Shop && HasServices) OpenServices();
            else Close();
        }

        private void Update()
        {
            if (page == Page.Closed) return;
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
            else if (page == Page.Shop && (shownBalance != run.Wallet.Coins ||
                     shownOwned != run.Inventory.Count(selected?.Item) || shownPrice != (selected?.Price ?? 0))) Render();
        }

        private void Close()
        {
            if (page == Page.Closed) return;
            page = Page.Closed;
            pending = null;
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
