using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;

namespace GamecommMentor.DaengSaka
{
    internal enum GmJournalPage { Journal, Map, Bag, Pause }

    internal sealed class GmQuestJournalView
    {
        internal static readonly Color Paper = new Color32(249, 246, 236, 255);
        internal static readonly Color Inset = new Color32(239, 235, 221, 255);
        internal static readonly Color Leaf = new Color32(34, 73, 55, 255);
        internal static readonly Color LeafLight = new Color32(217, 231, 216, 255);
        internal static readonly Color Muted = new Color32(76, 89, 74, 255);
        internal static readonly Color Ink = new Color32(29, 42, 34, 255);
        internal static readonly Color Rule = new Color32(207, 207, 190, 255);
        private static readonly Color Amber = new Color32(119, 79, 34, 255);

        private readonly GmQuestJournal journal;
        private readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private readonly RectTransform root;
        private readonly RectTransform window, pausePanel;
        private readonly RectTransform journalPanel;
        private readonly GmQuestAreaMapView mapView;
        private readonly GmPartyBagView bagView;
        private readonly RectTransform list;
        private readonly ScrollRect scroll;
        private readonly List<(GmQuestEntry entry, Image background, Text label)> rows =
            new List<(GmQuestEntry, Image, Text)>();
        private readonly Button journalTab, mapTab, bagTab, close, activeTab, completeTab, trackAction, mapAction;
        private readonly Button resume, pauseJournal, pauseMap, pauseBag;
        private readonly Text category, title, description, objectives, trackLabel, tabHint, pauseHint;
        private bool showCompleted;
        private string selectedId, mapQuestId;

        internal bool IsOpen => root.gameObject.activeSelf;
        internal GmJournalPage Page { get; private set; }
        internal bool IsMap => Page == GmJournalPage.Map;

