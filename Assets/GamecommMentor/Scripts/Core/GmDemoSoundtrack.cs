using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmDemoSoundtrack : MonoBehaviour
    {
        private const string ProfilePath = "DaengSaka/Music/Soundtrack";
        private const float FadeSeconds = .95f;
        private static GmDemoSoundtrack instance;
        private readonly Dictionary<GmMusicCue, Voice> voices = new Dictionary<GmMusicCue, Voice>();
        private GmDemoSoundtrackProfile profile;
        private float musicGain;
        private float impactGain = 1f;
        private float impactTime;

        private sealed class Voice
        {
            internal AudioSource Source;
            internal float Level;
            internal float Mix;
            internal bool Playing;
            internal bool Paused;
        }

        internal static bool HasTitleMusic => instance != null && instance.profile.Clip(GmMusicCue.Kingdom) != null;

        internal static void EmphasizeImpact()
        {
            if (instance != null && GmGameOptions.EffectsGain > 0f) instance.impactTime = .30f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartSoundtrack()
        {
            if (instance != null) return;
            var soundtrack = Resources.Load<GmDemoSoundtrackProfile>(ProfilePath);
            if (soundtrack == null) return;
            var owner = new GameObject("Daeng Saka - Soundtrack");
            DontDestroyOnLoad(owner);
            instance = owner.AddComponent<GmDemoSoundtrack>();
            instance.profile = soundtrack;
            instance.musicGain = GmGameOptions.MusicGain;
        }

        internal static GmMusicCue CueFor(string sceneName, GmMainQuestStep step) => sceneName switch
        {
            "MainMenu" or "IbuKota" => GmMusicCue.Kingdom,
            "Battle_Encounter_Test" => GmMusicCue.Danger,
            "DesaSorelia" => step is GmMainQuestStep.TemuiKeluargaNelayan or GmMainQuestStep.SelamatkanKeluargaNelayan
                ? GmMusicCue.Danger : GmMusicCue.Village,
            "GoaLembanyung" or "GoaLembayung" => step is GmMainQuestStep.TemuiPakande or GmMainQuestStep.LawanPakande
                ? GmMusicCue.Danger : GmMusicCue.Cave,
            _ => GmMusicCue.None
        };

        private void Update()
        {
            if (profile == null) return;
            GmMusicCue cue = CueFor(SceneManager.GetActiveScene().name, GmMainQuestProgress.WorldStep);
            if (cue != GmMusicCue.None && !voices.ContainsKey(cue)) CreateVoice(cue);

            // Keep the fade gradual after a long scene-loading frame.
            float dt = Mathf.Min(Time.unscaledDeltaTime, .10f);
            musicGain = Mathf.MoveTowards(musicGain, GmGameOptions.MusicGain, dt * 6f);
            impactGain = Mathf.MoveTowards(impactGain, impactTime > 0f ? .74f : 1f, dt * (impactTime > 0f ? 4f : .7f));
            impactTime = Mathf.Max(0f, impactTime - dt);
            foreach (var entry in voices)
                Fade(entry.Key, entry.Value, cue == entry.Key, dt);
        }

        private void CreateVoice(GmMusicCue cue)
        {
            AudioClip clip = profile.Clip(cue);
            if (clip == null) return;
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.clip = clip;
            voices.Add(cue, new Voice { Source = source, Level = profile.Level(cue) });
        }

        private void Fade(GmMusicCue cue, Voice voice, bool selected, float dt)
        {
            if (selected && !voice.Playing)
            {
                if (voice.Paused) voice.Source.UnPause();
                else voice.Source.Play();
                voice.Playing = true;
                voice.Paused = false;
            }
            if (!voice.Playing) return;

            voice.Mix = Mathf.MoveTowards(voice.Mix, selected ? 1f : 0f, dt / FadeSeconds);
            voice.Source.volume = voice.Level * musicGain * impactGain * Mathf.Sin(voice.Mix * Mathf.PI * .5f);
            if (selected || voice.Mix > 0f) return;

            if (cue == GmMusicCue.Danger) voice.Source.Stop();
            else
            {
                voice.Source.Pause();
                voice.Paused = true;
            }
            voice.Playing = false;
        }
    }
}
