using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;
using Entry = GamecommMentor.DaengSaka.GmBlacksmithCounter.Entry;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBlacksmithView
    {
        private static readonly Color Gold = new Color32(255, 221, 151, 255);
        private static readonly Color Warning = new Color32(142, 59, 36, 255);
        private readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private readonly List<(Entry entry, Button button, Text badge)> rows = new List<(Entry, Button, Text)>();
        private readonly RectTransform root, list, detail, confirm, confirmActions;
        private readonly CanvasGroup controls;
        private readonly Button buyTab, enchantTab, close, commit;
        private readonly Text balance, freeOffer, title, wielder, power, attacks, projected, status, commitLabel, empty;
        private readonly Text confirmTitle, confirmPrice, confirmBalance;
        private readonly Image icon;
        private readonly Action<Entry> select;
        private readonly Action back;
        private Button selectedRow, confirmCancel;

        internal GmBlacksmithView(Action<bool> changeTab, Action<Entry> select, Action request, Action back)
        {
            this.select = select;
            this.back = back;
            var canvasObject = new GameObject("Toko Penempa Besi", typeof(RectTransform), typeof(Canvas),
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

            var panel = Rect("Layanan penempa", root, .075f, .075f, .925f, .925f);
            Fill(panel, Paper);
            controls = panel.gameObject.AddComponent<CanvasGroup>();
            var header = Rect("Kepala toko", panel, 0f, .844f, 1f, 1f);
            Fill(header, Leaf);
            AddText(font, header, "Nama toko", "Penempa Besi", 34, .035f, .12f, .43f, .87f, bold: true).color = Paper;
            balance = AddText(font, header, "Kas party", "", 23, .44f, .17f, .795f, .82f, TextAnchor.MiddleRight, true);
            balance.color = Gold;
            close = AddButton(font, header, "Tutup [Esc]", .823f, .24f, .962f, .76f, back);

            buyTab = AddButton(font, panel, "Senjata", .035f, .727f, .19f, .815f, () => changeTab(false));
            enchantTab = AddButton(font, panel, "Enchant", .20f, .727f, .355f, .815f, () => changeTab(true));
            freeOffer = AddText(font, panel, "Jatah enchant", "Enchant pertama gratis untuk satu senjata",
                17, .454f, .727f, .965f, .815f, TextAnchor.MiddleRight);
            freeOffer.color = Leaf;
            list = CreateScrollContent(Rect("Senjata", panel, .035f, .053f, .43f, .695f), out _);
            var card = Rect("Rincian", panel, .454f, .053f, .965f, .695f);
            Fill(card, Inset);
            empty = AddText(font, card, "Daftar kosong", "", 22, .07f, .37f, .93f, .63f, TextAnchor.MiddleCenter);
            detail = Rect("Senjata terpilih", card, .04f, .04f, .96f, .96f);
            icon = CreateIcon(Rect("Ikon", detail, .024f, .70f, .174f, .98f));
            title = AddText(font, detail, "Nama senjata", "", 30, .20f, .85f, .98f, .99f, bold: true);
            wielder = AddText(font, detail, "Pengguna dan posisi", "", 17, .20f, .71f, .98f, .85f);
            wielder.color = Muted;
            Fill(Rect("Garis detail", detail, .025f, .674f, .975f, .679f), Rule);
            power = AddText(font, detail, "Bonus daya", "", 22, .026f, .529f, .98f, .645f, bold: true);
            attacks = AddText(font, detail, "Serangan biasa", "", 20, .026f, .414f, .98f, .529f);
            projected = AddText(font, detail, "Perkiraan saldo", "", 19, .026f, .30f, .98f, .41f);
            commit = AddButton(font, detail, "Proses transaksi", .026f, .127f, .66f, .294f, request, primary: true);
            commitLabel = commit.GetComponentInChildren<Text>(true);
            status = AddText(font, detail, "Status transaksi", "", 17, .026f, 0f, .98f, .119f);

            confirm = Rect("Konfirmasi transaksi", root, 0f, 0f, 1f, 1f);
            Fill(confirm, new Color(.07f, .115f, .095f, .77f)).raycastTarget = true;
            var sheet = Rect("Konfirmasi", confirm, .268f, .26f, .732f, .74f);
            Fill(sheet, Paper);
            confirmTitle = AddText(font, sheet, "Pertanyaan", "", 27, .08f, .71f, .92f, .942f, bold: true);
            Fill(Rect("Garis konfirmasi", sheet, .08f, .69f, .92f, .696f), Rule);
            confirmPrice = AddText(font, sheet, "Biaya", "", 20, .08f, .52f, .92f, .65f);
            confirmBalance = AddText(font, sheet, "Saldo akhir", "", 20, .08f, .37f, .92f, .50f, bold: true);
            confirmActions = Rect("Tindakan konfirmasi", sheet, .08f, .082f, .92f, .304f);
            root.gameObject.SetActive(false);
        }

        internal void Show()
        {
            HideConfirmation();
            root.gameObject.SetActive(true);
        }

        internal void ShowEntries(IReadOnlyList<Entry> entries, bool enchanting)
        {
            SetSelected(buyTab, !enchanting);
            SetSelected(enchantTab, enchanting);
            empty.text = enchanting ? "Belum ada senjata yang dapat ditempa." : "Belum ada senjata di etalase.";
            ClearChildren(list);
            rows.Clear();
            foreach (var entry in entries)
            {
                var row = Rect(entry.Name, list, 0f, 0f, 1f, 1f);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
                var button = Clickable(Fill(row, Paper), () => select(entry));
                SetIcon(CreateIcon(Rect("Ikon", row, .032f, .10f, .17f, .90f)), entry.Definition.Icon);
                AddText(font, row, "Nama", entry.Name, 19, .19f, .12f, .665f, .88f, bold: true);
                var badge = AddText(font, row, "Keterangan", "", 16, .675f, .12f, .96f, .88f, TextAnchor.MiddleRight);
                badge.color = Muted;
                rows.Add((entry, button, badge));
            }
        }

        internal void ShowSelection(Entry entry, int coins, bool freeEnchant, Func<Entry, string> badgeFor)
        {
            balance.text = $"Kas party   {coins:N0} koin";
            freeOffer.gameObject.SetActive(freeEnchant);
            selectedRow = null;
            foreach (var row in rows)
            {
                bool selected = row.entry == entry;
                row.badge.text = badgeFor(row.entry);
                SetSelected(row.button, selected);
                if (selected) selectedRow = row.button;
            }
            empty.gameObject.SetActive(entry == null);
            detail.gameObject.SetActive(entry != null);
            if (entry == null) return;
            title.text = entry.Name;
            SetIcon(icon, entry.Definition.Icon);
        }

        internal void RenderDetail(string owner, string bonus, string attack, string action, int price, int coins,
            string reason, string notice)
        {
            wielder.text = owner;
            power.text = bonus;
            attacks.text = attack;
            projected.text = reason == null ? $"Saldo setelah transaksi   {coins - price:N0} koin" : "";
            commit.interactable = reason == null;
            commit.targetGraphic.color = commit.interactable ? Leaf : Paper;
            commitLabel.color = commit.interactable ? Paper : Ink;
            commitLabel.text = price >= 0 ? $"{action} · {PriceLabel(price)}" : action;
            status.text = notice ?? reason ?? "";
            status.color = notice == null && reason != null ? Warning : Leaf;
        }

        internal void ShowConfirmation(string title, bool enchant, int price, int before, Action accept)
        {
            ClearChildren(confirmActions);
            confirmTitle.text = title;
            confirmPrice.text = "Biaya   " + PriceLabel(price);
            confirmBalance.text = $"Saldo   {before:N0} → {before - price:N0} koin";
            confirmCancel = AddButton(font, confirmActions, "Batal [Esc]", 0f, 0f, .46f, 1f, back);
            var acceptButton = AddButton(font, confirmActions, enchant ? "Konfirmasi enchant" : "Konfirmasi beli",
                .51f, 0f, 1f, 1f, accept, primary: true);
            GmMenuButton.Link(confirmCancel, null, acceptButton);
            GmMenuButton.Link(acceptButton, confirmCancel, null);
            controls.interactable = controls.blocksRaycasts = false;
            confirm.gameObject.SetActive(true);
            GmMenuButton.Focus(confirmCancel);
            UpdateInputHints();
        }

        internal void HideConfirmation()
        {
            confirm.gameObject.SetActive(false);
            ClearChildren(confirmActions);
            confirmCancel = null;
            controls.interactable = controls.blocksRaycasts = true;
        }

        internal void FocusSelection()
        {
            GmMenuButton.Focus(selectedRow != null ? selectedRow : close);
            UpdateInputHints();
        }

        internal void UpdateInputHints()
        {
            bool pad = GmGamepadInput.UsingGamepad;
            string key = GmGamepadInput.BackLabel;
            Caption(close).text = $"Tutup [{key}]";
            Caption(buyTab).text = pad ? "Senjata [LB]" : "Senjata";
            Caption(enchantTab).text = pad ? "Enchant [RB]" : "Enchant";
            if (confirmCancel != null) Caption(confirmCancel).text = $"Batal [{key}]";
            if (!pad) return;
            if (confirm.gameObject.activeSelf) GmMenuButton.FocusWithin(confirm, confirmCancel);
            else GmMenuButton.FocusWithin(root, selectedRow != null ? selectedRow : close);
        }

        internal void Hide()
        {
            EventSystem events = EventSystem.current;
            GameObject focused = events != null ? events.currentSelectedGameObject : null;
            if (focused != null && focused.transform.IsChildOf(root)) events.SetSelectedGameObject(null);
            root.gameObject.SetActive(false);
        }

        internal void Dispose() => UnityEngine.Object.Destroy(root.gameObject);

        private static string PriceLabel(int price) => price == 0 ? "Gratis" : $"{price:N0} koin";

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

    }
}