        public GmQuestJournalView(RectTransform canvas, GmQuestJournal journal)
        {
            this.journal = journal;
            root = Rect("Quest Menu", canvas, 0f, 0f, 1f, 1f);
            Fill(root, new Color(0.07f, 0.12f, 0.09f, .82f)).raycastTarget = true;
            window = Rect("Window", root, .04f, .055f, .96f, .945f);
            Fill(window, Paper);
            RectTransform header = Rect("Header", window, 0f, .865f, 1f, 1f);
            Fill(header, Inset);
            AddText(font, header, "Game", "Daeng Saka", 26, .026f, .49f, .31f, .93f, bold: true);
            tabHint = AddText(font, header, "Pintasan tab", "", 15, .026f, .13f, .31f, .47f);
            tabHint.color = Muted;
            journalTab = AddButton(font, header, "Jurnal  [J]", .35f, .21f, .50f, .79f, ShowJournal);
            mapTab = AddButton(font, header, "Peta  [M]", .51f, .21f, .64f, .79f, journal.OpenMap);
            Caption(mapTab).supportRichText = true;
            bagTab = AddButton(font, header, "Tas  [I]", .65f, .21f, .78f, .79f, ShowBag);
            close = AddButton(font, header, "Tutup  [Esc]", .79f, .21f, .973f, .79f, journal.Back);

            journalPanel = Rect("Jurnal", window, .026f, .045f, .974f, .833f);
            RectTransform sidebar = Rect("Quest List", journalPanel, 0f, 0f, .328f, 1f);
            Fill(sidebar, Inset);
            activeTab = AddButton(font, sidebar, "Aktif", .045f, .885f, .48f, .977f, () => SetFilter(false));
            completeTab = AddButton(font, sidebar, "Selesai", .515f, .885f, .955f, .977f, () => SetFilter(true));
            RectTransform viewport = Rect("Viewport", sidebar, .04f, .024f, .96f, .867f);
            list = CreateScrollContent(viewport, out scroll);

            RectTransform detail = Rect("Quest Detail", journalPanel, .356f, 0f, 1f, 1f);
            category = AddText(font, detail, "Category", "", 15, 0f, .9f, 1f, .995f, bold: true);
            title = AddText(font, detail, "Title", "", 33, 0f, .746f, 1f, .911f, bold: true);
            description = AddText(font, detail, "Description", "", 19, 0f, .592f, 1f, .747f);
            description.color = Muted;
            Fill(Rect("Rule", detail, 0f, .582f, 1f, .587f), Rule);
            objectives = AddText(font, detail, "Objectives", "", 20, 0f, .167f, 1f, .563f,
                TextAnchor.UpperLeft);
            objectives.lineSpacing = 1.26f;
            trackAction = AddButton(font, detail, "Lacak quest", 0f, .016f, .43f, .129f, ToggleTracking, primary: true);
            trackLabel = trackAction.GetComponentInChildren<Text>();
            mapAction = AddButton(font, detail, "Lihat di peta", .451f, .016f, .798f, .129f, () => ShowMap(Selected()));

            RectTransform mapPanel = Rect("Peta Area", window, .026f, .045f, .974f, .833f);
            mapView = new GmQuestAreaMapView(mapPanel, journal, font);
            bagView = new GmPartyBagView(window, font);

            pausePanel = Rect("Jeda", root, .30f, .20f, .70f, .80f);
            Fill(pausePanel, Paper);
            Fill(Rect("Aksen", pausePanel, 0f, .98f, 1f, 1f), Leaf);
            AddText(font, pausePanel, "Judul jeda", "Jeda", 36, .09f, .795f, .91f, .96f, bold: true);
            Fill(Rect("Pilihan jeda", pausePanel, .075f, .16f, .925f, .785f), Inset);
            resume = AddButton(font, pausePanel, "Lanjutkan", .09f, .638f, .91f, .768f, journal.Close, primary: true);
            pauseJournal = AddButton(font, pausePanel, "Jurnal", .09f, .49f, .91f, .62f,
                () => journal.OpenFromPause(GmJournalPage.Journal));
            pauseMap = AddButton(font, pausePanel, "Peta", .09f, .342f, .91f, .472f,
                () => journal.OpenFromPause(GmJournalPage.Map));
            Caption(pauseMap).supportRichText = true;
            pauseBag = AddButton(font, pausePanel, "Tas", .09f, .194f, .91f, .324f,
                () => journal.OpenFromPause(GmJournalPage.Bag));
            GmMenuButton.Link(resume, null, pauseJournal);
            GmMenuButton.Link(pauseJournal, resume, pauseMap);
            GmMenuButton.Link(pauseMap, pauseJournal, pauseBag);
            GmMenuButton.Link(pauseBag, pauseMap, null);
            pauseHint = AddText(font, pausePanel, "Pintasan jeda", "", 16, .09f, .044f, .91f, .144f);
            pauseHint.color = Muted;
            root.gameObject.SetActive(false);
        }

        private void SelectPage(GmJournalPage page)
        {
            if (page != GmJournalPage.Bag) LeaveBag();
            Page = page;
            pausePanel.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            window.gameObject.SetActive(true);
            journalPanel.gameObject.SetActive(page == GmJournalPage.Journal);
            mapView.Panel.gameObject.SetActive(page == GmJournalPage.Map);
            bagView.Panel.gameObject.SetActive(page == GmJournalPage.Bag);
            SetSelected(journalTab, page == GmJournalPage.Journal);
            SetSelected(mapTab, page == GmJournalPage.Map);
            SetSelected(bagTab, page == GmJournalPage.Bag);
            if (page != GmJournalPage.Map)
                mapTab.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            GmMenuButton.Focus(PageTab);
            UpdateInputHints();
        }

        private Button PageTab => Page == GmJournalPage.Map ? mapTab : Page == GmJournalPage.Bag ? bagTab : journalTab;

        internal void ShowPause(GmJournalPage? previous)
        {
            LeaveBag();
            Page = GmJournalPage.Pause;
            window.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(true);
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            GmMenuButton.Focus(previous switch
            {
                GmJournalPage.Journal => pauseJournal,
                GmJournalPage.Map => pauseMap,
                GmJournalPage.Bag => pauseBag,
                _ => resume
            });
            UpdateInputHints();
        }

