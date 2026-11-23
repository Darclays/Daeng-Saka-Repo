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
        private readonly Text title, detail, primaryLabel, secondaryLabel, extraLabel;
        private readonly Button primary, secondary, extra;
        private Button defaultFocus;

        internal bool IsOpen => overlay.gameObject.activeSelf;

        internal GmControlDialog(RectTransform parent, Font font, Action onPrimary, Action onSecondary, Action onExtra = null)
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
            if (onExtra != null)
            {
                extra = AddButton(font, panel, "Pilihan ketiga", .65f, .075f, .925f, .25f, onExtra);
                extraLabel = extra.GetComponentInChildren<Text>(true);
            }
            Hide();
        }

        internal void Show(string heading, string copy, string first, string second = null, bool focusSecond = false,
            string third = null)
        {
            if (!IsOpen) GmGameplayAudio.Ui(GmUiSound.Open);
            title.text = heading;
            detail.text = copy;
            primaryLabel.text = first ?? "";
            primary.gameObject.SetActive(first != null);
            secondaryLabel.text = second ?? "";
            secondary.gameObject.SetActive(second != null);
            bool hasExtra = extra != null && third != null;
            if (extra != null)
            {
                extraLabel.text = third ?? "";
                extra.gameObject.SetActive(hasExtra);
                SetWidth(primary, .075f, hasExtra ? .335f : .48f);
                SetWidth(secondary, hasExtra ? .365f : .52f, hasExtra ? .625f : .925f);
                GmMenuButton.Link(extra, secondary, null);
            }
            GmMenuButton.Link(primary, null, second != null ? secondary : hasExtra ? extra : null);
            GmMenuButton.Link(secondary, primary, hasExtra ? extra : null);
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            defaultFocus = focusSecond && second != null ? secondary : first != null ? primary : null;
            GmMenuButton.Focus(defaultFocus);
        }

        private static void SetWidth(Button button, float left, float right)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(left, rect.anchorMin.y);
            rect.anchorMax = new Vector2(right, rect.anchorMax.y);
        }

        internal void FocusExtra() => GmMenuButton.Focus(extra);
        internal void RestoreFocus() => GmMenuButton.FocusWithin(overlay, defaultFocus);
        internal void Hide() => overlay.gameObject.SetActive(false);
    }
}
