using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal static class GmGameOptions
    {
        private const string Prefix = "GamecommMentor.DaengSaka.Options.";
        private const string MasterKey = Prefix + "Master";
        private const string MusicKey = Prefix + "Music";
        private const string EffectsKey = Prefix + "Effects";
        private const string CameraKey = Prefix + "Camera";
        private const string ModeKey = Prefix + "Fullscreen";
        private const string WidthKey = Prefix + "Width";
        private const string HeightKey = Prefix + "Height";
        private const string FpsKey = Prefix + "Fps";

        internal static int Master => Volume(MasterKey, 100);
        internal static int Music => Volume(MusicKey, 70);
        internal static int Effects => Volume(EffectsKey, 80);
        internal static float MusicGain => Music / 100f;
        internal static float EffectsGain => Effects / 100f;
        internal static int CameraSensitivity => Mathf.Clamp(PlayerPrefs.GetInt(CameraKey, 100), 50, 200);
        internal static float CameraScale { get; private set; } = 1f;
        internal static event Action MusicChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMusicListeners() => MusicChanged = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            AudioListener.volume = Master / 100f;
            CameraScale = CameraSensitivity / 100f;
            ApplyFrameLimit(FrameLimit);
            if (PlayerPrefs.GetInt(ModeKey, -1) >= 0)
                SetResolution(FullScreen, PreferredSize);
        }

        private static int Volume(string key, int fallback) => Mathf.Clamp(PlayerPrefs.GetInt(key, fallback), 0, 100);

        internal static void SetMaster(int value)
        {
            int volume = Mathf.Clamp(value, 0, 100);
            PlayerPrefs.SetInt(MasterKey, volume);
            AudioListener.volume = volume / 100f;
        }

        internal static void SetMusic(int value)
        {
            PlayerPrefs.SetInt(MusicKey, Mathf.Clamp(value, 0, 100));
            MusicChanged?.Invoke();
        }

        internal static void SetEffects(int value) => PlayerPrefs.SetInt(EffectsKey, Mathf.Clamp(value, 0, 100));

        internal static void SetCameraSensitivity(int value)
        {
            int sensitivity = Mathf.Clamp(value, 50, 200);
            PlayerPrefs.SetInt(CameraKey, sensitivity);
            CameraScale = sensitivity / 100f;
        }

        internal static bool FullScreen => PlayerPrefs.GetInt(ModeKey, Screen.fullScreen ? 1 : 0) == 1;
        internal static int FrameLimit => NormalizeFrameLimit(PlayerPrefs.GetInt(FpsKey, 60));

        internal static Vector2Int PreferredSize
        {
            get
            {
                var saved = new Vector2Int(PlayerPrefs.GetInt(WidthKey, Screen.width),
                    PlayerPrefs.GetInt(HeightKey, Screen.height));
                List<Vector2Int> sizes = AvailableSizes();
                if (sizes.Contains(saved)) return saved;
                var actual = new Vector2Int(Screen.width, Screen.height);
                return sizes.Contains(actual) ? actual : sizes[sizes.Count - 1];
            }
        }

        internal static List<Vector2Int> AvailableSizes()
        {
            var sizes = new List<Vector2Int>();
            foreach (Resolution mode in Screen.resolutions)
            {
                if (mode.width < 960 || mode.height < 540) continue;
                var size = new Vector2Int(mode.width, mode.height);
                if (!sizes.Contains(size)) sizes.Add(size);
            }

            var actual = new Vector2Int(Screen.width, Screen.height);
            if (actual.x >= 960 && actual.y >= 540 && !sizes.Contains(actual)) sizes.Add(actual);
            if (sizes.Count == 0)
                sizes.Add(actual.x > 0 && actual.y > 0 ? actual : new Vector2Int(1280, 720));
            sizes.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return sizes;
        }

        internal static void ApplyDisplay(bool fullScreen, Vector2Int size, int frameLimit)
        {
            if (!AvailableSizes().Contains(size)) size = PreferredSize;
            int fps = NormalizeFrameLimit(frameLimit);
            PlayerPrefs.SetInt(ModeKey, fullScreen ? 1 : 0);
            PlayerPrefs.SetInt(WidthKey, size.x);
            PlayerPrefs.SetInt(HeightKey, size.y);
            PlayerPrefs.SetInt(FpsKey, fps);
            SetResolution(fullScreen, size);
            ApplyFrameLimit(fps);
            PlayerPrefs.Save();
        }

        private static void SetResolution(bool fullScreen, Vector2Int size) =>
            Screen.SetResolution(size.x, size.y, fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

        private static int NormalizeFrameLimit(int fps) => fps == 30 || fps == -1 ? fps : 60;

        private static void ApplyFrameLimit(int fps)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }
    }
}