        internal void UpdateInputHints()
        {
            bool pad = GmGamepadInput.UsingGamepad;
            if (Page == GmJournalPage.Pause)
            {
                Caption(pauseMap).text = WithAlert("Peta", journal.HasMapQuest);
                pauseHint.text = pad ? "[A] Pilih    [B] Lanjutkan" : "[Enter] Pilih    [Esc] Lanjutkan";
                if (pad) GmMenuButton.FocusWithin(pausePanel, resume);
                return;
            }
            tabHint.text = pad ? "[LB/RB] Ganti tab" : "";
            Caption(journalTab).text = pad ? "Jurnal" : "Jurnal  [J]";
            Caption(mapTab).text = WithAlert(pad ? "Peta" : "Peta  [M]", journal.HasMapQuest);
            Caption(bagTab).text = pad ? "Tas" : "Tas  [I]";
            string action = journal.ReturnsToPause ? "Kembali" : "Tutup";
            Caption(close).text = $"{action}  [{GmGamepadInput.BackLabel}]";
            if (pad) GmMenuButton.FocusWithin(window, PageTab);
        }

        internal void ShowJournal()
        {
            GmQuestJournal.DismissNewQuestNotice();
            SelectPage(GmJournalPage.Journal);
            RenderJournal();
        }

        internal void ShowMap(GmQuestEntry quest)
        {
            mapQuestId = quest?.Id;
            SelectPage(GmJournalPage.Map);
            RenderMap(quest);
        }

        internal void ShowBag()
        {
            var run = GmPartyRun.Current;
            GmBagSection? section = run.NewItemSection;
            run.DismissItemNotice();
            SelectPage(GmJournalPage.Bag);
            bagView.Open(section);
        }

        internal void Refresh()
        {
            if (IsMap) RenderMap(FindEntry(mapQuestId));
            else if (Page == GmJournalPage.Bag) bagView.RefreshStock();
            else RenderJournal();
        }

        private void RenderMap(GmQuestEntry quest)
        {
            Button location = mapView.Render(quest, mapTab);
            mapTab.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = journalTab,
                selectOnRight = bagTab,
                selectOnDown = location
            };
        }

