using ProjectVN.Presentation.Actors;
using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmVnPortraitMotion
    {
        private const float EntryTime = .24f, FocusTime = .17f, BlendTime = .14f;
        private const float EntryDistance = 20f, SpeakerLift = 4f;
        private static readonly Color ListenerTint = new Color32(192, 199, 192, 255);
        private readonly Image image, outgoing;
        private readonly RectTransform rect, outgoingRect;
        private readonly float direction;
        private Object actor;
        private Sprite requested;
        private Vector2 rest;
        private float entry, focus, fromFocus, toFocus, focusTime, blend;

        internal GmVnPortraitMotion(Image image, Image outgoing, float direction)
        {
            this.image = image;
            this.outgoing = outgoing;
            this.direction = direction;
            rect = Prepare(image);
            outgoingRect = Prepare(outgoing);
            Clear();
        }

        private static RectTransform Prepare(Image portrait)
        {
            var transform = portrait.rectTransform;
            transform.pivot = new Vector2(.5f, 0f);
            transform.anchoredPosition = Vector2.zero;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            return transform;
        }

        internal void Present(VNActorVisualProfile profile, Sprite sprite, bool speaking)
        {
            if (sprite == null || (profile != null && !profile.HasStageVisual))
            {
                Clear();
                return;
            }

            requested = sprite;
            float target = speaking ? 1f : 0f;
            Object currentActor = profile != null ? (Object)profile : sprite;
            if (actor != currentActor)
            {
                actor = currentActor;
                entry = 0f;
                focusTime = FocusTime;
                fromFocus = focus = toFocus = target;
                image.sprite = sprite;
                ClearOutgoing();
            }
            else
            {
                if (toFocus != target)
                {
                    fromFocus = focus;
                    toFocus = target;
                    focusTime = 0f;
                }
                BeginBlendIfReady();
            }

            rest = profile != null ? profile.DefaultLocalOffset : Vector2.zero;
            float scale = profile != null ? profile.DefaultScale : 1f;
            rect.localScale = outgoingRect.localScale = Vector3.one * scale;
            image.enabled = true;
            Paint();
        }

        internal void Tick(float dt)
        {
            if (actor == null || dt <= 0f ||
                (entry >= EntryTime && focusTime >= FocusTime && !outgoing.enabled)) return;

            entry = Mathf.Min(EntryTime, entry + dt);
            focusTime = Mathf.Min(FocusTime, focusTime + dt);
            focus = Mathf.Lerp(fromFocus, toFocus, Ease(focusTime / FocusTime));

            if (outgoing.enabled)
            {
                blend += dt;
                while (outgoing.enabled && blend >= BlendTime)
                {
                    float elapsedAfterBlend = blend - BlendTime;
                    ClearOutgoing();
                    BeginBlendIfReady();
                    blend = elapsedAfterBlend;
                }
            }

            Paint();
        }

        private void BeginBlendIfReady()
        {
            // Wait for the visible blend before retargeting so rapid advances do not flash.
            if (outgoing.enabled || image.sprite == requested) return;
            outgoing.sprite = image.sprite;
            outgoing.enabled = true;
            image.sprite = requested;
            blend = 0f;
        }

        private void Paint()
        {
            float opacity = Ease(entry / EntryTime);
            var position = rest + new Vector2(direction * EntryDistance * (1f - opacity), SpeakerLift * focus);
            rect.anchoredPosition = outgoingRect.anchoredPosition = position;

            var tint = Color.Lerp(ListenerTint, Color.white, focus);
            tint.a = opacity;
            if (outgoing.enabled)
            {
                // The slight overlap holds the portrait's opacity where the two silhouettes meet.
                float mix = Ease(blend / BlendTime);
                tint.a = opacity * (1f - mix * mix);
                outgoing.color = tint;
                tint.a = opacity * (2f * mix - mix * mix);
            }
            image.color = tint;
        }

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private void ClearOutgoing()
        {
            outgoing.enabled = false;
            outgoing.sprite = null;
            blend = 0f;
        }

        internal void Clear()
        {
            actor = null;
            requested = null;
            ClearOutgoing();
            image.enabled = false;
            image.sprite = null;
            rect.anchoredPosition = outgoingRect.anchoredPosition = Vector2.zero;
            rect.localScale = outgoingRect.localScale = Vector3.one;
        }
    }
}
