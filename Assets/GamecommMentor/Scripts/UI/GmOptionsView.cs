using System;
using System.Collections.Generic;
using ProjectVN.Preferences;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmOptionsView
    {
        internal static readonly Color Navy = new Color32(38, 58, 84, 255);
        internal static readonly Color Gold = new Color32(170, 124, 67, 255);
        private static readonly Color Paper = new Color32(249, 246, 239, 255);
        private static readonly Color Line = new Color32(222, 214, 200, 255);
        private static readonly int[] FrameLimits = { 30, 60, -1 };
        private static readonly VNTextSpeedMode[] TextSpeeds =
            { VNTextSpeedMode.Slow, VNTextSpeedMode.Normal, VNTextSpeedMode.Fast, VNTextSpeedMode.Instant };
        private static readonly string[] TextSpeedNames = { "Lambat", "Normal", "Cepat", "Instan" };

        private readonly RectTransform root;
        private readonly Font font;
        private readonly Text context, hint, applied;
        private readonly Button[] tabs = new Button[3];
        private readonly RectTransform[] pages = new RectTransform[3];
        private readonly Selectable[][] rows = new Selectable[3][];
        private readonly Slider master, music, effects, mode, size, fps, reading, sensitivity;
        private readonly Button apply, back;
        private readonly Action onBack;
        private List<Vector2Int> sizes = GmGameOptions.AvailableSizes();
        private int selectedTab;
        private bool readingSavedValues;

        internal bool IsOpen => root.gameObject.activeSelf;

        internal GmOptionsView(RectTransform canvas, Action onBack)
        {
            this.onBack = onBack;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = Rect("Opsi", canvas, 0f, 0f, 1f, 1f);
            Fill(root, new Color(.08f, .11f, .16f, .70f)).raycastTarget = true;
            RectTransform content = Rect("Rasio Opsi", root, 0f, 0f, 1f, 1f);
            var fitter = content.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            RectTransform panel = Rect("Panel Opsi", content, .14f, .20f, .86f, .80f);
            Fill(panel, Paper);
            Fill(Rect("Aksen", panel, 0f, .987f, 1f, 1f), Gold);
            var title = Label(font, panel, "Judul", "Opsi", 42, .046f, .817f, .38f, .962f, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            context = Label(font, panel, "Konteks", "", 18, .60f, .817f, .954f, .95f, TextAnchor.MiddleRight);
            Fill(Rect("Garis atas", panel, .046f, .811f, .954f, .815f), Line);
            Fill(Rect("Garis kategori", panel, .262f, .169f, .264f, .782f), Line);

            string[] names = { "Audio", "Tampilan", "Kontrol" };
            for (int i = 0; i < names.Length; i++)
            {
                int tab = i;
                float bottom = .665f - i * .121f;
                tabs[i] = Button(panel, names[i], .046f, bottom, .235f, bottom + .097f, () => SelectTab(tab));
                pages[i] = Rect(names[i] + " Panel", panel, .299f, .18f, .955f, .782f);
            }

            master = Volume(pages[0], "Volume utama", 0, GmGameOptions.SetMaster);
            music = Volume(pages[0], "Musik", 1, GmGameOptions.SetMusic);
            effects = Volume(pages[0], "Efek suara", 2, GmGameOptions.SetEffects);
            rows[0] = new Selectable[] { master, music, effects };

            mode = Choice(pages[1], "Mode layar", 0, 1, value => value == 1 ? "Layar penuh" : "Jendela");
            size = Choice(pages[1], "Resolusi", 1, sizes.Count - 1, SizeCaption);
            fps = Choice(pages[1], "Batas FPS", 2, FrameLimits.Length - 1,
                value => FrameLimits[value] < 0 ? "Tanpa batas" : $"{FrameLimits[value]} FPS");
            reading = Choice(pages[1], "Kecepatan teks dialog", 3, TextSpeeds.Length - 1,
                value => TextSpeedNames[value], value => GmVnReadingSpeed.Set(TextSpeeds[value]));
            rows[1] = new Selectable[] { mode, size, fps, reading };

            sensitivity = Meter(pages[2], "Sensitivitas kamera", 0, 50, 200, GmGameOptions.SetCameraSensitivity);
            rows[2] = new Selectable[] { sensitivity };
            AddBindings(pages[2]);

            Fill(Rect("Garis bawah", panel, .046f, .154f, .954f, .158f), Line);
            hint = Label(font, panel, "Petunjuk", "", 16, .046f, .035f, .58f, .137f, TextAnchor.MiddleLeft);
            applied = Label(font, panel, "Status tampilan", "", 14, .51f, .158f, .94f, .192f, TextAnchor.MiddleRight);
            apply = Button(panel, "Terapkan", .605f, .044f, .773f, .122f, ApplyDisplay, primary: true);
            back = Button(panel, "Kembali", .79f, .044f, .954f, .122f, GoBack);
            root.gameObject.SetActive(false);
        }

        internal void Show(string origin)
        {
            GmGameplayAudio.Ui(GmUiSound.Open);
            context.text = origin;
            applied.text = "";
            readingSavedValues = true;
            sizes = GmGameOptions.AvailableSizes();
            size.maxValue = sizes.Count - 1;
            master.value = GmGameOptions.Master;
            music.value = GmGameOptions.Music;
            effects.value = GmGameOptions.Effects;
            mode.value = GmGameOptions.FullScreen ? 1 : 0;
            size.value = Mathf.Max(0, sizes.IndexOf(GmGameOptions.PreferredSize));
            size.GetComponentInChildren<Text>(true).text = SizeCaption((int)size.value);
            fps.value = Mathf.Max(0, Array.IndexOf(FrameLimits, GmGameOptions.FrameLimit));
            reading.value = Mathf.Max(0, Array.IndexOf(TextSpeeds, GmVnReadingSpeed.Load()));
            sensitivity.value = GmGameOptions.CameraSensitivity;
            readingSavedValues = false;
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            SelectTab(0);
            UpdateHint();
        }

        internal void Hide()
        {
            if (!IsOpen) return;
            PlayerPrefs.Save();
            root.gameObject.SetActive(false);
        }

        internal void UpdateInput()
        {
            if (!IsOpen) return;
            if (GmGamepadInput.PressedBack() || GmGamepadInput.PressedMenu())
            {
                GoBack();
                return;
            }

            if (Application.isFocused)
            {
                bool previous = GmGamepadInput.PreviousTabPressed;
                bool next = GmGamepadInput.NextTabPressed;
#if ENABLE_INPUT_SYSTEM
                previous |= Keyboard.current?.qKey.wasPressedThisFrame == true;
                next |= Keyboard.current?.eKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
                previous |= Input.GetKeyDown(KeyCode.Q);
                next |= Input.GetKeyDown(KeyCode.E);
#endif
                if (previous != next)
                {
                    SelectTab((selectedTab + (previous ? 2 : 1)) % 3);
                    return;
                }
            }

            var events = EventSystem.current;
            GameObject selection = events != null ? events.currentSelectedGameObject : null;
            if (selection == null || !selection.activeInHierarchy || !selection.transform.IsChildOf(root))
                GmMenuButton.Focus(tabs[selectedTab]);
            UpdateHint();
        }

        private void SelectTab(int index)
        {
            if (selectedTab != index) GmGameplayAudio.Ui(GmUiSound.Tab);
            selectedTab = index;
            for (int i = 0; i < tabs.Length; i++)
            {
                pages[i].gameObject.SetActive(i == index);
                bool current = i == index;
                tabs[i].targetGraphic.color = current ? Navy : Color.white;
                tabs[i].GetComponentInChildren<Text>().color = current ? Color.white : GmQuestJournalView.Ink;
                SetLinks(tabs[i], i > 0 ? tabs[i - 1] : null, i < tabs.Length - 1 ? tabs[i + 1] : back,
                    right: rows[index][0]);
            }
            bool display = index == 1;
            apply.gameObject.SetActive(display);
            applied.gameObject.SetActive(display);
            Selectable[] list = rows[index];
            for (int i = 0; i < list.Length; i++)
                SetLinks(list[i], i > 0 ? list[i - 1] : tabs[index],
                    i < list.Length - 1 ? list[i + 1] : display ? apply : back);
            SetLinks(apply, list[list.Length - 1], back, right: back);
            SetLinks(back, display ? apply : list[list.Length - 1], tabs[index], left: display ? apply : tabs[index]);
            GmMenuButton.Focus(tabs[index]);
        }

        private void UpdateHint()
        {
            hint.text = GmGamepadInput.UsingGamepad
                ? "[LB/RB] Ganti tab    [←/→] Ubah    [B] Kembali"
                : "[Q/E] Ganti tab    [←/→] Ubah    [Esc] Kembali";
        }

        private void ApplyDisplay()
        {
            GmGameOptions.ApplyDisplay(mode.value == 1f, sizes[(int)size.value], FrameLimits[(int)fps.value]);
            applied.text = "Tampilan diterapkan";
        }

        private string SizeCaption(int index) => $"{sizes[index].x} × {sizes[index].y}";

        private Slider Volume(RectTransform page, string name, int row, Action<int> change) =>
            Meter(page, name, row, 0, 100, change);

        private Slider Meter(RectTransform page, string name, int index, int minimum, int maximum, Action<int> change)
        {
            RectTransform row = MakeRow(page, name, index);
            RectTransform track = Rect("Slider", row, .48f, .22f, .84f, .78f);
            Image background = Fill(Rect("Rel", track, 0f, .40f, 1f, .60f), Line);
            RectTransform fill = Rect("Volume", background.rectTransform, 0f, 0f, 1f, 1f);
            Fill(fill, Navy);
            Image handle = Fill(Rect("Pegangan", track, 0f, .15f, 0f, .85f), Color.white);
            var border = handle.gameObject.AddComponent<Outline>();
            border.effectColor = Navy;
            border.effectDistance = new Vector2(1.5f, -1.5f);
            handle.rectTransform.sizeDelta = new Vector2(10f, 0f);
            Slider slider = SliderFor(track, minimum, maximum, handle);
            background.raycastTarget = true;
            slider.fillRect = fill;
            slider.handleRect = handle.rectTransform;
            Text value = Label(font, row, "Persen", "", 17, .86f, 0f, .99f, 1f, TextAnchor.MiddleRight);
            slider.onValueChanged.AddListener(amount =>
            {
                value.text = $"{(int)amount}%";
                if (readingSavedValues) return;
                change((int)amount);
                GmGameplayAudio.Ui(GmUiSound.Adjust);
            });
            value.text = $"{(int)slider.value}%";
            return slider;
        }

        private Slider Choice(RectTransform page, string name, int rowIndex, int max, Func<int, string> caption,
            Action<int> change = null)
        {
            RectTransform row = MakeRow(page, name, rowIndex);
            RectTransform control = Rect("Pilihan", row, .48f, .12f, 1f, .88f);
            Image background = Fill(control, Color.white);
            background.raycastTarget = true;
            Slider slider = SliderFor(control, 0, max, background);
            Text value = Label(font, control, "Nilai", caption(0), 17, .17f, 0f, .83f, 1f);
            slider.onValueChanged.AddListener(amount =>
            {
                value.text = caption((int)amount);
                if (applied != null) applied.text = "";
                if (readingSavedValues) return;
                change?.Invoke((int)amount);
                GmGameplayAudio.Ui(GmUiSound.Adjust);
            });
            AddStep(-1, 0f, .14f);
            AddStep(1, .86f, 1f);
            return slider;

            void AddStep(int direction, float left, float right)
            {
                Button arrow = Button(control, direction < 0 ? "‹" : "›", left, 0f, right, 1f, () =>
                {
                    slider.value += direction;
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(slider.gameObject);
                });
                arrow.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        private RectTransform MakeRow(RectTransform page, string name, int index)
        {
            float bottom = .776f - index * .239f;
            RectTransform row = Rect(name, page, 0f, bottom, 1f, bottom + .218f);
            Label(font, row, "Nama", name, 18, 0f, 0f, .465f, 1f, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            Fill(Rect("Pemisah", row, 0f, 0f, 1f, .02f), Line);
            return row;
        }

        private void AddBindings(RectTransform parent)
        {
            string[,] bindings =
            {
                { "Aksi", "Keyboard / mouse", "Gamepad" },
                { "Gerak", "WASD", "Stick kiri" },
                { "Kamera", "Tahan klik kanan", "Stick kanan" },
                { "Sprint", "Shift", "RT" },
                { "Interaksi", "E", "A" },
                { "Jeda", "P", "Menu" }
            };
            float[] columns = { 0f, .27f, .68f, 1f };
            for (int row = 0; row < bindings.GetLength(0); row++)
            {
                float bottom = .622f - row * .109f;
                for (int column = 0; column < 3; column++)
                    Label(font, parent, bindings[row, column], bindings[row, column], 16, columns[column],
                        bottom, columns[column + 1], bottom + .104f, TextAnchor.MiddleLeft);
                Fill(Rect("Garis pintasan", parent, 0f, bottom, 1f, bottom + .006f), Line);
            }
        }

        private static Slider SliderFor(RectTransform rect, int min, int max, Graphic target)
        {
            Slider slider = rect.gameObject.AddComponent<Slider>();
            slider.maxValue = max;
            slider.minValue = min;
            slider.wholeNumbers = true;
            slider.targetGraphic = target;
            var colors = slider.colors;
            colors.selectedColor = new Color32(231, 185, 110, 255);
            colors.highlightedColor = colors.selectedColor;
            slider.colors = colors;
            return slider;
        }

        private void GoBack()
        {
            GmGameplayAudio.Ui(GmUiSound.Back);
            onBack();
        }

        private Button Button(RectTransform parent, string text, float left, float bottom, float right, float top,
            Action action, bool primary = false)
        {
            RectTransform rect = Rect(text, parent, left, bottom, right, top);
            Button button = Clickable(Fill(rect, primary ? Navy : Color.white), action);
            Text caption = Label(font, rect, "Label", text, 18, .04f, 0f, .96f, 1f);
            if (primary) caption.color = Color.white;
            return button;
        }

        private static void SetLinks(Selectable item, Selectable up, Selectable down,
            Selectable left = null, Selectable right = null)
        {
            item.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }
    }
}
