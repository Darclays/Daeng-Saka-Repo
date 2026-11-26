using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka.Launching
{
    internal sealed class LaunchingAtlas
    {
        private const string Folder = "DaengSaka/BattleStage/";

        [Serializable] private sealed class Manifest { public Clip[] clips; }

        [Serializable] internal sealed class Clip
        {
            public string key;
            public int width, height, count, anchorX, anchorY;
            [NonSerialized] internal Texture2D texture;
        }

        private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();

        internal LaunchingAtlas()
        {
            TextAsset asset = Resources.Load<TextAsset>(Folder + "Atlas");
            if (asset == null) return;
            Clip[] entries = JsonUtility.FromJson<Manifest>(asset.text)?.clips;
            if (entries == null) return;
            foreach (Clip entry in entries)
                clips[entry.key] = entry;
        }

        internal Clip Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (clips.TryGetValue(key, out Clip entry))
            {
                if (entry.texture == null) entry.texture = Resources.Load<Texture2D>(Folder + key);
                if (entry.count > 0 && entry.texture != null && entry.texture.width == entry.width * entry.count &&
                    entry.texture.height == entry.height) return entry;
            }
            Debug.LogWarning("[Daeng Saka Launching] Atlas tidak lengkap untuk " + key);
            return null;
        }

        internal static void Draw(RawImage image, Clip clip, int frame, float scale)
        {
            image.enabled = clip != null;
            if (clip == null) return;
            image.texture = clip.texture;
            image.uvRect = new Rect((float)frame / clip.count, 0f, 1f / clip.count, 1f);
            image.rectTransform.sizeDelta = new Vector2(clip.width * scale, clip.height * scale);
            image.rectTransform.anchoredPosition = new Vector2(-clip.anchorX * scale, (clip.anchorY - clip.height) * scale);
        }
    }
}
