using System;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GmCutsceneSprite : MonoBehaviour
    {
        [Serializable]
        public struct Directions
        {
            public Sprite[] front, back, left, right;
            // NPCs share one right-facing side sheet. Heroes have separate left and right art.
            public bool mirrorLeft;

            internal Sprite[] GetFrames(Vector2 facing, out bool flip)
            {
                flip = false;
                if (facing.x < -.5f)
                {
                    if (mirrorLeft)
                    {
                        flip = true;
                        return right;
                    }
                    return left;
                }
                if (facing.x > .5f) return right;
                return facing.y > .5f ? back : front;
            }
        }

        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Directions idle, walking;
        private Vector2 facing;

        public void Configure(SpriteRenderer renderer, Directions idleFrames, Directions walkFrames)
        {
            sprite = renderer;
            idle = idleFrames;
            walking = walkFrames;
            sprite.sprite = idle.front[0];
        }

        public void ResetFacing() => facing = Vector2.zero;

        internal bool HasArt => sprite != null && HasFrames(idle.front);

        internal void Sample(Camera camera, Vector3 point, Vector3 heading, bool moving, float seconds)
        {
            facing = GmCardinalFacing.FromWorld(heading, camera, facing);
            Sprite[] frames = moving ? walking.GetFrames(facing, out _) : null;
            bool useWalk = HasFrames(frames);
            frames = (useWalk ? walking : idle).GetFrames(facing, out bool flip);
            if (!HasFrames(frames))
            {
                frames = idle.front;
                flip = false;
            }

            int frame = Mathf.FloorToInt(seconds * (useWalk ? 9f : 5f)) % frames.Length;
            sprite.sprite = frames[frame];
            sprite.flipX = flip;
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            transform.SetPositionAndRotation(point, Quaternion.LookRotation(forward, Vector3.up));
        }

        private static bool HasFrames(Sprite[] frames) =>
            frames != null && frames.Length > 0 && frames[0] != null;
    }
}
