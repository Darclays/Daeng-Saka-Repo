using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class GmMainQuestEncounterHint : MonoBehaviour
    {
        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private GmControlGate controls;
        private RectTransform canvasRect;
        private RectTransform objectivePanel;
        private Text objectiveLabel;
        private Text markerLabel;
        private GmEnemyEncounter displayedEncounter;

        public void Configure(GmMainQuestProgress progress, GmControlGate controlGate)
        {
            quest = progress;
            controls = controlGate;
        }

        private void Awake()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 39;
            canvasRect = (RectTransform)transform;
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Image background = new GameObject("Objective", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            background.color = new Color(0.035f, 0.05f, 0.065f, 0.92f);
            background.raycastTarget = false;
            objectivePanel = background.rectTransform;
            objectivePanel.SetParent(canvasRect, false);
            objectivePanel.anchorMin = objectivePanel.anchorMax = objectivePanel.pivot = new Vector2(0f, 1f);
            objectivePanel.anchoredPosition = new Vector2(24f, -24f);
            objectivePanel.sizeDelta = new Vector2(430f, 42f);
            objectiveLabel = CreateLabel("Objective Text", objectivePanel, font, TextAnchor.MiddleLeft);
            RectTransform textRect = objectiveLabel.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);
            objectivePanel.gameObject.SetActive(false);

            markerLabel = CreateLabel("Encounter Marker", canvasRect, font, TextAnchor.MiddleCenter);
            markerLabel.color = new Color(1f, 0.88f, 0.62f);
            RectTransform markerRect = markerLabel.rectTransform;
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(320f, 28f);
            Outline outline = markerLabel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.025f, 0.03f, 0.94f);
            outline.effectDistance = new Vector2(1f, -1f);
            markerLabel.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            GmEnemyEncounter encounter = quest != null && quest.isActiveAndEnabled ? quest.ActiveEncounter : null;
            bool visible = encounter != null && encounter.isActiveAndEnabled && !encounter.IsDefeated &&
                controls != null && controls.CanExplore;
            objectivePanel.gameObject.SetActive(visible);
            if (!visible)
            {
                markerLabel.gameObject.SetActive(false);
                return;
            }

            if (displayedEncounter != encounter)
            {
                displayedEncounter = encounter;
                string name = encounter.gameObject.name;
                objectiveLabel.text = "Kalahkan " + name;
                markerLabel.text = "▼ " + name;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                markerLabel.gameObject.SetActive(false);
                return;
            }
            Vector3 screen = camera.WorldToScreenPoint(encounter.transform.position + Vector3.up * 1.65f);
            bool onScreen = screen.z > 0f && camera.pixelRect.Contains((Vector2)screen);
            Vector2 local = Vector2.zero;
            bool positioned = onScreen && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            markerLabel.gameObject.SetActive(positioned);
            if (positioned) markerLabel.rectTransform.anchoredPosition = local;
        }

        private static Text CreateLabel(string name, RectTransform parent, Font font, TextAnchor alignment)
        {
            Text label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.rectTransform.SetParent(parent, false);
            label.font = font;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }
    }
}
