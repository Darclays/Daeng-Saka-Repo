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
        private static readonly Color QuestPin = new Color32(242, 201, 106, 255);
        private static readonly Color PlayerPin = new Color32(49, 112, 109, 255);
        private static readonly Color PortalPin = new Color32(66, 85, 105, 255);
        private static readonly Color OfferInk = new Color32(182, 34, 55, 255);
        private readonly GmQuestJournal journal;
        private readonly Font font;
        private readonly RectTransform plot, pins, list;
        private readonly Rect bounds;
        private readonly ScrollRect scroll;
        private readonly Text locationTitle, locationText, objectiveText, offerCount;
        private readonly List<Location> locations = new List<Location>();
        private string trackedLandmark;
        private string selectedLandmark;
        private Button tab;

        private sealed class Location
        {
            internal GmQuestLandmark Landmark;
            internal Image Mark;
            internal Text Icon;
            internal Outline Border;
            internal Button Row;
            internal bool Outside;
            internal GmSideQuestDefinition Offer;
        }

        internal RectTransform Panel { get; }

        public GmQuestAreaMapView(RectTransform panel, GmQuestJournal journal, Font font)
        {
            Panel = panel;
            this.journal = journal;
            this.font = font;
            Texture2D texture = GmQuestMapArtwork.Resolve(journal, out bounds);
            AddText(font, panel, "Area", GmQuestCatalog.AreaName(journal.Area), 32, 0f, .884f, .64f, 1f, bold: true);
            offerCount = AddText(font, panel, "Tawaran quest", "", 18, .49f, .885f, .993f, .993f,
                TextAnchor.MiddleRight, bold: true);
            offerCount.color = OfferInk;
            RectTransform chart = Rect("Area Map", panel, 0f, .087f, .704f, .873f);
            Fill(chart, Ink);
            RectTransform interior = Rect("Peta", chart, .012f, .018f, .988f, .982f);
            Fill(interior, new Color32(49, 63, 54, 255));
            plot = Rect("Map Coordinates", interior, 0f, 0f, 1f, 1f);
            var fit = plot.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = texture != null ? texture.width / (float)texture.height : bounds.width / bounds.height;
            if (texture != null)
            {
                var artwork = Rect("Artwork", plot, 0f, 0f, 1f, 1f).gameObject.AddComponent<RawImage>();
                artwork.texture = texture;
                artwork.raycastTarget = false;
            }
            else
            {
                Fill(plot, Field);
                for (float fraction = 0f; fraction <= 1f; fraction += .25f)
                {
                    Fill(Rect("Longitude", plot, fraction, 0f, fraction + .0016f, 1f), Grid);
                    Fill(Rect("Latitude", plot, 0f, fraction, 1f, fraction + .0027f), Grid);
                }
            }
            pins = Rect("Pins", plot, 0f, 0f, 1f, 1f);
            RectTransform north = Rect("Kompas", chart, .034f, .966f, .034f, .966f);
            north.pivot = new Vector2(0f, 1f);
            north.sizeDelta = new Vector2(39f, 54f);
            Fill(north, Ink);
            AddText(font, north, "North", "↑\nU", 19, 0f, 0f, 1f, 1f, TextAnchor.MiddleCenter, bold: true).color = Paper;
            var legend = AddText(font, panel, "Legenda",
                "▲  Saka    ◆  Objektif    <color=#B62237>!</color>  Tawaran quest    ↗  Portal    ●  Lokasi", 16,
                .004f, .001f, .704f, .075f);
            legend.supportRichText = true;
            legend.color = Muted;

            RectTransform sidebar = Rect("Map Detail", panel, .72f, .087f, 1f, .873f);
            Fill(sidebar, Inset);
            AddText(font, sidebar, "Locations", "Quest & lokasi", 21, .065f, .875f, .935f, .985f, bold: true);
            RectTransform viewport = Rect("Location List", sidebar, .066f, .435f, .934f, .868f);
            list = CreateScrollContent(viewport, out scroll);
            Fill(Rect("Rule", sidebar, .066f, .397f, .934f, .402f), Rule);
            locationTitle = AddText(font, sidebar, "Place", "", 21, .066f, .296f, .934f, .391f, bold: true);
            locationText = AddText(font, sidebar, "Place Detail", "", 16, .066f, .149f, .934f, .288f,
                TextAnchor.UpperLeft);
            locationText.color = Muted;
            objectiveText = AddText(font, sidebar, "Quest Location", "", 16, .066f, .013f, .934f, .144f,
                TextAnchor.UpperLeft, bold: true);
            objectiveText.color = Leaf;
        }

        internal Button Render(GmQuestEntry focus, Button mapTab)
        {
            tab = mapTab;
            ClearChildren(pins);
            ClearChildren(list);
            locations.Clear();
            GmQuestEntry tracked = journal.Tracked;
            bool trackingOffer = journal.TryGetTrackedOffer(out var offer, out var giverLocation) && focus == null;
            trackedLandmark = trackingOffer ? giverLocation.Id : NavigationId(tracked);
            string focusedLandmark = NavigationId(focus);

            foreach (var (giver, quest) in journal.LocalOffers())
                AddLocation(journal.OfferLandmark(giver, quest), quest);
            offerCount.text = locations.Count > 0 ? $"{locations.Count} quest tersedia · Pilih untuk melacak" : "";
            if (journal.Player != null)
                AddLocation(new GmQuestLandmark(PlayerId, "Saka", "Posisi dan arah hadap Saka saat membuka peta.", journal.Player));
            foreach (GmQuestLandmark landmark in journal.Landmarks)
            {
                if (landmark.IsVisible && !locations.Exists(location => location.Offer != null && location.Landmark.Id == landmark.Id))
                    AddLocation(landmark);
            }
            foreach (Location location in locations)
                if (location.Offer != null && location.Mark != null) location.Mark.rectTransform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;

            GmQuestEntry context = focus != null && !focus.IsComplete ? focus : tracked;
            if (trackingOffer)
                ShowOffer(offer);
            else if (context == null)
                objectiveText.text = "";
            else
            {
                GmQuestGoal goal = context.CurrentGoal;
                string heading = context.Id == tracked?.Id ? "Dilacak" : "Objektif";
                objectiveText.text = heading + "\n" + goal.Text +
                    (goal.Area != journal.Area ? " · " + GmQuestCatalog.AreaName(goal.Area) : "");
            }

            string selected = focusedLandmark ?? trackedLandmark ?? PlayerId;
            Select(selected);
            if (context != null && NavigationId(context) == null)
            {
                locationTitle.text = GmQuestCatalog.AreaName(context.CurrentGoal.Area);
                locationText.text = context.CurrentGoal.Area == journal.Area
                    ? "Lokasi objektif belum ditandai."
                    : "Tujuan berada di area lain.";
                SelectRow(null);
            }
            return LinkLocations();
        }

        internal void AnimateOffers()
        {
            float pulse = .5f + .5f * Mathf.Sin((float)Time.unscaledTimeAsDouble * 5f);
            foreach (Location location in locations)
                if (location.Offer != null && location.Icon != null)
                    location.Icon.rectTransform.localScale = Vector3.one * (1f + .24f * pulse);
        }

        private string NavigationId(GmQuestEntry entry) =>
            entry != null && !entry.IsComplete && journal.TryGetNavigation(entry.CurrentGoal, out GmQuestLandmark landmark)
                ? landmark.Id : null;

        private void AddLocation(GmQuestLandmark landmark, GmSideQuestDefinition offer = null)
        {
            landmark.Point.TryGetComponent(out GmPixelCrushersConversation giver);
            bool playerPin = landmark.Id == PlayerId;
            bool questPin = landmark.Id == trackedLandmark;
            string glyph = playerPin ? "▲" : offer != null ? "!" :
                questPin ? "◆" : GmQuestJournal.IsRouteLandmark(landmark.Id) ? "↗" : "●";
            Vector2 normalized = GmQuestMapArtwork.Project(bounds, landmark.Point.position, out bool outside);
            var location = new Location { Landmark = landmark, Outside = outside, Offer = offer };
            if (!outside)
            {
                RectTransform marker = Rect(landmark.Label, pins, normalized.x, normalized.y, normalized.x, normalized.y);
                marker.sizeDelta = offer != null ? new Vector2(31f, 29f) :
                    questPin ? new Vector2(28f, 26f) : new Vector2(23f, 21f);
                location.Mark = Fill(marker, Paper);
                location.Border = marker.gameObject.AddComponent<Outline>();
                location.Border.effectColor = Ink;
                location.Border.effectDistance = new Vector2(1f, -1f);
                Clickable(location.Mark, Activate).navigation = new Navigation { mode = Navigation.Mode.None };
                location.Icon = AddText(font, marker, "Symbol", glyph, offer != null ? 20 : 15, 0f, 0f, 1f, 1f,
                    TextAnchor.MiddleCenter, bold: true);
                if (playerPin)
                {
                    GmSakaMotor motor = landmark.Point.GetComponent<GmSakaMotor>();
                    Vector3 direction = motor != null ? motor.WorldFacing : landmark.Point.forward;
                    float angle = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    location.Icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            string rowText = offer != null ? $"!  {offer.Title}\n    {landmark.Label}" : glyph + "   " + landmark.Label;
            location.Row = AddButton(font, list, rowText, 0f, 0f, 1f, 1f, Activate);
            var caption = Caption(location.Row);
            caption.alignment = TextAnchor.MiddleLeft;
            if (offer != null) caption.color = OfferInk;
            location.Row.gameObject.AddComponent<LayoutElement>().preferredHeight = offer != null ? 47f : 34f;
            locations.Add(location);

            void Activate()
            {
                if (offer != null && journal.TryTrackOffer(giver, offer.Id))
                {
                    trackedLandmark = landmark.Id;
                    ShowOffer(offer);
                }
                Select(landmark.Id);
            }
        }

        private void ShowOffer(GmSideQuestDefinition offer) => objectiveText.text = "Tawaran dilacak\n" + offer.Title;

        private void Select(string id)
        {
            SelectRow(id);
            foreach (var location in locations)
            {
                if (location.Landmark.Id != id) continue;
                GmQuestLandmark landmark = location.Landmark;
                locationTitle.text = landmark.Label;
                locationText.text = location.Offer != null ? $"Bicara dengan {landmark.Label} untuk mengambil quest." :
                    GmQuestJournal.IsRouteLandmark(landmark.Id) && !journal.IsRouteOpen(landmark.Id)
                    ? "Portal terkunci. Lanjutkan quest utama untuk membukanya."
                    : location.Outside ? "Lokasi ini berada di luar cakupan peta rute." : landmark.Description;
                if (location.Mark != null) location.Mark.rectTransform.SetAsLastSibling();
                return;
            }
            locationTitle.text = GmQuestCatalog.AreaName(journal.Area);
            locationText.text = "";
        }

        private void SelectRow(string id)
        {
            selectedLandmark = id;
            foreach (var location in locations)
            {
                string key = location.Landmark.Id;
                bool selected = key == id;
                bool tracked = key == trackedLandmark;
                SetSelected(location.Row, selected);
                if (location.Mark == null) continue;
                bool portal = GmQuestJournal.IsRouteLandmark(key);
                location.Mark.color = location.Offer != null ? OfferInk : key == PlayerId ? PlayerPin : tracked ? QuestPin :
                    portal ? PortalPin : selected ? LeafLight : Paper;
                location.Icon.color = location.Offer != null || key == PlayerId || (portal && !tracked) ? Paper : Ink;
                location.Border.effectColor = location.Offer != null && tracked ? QuestPin : Ink;
            }
        }

        private Button LinkLocations()
        {
            Button first = locations.Count > 0 ? locations[0].Row : null;
            for (int i = 0; i < locations.Count; i++)
            {
                Location location = locations[i];
                location.Row.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? locations[i - 1].Row : tab,
                    selectOnDown = i + 1 < locations.Count ? locations[i + 1].Row : null,
                    selectOnLeft = tab
                };
                if (location.Landmark.Id == selectedLandmark) first = location.Row;
            }
            return first;
        }
    }
}
