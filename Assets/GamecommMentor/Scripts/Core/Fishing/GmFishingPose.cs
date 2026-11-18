using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmFishingPose : IDisposable
    {
        private const int Width = 64, Height = 52, Columns = 16;
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

        internal Vector3 Hand => original.transform.TransformPoint(new Vector3(.04f, 1.64f, 0f));

        internal void Draw(GmFishingPhase phase, float elapsed)
        {
            int index;
            switch (phase)
            {
                case GmFishingPhase.Waiting:
                    index = elapsed < 28f / 26f ? Frame(0, 28, 26f, elapsed, false)
                        : Frame(28, 8, 9f, elapsed - 28f / 26f, true);
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
                frames[index] = Sprite.Create(atlas, rect, new Vector2(36f / Width, 3f / Height),
                    10f, 0, SpriteMeshType.FullRect);
            }
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
