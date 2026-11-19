using System;
using PixelCrushers.DialogueSystem;
using ProjectVN.Presentation.Actors;
using ProjectVN.UI.Dialogue;
using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmVnDialogueStage
    {
        private static readonly Color InactiveActor = new Color32(151, 160, 152, 245);
        private static readonly Color Ochre = new Color32(226, 180, 105, 255);
        private readonly RectTransform stage;
        private readonly Image left, right;
        private readonly Text speaker, line;
        private readonly Button advance;
        private readonly VNNextIndicatorController indicator;

        internal GmVnDialogueStage(RectTransform canvas, Font font, Action onAdvance)
        {
            stage = Rect("Panggung", canvas, 0f, 0f, 1f, 1f);
            Fill(stage, new Color32(12, 26, 21, 105)).raycastTarget = true;
            var frame = Rect("Bingkai VN", stage, .5f, .5f, .5f, .5f);
            frame.sizeDelta = new Vector2(1920f, 1080f);
            var plate = Rect("Panel dialog", frame, .5f, 0f, .5f, 0f);
            plate.pivot = new Vector2(.5f, 0f);
            plate.anchoredPosition = new Vector2(0f, 54f);
            plate.sizeDelta = new Vector2(1560f, 188f);
            Fill(plate, Paper);

            var actors = Rect("Aktor", plate, 0f, 1f, 1f, 1f);
            actors.offsetMin = Vector2.zero;
            actors.offsetMax = new Vector2(0f, 360f);
            left = Portrait(actors, "Aktor kiri", .035f, .34f);
            right = Portrait(actors, "Aktor kanan", .66f, .965f);
            Fill(Rect("Garis warna", plate, 0f, .965f, 1f, 1f), Ochre);
            speaker = AddText(font, plate, "Nama pembicara", "", 31, .035f, .645f, .965f, .93f, bold: true);
            speaker.color = Leaf;
            line = AddText(font, plate, "Ucapan", "", 40, .035f, .12f, .955f, .68f);
            line.resizeTextMinSize = 23;

            var key = Rect("Lanjut", plate, .71f, 0f, 1f, 0f);
            key.offsetMin = new Vector2(0f, -42f);
            key.offsetMax = Vector2.zero;
            var surface = Fill(key, Leaf);
            advance = Clickable(surface, onAdvance);
            var caption = AddText(font, key, "Tombol lanjut", "Lanjut   [E / Spasi]", 22,
                .055f, 0f, .86f, 1f, TextAnchor.MiddleRight, true);
            caption.color = Paper;
            var mark = Rect("Tanda lanjut", key, .925f, .5f, .925f, .5f);
            mark.sizeDelta = new Vector2(9f, 9f);
            mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Fill(mark, Ochre);
            indicator = mark.gameObject.AddComponent<VNNextIndicatorController>();
            stage.gameObject.AddComponent<VNDialogueReferenceLayout>().Configure(frame, plate, null, null);
            Hide();
        }

        private static Image Portrait(RectTransform parent, string name, float left, float right)
        {
            var image = Fill(Rect(name, parent, left, 0f, right, 1f), Color.white);
            image.preserveAspect = true;
            return image;
        }

        internal void Show(GmVnDialoguePilot cast, Subtitle subtitle)
        {
            var info = subtitle.speakerInfo;
            var active = cast.Speaker(info);
            var identity = new VNActorIdentity(info?.id ?? 0, info?.nameInDatabase, info?.Name);
            speaker.text = active != null ? active.GetResolvedDisplayName(identity) :
                string.IsNullOrWhiteSpace(identity.DisplayName) ? identity.PixelActorName : identity.DisplayName;
            line.text = subtitle.formattedText.text;
            SetPortrait(left, cast.Left, active);
            SetPortrait(right, cast.Right, active);
            stage.gameObject.SetActive(true);
        }

        private static void SetPortrait(Image image, VNActorVisualProfile profile, VNActorVisualProfile active)
        {
            Sprite sprite = null;
            if (profile != null) sprite = profile.TryResolveDefaultStageSprite(out var result) ?
                result.sprite : profile.DefaultPortraitSprite;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = profile == active ? Color.white : InactiveActor;
        }

        internal void SetContinue(bool visible)
        {
            advance.gameObject.SetActive(visible);
            indicator.SetVisible(visible, true);
        }

        internal void Hide()
        {
            SetContinue(false);
            stage.gameObject.SetActive(false);
        }
    }
}
