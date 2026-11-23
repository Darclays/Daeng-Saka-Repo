using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal enum GmMusicCue { None, Kingdom, Village, Danger, Cave }

    [CreateAssetMenu(menuName = "GamecommMentor/Daeng Saka/Soundtrack")]
    public sealed class GmDemoSoundtrackProfile : ScriptableObject
    {
        [SerializeField] private AudioClip kingdom;
        [SerializeField] private AudioClip village;
        [SerializeField] private AudioClip danger;
        [SerializeField] private AudioClip cave;
        [SerializeField, Range(0f, 1f)] private float kingdomLevel = .49f;
        [SerializeField, Range(0f, 1f)] private float villageLevel = .52f;
        [SerializeField, Range(0f, 1f)] private float dangerLevel = .45f;
        [SerializeField, Range(0f, 1f)] private float caveLevel = .38f;

        internal AudioClip Clip(GmMusicCue cue) => cue switch
        {
            GmMusicCue.Kingdom => kingdom,
            GmMusicCue.Village => village,
            GmMusicCue.Danger => danger,
            GmMusicCue.Cave => cave,
            _ => null
        };

        internal float Level(GmMusicCue cue) => cue switch
        {
            GmMusicCue.Kingdom => kingdomLevel,
            GmMusicCue.Village => villageLevel,
            GmMusicCue.Danger => dangerLevel,
            GmMusicCue.Cave => caveLevel,
            _ => 0f
        };
    }
}
