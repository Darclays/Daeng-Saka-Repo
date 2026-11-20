using System.Collections;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmDialogueCameraShot
    {
        private const float HalfFadeSeconds = .14f;
        private static readonly float[] SightHeights = { .18f, .82f, 1.48f };
        private readonly Camera camera;
        private readonly GmOrbitFollowCamera orbit;
        private readonly GmVnDialogueUI presentation;
        private readonly Transform player, partner;
        private readonly GmAnimatorDriver playerAnimator;
        private readonly GmNpcDirectionalAnimator partnerAnimator;
        private readonly Pose exploration;
        private readonly Pose? framing;
        private bool held = true, applied;
        private float fade;

        internal bool HasWorldShot => framing.HasValue;

        private GmDialogueCameraShot(Camera camera, GmOrbitFollowCamera orbit, Transform player,
            Transform partner, GmVnDialogueUI presentation, Pose? framing)
        {
            this.camera = camera;
            this.orbit = orbit;
            this.presentation = presentation;
            this.player = player;
            this.partner = partner;
            this.framing = framing;
            exploration = new Pose(camera.transform.position, camera.transform.rotation);
            playerAnimator = player.GetComponentInChildren<GmAnimatorDriver>();
            partnerAnimator = partner.GetComponentInChildren<GmNpcDirectionalAnimator>();
        }

        internal static GmDialogueCameraShot TryHold(Transform player, Transform partner, GmVnDialogueUI presentation)
        {
            Camera camera = Camera.main;
            if (camera == null || player == null || partner == null) return null;
            var orbit = camera.GetComponent<GmOrbitFollowCamera>();
            if (orbit == null || orbit.Target != player) return null;
            Physics.SyncTransforms();
            Pose? framing = PickShot(camera, player, partner);
            var shot = new GmDialogueCameraShot(camera, orbit, player, partner, presentation, framing);
            return orbit.TryHoldDialogueView() ? shot : null;
        }

        internal IEnumerator Enter()
        {
            if (!framing.HasValue || !held) yield break;
            yield return FadeTo(1f);
            if (!held) yield break;
            if (camera != null && player != null && partner != null &&
                player.gameObject.activeInHierarchy && partner.gameObject.activeInHierarchy)
            {
                Pose pose = framing.Value;
                camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                if (playerAnimator != null) playerAnimator.SetDialogueFacing(partner, camera);
                if (partnerAnimator != null) partnerAnimator.SetDialogueFront(true);
                applied = true;
            }
            else if (presentation != null) presentation.SetWorldFraming(false);
            yield return FadeTo(0f);
        }

        internal IEnumerator Exit()
        {
            if (applied)
            {
                yield return FadeTo(1f);
                if (!held) yield break;
                RestorePose();
            }
            if (fade > 0f) yield return FadeTo(0f);
            Restore();
        }

        internal void Restore()
        {
            if (!held) return;
            held = false;
            RestorePose();
            SetFade(0f);
            if (orbit != null) orbit.ReleaseDialogueView();
        }

        private void RestorePose()
        {
            if (!applied) return;
            applied = false;
            if (camera != null) camera.transform.SetPositionAndRotation(exploration.position, exploration.rotation);
            if (playerAnimator != null) playerAnimator.SetDialogueFacing(null, null);
            if (partnerAnimator != null) partnerAnimator.SetDialogueFront(false);
        }

        private IEnumerator FadeTo(float target)
        {
            float initial = fade;
            for (float elapsed = 0f; held && presentation != null && elapsed < HalfFadeSeconds;)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.Clamp01(elapsed / HalfFadeSeconds);
                SetFade(Mathf.Lerp(initial, target, amount * amount * (3f - 2f * amount)));
                yield return null;
            }
            if (held) SetFade(target);
        }

        private void SetFade(float alpha)
        {
            fade = alpha;
            if (presentation != null) presentation.SetCameraFade(alpha);
        }

        private static Pose? PickShot(Camera camera, Transform player, Transform partner)
        {
            Vector3 axis = Vector3.ProjectOnPlane(partner.position - player.position, Vector3.up);
            float spacing = axis.magnitude;
            if (camera.orthographic || spacing < .75f) return null;

            axis /= spacing;
            Vector3 side = Vector3.Cross(Vector3.up, axis);
            Vector3 middle = (player.position + partner.position) * .5f;
            Vector3 focus = middle + Vector3.up * .84f;
            float distance = Mathf.Clamp(5f + spacing * .3f, 5.2f, 6.4f);
            float vertical = 2f * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
            float horizontal = vertical * Mathf.Max(.1f, camera.aspect);
            float eyeRadius = Mathf.Max(.24f,
                camera.nearClipPlane * Mathf.Sqrt(1f + (horizontal * horizontal + vertical * vertical) * .25f));
            Pose? best = null;
            float nearest = float.PositiveInfinity;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                // Both viewpoints stay on the Saka side of Nelayan's front-facing sprite.
                Vector3 eye = middle + Vector3.up * 2f + distance * (-axis * .64f + side * (.77f * sign));
                float score = (eye - camera.transform.position).sqrMagnitude;
                if (score >= nearest || Physics.CheckSphere(eye, eyeRadius, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)) continue;
                var pose = new Pose(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                if (!FitsFrame(pose, player.position, horizontal, vertical, out Vector2 first) ||
                    !FitsFrame(pose, partner.position, horizontal, vertical, out Vector2 second) ||
                    (first.y + .02f > second.x && second.y + .02f > first.x)) continue;
                if (!HasSight(eye, player, partner, player.position) ||
                    !HasSight(eye, player, partner, partner.position)) continue;
                best = pose;
                nearest = score;
            }
            return best;
        }

        private static bool FitsFrame(Pose pose, Vector3 feet, float horizontal, float vertical, out Vector2 span)
        {
            Quaternion inverse = Quaternion.Inverse(pose.rotation);
            Vector3 low = inverse * (feet - pose.position);
            Vector3 high = inverse * (feet + Vector3.up * 1.62f - pose.position);
            span = default;
            if (low.z <= .1f || high.z <= .1f ||
                .5f + low.y / (vertical * low.z) < .24f ||
                .5f + high.y / (vertical * high.z) > .92f) return false;
            float nearX = .5f + low.x / (horizontal * low.z);
            float farX = .5f + high.x / (horizontal * high.z);
            float halfWidth = .30f / (horizontal * Mathf.Min(low.z, high.z));
            span = new Vector2(Mathf.Min(nearX, farX) - halfWidth, Mathf.Max(nearX, farX) + halfWidth);
            return span.x > .06f && span.y < .94f;
        }

        private static bool HasSight(Vector3 eye, Transform player, Transform partner, Vector3 feet)
        {
            foreach (float height in SightHeights)
            {
                Vector3 ray = feet + Vector3.up * height - eye;
                foreach (RaycastHit hit in Physics.RaycastAll(eye, ray.normalized, ray.magnitude,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null) continue;
                    Transform blocker = hit.collider.transform;
                    if (!blocker.IsChildOf(player) && !blocker.IsChildOf(partner)) return false;
                }
            }
            return true;
        }
    }
}
