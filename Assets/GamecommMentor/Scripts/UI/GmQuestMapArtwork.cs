using System;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal static class GmQuestMapArtwork
    {
        private const string ResourceFolder = "DaengSaka/QuestMaps/";

        [Serializable]
        private sealed class Calibration
        {
            public float minX, minZ, maxX, maxZ;
        }

        internal static Texture2D Resolve(GmQuestJournal journal, out Rect bounds)
        {
            bounds = journal.MapBounds;
            Texture2D custom = journal.MapArtwork;
            if (custom != null && Valid(bounds)) return custom;

            string name = journal.gameObject.scene.name;
            string resource = (journal.Area, name) switch
            {
                (GmQuestArea.Kerajaan, "IbuKota") => "IbuKota",
                (GmQuestArea.DesaSorelia, "DesaSorelia") => "DesaSorelia",
                (GmQuestArea.GoaLembayung, "GoaLembanyung") => "GoaLembayung",
                (GmQuestArea.GoaLembayung, "GoaLembayung") => "GoaLembayung",
                _ => null
            };
            if (resource != null)
            {
                var texture = Resources.Load<Texture2D>(ResourceFolder + resource);
                var calibration = Resources.Load<TextAsset>(ResourceFolder + resource + "_Bounds");
                if (texture != null && TryReadBounds(calibration, out Rect calibratedBounds))
                {
                    bounds = calibratedBounds;
                    return texture;
                }
            }
            if (!Valid(bounds))
            {
                Vector3 center = journal.Player != null ? journal.Player.position : Vector3.zero;
                bounds = new Rect(center.x - 10f, center.z - 10f, 20f, 20f);
            }
            return null;
        }

        private static bool TryReadBounds(TextAsset asset, out Rect bounds)
        {
            bounds = default;
            if (asset == null) return false;
            Calibration data;
            try { data = JsonUtility.FromJson<Calibration>(asset.text); }
            catch (ArgumentException) { return false; }
            if (data == null) return false;
            bounds = new Rect(data.minX, data.minZ, data.maxX - data.minX, data.maxZ - data.minZ);
            return Valid(bounds);
        }

        private static bool Valid(Rect bounds) => bounds.width > .001f && bounds.height > .001f &&
            Finite(bounds.xMin) && Finite(bounds.yMin) && Finite(bounds.xMax) && Finite(bounds.yMax);

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static Vector2 Project(Rect bounds, Vector3 position, out bool outside)
        {
            outside = position.x < bounds.xMin || position.x > bounds.xMax ||
                position.z < bounds.yMin || position.z > bounds.yMax;
            return new Vector2(Mathf.InverseLerp(bounds.xMin, bounds.xMax, position.x),
                Mathf.InverseLerp(bounds.yMin, bounds.yMax, position.z));
        }
    }
}
