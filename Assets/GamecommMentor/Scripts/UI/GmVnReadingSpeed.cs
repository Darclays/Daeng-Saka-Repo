using ProjectVN.Preferences;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal static class GmVnReadingSpeed
    {
        private const string Key = "GamecommMentor.DaengSaka.Dialogue.TextSpeed";

        internal static VNTextSpeedMode Load()
        {
            var saved = (VNTextSpeedMode)PlayerPrefs.GetInt(Key, VNPreferenceKeys.DefaultTextSpeedMode);
            return saved == VNTextSpeedMode.Slow || saved == VNTextSpeedMode.Fast || saved == VNTextSpeedMode.Instant
                ? saved : VNTextSpeedMode.Normal;
        }

        internal static VNTextSpeedMode Cycle(VNTextSpeedMode current)
        {
            var next = current switch
            {
                VNTextSpeedMode.Slow => VNTextSpeedMode.Normal,
                VNTextSpeedMode.Normal => VNTextSpeedMode.Fast,
                VNTextSpeedMode.Fast => VNTextSpeedMode.Instant,
                _ => VNTextSpeedMode.Slow
            };
            PlayerPrefs.SetInt(Key, (int)next);
            PlayerPrefs.Save();
            return next;
        }

        internal static string Caption(VNTextSpeedMode speed) => speed switch
        {
            VNTextSpeedMode.Slow => "Tempo   Lambat   [T]",
            VNTextSpeedMode.Fast => "Tempo   Cepat   [T]",
            VNTextSpeedMode.Instant => "Tempo   Instan   [T]",
            _ => "Tempo   Normal   [T]"
        };
    }
}
