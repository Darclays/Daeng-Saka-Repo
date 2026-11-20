using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [Serializable]
    public struct GmQuestLandmark
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField] private string description;
        [SerializeField] private Transform point;

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public Transform Point => point;
        public bool IsVisible => point != null && point.gameObject.activeInHierarchy;

        public GmQuestLandmark(string id, string label, string description, Transform point)
        {
            this.id = id;
            this.label = label;
            this.description = description;
            this.point = point;
        }
    }

    [DefaultExecutionOrder(-210)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(GmMainQuestEncounterHint))]
    public sealed class GmQuestJournal : MonoBehaviour
    {
        private sealed class SideRun
        {
            public readonly GmSideQuestDefinition Definition;
            public int CompletedSteps;

            public SideRun(GmSideQuestDefinition definition) => Definition = definition;
        }

        // The world uses single-scene portals. Keep the journal session across them, like main quest progress.
        private static readonly Dictionary<string, SideRun> SideRuns = new Dictionary<string, SideRun>();
        private static int revision;
        private static bool autoTrackMain = true;
        private static string trackedSideId;

        [SerializeField] private GmMainQuestProgress mainQuest;
        [SerializeField] private GmInteractor player;
        [SerializeField] private GmQuestArea area;
        [SerializeField] private GmWorldPortal exit;
        [SerializeField] private GmQuestArea exitArea;
        [SerializeField] private Rect mapBoundsXZ;
        [SerializeField] private Texture2D mapArtwork;
        [SerializeField] private GmQuestLandmark[] landmarks = Array.Empty<GmQuestLandmark>();

        private readonly List<GmQuestEntry> entries = new List<GmQuestEntry>();
        private int lastRevision = -1;
        private GmMainQuestStep lastStep;
        private GmQuestJournalView view;

        internal GmQuestArea Area => area;
        internal GmQuestArea ExitArea => exitArea;
        internal bool ExitOpen => exit != null && exit.isActiveAndEnabled && exit.IsOpen;
        internal Rect MapBounds => mapBoundsXZ;
        internal Texture2D MapArtwork => mapArtwork;
        internal Transform Player => player != null ? player.transform : null;
        internal IReadOnlyList<GmQuestLandmark> Landmarks => landmarks;

        internal IReadOnlyList<GmQuestEntry> Entries
        {
            get { RefreshEntries(); return entries; }
        }

        internal GmQuestEntry Tracked
        {
            get
            {
                RefreshEntries();
                foreach (GmQuestEntry entry in entries)
                {
                    if (!entry.IsComplete &&
                        (entry.IsSideQuest ? entry.Id == trackedSideId : autoTrackMain))
                        return entry;
                }
                return null;
            }
        }

        internal bool ShowMainCompletion => autoTrackMain && mainQuest != null &&
            mainQuest.Step == GmMainQuestStep.Selesai;

        public void Configure(GmMainQuestProgress progress, GmInteractor interactor, GmQuestArea currentArea,
            GmWorldPortal portal, GmQuestArea destination, Rect bounds, GmQuestLandmark[] areaLandmarks)
        {
            mainQuest = progress;
            player = interactor;
            area = currentArea;
            exit = portal;
            exitArea = destination;
            mapBoundsXZ = bounds;
            landmarks = areaLandmarks ?? Array.Empty<GmQuestLandmark>();
            lastRevision = -1;
        }

        public void SetAreaLandmarks(Rect bounds, params GmQuestLandmark[] updatedLandmarks)
        {
            var merged = new List<GmQuestLandmark>(landmarks);
            foreach (var landmark in updatedLandmarks)
            {
                int index = merged.FindIndex(existing => existing.Id == landmark.Id);
                if (index >= 0) merged[index] = landmark;
                else merged.Add(landmark);
            }
            landmarks = merged.ToArray();
            if (mapArtwork == null) mapBoundsXZ = bounds;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRun()
        {
            SideRuns.Clear();
            revision = 0;
            autoTrackMain = true;
            trackedSideId = null;
        }

        public bool TryStartSideQuest(GmSideQuestDefinition definition)
        {
            if (definition == null || GmQuestCatalog.IsMainQuestId(definition.Id) || SideRuns.ContainsKey(definition.Id))
                return false;
            SideRuns.Add(definition.Id, new SideRun(definition));
            revision++;
            return true;
        }

        public bool TryCompleteSideStep(string questId, int stepIndex)
        {
            if (string.IsNullOrEmpty(questId) || !SideRuns.TryGetValue(questId, out SideRun run) ||
                run.CompletedSteps != stepIndex || stepIndex >= run.Definition.Goals.Count)
                return false;

            run.CompletedSteps++;
            if (run.CompletedSteps == run.Definition.Goals.Count && trackedSideId == questId)
            {
                trackedSideId = null;
                autoTrackMain = true;
            }
            revision++;
            return true;
        }

        public int CompletedSideSteps(string questId) =>
            !string.IsNullOrEmpty(questId) && SideRuns.TryGetValue(questId, out SideRun run) ? run.CompletedSteps : -1;

        internal void ToggleTracking(GmQuestEntry entry)
        {
            if (entry == null || entry.IsComplete) return;
            bool stop = Tracked?.Id == entry.Id;
            autoTrackMain = !stop && !entry.IsSideQuest;
            trackedSideId = !stop && entry.IsSideQuest ? entry.Id : null;
        }

        private bool RefreshEntries()
        {
            if (mainQuest == null || !mainQuest.isActiveAndEnabled)
            {
                bool hadEntries = entries.Count != 0;
                entries.Clear();
                lastRevision = -1;
                return hadEntries;
            }
            if (lastRevision == revision && lastStep == mainQuest.Step) return false;
            lastRevision = revision;
            lastStep = mainQuest.Step;
            entries.Clear();
            GmQuestCatalog.AddMainEntries(mainQuest, entries);
            foreach (SideRun run in SideRuns.Values)
            {
                GmSideQuestDefinition quest = run.Definition;
                entries.Add(new GmQuestEntry(quest.Id, quest.Title, quest.Description, true,
                    quest.Goals, run.CompletedSteps));
            }
            return true;
        }

        internal bool TryGetLandmark(string id, out GmQuestLandmark landmark)
        {
            foreach (GmQuestLandmark candidate in landmarks)
            {
                if (candidate.Id == id && candidate.IsVisible)
                {
                    landmark = candidate;
                    return true;
                }
            }
            landmark = default;
            return false;
        }

        internal bool TryGetNavigation(GmQuestGoal goal, out GmQuestLandmark landmark)
        {
            if (goal.Area == area)
            {
                if (TryGetLandmark(goal.Landmark, out landmark)) return true;
                // The original IbuKota scene uses its guide for all three conversations.
                return area == GmQuestArea.Kerajaan &&
                    (goal.Landmark == GmQuestCatalog.Merchant || goal.Landmark == GmQuestCatalog.Blacksmith) &&
                    !Array.Exists(landmarks, point => point.Id == goal.Landmark) &&
                    TryGetLandmark(GmQuestCatalog.Guide, out landmark);
            }
            bool leadsToGoal = goal.Area == exitArea ||
                (area == GmQuestArea.Kerajaan && goal.Area == GmQuestArea.GoaLembayung);
            if (leadsToGoal && ExitOpen)
                return TryGetLandmark(GmQuestCatalog.Exit, out landmark);
            landmark = default;
            return false;
        }

        private void Update()
        {
            if (player == null || player.gameObject.scene != SceneManager.GetActiveScene())
            {
                Close();
                return;
            }

            GmJournalPage? requested = null;
            bool closePressed = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.jKey.wasPressedThisFrame) requested = GmJournalPage.Journal;
                else if (keyboard.mKey.wasPressedThisFrame) requested = GmJournalPage.Map;
                else if (keyboard.iKey.wasPressedThisFrame) requested = GmJournalPage.Bag;
                closePressed = keyboard.escapeKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.J)) requested = GmJournalPage.Journal;
            else if (Input.GetKeyDown(KeyCode.M)) requested = GmJournalPage.Map;
            else if (Input.GetKeyDown(KeyCode.I)) requested = GmJournalPage.Bag;
            closePressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (view != null && view.IsOpen)
            {
                if (closePressed || requested == view.Page) Close();
                else if (requested.HasValue) ShowPage(requested.Value);
                else if (view.Page == GmJournalPage.Bag || RefreshEntries()) view.Refresh();
                return;
            }
            if (!requested.HasValue || !player.ControlGate.CanExplore) return;
            view ??= new GmQuestJournalView((RectTransform)transform, this);
            player.ControlGate.Block(this);
            ShowPage(requested.Value);
        }

        private void ShowPage(GmJournalPage page)
        {
            if (page == GmJournalPage.Bag) view.ShowBag();
            else if (page == GmJournalPage.Map) view.ShowMap(Tracked);
            else view.ShowJournal();
        }

        internal void Close()
        {
            if (view == null || !view.IsOpen) return;
            view.Hide();
            if (player == null) return;
            player.ControlGate.Unblock(this);
            player.SuppressInteraction();
        }

        private void OnDisable() => Close();
    }
}
