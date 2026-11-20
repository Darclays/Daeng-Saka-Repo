namespace ProjectVN.Preferences
{
    /// <summary>
    /// Patch 10C.5C - Reading text speed mode.
    ///
    /// Patch 10C.5C.1:
    /// - Adds VerySlow without changing the numeric IDs of existing modes.
    /// - This preserves compatibility with preferences.json created by 10C.5C.
    /// </summary>
    public enum VNTextSpeedMode
    {
        Normal = 0,
        Slow = 1,
        Fast = 2,
        Instant = 3,
        Custom = 4,

        // Added later to preserve the previous enum values.
        VerySlow = 5
    }
}
