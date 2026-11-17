using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmQuestAreaMapView
    {
        private const string PlayerId = "@saka";
        private static readonly Color Field = new Color32(226, 232, 217, 255);
        private static readonly Color Grid = new Color32(198, 209, 190, 255);
        private readonly GmQuestJournal journal;
        private readonly Font font;
        private readonly RectTransform plot, pins, list;
        private readonly ScrollRect scroll;
        private readonly Text locationTitle, locationText, objectiveText;
        private readonly List<(GmQuestLandmark landmark, Image mark, Text icon, Button row)> locations =
            new List<(GmQuestLandmark, Image, Text, Button)>();
        private string trackedLandmark;
        private string focusedLandmark;

        internal RectTransform Panel { get; }

        public GmQuestAreaMapView(RectTransform panel, GmQuestJournal journal, Font font)
        {
            Panel = panel;
            this.journal = journal;
            this.font = font;
            AddText(font, panel, "Area", GmQuestCatalog.AreaName(journal.Area), 30, 0f, .884f, .64f, 1f, bold: true);
            string subtitle = journal.MapArtwork != null ? "Peta area" : "Skema area";
            AddText(font, panel, "Map Type", subtitle, 15, .70f, .899f, 1f, .991f, TextAnchor.MiddleRight).color = Muted;
            RectTransform chart = Rect("Area Map", panel, 0f, .071f, .696f, .873f);
            Fill(chart, Field);
            plot = Rect("Map Coordinates", chart, .084f, .094f, .916f, .906f);
            if (journal.MapArtwork != null)
            {
                var artwork = Rect("Artwork", plot, 0f, 0f, 1f, 1f).gameObject.AddComponent<RawImage>();
                artwork.texture = journal.MapArtwork;
                artwork.raycastTarget = false;
            }
            else
            {
                for (float fraction = 0f; fraction <= 1f; fraction += .25f)
                {
                    Fill(Rect("Longitude", plot, fraction, 0f, fraction + .0016f, 1f), Grid);
                    Fill(Rect("Latitude", plot, 0f, fraction, 1f, fraction + .0027f), Grid);
                }
            }
            AddText(font, chart, "North", "↑ U", 16, .014f, .859f, .09f, .979f, TextAnchor.MiddleCenter, bold: true).color = Muted;
            pins = Rect("Pins", plot, 0f, 0f, 1f, 1f);
            AddText(font, panel, "Region Route", "Kerajaan  →  Desa Sorelia  ↔  Goa Lembayung", 16,
                0f, 0f, .696f, .061f).color = Muted;

            RectTransform sidebar = Rect("Map Detail", panel, .72f, .071f, 1f, .873f);
            Fill(sidebar, Inset);
            AddText(font, sidebar, "Locations", "Lokasi", 17, .065f, .883f, .935f, .984f, bold: true);
            RectTransform viewport = Rect("Location List", sidebar, .066f, .421f, .934f, .872f);
            list = CreateScrollContent(viewport, out scroll);
            Fill(Rect("Rule", sidebar, .066f, .397f, .934f, .402f), Rule);
            locationTitle = AddText(font, sidebar, "Place", "", 19, .066f, .304f, .934f, .391f, bold: true);
            locationText = AddText(font, sidebar, "Place Detail", "", 16, .066f, .151f, .934f, .301f,
                TextAnchor.UpperLeft);
            locationText.color = Muted;
            objectiveText = AddText(font, sidebar, "Quest Location", "", 16, .066f, .013f, .934f, .144f,
                TextAnchor.UpperLeft, bold: true);
            objectiveText.color = Leaf;
        }

        internal void Render(GmQuestEntry focus)
        {
            ClearChildren(pins);
            ClearChildren(list);
            locations.Clear();
            GmQuestEntry tracked = journal.Tracked;
            trackedLandmark = NavigationId(tracked);
            focusedLandmark = NavigationId(focus);

            foreach (GmQuestLandmark landmark in journal.Landmarks)
                if (landmark.IsVisible) AddLocation(landmark);
            if (journal.Player != null)
                AddLocation(new GmQuestLandmark(PlayerId, "Saka", "Posisi Saka saat peta dibuka.", journal.Player));
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;

            GmQuestEntry context = focus != null && !focus.IsComplete ? focus : tracked;
            if (context == null)
                objectiveText.text = "";
            else
            {
                GmQuestGoal goal = context.CurrentGoal;
                string heading = context.Id == tracked?.Id ? "Quest dilacak" : "Lokasi objektif";
                objectiveText.text = heading + "\n" + GmQuestCatalog.AreaName(goal.Area) + " · " + goal.Text;
            }

            string selected = focusedLandmark ?? trackedLandmark ?? PlayerId;
            Select(selected);
            if (focus != null && !focus.IsComplete && focusedLandmark == null)
            {
                locationTitle.text = GmQuestCatalog.AreaName(focus.CurrentGoal.Area);
                locationText.text = focus.CurrentGoal.Area == journal.Area
                    ? "Lokasi objektif belum ditandai."
                    : "Tujuan berada di area lain.";
                SelectRow(null);
            }
        }

        private string NavigationId(GmQuestEntry entry) =>
            entry != null && !entry.IsComplete && journal.TryGetNavigation(entry.CurrentGoal, out GmQuestLandmark landmark)
                ? landmark.Id : null;

        private void AddLocation(GmQuestLandmark landmark)
        {
            UnityEngine.Rect bounds = journal.MapBounds;
            Vector3 position = landmark.Point.position;
            Vector2 normalized = new Vector2(Mathf.InverseLerp(bounds.xMin, bounds.xMax, position.x),
                Mathf.InverseLerp(bounds.yMin, bounds.yMax, position.z));
            RectTransform marker = Rect(landmark.Label, pins, normalized.x, normalized.y, normalized.x, normalized.y);
            marker.sizeDelta = new Vector2(34f, 30f);
            Image mark = Fill(marker, Paper);
            Clickable(mark, () => Select(landmark.Id));
            string glyph = landmark.Id == PlayerId ? "▲" : landmark.Id == trackedLandmark ? "◆" : "●";
            Text icon = AddText(font, marker, "Symbol", glyph, 17, 0f, 0f, 1f, 1f, TextAnchor.MiddleCenter, bold: true);

            string label = landmark.Id == trackedLandmark ? "◆   " + landmark.Label : landmark.Label;
            Button row = AddButton(font, list, label, 0f, 0f, 1f, 1f, () => Select(landmark.Id));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            locations.Add((landmark, mark, icon, row));
        }

        private void Select(string id)
        {
            SelectRow(id);
            foreach (var location in locations)
            {
                if (location.landmark.Id != id) continue;
                locationTitle.text = location.landmark.Label;
                locationText.text = location.landmark.Id == GmQuestCatalog.Exit && !journal.ExitOpen
                    ? "Portal terkunci. Lanjutkan quest utama untuk membukanya."
                    : location.landmark.Description;
                location.mark.rectTransform.SetAsLastSibling();
                return;
            }
            locationTitle.text = GmQuestCatalog.AreaName(journal.Area);
            locationText.text = "";
        }

        private void SelectRow(string id)
        {
            foreach (var location in locations)
            {
                bool selected = location.landmark.Id == id;
                bool tracked = location.landmark.Id == trackedLandmark;
                location.mark.color = tracked ? Leaf : selected ? LeafLight : Paper;
                location.icon.color = tracked ? Paper : Ink;
                SetSelected(location.row, selected);
            }
        }
    }
}
