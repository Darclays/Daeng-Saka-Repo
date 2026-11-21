using System;
using Subtitle = PixelCrushers.DialogueSystem.Subtitle;
using ProjectVN.Preferences;
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
        private const float LineChangeGrace = .12f, PanelEntryTime = .13f;
        private static readonly Color Ochre = new Color32(226, 180, 105, 255);
        private static readonly Color PortraitBackdrop = new Color32(12, 26, 21, 105);
        private static readonly Color DimBackdrop = new Color32(12, 26, 21, 48);
        private readonly RectTransform stage, actors;
        private readonly CanvasGroup stageOpacity;
        private readonly Image backdrop, cameraFade;
        private readonly GmVnPortraitMotion left, right;
        private readonly GmVnPortraitCues portraitCues;
        private readonly GmVnLineFade lineFade;
        private readonly Text speaker, line, actionCaption, speedCaption, backdropCaption;
        private readonly Button advance, backdropButton;
        private readonly VNNextIndicatorController indicator;
        private float clearCaptionAfter = -1f;
        private float panelTime;
        private bool worldFraming;

        internal GmVnDialogueStage(RectTransform canvas, Font font, Action onAdvance, Action onChangeSpeed,
            Action onChangeBackdrop)
        {
            portraitCues = Resources.Load<GmVnPortraitCues>(GmVnPortraitCues.ResourcePath);
            // This layer stays behind the dialogue panel and can finish the cut after the panel closes.
            cameraFade = Fill(Rect("Transisi kamera", canvas, 0f, 0f, 1f, 1f), Color.clear);
            cameraFade.raycastTarget = false;
            cameraFade.enabled = false;
            stage = Rect("Panggung", canvas, 0f, 0f, 1f, 1f);
            stageOpacity = stage.gameObject.AddComponent<CanvasGroup>();
            backdrop = Fill(stage, PortraitBackdrop);
            backdrop.raycastTarget = true;
            var frame = Rect("Bingkai VN", stage, .5f, .5f, .5f, .5f);
            frame.sizeDelta = new Vector2(1920f, 1080f);
            var plate = Rect("Panel dialog", frame, .5f, 0f, .5f, 0f);
            plate.pivot = new Vector2(.5f, 0f);
            plate.anchoredPosition = new Vector2(0f, 54f);
            plate.sizeDelta = new Vector2(1560f, 188f);
            Fill(plate, Paper);

            actors = Rect("Aktor", plate, 0f, 1f, 1f, 1f);
            actors.offsetMin = Vector2.zero;
            actors.offsetMax = new Vector2(0f, 360f);
            left = Portrait(actors, "Aktor kiri", .035f, .34f, -1f);
            right = Portrait(actors, "Aktor kanan", .66f, .965f, 1f);
            Fill(Rect("Garis warna", plate, 0f, .965f, 1f, 1f), Ochre);
            speaker = AddText(font, plate, "Nama pembicara", "", 31, .035f, .645f, .965f, .93f, bold: true);
            speaker.color = Leaf;
            line = AddText(font, plate, "Ucapan", "", 40, .035f, .12f, .955f, .68f);
            line.resizeTextMinSize = 23;
            lineFade = line.gameObject.AddComponent<GmVnLineFade>();

            var tempo = Rect("Tempo baca", plate, 0f, 0f, .27f, 0f);
            tempo.offsetMin = new Vector2(0f, -42f);
            tempo.offsetMax = Vector2.zero;
            var tempoButton = Clickable(Fill(tempo, Leaf), onChangeSpeed);
            // Keyboard commands are owned by the dialogue UI, including after a pointer click.
            tempoButton.navigation = new Navigation { mode = Navigation.Mode.None };
            speedCaption = AddText(font, tempo, "Label tempo", "", 22, .07f, 0f, .96f, 1f,
                TextAnchor.MiddleLeft, true);
            speedCaption.color = Paper;

            var backdropControl = Rect("Mode latar", plate, .28f, 0f, .67f, 0f);
            backdropControl.offsetMin = new Vector2(0f, -42f);
            backdropControl.offsetMax = Vector2.zero;
            backdropButton = Clickable(Fill(backdropControl, Leaf), onChangeBackdrop);
            backdropButton.navigation = new Navigation { mode = Navigation.Mode.None };
            backdropCaption = AddText(font, backdropControl, "Label mode latar", "", 22, .04f, 0f, .96f, 1f,
                TextAnchor.MiddleCenter, true);
            backdropCaption.color = Paper;
            backdropButton.gameObject.SetActive(false);

            var key = Rect("Lanjut", plate, .68f, 0f, 1f, 0f);
            key.offsetMin = new Vector2(0f, -42f);
            key.offsetMax = Vector2.zero;
            var surface = Fill(key, Leaf);
            advance = Clickable(surface, onAdvance);
            advance.navigation = new Navigation { mode = Navigation.Mode.None };
            actionCaption = AddText(font, key, "Tombol lanjut", "", 22,
                .055f, 0f, .86f, 1f, TextAnchor.MiddleRight, true);
            actionCaption.color = Paper;
            var mark = Rect("Tanda lanjut", key, .925f, .5f, .925f, .5f);
            mark.sizeDelta = new Vector2(9f, 9f);
            mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Fill(mark, Ochre);
            indicator = mark.gameObject.AddComponent<VNNextIndicatorController>();
            stage.gameObject.AddComponent<VNDialogueReferenceLayout>().Configure(frame, plate, null, null);
            Hide();
        }

        private static GmVnPortraitMotion Portrait(RectTransform parent, string name, float left, float right, float direction)
        {
            var outgoing = Fill(Rect("Transisi " + name, parent, left, 0f, right, 1f), Color.white);
            var image = Fill(Rect(name, parent, left, 0f, right, 1f), Color.white);
            return new GmVnPortraitMotion(image, outgoing, direction);
        }

        internal void Show(GmVnDialoguePilot cast, string conversation, Subtitle subtitle, GmVnTextReveal reveal)
        {
            var info = subtitle.speakerInfo;
            var active = cast.Speaker(info);
            var identity = new VNActorIdentity(info?.id ?? 0, info?.nameInDatabase, info?.Name);
            speaker.text = active != null ? active.GetResolvedDisplayName(identity) :
                string.IsNullOrWhiteSpace(identity.DisplayName) ? identity.PixelActorName : identity.DisplayName;
            if (cast.UsesSpeakerPortraits)
            {
                SetPortrait(left, active, active, null, conversation, subtitle, allowUnmappedSpeaker: true);
            }
            else if (!worldFraming)
            {
                var listener = cast.Speaker(subtitle.listenerInfo);
                SetPortrait(left, cast.Left, active, listener, conversation, subtitle);
                SetPortrait(right, cast.Right, active, listener, conversation, subtitle);
            }
            BindLine(reveal);
        }

        internal void Show(VNActorVisualProfile profile, string displayName, GmVnTextReveal reveal)
        {
            var identity = new VNActorIdentity(0, null, displayName);
            speaker.text = profile != null ? profile.GetResolvedDisplayName(identity) : displayName;
            SetPortrait(left, profile, profile, null, null, null);
            BindLine(reveal);
        }

        private void BindLine(GmVnTextReveal reveal)
        {
            clearCaptionAfter = -1f;
            lineFade.Bind(line, reveal);
            stage.gameObject.SetActive(true);
        }

        internal void SetWorldFraming(bool value, bool singlePortrait)
        {
            worldFraming = value;
            backdrop.color = value || singlePortrait ? Color.clear : PortraitBackdrop;
            actors.offsetMax = new Vector2(0f, singlePortrait ? 264f : 360f);
            if (value || singlePortrait)
            {
                left.Clear();
                right.Clear();
            }
        }

        internal void SetBackdropMode(bool available, bool dim, string caption)
        {
            backdropButton.gameObject.SetActive(available);
            backdropCaption.text = caption;
            if (available) backdrop.color = dim ? DimBackdrop : Color.clear;
        }

        internal void SetCameraFade(float alpha)
        {
            cameraFade.color = new Color(.04f, .07f, .05f, alpha);
            cameraFade.enabled = alpha > 0f;
        }

        private void SetPortrait(GmVnPortraitMotion slot, VNActorVisualProfile profile, VNActorVisualProfile active,
            VNActorVisualProfile listener, string conversation, Subtitle subtitle, bool allowUnmappedSpeaker = false)
        {
            Sprite sprite = profile != null ? profile.DefaultPortraitSprite : null;
            if (profile != null)
            {
                // The original profiles put the same placeholder in both slots; keep their expression
                // lookup. A distinct dialogue portrait can replace that placeholder independently.
                if ((sprite == null || sprite == profile.DefaultStageSprite) && profile.TryResolveDefaultStageSprite(out var result))
                    sprite = result.sprite;
                if (sprite == null && profile == active) sprite = subtitle?.speakerInfo?.portrait;
                else if (sprite == null && profile == listener) sprite = subtitle?.listenerInfo?.portrait;

                if (portraitCues != null && subtitle?.dialogueEntry != null)
                    sprite = portraitCues.Resolve(conversation, subtitle.dialogueEntry.id, profile, sprite);
            }
            else if (allowUnmappedSpeaker)
                sprite = subtitle?.speakerInfo?.portrait;
            slot.Present(profile, sprite, profile == active);
        }

        internal void RefreshText() => line.SetVerticesDirty();

        internal void EndSubtitle()
        {
            SetAction(false, false);
            clearCaptionAfter = LineChangeGrace;
        }

        internal void Tick(float dt)
        {
            if (!stage.gameObject.activeSelf) return;
            if (panelTime < PanelEntryTime)
            {
                panelTime = Mathf.Min(PanelEntryTime, panelTime + dt);
                float t = panelTime / PanelEntryTime;
                stageOpacity.alpha = t * t * (3f - 2f * t);
            }
            left.Tick(dt);
            right.Tick(dt);
            if (clearCaptionAfter < 0f) return;
            clearCaptionAfter -= dt;
            if (clearCaptionAfter <= 0f)
            {
                clearCaptionAfter = -1f;
                speaker.text = line.text = string.Empty;
            }
        }

        internal void SetSpeed(VNTextSpeedMode speed) => speedCaption.text = GmVnReadingSpeed.Caption(speed);

        internal void SetAction(bool writing, bool canContinue)
        {
            advance.gameObject.SetActive(writing || canContinue);
            actionCaption.text = writing ? "Tampilkan semua   [E / Spasi]" : "Lanjut   [E / Spasi]";
            indicator.SetVisible(!writing && canContinue, true);
        }

        internal void Hide()
        {
            clearCaptionAfter = -1f;
            panelTime = 0f;
            stageOpacity.alpha = 0f;
            speaker.text = line.text = string.Empty;
            left.Clear();
            right.Clear();
            SetAction(false, false);
            stage.gameObject.SetActive(false);
        }
    }
}
