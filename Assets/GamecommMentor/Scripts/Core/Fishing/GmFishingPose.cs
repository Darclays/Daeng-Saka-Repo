using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmFishingPose : IDisposable
    {
        private const int Width = 64, Height = 52, Columns = 16;
        private const float CastFps = 26f, PivotX = 36f, PivotY = 3f, PixelsPerUnit = 10f;
        internal const float ReleaseTime = 15f / CastFps;

        // In upper-left atlas pixels. After the forward cast, the painted rod exits row 48.
        private static readonly float[] ReleaseTipX = { 30.5f, 33.5f, 33.5f, 35.5f, 35.5f };
        private static readonly float[] ReelTipX =
        {
            36f, 36f, 36.5f, 35.5f, 35.5f, 35.5f, 35.5f, 35.5f, 34.5f, 34.5f,
            35.5f, 35.5f, 35.5f, 35.5f, 36.5f, 35.5f, 35.5f, 35.5f, 35.5f, 35.5f,
            36.5f, 36.5f, 35.5f, 35.5f, 35.5f, 35.5f, 34.5f, 35.5f, 36.5f, 36.5f,
            35.5f, 35.5f, 35.5f, 35.5f, 34.5f, 34.5f, 35.5f, 35.5f, 35.5f, 35.5f,
            35.5f, 35.5f
        };
        private int frameIndex;
        private readonly Texture2D atlas;
        private readonly Sprite[] frames = new Sprite[133];
        private readonly SpriteRenderer original;
        private readonly SpriteRenderer fishing;
        private bool originalEnabled, showing;

        internal GmFishingPose(SpriteRenderer original, Texture2D atlas)
        {
            this.original = original;
            this.atlas = atlas;
            var go = new GameObject("Saka - Fishing Pose");
            go.transform.SetParent(original.transform, false);
            go.layer = original.gameObject.layer;
            fishing = go.AddComponent<SpriteRenderer>();
            fishing.sharedMaterial = original.sharedMaterial;
            fishing.sortingLayerID = original.sortingLayerID;
            fishing.sortingOrder = original.sortingOrder;
            fishing.enabled = false;
        }

        internal void Show()
        {
            originalEnabled = original.enabled;
            original.enabled = false;
            fishing.color = original.color;
            fishing.enabled = showing = true;
        }

        internal void FaceCamera(Camera camera)
        {
            Vector3 forward = Vector3.ProjectOnPlane(fishing.transform.position - camera.transform.position, Vector3.up);
            if (forward.sqrMagnitude > .0001f) fishing.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        internal Vector3 RodTip
        {
            get
            {
                float x = PivotX;
                if (frameIndex >= 15 && frameIndex <= 19) x = ReleaseTipX[frameIndex - 15];
                else if (frameIndex >= 51 && frameIndex <= 92) x = ReelTipX[frameIndex - 51];
                return fishing.transform.TransformPoint(new Vector3((x - PivotX) / PixelsPerUnit,
                    (Height - 48.5f - PivotY) / PixelsPerUnit, 0f));
            }
        }

        internal void Draw(GmFishingPhase phase, float elapsed)
        {
            int index;
            switch (phase)
            {
                case GmFishingPhase.Waiting:
                    index = elapsed < 28f / CastFps ? Frame(0, 28, CastFps, elapsed, false)
                        : Frame(28, 8, 9f, elapsed - 28f / CastFps, true);
                    break;
                case GmFishingPhase.Bite: index = Frame(36, 15, 14f, elapsed, false); break;
                case GmFishingPhase.Reeling: index = Frame(51, 42, 22f, elapsed, true); break;
                case GmFishingPhase.Caught: index = Frame(93, 19, 14f, elapsed, false); break;
                case GmFishingPhase.Escaped: index = Frame(112, 21, 16f, elapsed, false); break;
                default: index = 0; break;
            }
            if (frames[index] == null)
            {
                var rect = new Rect(index % Columns * Width, atlas.height - (index / Columns + 1) * Height, Width, Height);
                frames[index] = Sprite.Create(atlas, rect, new Vector2(PivotX / Width, PivotY / Height),
                    PixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            frameIndex = index;
            fishing.sprite = frames[index];
        }

        private static int Frame(int start, int count, float fps, float time, bool loop)
        {
            int index = Mathf.FloorToInt(time * fps);
            return start + (loop ? index % count : Math.Min(count - 1, index));
        }

        internal void Hide()
        {
            if (!showing) return;
            showing = false;
            if (fishing != null) fishing.enabled = false;
            if (original != null) original.enabled = originalEnabled;
        }

        public void Dispose()
        {
            Hide();
            if (fishing != null) Object.Destroy(fishing.gameObject);
            foreach (Sprite sprite in frames)
                if (sprite != null) Object.Destroy(sprite);
        }
    }
}
