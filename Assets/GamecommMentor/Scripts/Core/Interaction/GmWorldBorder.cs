using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmWorldPortal))]
    public sealed class GmWorldBorder : MonoBehaviour
    {
        private static readonly Color OpenColor = Color.white;
        private static readonly Color LockedColor = new Color(.72f, .53f, .31f, .82f);

        [SerializeField] private GmWorldPortal portal;
        [SerializeField] private GmControlGate gate;
        [SerializeField] private BoxCollider barrier;
        [SerializeField] private SpriteRenderer weave, crest;

        public void Configure(GmWorldPortal route, GmControlGate controls, BoxCollider blockingVolume,
            SpriteRenderer strand, SpriteRenderer emblem)
        {
            portal = route;
            gate = controls;
            barrier = blockingVolume;
            weave = strand;
            crest = emblem;
            RefreshArt();
        }

        internal Vector3 ClosestFocus(Vector3 from)
        {
            if (barrier == null) return portal != null ? portal.FocusPosition : transform.position;
            Transform volume = barrier.transform;
            Vector3 local = volume.InverseTransformPoint(from);
            Vector3 half = barrier.size * .5f;
            Vector3 center = barrier.center;
            return volume.TransformPoint(new Vector3(
                Mathf.Clamp(local.x, center.x - half.x, center.x + half.x),
                Mathf.Clamp(local.y, center.y - half.y + .2f, center.y + half.y - .2f),
                center.z));
        }

        private void LateUpdate() => RefreshArt();

        private void RefreshArt()
        {
            if (portal == null || weave == null || crest == null) return;
            bool exploring = gate == null || gate.CanExplore;
            Color tint = portal.IsOpen ? OpenColor : LockedColor;
            if (weave.enabled != exploring) weave.enabled = crest.enabled = exploring;
            if (!weave.color.Equals(tint)) weave.color = crest.color = tint;
        }
    }
}
