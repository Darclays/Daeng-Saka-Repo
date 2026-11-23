using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class GmMainQuestEncounterHint : MonoBehaviour
    {
        private const float TrackedNameOffset = 22f;
        private const float NoticeTop = 73f;
        private const float NoticeSpacing = 59f;
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
        private RectTransform edgeMarker;
        private Text edgeArrow;
        private Text edgeDistance;
        private int shownDistance = -1;
        private Transform displayedTarget;
        private GmQuestJournal journal;
        private GmNpcGuidance npcGuidance;
        private GmInteractor legacyPlayer;
        private Text questTitle;
        private Text mapKey, menuKey, sprintKey;
        private ShortcutNotice questNotice;
        private ShortcutNotice itemNotice;
        private ShortcutNotice mapNotice;

        private sealed class ShortcutNotice
        {
            private readonly Text shortcut;
            private readonly RectTransform panel;
            private readonly Text arrow;
            private readonly Text label;
            private readonly string heading;
            private readonly string guidance;
            private string displayedTitle, displayedGuidance;

            internal ShortcutNotice(RectTransform canvas, Font font, Text shortcut, string name, string heading, string guidance,
                string marker = "↑")
            {
                this.shortcut = shortcut;
                this.heading = heading;
                this.guidance = guidance;
                Image background = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                background.color = new Color(0.08f, 0.10f, 0.12f, 0.96f);
                background.raycastTarget = false;
                panel = background.rectTransform;
                panel.SetParent(canvas, false);
                panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one;
                panel.sizeDelta = new Vector2(320f, 51f);
                label = CreateLabel("Notice Text", panel, font, TextAnchor.MiddleLeft);
                RectTransform textRect = label.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(14f, 3f);
                textRect.offsetMax = new Vector2(-10f, -3f);
                label.fontSize = 15;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 15;
                panel.gameObject.SetActive(false);

                arrow = CreateLabel("Notice Arrow", shortcut.rectTransform, font, TextAnchor.MiddleCenter);
                arrow.text = marker;
                arrow.fontSize = 20;
                arrow.gameObject.AddComponent<Outline>().effectColor = Color.black;
                arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(.5f, 0f);
                arrow.rectTransform.sizeDelta = new Vector2(25f, 22f);
                arrow.gameObject.SetActive(false);
            }

            internal bool Render(bool visible, string title, float elapsed, float top,
                string shortcutText, string inputGuidance = null)
            {
                shortcut.text = shortcutText;
                shortcut.gameObject.SetActive(visible);
                bool showNotice = visible && title != null;
                panel.gameObject.SetActive(showNotice);
                arrow.gameObject.SetActive(showNotice);
                if (!showNotice)
                {
                    shortcut.color = Color.white;
                    return false;
                }

                string help = inputGuidance ?? guidance;
                if (displayedTitle != title || displayedGuidance != help)
                {
                    displayedTitle = title;
                    displayedGuidance = help;
                    label.text = heading + " · " + title + "\n" + help;
                }
                float pulse = .5f + .5f * Mathf.Sin(elapsed * 6f);
                panel.anchoredPosition = new Vector2(-24f, -top);
                Color accent = arrow.text == "!" ? new Color(1f, .46f, .51f) : new Color(1f, .77f, .35f);
                shortcut.color = arrow.color = Color.Lerp(accent, Color.white, pulse * .6f);
                arrow.rectTransform.anchoredPosition = new Vector2(0f, -7f + 3f * pulse);
                return true;
            }
        }

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
            npcGuidance = new GmNpcGuidance(canvasRect);
            legacyPlayer = controls != null ? controls.GetComponent<GmInteractor>() : null;
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
            CreateEdgeMarker(font);

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
                const float gap = 8f;
                const float menuWidth = 110f, bagWidth = 78f, mapWidth = 100f, journalWidth = 89f;
                float inset = 24f;
                menuKey = CreateShortcut(font, "Menu Shortcut", "[P] Jeda", inset, menuWidth);
                inset += menuWidth + gap;
                Text bagKey = CreateShortcut(font, "Bag Shortcut", "[I] Tas", inset, bagWidth);
                inset += bagWidth + gap;
                mapKey = CreateShortcut(font, "Map Shortcut", "[M] Peta", inset, mapWidth);
                mapKey.supportRichText = true;
                inset += mapWidth + gap;
                Text journalKey = CreateShortcut(font, "Journal Shortcut", "[J] Jurnal", inset, journalWidth);
                sprintKey = CreateLabel("Sprint Shortcut", canvasRect, font, TextAnchor.MiddleLeft);
                sprintKey.gameObject.AddComponent<Outline>().effectColor = Color.black;
                RectTransform sprintRect = sprintKey.rectTransform;
                sprintRect.anchorMin = sprintRect.anchorMax = sprintRect.pivot = Vector2.zero;
                sprintRect.anchoredPosition = new Vector2(24f, 24f);
                sprintRect.sizeDelta = new Vector2(160f, 32f);
                questNotice = new ShortcutNotice(canvasRect, font, journalKey, "New Quest Notice",
                    "Quest baru", "[J] Jurnal → Lacak quest");
                itemNotice = new ShortcutNotice(canvasRect, font, bagKey, "New Item Notice",
                    "Item baru", "[I] Tas → Cek item");
                mapNotice = new ShortcutNotice(canvasRect, font, mapKey, "Quest Available Notice",
                    "Quest tersedia", "[M] Peta → Temukan pemberi quest", "!");
            }
        }

        private Text CreateShortcut(Font font, string name, string text, float inset, float width)
        {
            Text shortcut = CreateLabel(name, canvasRect, font, TextAnchor.MiddleCenter);
            shortcut.text = text;
            shortcut.gameObject.AddComponent<Outline>().effectColor = Color.black;
            RectTransform rect = shortcut.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-inset, -24f);
            rect.sizeDelta = new Vector2(width, 32f);
            return shortcut;
        }

        private void CreateEdgeMarker(Font font)
        {
            Image badge = new GameObject("Offscreen Quest Marker", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            badge.color = new Color(0.035f, 0.05f, 0.065f, 0.92f);
            badge.raycastTarget = false;
            edgeMarker = badge.rectTransform;
            edgeMarker.SetParent(canvasRect, false);
            edgeMarker.anchorMin = edgeMarker.anchorMax = edgeMarker.pivot = new Vector2(0.5f, 0.5f);
            edgeMarker.sizeDelta = new Vector2(96f, 34f);

            edgeArrow = CreateLabel("Direction", edgeMarker, font, TextAnchor.MiddleCenter);
            edgeArrow.text = "▲";
            edgeArrow.color = markerLabel.color;
            RectTransform arrowRect = edgeArrow.rectTransform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(17f, 0f);
            arrowRect.sizeDelta = new Vector2(24f, 26f);

            edgeDistance = CreateLabel("Distance", edgeMarker, font, TextAnchor.MiddleCenter);
            edgeDistance.fontSize = 16;
            RectTransform distanceRect = edgeDistance.rectTransform;
            distanceRect.anchorMin = Vector2.zero;
            distanceRect.anchorMax = Vector2.one;
            distanceRect.offsetMin = new Vector2(32f, 0f);
            distanceRect.offsetMax = new Vector2(-6f, 0f);
            edgeMarker.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            bool exploring = controls != null && controls.CanExplore && gameObject.scene == SceneManager.GetActiveScene();
            Camera camera = viewCamera != null && viewCamera.isActiveAndEnabled ? viewCamera : Camera.main;
            if (journal != null && journal.isActiveAndEnabled)
            {
                displayedTarget = null;
                UpdateJournalHint(exploring, camera);
                return;
            }
            if (mapKey != null)
            {
                UpdateShortcuts(false);
                questTitle.text = "";
            }
            GmEnemyEncounter encounter = quest != null && quest.isActiveAndEnabled ? quest.ActiveEncounter : null;
            bool trackingEncounter = encounter != null && encounter.isActiveAndEnabled && !encounter.IsDefeated;
            Transform target = trackingEncounter ? encounter.transform :
                routePortal != null && routePortal.isActiveAndEnabled && routePortal.IsOpen ? routePortal.transform : null;
            bool completed = showQuestCompletion && quest != null && quest.isActiveAndEnabled &&
                quest.Step == GmMainQuestStep.Selesai;
            npcGuidance.Render(exploring ? legacyPlayer : null, camera, target);
            bool visible = (target != null || completed) && exploring;
            objectivePanel.gameObject.SetActive(visible);
            if (!visible || completed)
            {
                HideMarkers();
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

            PlaceMarker(target, camera);
        }

        private void UpdateShortcuts(bool visible)
        {
            bool pad = GmGamepadInput.UsingGamepad;
            menuKey.gameObject.SetActive(visible);
            sprintKey.gameObject.SetActive(visible);
            menuKey.text = pad ? "[Menu] Jeda" : "[P] Jeda";
            sprintKey.text = pad ? "[RT] Sprint" : "[Shift] Sprint";
            bool showQuest = questNotice.Render(visible, journal.NewQuestTitle,
                GmQuestJournal.QuestNoticeDuration - journal.QuestNoticeRemaining, NoticeTop,
                pad ? "[Y] Jurnal" : "[J] Jurnal", pad ? "[Y] Jurnal → Lacak quest" : null);
            GmPartyRun run = GmPartyRun.Current;
            bool showItem = itemNotice.Render(visible, run.NewItemSummary,
                GmPartyRun.ItemNoticeDuration - run.ItemNoticeRemaining,
                showQuest ? NoticeTop + NoticeSpacing : NoticeTop, pad ? "[RB] Tas" : "[I] Tas",
                pad ? "[RB] Tas → Cek item" : null);
            int offers = visible ? journal.LocalOfferCount : 0;
            string mapLabel = pad ? "[LB] Peta" : "[M] Peta";
            if (visible && offers == 0 && journal.HasMapQuest) mapLabel += "  ◆";
            float mapNoticeTop = NoticeTop + NoticeSpacing * ((showQuest ? 1 : 0) + (showItem ? 1 : 0));
            mapNotice.Render(visible, offers > 0 ? $"{offers} di area ini" : null,
                (float)Time.unscaledTimeAsDouble, mapNoticeTop, mapLabel, pad ? "[LB] Peta → Temukan pemberi quest" : null);
        }

        private void UpdateJournalHint(bool exploring, Camera camera)
        {
            UpdateShortcuts(exploring);
            GmSideQuestDefinition offer = null;
            GmQuestLandmark landmark = default;
            bool tracksOffer = exploring && journal.TryGetTrackedOffer(out offer, out landmark);
            GmQuestEntry tracked = exploring && !tracksOffer ? journal.Tracked : null;
            bool targetFound = tracksOffer || (tracked != null && journal.TryGetNavigation(tracked.CurrentGoal, out landmark));
            npcGuidance.Render(exploring ? journal.Interactor : null, camera, targetFound ? landmark.Point : null);
            bool showCompletion = exploring && showQuestCompletion && journal.ShowMainCompletion;
            objectivePanel.gameObject.SetActive(tracksOffer || tracked != null || showCompletion);
            if (tracksOffer)
            {
                questTitle.text = offer.Title;
                objectiveLabel.text = "Bicara dengan " + landmark.Label;
            }
            else if (tracked != null)
            {
                GmQuestGoal goal = tracked.CurrentGoal;
                questTitle.text = tracked.Title;
                objectiveLabel.text = string.IsNullOrEmpty(goal.Progress) ? goal.Text : goal.Text + "   " + goal.Progress;
            }
            else
            {
                HideMarkers();
                if (showCompletion)
                {
                    questTitle.text = "Quest Utama";
                    objectiveLabel.text = "Quest utama selesai";
                }
                return;
            }

            if (!targetFound)
            {
                HideMarkers();
                return;
            }

            markerLabel.text = "▼ " + landmark.Label;
            PlaceMarker(landmark.Point, camera);
        }

        private void PlaceMarker(Transform target, Camera camera)
        {
            if (camera == null || !camera.isActiveAndEnabled)
            {
                HideMarkers();
                return;
            }
            Vector3 position = target.position;
            if (npcGuidance.TryGetPosition(target, out Vector2 badge))
            {
                markerLabel.rectTransform.anchoredPosition = badge + Vector2.up * TrackedNameOffset;
                markerLabel.gameObject.SetActive(true);
                edgeMarker.gameObject.SetActive(false);
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(position + Vector3.up * 1.65f);
            Rect viewport = camera.pixelRect;
            bool onScreen = screen.z > 0.01f && viewport.Contains((Vector2)screen);
            Vector2 local = Vector2.zero;
            bool positioned = onScreen && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            markerLabel.gameObject.SetActive(positioned);
            if (positioned)
            {
                markerLabel.rectTransform.anchoredPosition = local;
                edgeMarker.gameObject.SetActive(false);
                return;
            }

            // Leave room for the objective, shortcuts, and interaction prompt.
            Rect canvas = canvasRect.rect;
            Vector2 edge = new Vector2(canvas.width * 0.5f - 24f, canvas.height * 0.5f - 92f) -
                edgeMarker.sizeDelta * 0.5f;
            if (edge.x <= 0f || edge.y <= 0f)
            {
                edgeMarker.gameObject.SetActive(false);
                return;
            }

            Vector2 direction;
            if (screen.z <= 0.01f)
            {
                // Behind the camera, point toward the nearest side to turn.
                float side = Vector3.Dot(position - camera.transform.position, camera.transform.right);
                direction = side < -0.01f ? Vector2.left : Vector2.right;
            }
            else
            {
                direction = (Vector2)screen - new Vector2(viewport.xMin + viewport.width * 0.5f,
                    viewport.yMin + viewport.height * 0.5f);
                if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
            }
            float scale = Mathf.Max(Mathf.Abs(direction.x) / edge.x, Mathf.Abs(direction.y) / edge.y);
            edgeMarker.anchoredPosition = direction / scale;
            edgeArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);

            Transform player = journal != null ? journal.Player : null;
            Vector3 offset = position - (player != null ? player.position : controls.transform.position);
            int distance = Mathf.Max(1, Mathf.RoundToInt(Vector3.ProjectOnPlane(offset, Vector3.up).magnitude));
            if (distance != shownDistance)
            {
                shownDistance = distance;
                edgeDistance.text = distance + " m";
            }
            edgeMarker.gameObject.SetActive(true);
        }

        private void HideMarkers()
        {
            markerLabel.gameObject.SetActive(false);
            edgeMarker.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (npcGuidance == null) return;
            npcGuidance.Render(null, null, null);
            HideMarkers();
            objectivePanel.gameObject.SetActive(false);
            if (mapKey != null) UpdateShortcuts(false);
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
