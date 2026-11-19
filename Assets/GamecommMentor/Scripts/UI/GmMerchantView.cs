using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmMerchantView
    {
        private static readonly Color Gold = new Color32(255, 221, 151, 255);
        private static readonly Color Warning = new Color32(142, 59, 36, 255);
        private readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private readonly List<(GmMerchantOffer offer, Button button, Text price)> rows =
            new List<(GmMerchantOffer, Button, Text)>();
        private readonly RectTransform root, services, shop, list, detail, confirm, confirmActions;
        private readonly CanvasGroup shopControls;
        private readonly Button talk, back, buy;
        private readonly Text balance, backLabel, title, recovery, stock, projected, status, buyLabel, empty;
        private readonly Text confirmTitle, confirmPrice, confirmBalance;
        private readonly Image itemIcon;
        private readonly Action<GmMerchantOffer> select;
        private readonly Action backOrClose;
        private Button selectedRow;

        internal GmMerchantView(Action talkToNpc, Action openShop, Action<GmMerchantOffer> selectOffer,
            Action request, Action backOrClose)
        {
            select = selectOffer;
            this.backOrClose = backOrClose;
            var canvasObject = new GameObject("Toko Merchant", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            root = (RectTransform)canvasObject.transform;
            Fill(root, new Color(.04f, .08f, .065f, .76f)).raycastTarget = true;

            services = Rect("Layanan Pemandu Kerajaan", root, .285f, .25f, .715f, .75f);
            Fill(services, Paper);
            AddText(font, services, "Pemandu", "Pemandu Kerajaan", 32, .075f, .76f, .925f, .95f, bold: true);
            talk = AddButton(font, services, "Bicara dengan Penempa", .075f, .52f, .925f, .70f, talkToNpc, primary: true);
            AddButton(font, services, "Belanja di Merchant", .075f, .31f, .925f, .48f, openShop);
            AddButton(font, services, "Tutup [Esc]", .075f, .075f, .925f, .23f, backOrClose);

            shop = Rect("Etalase Merchant", root, .075f, .075f, .925f, .925f);
            Fill(shop, Paper);
            shopControls = shop.gameObject.AddComponent<CanvasGroup>();
            var header = Rect("Kepala toko", shop, 0f, .827f, 1f, 1f);
            Fill(header, Leaf);
            var merchant = AddText(font, header, "Nama toko", "Merchant", 34, .035f, .12f, .43f, .87f, bold: true);
            merchant.color = Paper;
            balance = AddText(font, header, "Kas Party", "", 23, .44f, .17f, .795f, .82f, TextAnchor.MiddleRight, true);
            balance.color = Gold;
            back = AddButton(font, header, "Kembali atau tutup", .823f, .24f, .962f, .76f, backOrClose);
            backLabel = back.GetComponentInChildren<Text>(true);

            var shelf = Rect("Rak", shop, .034f, .086f, .39f, .77f);
            Fill(shelf, Inset);
            AddText(font, shelf, "Persediaan", "Persediaan", 23, .06f, .82f, .955f, .975f, bold: true);
            list = CreateScrollContent(Rect("Barang", shelf, .04f, .045f, .96f, .81f), out _);

            var panel = Rect("Detail barang", shop, .414f, .086f, .965f, .77f);
            Fill(panel, Inset);
            empty = AddText(font, panel, "Rak kosong", "Belum ada barang untuk dibeli.", 22, .08f, .37f, .92f, .63f,
                TextAnchor.MiddleCenter);
            detail = Rect("Penawaran", panel, .04f, .04f, .96f, .96f);
            itemIcon = CreateIcon(Rect("Ikon barang", detail, .025f, .68f, .215f, .99f));
            title = AddText(font, detail, "Nama barang", "", 30, .245f, .825f, .98f, .99f, bold: true);
            recovery = AddText(font, detail, "Efek item", "", 18, .245f, .694f, .98f, .825f);
            recovery.color = Muted;
            Fill(Rect("Garis detail", detail, .025f, .646f, .975f, .651f), Rule);
            stock = AddText(font, detail, "Stok di Tas", "", 20, .025f, .49f, .98f, .62f);
            projected = AddText(font, detail, "Perkiraan saldo", "", 20, .025f, .355f, .98f, .49f);
            buy = AddButton(font, detail, "Beli 1", .025f, .142f, .54f, .31f, request, primary: true);
            buyLabel = buy.GetComponentInChildren<Text>(true);
            status = AddText(font, detail, "Status transaksi", "", 17, .025f, .01f, .98f, .125f);

            confirm = Rect("Konfirmasi pembelian", root, 0f, 0f, 1f, 1f);
            Fill(confirm, new Color(.07f, .115f, .095f, .77f)).raycastTarget = true;
            var sheet = Rect("Konfirmasi", confirm, .286f, .26f, .714f, .74f);
            Fill(sheet, Paper);
            confirmTitle = AddText(font, sheet, "Pertanyaan", "", 28, .08f, .71f, .92f, .942f, bold: true);
            Fill(Rect("Garis konfirmasi", sheet, .08f, .69f, .92f, .696f), Rule);
            confirmPrice = AddText(font, sheet, "Biaya", "", 20, .08f, .52f, .92f, .65f);
            confirmBalance = AddText(font, sheet, "Saldo akhir", "", 20, .08f, .37f, .92f, .50f, bold: true);
            confirmActions = Rect("Tindakan konfirmasi", sheet, .08f, .082f, .92f, .304f);
            root.gameObject.SetActive(false);
        }

        internal void ShowServices()
        {
            root.gameObject.SetActive(true);
            confirm.gameObject.SetActive(false);
            shop.gameObject.SetActive(false);
            services.gameObject.SetActive(true);
            Focus(talk);
        }

        internal void ShowShop(IReadOnlyList<GmMerchantOffer> offers, bool hasQuestDialogue)
        {
            root.gameObject.SetActive(true);
            services.gameObject.SetActive(false);
            shop.gameObject.SetActive(true);
            backLabel.text = hasQuestDialogue ? "Kembali [Esc]" : "Tutup [Esc]";
            ClearChildren(list);
            rows.Clear();
            foreach (GmMerchantOffer offer in offers)
            {
                GmItemDefinition item = offer.Item;
                var row = Rect(item.Name, list, 0f, 0f, 1f, 1f);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 67f;
                var button = Clickable(Fill(row, Paper), () => select(offer));
                var icon = CreateIcon(Rect("Ikon", row, .035f, .09f, .215f, .91f));
                SetIcon(icon, offer.Icon);
                AddText(font, row, "Nama", item.Name, 19, .23f, .10f, .705f, .9f, bold: true);
                var price = AddText(font, row, "Harga", "", 16, .71f, .1f, .957f, .9f, TextAnchor.MiddleRight);
                price.color = Muted;
                rows.Add((offer, button, price));
            }
        }

        internal void RenderOffer(GmMerchantOffer offer, GmItemDefinition item, int coins, int owned,
            string reason, string notice)
        {
            balance.text = $"Kas party   {coins:N0} koin";
            selectedRow = null;
            foreach (var row in rows)
            {
                bool selected = row.offer == offer;
                SetSelected(row.button, selected);
                row.price.text = $"{row.offer.Price:N0} koin";
                if (selected) selectedRow = row.button;
            }
            detail.gameObject.SetActive(item != null);
            empty.gameObject.SetActive(item == null);
            if (item == null) return;

            SetIcon(itemIcon, offer.Icon);
            title.text = item.Name;
            recovery.text = item.HpRecovery > 0 ? $"Pulihkan hingga {item.HpRecovery} HP saat battle" :
                item.MpRecovery > 0 ? $"Pulihkan hingga {item.MpRecovery} MP saat battle" : "";
            stock.text = reason == null ? $"Stok di Tas   {owned:N0} → {owned + 1:N0} botol" : $"Stok di Tas   {owned:N0} botol";
            projected.text = reason == null ? $"Saldo setelah beli   {coins - offer.Price:N0} koin" : "";
            buy.interactable = reason == null;
            buy.targetGraphic.color = buy.interactable ? Leaf : Paper;
            buyLabel.color = buy.interactable ? Paper : Ink;
            buyLabel.text = $"Beli 1 · {offer.Price:N0} koin";
            status.text = notice != null && reason != null ? notice + " " + reason : notice ?? reason ?? "";
            status.color = reason != null ? Warning : Leaf;
        }

        internal void ShowConfirmation(GmItemDefinition item, int price, int before, Action accept)
        {
            ClearChildren(confirmActions);
            confirmTitle.text = $"Beli 1 {item.Name}?";
            confirmPrice.text = $"Biaya   {price:N0} koin";
            confirmBalance.text = $"Saldo   {before:N0} → {before - price:N0} koin";
            var cancel = AddButton(font, confirmActions, "Batal [Esc]", 0f, 0f, .46f, 1f, backOrClose);
            AddButton(font, confirmActions, "Konfirmasi beli", .51f, 0f, 1f, 1f, accept, primary: true);
            shopControls.interactable = shopControls.blocksRaycasts = false;
            confirm.gameObject.SetActive(true);
            Focus(cancel);
        }

        internal void HideConfirmation()
        {
            ClearChildren(confirmActions);
            confirm.gameObject.SetActive(false);
            shopControls.interactable = shopControls.blocksRaycasts = true;
            Focus(selectedRow != null ? selectedRow : back);
        }

        internal void Hide()
        {
            if (root == null) return;
            EventSystem events = EventSystem.current;
            GameObject focused = events != null ? events.currentSelectedGameObject : null;
            if (focused != null && focused.transform.IsChildOf(root)) events.SetSelectedGameObject(null);
            root.gameObject.SetActive(false);
        }

        internal void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }

        private static Image CreateIcon(RectTransform rect)
        {
            var image = Fill(rect, Color.white);
            image.preserveAspect = true;
            return image;
        }

        private static void SetIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void Focus(Button button)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
