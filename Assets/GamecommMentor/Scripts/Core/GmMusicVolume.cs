using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GmMusicVolume : MonoBehaviour
    {
        private AudioSource source;
        private float baseVolume;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            baseVolume = source.volume;
        }

        private void OnEnable()
        {
            GmGameOptions.MusicChanged += ApplyVolume;
            ApplyVolume();
        }

        private void OnDisable()
        {
            GmGameOptions.MusicChanged -= ApplyVolume;
            if (source != null) source.volume = baseVolume;
        }

        private void ApplyVolume() => source.volume = baseVolume * GmGameOptions.MusicGain;
    }
}
