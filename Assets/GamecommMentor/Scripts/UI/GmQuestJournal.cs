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
        internal const float QuestNoticeDuration = 10f;
        private static string newQuestTitle;
        private static float questNoticeRemaining;

        [SerializeField] private GmMainQuestProgress mainQuest;
        [SerializeField] private GmInteractor player;
        [SerializeField] private GmQuestArea area;
        [SerializeField] private GmWorldPortal exit;
        [SerializeField] private GmWorldPortal returnPortal;
        [SerializeField] private GmQuestArea exitArea;
        [SerializeField] private Rect mapBoundsXZ;
        [SerializeField] private Texture2D mapArtwork;
        [SerializeField] private GmQuestLandmark[] landmarks = Array.Empty<GmQuestLandmark>();

        private readonly List<GmQuestEntry> entries = new List<GmQuestEntry>();
        private int lastRevision = -1;
        private GmMainQuestStep lastStep;
        private GmQuestJournalView view;
        private GmJournalPage? pauseSelection;
        private GmPixelCrushersConversation trackedGiver;
        private string trackedOfferId;

        internal GmQuestArea Area => area;
        internal GmQuestArea ExitArea => exitArea;
        internal bool ExitOpen => IsRouteOpen(GmQuestCatalog.Exit);
        internal Rect MapBounds => mapBoundsXZ;
        internal Texture2D MapArtwork => mapArtwork;
        internal GmInteractor Interactor => player;
        internal Transform Player => player != null ? player.transform : null;
        internal IReadOnlyList<GmQuestLandmark> Landmarks => landmarks;
        internal string NewQuestTitle => questNoticeRemaining > 0f ? newQuestTitle : null;
        internal float QuestNoticeRemaining => questNoticeRemaining;

        internal IReadOnlyList<GmQuestEntry> Entries
        {
            get { RefreshEntries(); return entries; }
        }

        internal GmQuestEntry Tracked
        {
            get
            {
                if (TryGetTrackedOffer(out _, out _)) return null;
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
        internal bool ReturnsToPause => pauseSelection.HasValue;

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

        public void SetLandmark(GmQuestLandmark landmark) => SetAreaLandmarks(mapBoundsXZ, landmark);

        public void SetTravelPortal(GmWorldPortal portal, GmQuestArea destination, bool returning = false)
        {
            if (returning) returnPortal = portal;
            else
            {
                exit = portal;
                exitArea = destination;
            }
            string name = GmQuestCatalog.AreaName(destination);
            string id = returning ? GmQuestCatalog.Return : GmQuestCatalog.Exit;
            SetLandmark(new GmQuestLandmark(id, "Ke " + name, "Temui garis batas untuk pergi ke " + name + ".",
                portal.transform));
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
        internal static void ResetRun()
        {
            SideRuns.Clear();
            revision = 0;
            autoTrackMain = true;
            trackedSideId = null;
            DismissNewQuestNotice();
        }

        internal static void NotifyNewQuest(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            newQuestTitle = title;
            questNoticeRemaining = QuestNoticeDuration;
            GmGameplayAudio.World("QuestNew");
        }

        internal static void DismissNewQuestNotice()
        {
            newQuestTitle = null;
            questNoticeRemaining = 0f;
        }

        public bool TryStartSideQuest(GmSideQuestDefinition definition)
        {
            if (definition == null || GmQuestCatalog.IsMainQuestId(definition.Id) || SideRuns.ContainsKey(definition.Id))
                return false;
            SideRuns.Add(definition.Id, new SideRun(definition));
            revision++;
            NotifyNewQuest(definition.Title);
            if (trackedOfferId == definition.Id) TrackSideQuest(definition.Id);
            return true;
        }

        public bool TryCompleteSideStep(string questId, int stepIndex)
        {
            if (string.IsNullOrEmpty(questId) || !SideRuns.TryGetValue(questId, out SideRun run) ||
                run.CompletedSteps != stepIndex || stepIndex >= run.Definition.Goals.Count)
                return false;

            run.CompletedSteps++;
            if (run.CompletedSteps == run.Definition.Goals.Count)
            {
                if (trackedSideId == questId)
                {
                    trackedSideId = null;
                    autoTrackMain = true;
                }
                GmGameplayAudio.World("QuestComplete");
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
            ClearOfferTracking();
            autoTrackMain = !stop && !entry.IsSideQuest;
            trackedSideId = !stop && entry.IsSideQuest ? entry.Id : null;
        }

        internal void TrackSideQuest(string id)
        {
            ClearOfferTracking();
            autoTrackMain = false;
            trackedSideId = id;
        }

        internal GmSideQuestDefinition QuestOfferAt(GmPixelCrushersConversation giver) =>
            giver != null && giver.gameObject.scene == gameObject.scene ? giver.QuestToOffer(player) : null;

        internal IEnumerable<(GmPixelCrushersConversation giver, GmSideQuestDefinition quest)> LocalOffers()
        {
            foreach (var giver in GmPixelCrushersConversation.ActiveConversations)
            {
                var offered = QuestOfferAt(giver);
                if (offered != null) yield return (giver, offered);
            }
        }

        internal GmQuestLandmark OfferLandmark(GmPixelCrushersConversation giver, GmSideQuestDefinition offer)
        {
            foreach (GmQuestLandmark landmark in landmarks)
                if (landmark.Point == giver.transform) return landmark;
            return new GmQuestLandmark(offer.Id, giver.name, "Bicara untuk mengambil quest ini.", giver.transform);
        }

        internal bool TryGetTrackedOffer(out GmSideQuestDefinition quest, out GmQuestLandmark landmark)
        {
            quest = QuestOfferAt(trackedGiver);
            if (quest != null && quest.Id == trackedOfferId)
            {
                landmark = OfferLandmark(trackedGiver, quest);
                return true;
            }
            landmark = default;
            return false;
        }

        internal bool TryTrackOffer(GmPixelCrushersConversation giver, string id)
        {
            if (id == null || QuestOfferAt(giver)?.Id != id) return false;
            trackedGiver = giver;
            trackedOfferId = id;
            return true;
        }

        private void ClearOfferTracking()
        {
            trackedGiver = null;
            trackedOfferId = null;
        }

        internal int LocalOfferCount
        {
            get
            {
                int count = 0;
                foreach (var giver in GmPixelCrushersConversation.ActiveConversations)
                    if (QuestOfferAt(giver) != null) count++;
                return count;
            }
        }

        internal bool HasMapQuest => LocalActiveQuest() != null || LocalOfferCount > 0;

        private GmQuestEntry LocalActiveQuest()
        {
            RefreshEntries();
            foreach (GmQuestEntry entry in entries)
                if (!entry.IsComplete && entry.CurrentGoal.Area == area && TryGetNavigation(entry.CurrentGoal, out _))
                    return entry;
            return null;
        }

        internal void OpenMap()
        {
            if (TryGetTrackedOffer(out _, out _))
            {
                view.ShowMap(null);
                return;
            }
            GmPixelCrushersConversation nearest = null;
            string nearestId = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var (giver, offered) in LocalOffers())
            {
                float distance = (giver.transform.position - Player.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = giver;
                nearestId = offered.Id;
                nearestDistance = distance;
            }
            if (nearest != null && TryTrackOffer(nearest, nearestId))
            {
                view.ShowMap(null);
                return;
            }

            ClearOfferTracking();
            GmQuestEntry target = Tracked;
            if (target == null || (!target.IsSideQuest && target.CurrentGoal.Area != area))
            {
                GmQuestEntry local = LocalActiveQuest();
                if (local != null)
                {
                    if (local.IsSideQuest) TrackSideQuest(local.Id);
                    else
                    {
                        autoTrackMain = true;
                        trackedSideId = null;
                    }
                    target = local;
                }
            }
            view.ShowMap(target);
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
            string route = null;
            if (area == GmQuestArea.DesaSorelia && goal.Area == GmQuestArea.Kerajaan)
                route = GmQuestCatalog.Return;
            else if (goal.Area == exitArea || (exitArea == GmQuestArea.DesaSorelia &&
                ((area == GmQuestArea.Kerajaan && goal.Area == GmQuestArea.GoaLembayung) ||
                 (area == GmQuestArea.GoaLembayung && goal.Area == GmQuestArea.Kerajaan))))
                route = GmQuestCatalog.Exit;
            if (route != null && IsRouteOpen(route)) return TryGetLandmark(route, out landmark);
            landmark = default;
            return false;
        }

        internal static bool IsRouteLandmark(string id) => id == GmQuestCatalog.Exit || id == GmQuestCatalog.Return;

        internal bool IsRouteOpen(string id)
        {
            GmWorldPortal portal = id == GmQuestCatalog.Return ? returnPortal : exit;
            return portal != null && portal.isActiveAndEnabled && portal.IsOpen;
        }

        private void Update()
        {
            if (player == null || !player.isActiveAndEnabled || player.gameObject.scene != SceneManager.GetActiveScene())
            {
                Close();
                return;
            }

            if (GmDemoScenes.IsLoading) return;
            if (view?.IsOpen == true && view.Page == GmJournalPage.Options)
            {
                view.UpdateOptionsInput();
                return;
            }
            if (view?.IsOpen == true && view.Page == GmJournalPage.ReturnToTitle)
            {
                if (GmGamepadInput.PressedBack() || GmGamepadInput.PressedMenu()) Back();
                else view.RestoreReturnFocus();
                return;
            }
            GmJournalPage? requested = ReadPageShortcut();
            if (view != null && view.IsOpen)
            {
                if (GmGamepadInput.PressedMenu())
                {
                    if (view.Page == GmJournalPage.Pause) Back();
                    else ShowPause();
                }
                else if (GmGamepadInput.PressedBack() || requested == view.Page) Back();
                else if (view.Page == GmJournalPage.Pause)
                {
                    GmJournalPage? page = requested ?? WorldShoulderShortcut();
                    if (page.HasValue) OpenFromPause(page.Value);
                }
                else if (requested.HasValue) ShowPage(requested.Value);
                else if (GmGamepadInput.NextTabPressed) ShowPage((GmJournalPage)(((int)view.Page + 1) % 3));
                else if (GmGamepadInput.PreviousTabPressed) ShowPage((GmJournalPage)(((int)view.Page + 2) % 3));
                else if (view.Page == GmJournalPage.Bag || RefreshEntries()) view.Refresh();
                if (view.IsOpen) view.UpdateInputHints();
                return;
            }
            if (!player.ControlGate.CanExplore) return;
            if (GmGamepadInput.PressedMenu()) requested = GmJournalPage.Pause;
            else requested ??= WorldShoulderShortcut();
            if (!requested.HasValue)
            {
                if (questNoticeRemaining > 0f)
                    questNoticeRemaining = Mathf.Max(0f, questNoticeRemaining - Time.unscaledDeltaTime);
                GmPartyRun.Current.TickItemNotice(Time.unscaledDeltaTime);
                return;
            }
            view ??= new GmQuestJournalView((RectTransform)transform, this);
            player.ControlGate.Block(this);
            if (requested == GmJournalPage.Pause) ShowPause();
            else ShowPage(requested.Value);
        }

        private static GmJournalPage? WorldShoulderShortcut() =>
            GmGamepadInput.PreviousTabPressed ? GmJournalPage.Map :
            GmGamepadInput.NextTabPressed ? GmJournalPage.Bag : null;

        private static GmJournalPage? ReadPageShortcut()
        {
            if (GmGamepadInput.JournalPressed) return GmJournalPage.Journal;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.jKey.wasPressedThisFrame) return GmJournalPage.Journal;
                if (keyboard.mKey.wasPressedThisFrame) return GmJournalPage.Map;
                if (keyboard.iKey.wasPressedThisFrame) return GmJournalPage.Bag;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.J)) return GmJournalPage.Journal;
            if (Input.GetKeyDown(KeyCode.M)) return GmJournalPage.Map;
            if (Input.GetKeyDown(KeyCode.I)) return GmJournalPage.Bag;
#endif
            return null;
        }

        internal void OpenFromPause(GmJournalPage page)
        {
            if (view?.IsOpen != true || view.Page != GmJournalPage.Pause) return;
            pauseSelection = page;
            ShowPage(page);
        }

        private void ShowPause()
        {
            GmJournalPage? selected = pauseSelection;
            pauseSelection = null;
            view.ShowPause(selected);
        }

        internal void Back()
        {
            if (view?.IsOpen != true) return;
            GmGameplayAudio.Ui(GmUiSound.Back);
            if (pauseSelection.HasValue) ShowPause();
            else Close();
        }

        private void ShowPage(GmJournalPage page)
        {
            if (page == GmJournalPage.Bag) view.ShowBag();
            else if (page == GmJournalPage.Map) OpenMap();
            else if (page == GmJournalPage.Options) view.ShowOptions();
            else if (page == GmJournalPage.ReturnToTitle) view.ShowReturnConfirmation();
            else view.ShowJournal();
        }

        internal void Close()
        {
            if (view == null || !view.IsOpen) return;
            pauseSelection = null;
            view.Hide();
            if (player == null) return;
            player.ControlGate.Unblock(this);
            player.SuppressInteraction();
        }

        private void OnDisable() => Close();
    }
}
