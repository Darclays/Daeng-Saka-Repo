using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmMenuButton : Button
    {
        private RectTransform marker;

        public override void OnSubmit(BaseEventData eventData)
        {
            if (IsActive() && IsInteractable() && GmGamepadInput.MaySubmitUi()) base.OnSubmit(eventData);
        }

        public override void OnMove(AxisEventData eventData)
        {
            GameObject before = EventSystem.current?.currentSelectedGameObject;
            base.OnMove(eventData);
            if (IsActive() && IsInteractable() && EventSystem.current?.currentSelectedGameObject != before)
                GmGameplayAudio.Ui(GmUiSound.Move);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsActive() && IsInteractable() && !GmGamepadInput.UsingGamepad)
                GmGameplayAudio.Ui(GmUiSound.Move);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            if (targetGraphic == null || !IsInteractable()) return;
            if (marker == null) MakeMarker();
            marker.gameObject.SetActive(true);
            if (GmGamepadInput.UsingGamepad) RevealInScrollView(GetComponentInParent<ScrollRect>(), targetGraphic.rectTransform);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            HideMarker();
            base.OnDeselect(eventData);
        }

        protected override void OnDisable()
        {
            HideMarker();
            base.OnDisable();
        }

        private void HideMarker()
        {
            if (marker != null) marker.gameObject.SetActive(false);
        }

        internal static void RevealInScrollView(ScrollRect scroll, RectTransform rect)
        {
            if (scroll == null || !scroll.vertical || scroll.viewport == null || scroll.content == null) return;
            Canvas.ForceUpdateCanvases();
            Rect bounds = scroll.viewport.rect;
            float travel = scroll.content.rect.height - bounds.height;
            if (travel <= 0f) return;
            Bounds item = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, rect);
            float below = item.min.y - bounds.yMin;
            float above = item.max.y - bounds.yMax;
            if (below < 0f || above > 0f)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition +
                    (below < 0f ? below : above) / travel);
            }
        }

        private void MakeMarker()
        {
            marker = Rect("Sorot fokus", targetGraphic.rectTransform, 0f, 0f, 1f, 1f);
            marker.offsetMin = new Vector2(-2f, -2f);
            marker.offsetMax = new Vector2(2f, 2f);
            DrawEdge("Atas", 0f, 1f, 1f, 1f, new Vector2(0f, -3f), Vector2.zero);
            DrawEdge("Bawah", 0f, 0f, 1f, 0f, Vector2.zero, new Vector2(0f, 3f));
            DrawEdge("Kiri", 0f, 0f, 0f, 1f, Vector2.zero, new Vector2(3f, 0f));
            DrawEdge("Kanan", 1f, 0f, 1f, 1f, new Vector2(-3f, 0f), Vector2.zero);
            RectTransform diamond = Rect("Penanda fokus", marker, 0f, 1f, 0f, 1f);
            diamond.sizeDelta = new Vector2(10f, 10f);
            diamond.anchoredPosition = new Vector2(12f, 0f);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Paint(diamond);
        }

        private void DrawEdge(string name, float left, float bottom, float right, float top, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, marker, left, bottom, right, top);
            rect.offsetMin = min;
            rect.offsetMax = max;
            Paint(rect);
        }

        private static void Paint(RectTransform rect)
        {
            Fill(rect, new Color(1f, .89f, .58f));
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.09f, .14f, .11f, 1f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
        }

        internal static bool Available(Button button) => button != null && button.IsActive() && button.IsInteractable();

        internal static void Focus(Button button)
        {
            EventSystem events = EventSystem.current;
            GameObject target = Available(button) ? button.gameObject : null;
            if (events != null && !events.alreadySelecting && events.currentSelectedGameObject != target)
                events.SetSelectedGameObject(target);
        }

        internal static void FocusWithin(Transform scope, Button preferred = null)
        {
            EventSystem events = EventSystem.current;
            if (events == null || scope == null) return;
            GameObject current = events.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(scope) && Available(current.GetComponent<Button>())) return;
            if (Available(preferred)) { Focus(preferred); return; }
            foreach (Button button in scope.GetComponentsInChildren<Button>())
            {
                if (!Available(button)) continue;
                Focus(button);
                return;
            }
        }

        internal static void Link(Button button, Button previous, Button next)
        {
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = previous,
                selectOnRight = next,
                selectOnUp = previous,
                selectOnDown = next
            };
        }
    }
}