        internal void Hide()
        {
            LeaveBag();
            EventSystem system = EventSystem.current;
            GameObject selected = system != null ? system.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(root)) system.SetSelectedGameObject(null);
            root.gameObject.SetActive(false);
        }

        private void LeaveBag()
        {
            if (IsOpen && Page == GmJournalPage.Bag) bagView.MarkViewed();
        }

        private void SetFilter(bool completed)
        {
            if (showCompleted == completed) return;
            showCompleted = completed;
            selectedId = null;
            RenderJournal();
        }

        private GmQuestEntry FindEntry(string id)
        {
            foreach (GmQuestEntry entry in journal.Entries)
                if (entry.Id == id) return entry;
            return null;
        }

        private GmQuestEntry Selected() => FindEntry(selectedId);

        private void RenderJournal()
        {
            ClearChildren(list);
            rows.Clear();
            SetSelected(activeTab, !showCompleted);
            SetSelected(completeTab, showCompleted);
            GmQuestEntry selection = Selected();
            if (selection == null || selection.IsComplete != showCompleted) selectedId = null;
            AddRows(false, "Quest Utama");
            AddRows(true, "Quest Sampingan");
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
            RenderSelection();
        }

        private void AddRows(bool side, string heading)
        {
            bool headingAdded = false;
            foreach (GmQuestEntry entry in journal.Entries)
            {
                if (entry.IsSideQuest != side || entry.IsComplete != showCompleted) continue;
                if (!headingAdded)
                {
                    RectTransform group = ListItem("Group", 25f);
                    Text label = AddText(font, group, "Group Name", heading, 15, .045f, 0f, .965f, 1f, bold: true);
                    label.color = side ? Amber : Muted;
                    headingAdded = true;
                }

                selectedId ??= entry.Id;
                RectTransform row = ListItem("Quest " + entry.Id, 52f);
                Image background = Fill(row, Paper);
                Clickable(background, () =>
                {
                    selectedId = entry.Id;
                    RenderSelection();
                });
                Text caption = AddText(font, row, "Title", entry.Title, 18, .045f, .05f, .96f, .95f, bold: true);
                rows.Add((entry, background, caption));
            }
        }

        private RectTransform ListItem(string name, float height)
        {
            RectTransform row = Rect(name, list, 0f, 0f, 1f, 1f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private void ToggleTracking()
        {
            journal.ToggleTracking(Selected());
            RenderSelection();
        }

        private void RenderSelection()
        {
            GmQuestEntry selection = Selected();
            string trackedId = journal.Tracked?.Id;
            foreach (var row in rows)
            {
                row.background.color = row.entry.Id == selectedId ? LeafLight : Paper;
                row.label.text = row.entry.Id == trackedId ? row.entry.Title + "\nDilacak" : row.entry.Title;
            }

            if (selection == null)
            {
                category.text = "";
                title.text = showCompleted ? "Belum ada quest selesai" : "Tidak ada quest aktif";
                description.text = objectives.text = "";
            }
            else
            {
                string kind = selection.IsSideQuest ? "Quest Sampingan" : "Quest Utama";
                category.text = kind + (selection.IsComplete ? "   •   Selesai" : "   •   Aktif");
                category.color = selection.IsSideQuest ? Amber : Leaf;
                title.text = selection.Title;
                description.text = selection.Description;
                var lines = new StringBuilder("Objektif\n\n");
                for (int i = 0; i < selection.Goals.Count; i++)
                {
                    GmQuestGoal goal = selection.Goals[i];
                    bool done = i < selection.CompletedSteps;
                    lines.Append(done ? "✓   " : i == selection.CompletedSteps ? "◆   " : "–   ").Append(goal.Text);
                    string progress = done ? goal.CompletedProgress : goal.Progress;
                    if (!string.IsNullOrEmpty(progress)) lines.Append("   ").Append(progress);
                    lines.AppendLine();
                }
                objectives.text = lines.ToString().TrimEnd();
            }

            bool actionable = selection != null && !selection.IsComplete;
            trackAction.gameObject.SetActive(actionable);
            mapAction.gameObject.SetActive(actionable);
            trackLabel.text = selection?.Id == trackedId ? "Hentikan pelacakan" : "Lacak quest";
        }

        internal static Text AddText(Font font, RectTransform parent, string name, string value, int size,
            float left, float bottom, float right, float top,
            TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            Text label = Label(font, parent, name, value, size, left, bottom, right, top, align);
            label.color = Ink;
            label.resizeTextMinSize = 13;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            return label;
        }

        internal static Button AddButton(Font font, RectTransform parent, string text,
            float left, float bottom, float right, float top, Action action, bool primary = false)
        {
            RectTransform rect = Rect(text, parent, left, bottom, right, top);
            Image background = Fill(rect, primary ? Leaf : Paper);
            Text caption = AddText(font, rect, "Label", text, 17, .04f, 0f, .96f, 1f, TextAnchor.MiddleCenter, bold: true);
            if (primary) caption.color = Paper;
            return Clickable(background, action);
        }

        internal static void SetSelected(Button button, bool selected)
        {
            // GmMenuButton draws the focus ring independently of the active tab or row.
            ColorBlock colors = button.colors;
            colors.selectedColor = colors.normalColor;
            colors.highlightedColor = new Color(.98f, .96f, .92f);
            button.colors = colors;
            ((Image)button.targetGraphic).color = selected ? LeafLight : Paper;
        }

        internal static Text Caption(Button button) => button.GetComponentInChildren<Text>(true);

        internal static string WithAlert(string label, bool visible) =>
            visible ? label + "  <color=#B62237>!</color>" : label;

        internal static RectTransform CreateScrollContent(RectTransform viewport, out ScrollRect scroll)
        {
            Fill(viewport, Inset).raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Items", viewport, 0f, 1f, 1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(3, 3, 2, 2);
            layout.spacing = 5f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return content;
        }

        internal static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }
    }
}
