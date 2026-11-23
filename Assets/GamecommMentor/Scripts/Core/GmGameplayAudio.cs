using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal enum GmUiSound { Move, Confirm, Back, Open, Tab, Adjust, Dialogue, Cook }

    internal static class GmGameplayAudio
    {
        private const string Folder = "DaengSaka/Feedback/";
        internal const string Purchase = Folder + "MerchantPurchase";
        internal const string FishingBite = Folder + "FishingBite";
        internal const string FishingCatch = Folder + "FishingCatch";
        internal const string ItemUse = Folder + "BattleItemUse";
        internal const string EffectsFolder = "DaengSaka/SFX/";
        private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private static AudioSource shared;
        private static int lastUiActionFrame = -1;
        private static double nextNavigationSound;
        private static double nextAdjustmentSound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            clips.Clear();
            shared = null;
            lastUiActionFrame = -1;
            nextNavigationSound = nextAdjustmentSound = 0d;
        }

        internal static AudioClip Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (clips.TryGetValue(path, out AudioClip clip) && clip != null) return clip;
            clip = Resources.Load<AudioClip>(path);
            if (clip != null) clips[path] = clip;
            return clip;
        }

        internal static void Ui(GmUiSound cue)
        {
            bool move = cue == GmUiSound.Move;
            bool adjust = cue == GmUiSound.Adjust;
            double now = Time.unscaledTimeAsDouble;
            if (move ? now < nextNavigationSound : adjust ? now < nextAdjustmentSound : lastUiActionFrame == Time.frameCount)
                return;
            if (!PlayShared(EffectsFolder + "UI/" + cue, move ? .62f : 1f)) return;
            if (move) nextNavigationSound = now + .085;
            else if (adjust)
            {
                nextAdjustmentSound = now + .10;
                lastUiActionFrame = Time.frameCount;
            }
            else lastUiActionFrame = Time.frameCount;
        }

        internal static void World(string cue) => PlayShared(EffectsFolder + "World/" + cue, .88f);

        private static bool PlayShared(string path, float level)
        {
            float gain = GmGameOptions.EffectsGain;
            if (gain <= 0f) return false;
            AudioClip clip = Load(path);
            if (clip == null) return false;
            if (shared == null)
            {
                var owner = new GameObject("Daeng Saka - Feedback Audio");
                Object.DontDestroyOnLoad(owner);
                shared = CreateSource(owner);
            }
            shared.PlayOneShot(clip, gain * level);
            return true;
        }

        internal static void Play(GameObject owner, ref AudioSource source, string path)
        {
            float gain = GmGameOptions.EffectsGain;
            if (gain <= 0f) return;
            AudioClip clip = Load(path);
            if (clip == null) return;
            if (source == null) source = CreateSource(owner);
            source.PlayOneShot(clip, gain);
        }

        private static AudioSource CreateSource(GameObject owner)
        {
            AudioSource source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = .8f;
            return source;
        }
    }
}
