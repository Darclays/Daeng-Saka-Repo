using System;
using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmControlDialog
    {
        private readonly RectTransform overlay;
        private readonly Text title, detail, primaryLabel, secondaryLabel;
        private readonly Button primary, secondary;

        internal bool IsOpen => overlay.gameObject.activeSelf;

        internal GmControlDialog(RectTransform parent, Font font, Action onPrimary, Action onSecondary)
        {
            overlay = Rect("Jeda dan info", parent, 0f, 0f, 1f, 1f);
            Fill(overlay, new Color(.045f, .08f, .065f, .76f)).raycastTarget = true;
            RectTransform panel = Rect("Panel jeda", overlay, .25f, .29f, .75f, .71f);
            Fill(panel, Paper);
            Fill(Rect("Garis warna", panel, 0f, .975f, 1f, 1f), Leaf);
            title = AddText(font, panel, "Judul", "", 35, .075f, .72f, .925f, .93f, bold: true);
            detail = AddText(font, panel, "Keterangan", "", 22, .075f, .28f, .925f, .72f);
            primary = AddButton(font, panel, "Lanjutkan", .075f, .075f, .48f, .25f, onPrimary);
            secondary = AddButton(font, panel, "Pilihan kedua", .52f, .075f, .925f, .25f, onSecondary, primary: true);
            primaryLabel = primary.GetComponentInChildren<Text>(true);
            secondaryLabel = secondary.GetComponentInChildren<Text>(true);
            Hide();
        }

        internal void Show(string heading, string copy, string first, string second = null, bool focusSecond = false)
        {
            title.text = heading;
            detail.text = copy;
            primaryLabel.text = first;
            secondaryLabel.text = second ?? "";
            secondary.gameObject.SetActive(second != null);
            GmMenuButton.Link(primary, null, second != null ? secondary : null);
            GmMenuButton.Link(secondary, primary, null);
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            GmMenuButton.Focus(focusSecond && second != null ? secondary : primary);
        }

        internal void RestoreFocus() => GmMenuButton.FocusWithin(overlay, primary);
        internal void Hide() => overlay.gameObject.SetActive(false);
    }
}
