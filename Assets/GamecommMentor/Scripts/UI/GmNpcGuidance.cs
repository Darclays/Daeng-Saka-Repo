using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    internal enum GmNpcGuideIcon { None, Dialogue, Shop, Quest }

    internal sealed class GmNpcGuidance
    {
        private const float IconSize = 34f;
        private const float HeadOffset = 16f;
        private const float DialogueRange = 12f;
        private const float ShopRange = 16f;
        private const float QuestRange = 20f;
        private const int FrameCount = 4;
        private const int IconKinds = 3;

        private readonly RectTransform layer;
        private readonly Texture2D artwork;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[24];
        private readonly List<Candidate> candidates = new List<Candidate>();
        private readonly List<Candidate> visible = new List<Candidate>();
        private readonly List<RawImage> badges = new List<RawImage>();

        private readonly struct Candidate
        {
            public Candidate(Transform actor, GmNpcGuideIcon icon, Vector2 position, float distanceSquared, bool tracked)
            {
                Actor = actor;
                Icon = icon;
                Position = position;
                DistanceSquared = distanceSquared;
                Priority = (int)icon + (tracked ? IconKinds : 0);
            }

            public Transform Actor { get; }
            public GmNpcGuideIcon Icon { get; }
            public Vector2 Position { get; }
            public float DistanceSquared { get; }
            public int Priority { get; }
        }

        public GmNpcGuidance(RectTransform canvas)
        {
            layer = (RectTransform)new GameObject("NPC Guidance Layer", typeof(RectTransform)).transform;
            layer.SetParent(canvas, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            artwork = Resources.Load<Texture2D>("DaengSaka/WorldGuidance/NpcIcons");
        }

        public void Render(GmInteractor player, Camera camera, Transform tracked)
        {
            visible.Clear();
            if (player != null && player.isActiveAndEnabled && camera != null && camera.isActiveAndEnabled && artwork != null)
            {
                FindCandidates(player, camera, tracked);
                float time = Time.unscaledTime;
                foreach (Candidate item in candidates)
                {
                    if (Overlaps(item.Position)) continue;
                    int index = visible.Count;
                    visible.Add(item);
                    RawImage badge = index < badges.Count ? badges[index] : CreateBadge();
                    badge.rectTransform.anchoredPosition = item.Position;
                    int ticks = Mathf.FloorToInt(time * 8f + index) % 12;
                    int frame = Math.Min(FrameCount - 1, Math.Min(ticks, 11 - ticks));
                    float width = 1f / (IconKinds * FrameCount);
                    float left = (((int)item.Icon - 1) * FrameCount + frame) * width;
                    badge.uvRect = new Rect(left, 0f, width, 1f);
                    badge.gameObject.SetActive(true);
                }
            }
            for (int index = visible.Count; index < badges.Count; index++) badges[index].gameObject.SetActive(false);
        }

        public bool TryGetPosition(Transform target, out Vector2 position)
        {
            foreach (Candidate item in visible)
            {
                if (target != item.Actor && !target.IsChildOf(item.Actor)) continue;
                position = item.Position;
                return true;
            }
            position = default;
            return false;
        }

        private void FindCandidates(GmInteractor player, Camera camera, Transform tracked)
        {
            candidates.Clear();
            Vector3 origin = player.transform.position;
            Rect viewport = camera.pixelRect;
            foreach (GmPixelCrushersConversation npc in GmPixelCrushersConversation.ActiveConversations)
            {
                if (npc == null || npc.gameObject.scene != player.gameObject.scene) continue;
                float distance = (npc.transform.position - origin).sqrMagnitude;
                if (distance > QuestRange * QuestRange) continue;
                GmNpcGuideIcon icon = npc.GuidanceIcon(player);
                if (icon == GmNpcGuideIcon.None || !InRange(icon, distance) || !npc.TryGetGuidanceHead(out Vector3 head)) continue;
                Vector3 screen = camera.WorldToScreenPoint(head);
                if (screen.z <= camera.nearClipPlane || !viewport.Contains((Vector2)screen) ||
                    screen.y > viewport.yMax - viewport.height * 0.13f || screen.y < viewport.yMin + viewport.height * 0.09f)
                    continue;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 point) ||
                    IsOccluded(camera.transform.position, head, player.transform, npc.transform)) continue;
                bool trackThis = tracked != null && (tracked == npc.transform || tracked.IsChildOf(npc.transform));
                candidates.Add(new Candidate(npc.transform, icon, point + Vector2.up * HeadOffset, distance, trackThis));
            }
            candidates.Sort(CompareMarkers);
        }

        private static bool InRange(GmNpcGuideIcon icon, float distanceSquared)
        {
            float range = icon == GmNpcGuideIcon.Quest ? QuestRange : icon == GmNpcGuideIcon.Shop ? ShopRange : DialogueRange;
            return distanceSquared <= range * range;
        }

        private static int CompareMarkers(Candidate left, Candidate right)
        {
            int byPriority = right.Priority.CompareTo(left.Priority);
            return byPriority != 0 ? byPriority : left.DistanceSquared.CompareTo(right.DistanceSquared);
        }

        private bool IsOccluded(Vector3 from, Vector3 head, Transform player, Transform actor)
        {
            Vector3 delta = head - from;
            float distance = delta.magnitude;
            if (distance <= 0.01f) return false;
            int count = Physics.RaycastNonAlloc(from, delta / distance, obstructionHits, distance, ~0, QueryTriggerInteraction.Ignore);
            if (count == obstructionHits.Length) return true;
            for (int i = 0; i < count; i++)
            {
                Transform hit = obstructionHits[i].collider.transform;
                if (!hit.IsChildOf(player) && !hit.IsChildOf(actor)) return true;
            }
            return false;
        }

        private bool Overlaps(Vector2 point)
        {
            foreach (Candidate item in visible)
            {
                Vector2 delta = item.Position - point;
                if (Mathf.Abs(delta.x) < IconSize + 3f && Mathf.Abs(delta.y) < IconSize + 3f) return true;
            }
            return false;
        }

        private RawImage CreateBadge()
        {
            RawImage badge = new GameObject("NPC Guidance", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            RectTransform rect = badge.rectTransform;
            rect.SetParent(layer, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * IconSize;
            badge.texture = artwork;
            badge.raycastTarget = false;
            badges.Add(badge);
            return badge;
        }
    }
}
