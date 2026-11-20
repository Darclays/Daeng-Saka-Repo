using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal static class GmGameplayAudio
    {
        private const string Folder = "DaengSaka/Feedback/";
        internal const string Purchase = Folder + "MerchantPurchase";
        internal const string FishingBite = Folder + "FishingBite";
        internal const string FishingCatch = Folder + "FishingCatch";
        internal const string ItemUse = Folder + "BattleItemUse";

        internal static void Play(GameObject owner, ref AudioSource source, string path)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null) return;

            if (source == null)
            {
                source = owner.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = .8f;
            }
            source.PlayOneShot(clip);
        }
    }
}
