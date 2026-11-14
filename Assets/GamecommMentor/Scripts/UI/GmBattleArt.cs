using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBattleArt : IDisposable
    {
        private const string ResourceFolder = "DaengSaka/BattleStage/";

        [Serializable]
        private sealed class Manifest
        {
            public Clip[] clips;
        }

        [Serializable]
        internal sealed class Clip
        {
            public string key;
            public int width;
            public int height;
            public int count;
            public int anchorX;
            public int anchorY;
            [NonSerialized] public Texture2D Texture;
        }

        private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        private readonly Material blur;
        private readonly Material backdropBlur;

        public GmBattleArt()
        {
            TextAsset data = Resources.Load<TextAsset>(ResourceFolder + "Atlas");
            if (data != null)
            {
                Manifest manifest = JsonUtility.FromJson<Manifest>(data.text);
                if (manifest?.clips != null)
                {
                    foreach (Clip clip in manifest.clips)
                    {
                        if (clip == null || string.IsNullOrEmpty(clip.key) || clip.width <= 0 ||
                            clip.height <= 0 || clip.count <= 0) continue;
                        clip.Texture = Resources.Load<Texture2D>(ResourceFolder + clip.key);
                        if (clip.Texture != null && clip.Texture.width == clip.width * clip.count &&
                            clip.Texture.height == clip.height)
                            clips[clip.key] = clip;
                    }
                }
            }

            Shader shader = Resources.Load<Shader>(ResourceFolder + "GmBattleBlur");
            if (shader != null && shader.isSupported)
            {
                blur = new Material(shader);
                backdropBlur = new Material(shader);
                backdropBlur.SetFloat("_Radius", 3f);
            }
        }

        public Clip Find(string key) => key != null && clips.TryGetValue(key, out Clip clip) ? clip : null;

        public static string IdleKey(GmBattleCharacter character) => character switch
        {
            GmBattleCharacter.Saka => "sakaIdle",
            GmBattleCharacter.Pacca => "paccaIdle",
            GmBattleCharacter.Daung => "daungIdle",
            _ => null
        };

        public Clip Animation(GmBattleMove move, GmBattleCharacter character)
        {
            if (move != GmBattleMove.BasicAttack) return Find(move?.AnimationKey);
            return character switch
            {
                GmBattleCharacter.Saka => Find("sakaBasic"),
                GmBattleCharacter.Pacca => Find("paccaBasic"),
                GmBattleCharacter.Daung => Find("daungBasic"),
                _ => null
            };
        }

        public Clip Impact(GmBattleMove move) => Find(move?.ImpactKey);

        public void SetBlur(RawImage image, bool soft, bool isBackdrop = false)
        {
            image.material = soft ? (isBackdrop ? backdropBlur : blur) : null;
        }

        public bool HasBlur => blur != null;

        public void Dispose()
        {
            if (blur != null) Object.Destroy(blur);
            if (backdropBlur != null) Object.Destroy(backdropBlur);
        }

        public static void Draw(RawImage image, Clip clip, int frame, float scale)
        {
            if (clip == null)
            {
                image.enabled = false;
                return;
            }

            image.enabled = true;
            image.texture = clip.Texture;
            float uvWidth = 1f / clip.count;
            frame = Mathf.Clamp(frame, 0, clip.count - 1);
            image.uvRect = new Rect(frame * uvWidth, 0f, uvWidth, 1f);
            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(clip.width * scale, clip.height * scale);
            rect.anchoredPosition = new Vector2(-clip.anchorX * scale, (clip.anchorY - clip.height) * scale);
        }
    }
}
