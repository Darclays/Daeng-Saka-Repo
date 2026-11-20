namespace ProjectVN.Preferences
{
    /// <summary>
    /// Centralized keys and defaults used by ProjectVN preferences.
    /// Keeping these in one file helps future UI, save migration, and debugging.
    ///
    /// Patch 10C.5C:
    /// - Adds text speed mode presets for typewriter.
    /// </summary>
    public static class VNPreferenceKeys
    {
        public const int CurrentVersion = 4;

        public const string PreferencesFolderName = "ProjectVN";
        public const string PreferencesFileName = "preferences.json";

        public const int DefaultTextSpeedMode = (int)VNTextSpeedMode.Normal;

        public const float VerySlowTextSpeed = 15f;
        public const float SlowTextSpeed = 25f;
        public const float NormalTextSpeed = 45f;
        public const float FastTextSpeed = 80f;
        public const float InstantTextSpeed = 120f;

        public const float DefaultTextSpeed = NormalTextSpeed;
        public const float DefaultAutoDelay = 1.25f;
        public const bool DefaultAutoAfterChoices = false;

        public const float DefaultMasterVolume = 1f;
        public const float DefaultBgmVolume = 0.8f;
        public const float DefaultSfxVolume = 0.8f;
        public const float DefaultVoiceVolume = 1f;

        public const bool DefaultSkipUnseenText = false;
        public const bool DefaultSkipAfterChoices = false;
        public const bool DefaultSkipTransitions = true;

        public const bool DefaultFullscreen = true;
        public const int DefaultTargetFrameRate = 60;

        public const float MinTextSpeed = 1f;
        public const float MaxTextSpeed = 120f;

        public const float MinAutoDelay = 0.1f;
        public const float MaxAutoDelay = 10f;

        public const int MinTargetFrameRate = 30;
        public const int MaxTargetFrameRate = 240;

        public static float GetTextSpeedForMode(VNTextSpeedMode mode)
        {
            switch (mode)
            {
                case VNTextSpeedMode.VerySlow:
                    return VerySlowTextSpeed;
                case VNTextSpeedMode.Slow:
                    return SlowTextSpeed;
                case VNTextSpeedMode.Fast:
                    return FastTextSpeed;
                case VNTextSpeedMode.Instant:
                    return InstantTextSpeed;
                case VNTextSpeedMode.Normal:
                    return NormalTextSpeed;
                case VNTextSpeedMode.Custom:
                    return DefaultTextSpeed;
                default:
                    return DefaultTextSpeed;
            }
        }
    }
}
