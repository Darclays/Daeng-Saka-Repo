using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.Launching.LaunchingDecoration.Shape;

namespace GamecommMentor.DaengSaka.Launching
{
    [Serializable]
    internal sealed class LaunchingView
    {
        private static readonly Color Ivory = new Color32(255, 240, 204, 255);
        private static readonly Color Gold = new Color32(184, 157, 106, 255);
        private static readonly Color Silver = new Color32(141, 151, 161, 255);
        private static readonly Color Panel = new Color32(26, 32, 39, 255);
        private static readonly Color Dark = new Color32(12, 18, 24, 255);
        private static readonly Color Subtitle = new Color32(207, 214, 218, 255);
        private const string Instruction = "Tahan tombol karakter selama 10 detik";
        private const string Announcement = "DAENG SAKA RESMI DILUNCURKAN";

        [SerializeField] private RectTransform canvas, stage;
        [SerializeField] private RawImage video;
        [SerializeField] private AspectRatioFitter videoFit;
        [SerializeField] private Image black;
        [SerializeField] private Text instruction, announcement, counter, fallback;
        [SerializeField] private CardView[] cards;

        internal bool Exists => canvas != null && cards != null && cards.Length == 3;

        [Serializable]
        private sealed class CardView
        {
            [SerializeField] internal RectTransform slot, charge;
            [SerializeField] internal LaunchingDecoration panel, aura, shadow, plaque, fill;
            [SerializeField] internal Image upperRule, lowerRule;
            [SerializeField] internal RawImage sprite, impact;
            [SerializeField] internal Text status, percentage;
            [SerializeField] internal GameObject badge;
            [SerializeField] internal Color accent;
            private LaunchingAtlas.Clip idle, cast, effect;
            private float energy, pop = -1f;
            private string lastStatus;
            private int lastTenths = -1;

            internal void Load(LaunchingAtlas atlas, string idleKey, string castKey, string effectKey)
            {
                idle = atlas.Load(idleKey);
                cast = atlas.Load(castKey) ?? idle;
                effect = atlas.Load(effectKey);
                LaunchingAtlas.Draw(sprite, idle, 0, 9f);
                impact.enabled = false;
            }

            internal void Reset()
            {
                energy = 0f;
                pop = -1f;
                lastStatus = null;
                lastTenths = -1;
                Present(null, 0, 0, false, 0);
            }

            internal void Present(LaunchingControls.Hold hold, double time, float dt, bool celebrating, float collective)
            {
                bool ready = hold?.Ready == true;
                bool charging = hold?.Charging == true;
                float progress = hold?.Progress ?? 0f;
                float age = ready ? (float)Math.Max(0, time - hold.ReadyAt) : 0f;
                energy = ready ? 1f : Mathf.MoveTowards(energy, progress, dt * 2.8f);
                float brightness = Mathf.SmoothStep(0f, 1f, energy);
                float wake = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, energy * 1.35f));
                slot.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, brightness);
                Color trim = Color.Lerp(Silver, accent, wake);
                Color activePanel = Color.Lerp(Dark, accent, .155f);
                panel.Tint(Color.Lerp(Panel, activePanel, wake), trim);
                plaque.Tint(Dark, trim);
                upperRule.color = lowerRule.color = WithAlpha(trim, .57f);
                shadow.Tint(Dark, WithAlpha(trim, .67f));
                float personalBurst = ready ? 1f - Mathf.Clamp01(age / .85f) : 0f;
                float ceremonyBurst = celebrating ? 1f - Mathf.Clamp01(collective / 1.45f) : 0f;
                aura.Tint(accent, trim);
                aura.Animate(wake, wake > 0f ? (float)time : 0f, Mathf.Max(personalBurst, ceremonyBurst));

                int frame = (int)(age * 12f);
                bool casting = ready && cast != null && frame < cast.count;
                LaunchingAtlas.Draw(sprite, casting ? cast : idle, casting ? frame : 0, 9f);
                sprite.rectTransform.localScale = casting || wake <= 0f
                    ? Vector3.one : new Vector3(1f, 1f + Mathf.Sin((float)time * 2.25f) * .023f * wake, 1f);
                sprite.color = new Color(brightness, 1f, 1f, 1f);

                int impactFrame = (int)((age - .36f) * 12f);
                bool showImpact = ready && effect != null && age >= .36f && impactFrame < effect.count;
                if (showImpact)
                {
                    float fit = Mathf.Min(9f, Mathf.Min(345f / effect.width, 244f / effect.height));
                    LaunchingAtlas.Draw(impact, effect, impactFrame, fit);
                    impact.color = new Color(1f, 1f, 1f, .72f);
                }
                else impact.enabled = false;

                string word = ready ? "READY" : charging ? "AWAKENING" : "MENUNGGU";
                if (lastStatus != word)
                {
                    lastStatus = word;
                    status.text = word;
                    status.fontSize = ready ? 37 : 24;
                    status.rectTransform.anchoredPosition = new Vector2(ready ? 0 : 29, 0);
                    badge.SetActive(!ready);
                }
                status.color = ready ? accent : charging ? Color.Lerp(Ivory, accent, wake) : Subtitle;
                float nextPop = ready ? 1f + .18f * Mathf.Sin(Mathf.Clamp01(age / .38f) * Mathf.PI) : 1f;
                if (nextPop != pop)
                {
                    status.rectTransform.localScale = Vector3.one * nextPop;
                    pop = nextPop;
                }

