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
        [SerializeField] private GmWorldPortal routePortal;
        [SerializeField] private string routeDestination;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private bool showQuestCompletion;
        private RectTransform canvasRect;
        private RectTransform objectivePanel;
        private Text objectiveLabel;
        private Text markerLabel;
        private Transform displayedTarget;
        private GmQuestJournal journal;
        private Text questTitle;
        private Text journalKeys;

        public void Configure(GmMainQuestProgress progress, GmControlGate controlGate, bool showQuestCompletion = false)
        {
            quest = progress;
            controls = controlGate;
            this.showQuestCompletion = showQuestCompletion;
        }

        public void SetRoute(GmWorldPortal portal, string destination, Camera camera)
        {
            routePortal = portal;
            routeDestination = destination;
            viewCamera = camera;
        }

        private void Awake()
        {
            journal = GetComponent<GmQuestJournal>();
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

            if (journal != null)
            {
                objectivePanel.sizeDelta = new Vector2(430f, 62f);
                textRect.offsetMax = new Vector2(-14f, -23f);
                questTitle = CreateLabel("Tracked Quest", objectivePanel, font, TextAnchor.MiddleLeft);
                questTitle.fontSize = 14;
                questTitle.color = new Color(0.76f, 0.89f, 0.77f);
                RectTransform titleRect = questTitle.rectTransform;
                titleRect.anchorMin = titleRect.anchorMax = new Vector2(0f, 1f);
                titleRect.pivot = new Vector2(0f, 1f);
                titleRect.anchoredPosition = new Vector2(14f, -6f);
                titleRect.sizeDelta = new Vector2(400f, 20f);
                journalKeys = CreateLabel("Journal Shortcuts", canvasRect, font, TextAnchor.MiddleRight);
                journalKeys.text = "[J] Jurnal     [M] Peta";
                journalKeys.gameObject.AddComponent<Outline>().effectColor = Color.black;
                RectTransform keysRect = journalKeys.rectTransform;
                keysRect.anchorMin = keysRect.anchorMax = keysRect.pivot = Vector2.one;
                keysRect.anchoredPosition = new Vector2(-24f, -24f);
                keysRect.sizeDelta = new Vector2(240f, 32f);
            }
        }

        private void LateUpdate()
        {
            if (journal != null && journal.isActiveAndEnabled)
            {
                displayedTarget = null;
                UpdateJournalHint();
                return;
            }
            if (journalKeys != null)
            {
                journalKeys.gameObject.SetActive(false);
                questTitle.text = "";
            }
            GmEnemyEncounter encounter = quest != null && quest.isActiveAndEnabled ? quest.ActiveEncounter : null;
            bool trackingEncounter = encounter != null && encounter.isActiveAndEnabled && !encounter.IsDefeated;
            Transform target = trackingEncounter ? encounter.transform :
                routePortal != null && routePortal.isActiveAndEnabled && routePortal.IsOpen ? routePortal.transform : null;
            bool completed = showQuestCompletion && quest != null && quest.isActiveAndEnabled &&
                quest.Step == GmMainQuestStep.Selesai;
            bool visible = (target != null || completed) && controls != null && controls.CanExplore;
            objectivePanel.gameObject.SetActive(visible);
            if (!visible || completed)
            {
                markerLabel.gameObject.SetActive(false);
                if (visible)
                {
                    displayedTarget = null;
                    objectiveLabel.text = "Quest utama selesai";
                }
                return;
            }

            if (displayedTarget != target)
            {
                displayedTarget = target;
                string name = trackingEncounter ? encounter.gameObject.name : routeDestination;
                objectiveLabel.text = (trackingEncounter ? "Kalahkan " : "Pergi ke ") + name;
                markerLabel.text = "▼ " + name;
            }

            PlaceMarker(target);
        }

        private void UpdateJournalHint()
        {
            bool exploring = controls != null && controls.CanExplore;
            journalKeys.gameObject.SetActive(exploring);
            GmQuestEntry tracked = exploring ? journal.Tracked : null;
            bool showCompletion = exploring && showQuestCompletion && journal.ShowMainCompletion;
            objectivePanel.gameObject.SetActive(tracked != null || showCompletion);
            if (tracked == null)
            {
                markerLabel.gameObject.SetActive(false);
                if (showCompletion)
                {
                    questTitle.text = "Quest Utama";
                    objectiveLabel.text = "Quest utama selesai";
                }
                return;
            }

            GmQuestGoal goal = tracked.CurrentGoal;
            questTitle.text = tracked.Title;
            objectiveLabel.text = string.IsNullOrEmpty(goal.Progress) ? goal.Text : goal.Text + "   " + goal.Progress;
            if (!journal.TryGetNavigation(goal, out GmQuestLandmark landmark))
            {
                markerLabel.gameObject.SetActive(false);
                return;
            }

            markerLabel.text = "▼ " + landmark.Label;
            PlaceMarker(landmark.Point);
        }

        private void PlaceMarker(Transform target)
        {
            Camera camera = viewCamera != null ? viewCamera : Camera.main;
            if (camera == null)
            {
                markerLabel.gameObject.SetActive(false);
                return;
            }
            Vector3 screen = camera.WorldToScreenPoint(target.position + Vector3.up * 1.65f);
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
