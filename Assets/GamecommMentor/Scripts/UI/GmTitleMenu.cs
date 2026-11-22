using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmTitleMenu : MonoBehaviour
    {
        private const string ArtFolder = "DaengSaka/MainMenu/";

        [SerializeField, Tooltip("Poster vertikal final. Gunakan ilustrasi dengan logo terpisah.")]
        private Texture2D poster;
        [SerializeField, Tooltip("Area yang dipakai dari poster final dalam UV (asal kiri bawah).")]
        private Rect posterRegion = new Rect(0f, 0f, 1f, 1f);
        [SerializeField, Tooltip("Logo transparan opsional. Jika kosong, judul dibuat dari teks.")]
        private Texture2D wordmark;
        [SerializeField, Tooltip("Musik loop untuk layar judul.")]
        private AudioClip music;

        private RectTransform menu;
        private CanvasGroup titleControls;
        private Button start, options, quit;
        private Text hint, status;
        private GmOptionsView optionsView;
        private GmControlDialog exitDialog;
        private Texture2D artworkFade;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var camera = gameObject.GetComponent<Camera>();
            if (camera == null) camera = gameObject.AddComponent<Camera>();
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            EnsureEventSystem();
            PlayMusic();

            var root = new GameObject("Layar Judul", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            var layer = (RectTransform)root.transform;
            Fill(Rect("Bidang putih", layer, 0f, 0f, 1f, 1f), Color.white);

            menu = Rect("Main Menu", layer, 0f, 0f, 1f, 1f);
            var fitter = menu.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            titleControls = menu.gameObject.AddComponent<CanvasGroup>();
            BuildPoster();

            Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildWordmark(uiFont);
            Fill(Rect("Garis judul", menu, .094f, .463f, .312f, .465f), GmOptionsView.Gold);
            start = MenuButton(uiFont, "Mulai Demo", .345f, TryStart, primary: true);
            options = MenuButton(uiFont, "Opsi", .267f, OpenOptions);
            quit = MenuButton(uiFont, "Keluar", .189f, AskToQuit);
            GmMenuButton.Link(start, null, options);
            GmMenuButton.Link(options, start, quit);
            GmMenuButton.Link(quit, options, null);
            hint = Label(uiFont, menu, "Petunjuk", "", 16, .094f, .052f, .55f, .10f, TextAnchor.MiddleLeft);
            status = Label(uiFont, menu, "Status", "", 16, .094f, .11f, .59f, .168f, TextAnchor.MiddleLeft);
            status.color = new Color32(121, 66, 45, 255);
            optionsView = new GmOptionsView(layer, () => BackToTitle(options));
            exitDialog = new GmControlDialog(layer, uiFont, () => BackToTitle(quit), QuitGame);
        }

        private void Start()
        {
            GmGamepadInput.ConfigureUi(EventSystem.current);
            GmMenuButton.Focus(start);
        }

        private void Update()
        {
            if (GmDemoScenes.IsLoading) return;
            if (optionsView.IsOpen)
            {
                optionsView.UpdateInput();
                return;
            }
            if (exitDialog.IsOpen)
            {
                if (GmGamepadInput.PressedBack()) BackToTitle(quit);
                else exitDialog.RestoreFocus();
                return;
            }
            if (GmGamepadInput.PressedBack())
            {
                AskToQuit();
                return;
            }
            hint.text = GmGamepadInput.UsingGamepad ? "[D-pad] Pindah    [A] Pilih" : "[↑/↓] Pindah    [Enter] Pilih";
            GmMenuButton.FocusWithin(menu, start);
        }

        private void BuildPoster()
        {
            bool temporary = poster == null;
            Texture2D image = temporary ? Resources.Load<Texture2D>(ArtFolder + "PosterSementara") : poster;
            if (image == null) return;
            Rect region = temporary ? new Rect(463f / 1600f, 6f / 1112f, 590f / 1600f, 882f / 1112f) : posterRegion;
            if (region.width <= 0f || region.height <= 0f) region = new Rect(0f, 0f, 1f, 1f);
            float aspect = image.width * region.width / (image.height * region.height);
            float width = Mathf.Min(.53f, aspect * 9f / 16f);
            float visibleHeight = Mathf.Min(1f, width / (aspect * 9f / 16f));
            RectTransform art = Rect("Poster kanan", menu, 1f - width, (1f - visibleHeight) * .5f,
                1f, (1f + visibleHeight) * .5f);
            RawImage illustration = art.gameObject.AddComponent<RawImage>();
            illustration.texture = image;
            illustration.uvRect = region;
            illustration.raycastTarget = false;
            var fade = Rect("Dissolve putih", art, 0f, 0f, 1f, 1f).gameObject.AddComponent<RawImage>();
            artworkFade = CreateFade(temporary);
            fade.texture = artworkFade;
            fade.raycastTarget = false;
        }

        private static Texture2D CreateFade(bool maskTemporaryLogo)
        {
            const int samples = 64;
            var tex = new Texture2D(samples, samples, TextureFormat.RGBA32, false)
                { name = "Baur putih Main Menu", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var colors = new Color[samples * samples];
            for (int y = 0; y < samples; y++)
            for (int x = 0; x < samples; x++)
            {
                float u = (float)x / (samples - 1), v = (float)y / (samples - 1);
                float left = 1f - Mathf.SmoothStep(0f, 1f, u / .21f);
                float top = maskTemporaryLogo ? Mathf.SmoothStep(0f, 1f, (v - .84f) / .07f)
                    * Mathf.SmoothStep(0f, 1f, (u - .20f) / .09f)
                    * Mathf.SmoothStep(0f, 1f, (.82f - u) / .12f) : 0f;
                colors[y * samples + x] = new Color(1f, 1f, 1f, 1f - (1f - left) * (1f - top));
            }
            tex.SetPixels(colors);
            tex.Apply(false, true);
            return tex;
        }

        private void BuildWordmark(Font fallback)
        {
            if (wordmark != null)
            {
                var space = Rect("Ruang logo", menu, .09f, .50f, .555f, .894f);
                var image = Rect("Logo", space, 0f, 0f, 1f, 1f).gameObject.AddComponent<RawImage>();
                image.texture = wordmark;
                image.raycastTarget = false;
                var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = wordmark.width / (float)wordmark.height;
                return;
            }

            Font serif = Resources.Load<Font>(ArtFolder + "LiberationSerif-Regular");
            if (serif == null) serif = fallback;
            Color ink = new Color32(55, 39, 33, 255);
            Text subtitle = Label(serif, menu, "The Odyssey of", "The Odyssey of", 29, .094f, .82f, .52f, .889f, TextAnchor.MiddleLeft);
            subtitle.fontStyle = FontStyle.Italic;
            subtitle.color = ink;
            Text first = Label(serif, menu, "Daeng", "DAENG", 120, .085f, .65f, .59f, .83f, TextAnchor.MiddleLeft);
            Text second = Label(serif, menu, "Saka", "SAKA", 120, .085f, .49f, .59f, .67f, TextAnchor.MiddleLeft);
            first.fontStyle = second.fontStyle = FontStyle.Bold;
            first.color = second.color = ink;
        }

        private Button MenuButton(Font font, string label, float bottom, Action action, bool primary = false)
        {
            RectTransform rect = Rect(label, menu, .09f, bottom, .346f, bottom + .064f);
            var button = Clickable(Fill(rect, primary ? GmOptionsView.Navy : new Color32(250, 248, 243, 255)), action);
            Color ink = primary ? Color.white : GmQuestJournalView.Ink;
            Text caption = Label(font, rect, "Label", label, 20, .071f, 0f, .87f, 1f, TextAnchor.MiddleLeft);
            caption.fontStyle = FontStyle.Bold;
            caption.color = ink;
            Label(font, rect, "Panah", "›", 23, .88f, 0f, .96f, 1f).color = ink;
            return button;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null && EventSystem.current.gameObject.scene == gameObject.scene) return;
            var events = new GameObject("EventSystem");
            events.transform.SetParent(transform, false);
            events.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var input = events.AddComponent<InputSystemUIInputModule>();
            if (input.actionsAsset == null) input.AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }

        private void PlayMusic()
        {
            if (music == null) return;
            gameObject.AddComponent<AudioListener>();
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = music;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            gameObject.AddComponent<GmMusicVolume>();
            source.Play();
        }

        private void TryStart()
        {
            if (!GmDemoScenes.TryStart())
            {
                status.text = "Ibu Kota belum bisa dibuka. Periksa daftar scene demo.";
                return;
            }
            status.text = "Membuka Ibu Kota…";
            SetMenuInput(false);
        }

        private void OpenOptions()
        {
            SetMenuInput(false);
            optionsView.Show("Main Menu");
        }

        private void AskToQuit()
        {
            SetMenuInput(false);
            exitDialog.Show("Keluar dari game?", "Demo akan ditutup.", "Batal", "Keluar");
        }

        private void QuitGame()
        {
            BackToTitle(quit);
            Application.Quit();
        }

        private void BackToTitle(Button focus)
        {
            optionsView.Hide();
            exitDialog.Hide();
            SetMenuInput(true);
            GmMenuButton.Focus(focus);
        }

        private void SetMenuInput(bool enabled)
        {
            titleControls.interactable = enabled;
            titleControls.blocksRaycasts = enabled;
        }

        private void OnDestroy()
        {
            optionsView?.Hide();
            if (artworkFade != null) Destroy(artworkFade);
        }
    }
}