                fill.enabled = energy > .001f;
                charge.sizeDelta = new Vector2(364f * energy, 13f);
                fill.color = accent;
                int tenths = ready ? 100 : charging ? Mathf.FloorToInt(progress * 100f) : 0;
                if (tenths != lastTenths)
                {
                    lastTenths = tenths;
                    percentage.text = ready ? "100%" : (tenths / 10f).ToString("0.#", CultureInfo.InvariantCulture) + " / 10 DETIK";
                }
            }
        }

        internal static LaunchingView Build(Transform parent, Font serif, Font sans, Material pixelMaterial)
        {
            var view = new LaunchingView();
            var canvasObject = new GameObject("Launching Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            var screen = canvasObject.GetComponent<Canvas>();
            screen.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            view.canvas = (RectTransform)canvasObject.transform;
            Fill(Stretch(view.canvas, "Latar layar"), Color.black);

            view.stage = Node(view.canvas, "Panggung 16:9");
            view.stage.anchorMin = view.stage.anchorMax = new Vector2(.5f, .5f);
            view.stage.sizeDelta = new Vector2(1920f, 1080f);
            Decor(At(view.stage, "Latar navy", 0, 0, 1920, 1080), Backdrop,
                new Color32(8, 13, 20, 255), new Color32(32, 43, 56, 190));
            Decor(At(view.stage, "Ornamen emas", 46, 41, 1828, 1000), Frame, Color.clear, WithAlpha(Gold, .46f));
            foreach (float x in new[] { 62f, 1839f })
                foreach (float y in new[] { 56f, 1006f })
                    Decor(At(view.stage, "Sudut", x, y, 19, 19), Diamond, Gold, Color.clear);
            Label(view.stage, "Acara", "O F F I C I A L   G A M E   L A U N C H", sans, 23, 270, 65, 1380, 40, Gold, true);
            Label(view.stage, "Judul", "DAENG SAKA", serif, 85, 230, 100, 1460, 96, Ivory, true);
            Label(view.stage, "Tagline", "Bangkitkan kekuatan para pahlawan", sans, 30, 320, 195, 1280, 45, Subtitle);
            Fill(At(view.stage, "Garis judul", 240, 250, 1440, 1.4f), WithAlpha(Gold, .48f));

            view.cards = new[]
            {
                BuildCard(view.stage, serif, sans, pixelMaterial, 450, "SAKA BARANI", "Sang Pejuang", "X",
                    new Color32(81, 184, 238, 255), new Color32(38, 115, 190, 255), 231f),
                BuildCard(view.stage, serif, sans, pixelMaterial, 960, "PACCA ANDI", "Sang Pelindung", "Y",
                    new Color32(232, 186, 86, 255), new Color32(198, 146, 31, 255), 220f),
                BuildCard(view.stage, serif, sans, pixelMaterial, 1470, "DAUNG", "Sang Penyembuh", "B",
                    new Color32(90, 217, 165, 255), new Color32(175, 44, 48, 255), 186f)
            };
            view.instruction = Label(view.stage, "Instruksi", Instruction, sans, 31, 160, 906, 1600, 49, Ivory, true);
            view.announcement = Label(view.stage, "Peresmian", Announcement, serif, 50, 120, 894, 1680, 69, Ivory, true);
            view.counter = Label(view.stage, "Jumlah siap", "", sans, 25, 360, 963, 1200, 49, Gold, true);

            var movie = Node(view.canvas, "Trailer fullscreen");
            movie.anchorMin = movie.anchorMax = new Vector2(.5f, .5f);
            view.video = movie.gameObject.AddComponent<RawImage>();
            view.video.raycastTarget = false;
            view.videoFit = movie.gameObject.AddComponent<AspectRatioFitter>();
            view.videoFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            view.videoFit.aspectRatio = 16f / 9f;
            view.black = Fill(Stretch(view.canvas, "Fade hitam"), Color.clear);
            view.fallback = Label(view.canvas, "Status trailer", "", sans, 34, 0, 0, 1200, 82, Ivory);
            var fallbackRect = view.fallback.rectTransform;
            fallbackRect.anchorMin = fallbackRect.anchorMax = fallbackRect.pivot = new Vector2(.5f, .5f);
            fallbackRect.anchoredPosition = Vector2.zero;
            view.LoadArt();
            view.Reset();
            return view;
        }

        private static CardView BuildCard(RectTransform parent, Font serif, Font sans, Material pixelMaterial, float centerX,
            string name, string role, string key, Color accent, Color badge, float foot)
        {
            var card = new CardView { accent = accent };
            card.slot = At(parent, name, centerX - 220, 264, 440, 620);
            card.slot.pivot = new Vector2(.5f, .5f);
            card.slot.anchoredPosition = new Vector2(centerX, -574f);
            card.panel = Decor(Stretch(card.slot, "Bingkai kartu"), Card, Panel, Silver);
            Label(card.slot, "Nama", name, serif, 41, 25, 16, 390, 52, Ivory, true);
            Label(card.slot, "Julukan", role, sans, 24, 25, 66, 390, 38, Subtitle);
            card.upperRule = Fill(At(card.slot, "Garis atas", 37, 110, 366, 1.4f), Silver);

            var art = At(card.slot, "Area karakter", 15, 113, 410, 362);
            art.gameObject.AddComponent<RectMask2D>();
            card.aura = Decor(At(art, "Halo dan partikel", 0, 2, 410, 350), Halo, accent, Silver);
            card.shadow = Decor(At(art, "Bayangan kaki", 91, 312, 240, 18), Shadow, Dark, Silver);
            var anchor = At(art, "Pijakan sprite", foot - 15f, 317f, 0, 0);
            card.sprite = SpriteImage(anchor, "Pixel sprite", pixelMaterial);
            card.impact = SpriteImage(anchor, "Impact skill", pixelMaterial);

            card.lowerRule = Fill(At(card.slot, "Garis bawah", 37, 477, 366, 1.4f), Silver);
            RectTransform stateBox = At(card.slot, "Plakat status", 36, 489, 368, 59);
            card.plaque = Decor(stateBox, Plaque, Dark, Silver);
            var badgeNode = At(stateBox, "Tombol Xbox", 15, 10, 40, 40);
            Decor(badgeNode, Badge, badge, Ivory);
            Label(badgeNode, "Huruf", key, sans, 27, 0, 0, 40, 40, Color.white, true);
            card.badge = badgeNode.gameObject;
            card.status = Label(stateBox, "Status", "", sans, 24, 0, 0, 368, 59, Subtitle, true);
            card.status.rectTransform.anchorMin = card.status.rectTransform.anchorMax = new Vector2(.5f, .5f);
            card.status.rectTransform.pivot = new Vector2(.5f, .5f);
            card.status.rectTransform.anchoredPosition = Vector2.zero;
            Decor(At(card.slot, "Trek progress", 38, 566, 364, 13), Plaque, new Color32(61, 70, 79, 255), Color.clear);
            card.charge = At(card.slot, "Isi progress", 38, 566, 364, 13);
            card.fill = Decor(card.charge, Plaque, accent, Color.clear);
            card.percentage = Label(card.slot, "Waktu", "", sans, 24, 35, 581, 370, 32, Subtitle);
            return card;
        }

        internal void LoadArt()
        {
            var atlas = new LaunchingAtlas();
            cards[0].Load(atlas, "sakaIdle", "legacyCast", "legacyImpact");
            cards[1].Load(atlas, "paccaIdle", "auraCast", null);
            cards[2].Load(atlas, "daungIdle", "sanctuaryCast", "sanctuaryImpact");
        }

        internal void Reset()
        {
            stage.gameObject.SetActive(true);
            video.gameObject.SetActive(false);
            video.texture = null;
            fallback.gameObject.SetActive(false);
            foreach (CardView card in cards) card.Reset();
            Footer(0, false);
            SetBlack(0);
        }

        internal void Draw(LaunchingControls.Hold[] holds, double now, float dt, bool celebrating, float age)
        {
            for (int i = 0; i < cards.Length; i++) cards[i].Present(holds[i], now, dt, celebrating, age);
        }

        internal void Footer(int ready, bool final)
        {
            instruction.gameObject.SetActive(!final);
            announcement.gameObject.SetActive(final);
            counter.text = ready + " / 3  PAHLAWAN SIAP";
        }

        internal void SetBlack(float alpha)
        {
            black.enabled = alpha > 0f;
            black.color = new Color(0f, 0f, 0f, alpha);
        }

        internal void ShowVideo(Texture texture, float aspect)
        {
            stage.gameObject.SetActive(false);
            video.texture = texture;
            videoFit.aspectRatio = aspect;
            video.gameObject.SetActive(true);
            SetBlack(0f);
        }

        internal void TrailerFailed(bool missing)
        {
            stage.gameObject.SetActive(false);
            video.gameObject.SetActive(false);
            video.texture = null;
            SetBlack(1f);
            fallback.text = missing ? "Trailer resmi belum tersedia" : "Trailer resmi tidak dapat diputar";
            fallback.gameObject.SetActive(true);
        }

        private static RawImage SpriteImage(RectTransform anchor, string name, Material material)
        {
            RectTransform rect = At(anchor, name, 0, 0, 1, 1);
            rect.pivot = Vector2.zero;
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            return image;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform At(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Node(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var rect = Node(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static LaunchingDecoration Decor(RectTransform rect, LaunchingDecoration.Shape shape, Color fill, Color edge)
        {
            var graphic = rect.gameObject.AddComponent<LaunchingDecoration>();
            graphic.Configure(shape, fill, edge);
            return graphic;
        }

        private static Image Fill(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(Transform parent, string name, string value, Font font, int size,
            float x, float y, float w, float h, Color color, bool bold = false)
        {
            var text = At(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
