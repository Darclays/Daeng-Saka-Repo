using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace GamecommMentor.DaengSaka.Launching
{
    [DisallowMultipleComponent]
    public sealed class LaunchingCeremony : MonoBehaviour
    {
        private const float CelebrationSeconds = 3f;
        private const float BlackoutSeconds = 1.5f;
        private const float TrailerEndSeconds = 1f;
        private const double VideoTimeout = 12d;

        [SerializeField] private VideoClip officialTrailer;
        [SerializeField] private AudioClip ceremonyMusic;
        [SerializeField] private AudioClip victory;
        [SerializeField] private AudioClip[] confirmation = new AudioClip[3];
        [SerializeField, Range(0f, 1f)] private float musicVolume = .48f;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = .7f;
        [SerializeField, Range(0f, 1f)] private float trailerVolume = 1f;
        [SerializeField] private Material pixelMaterial;
        [SerializeField] private Font titleFont;
        [SerializeField, HideInInspector] private LaunchingView view;
        [SerializeField, HideInInspector] private AudioSource music, effects;
        [SerializeField, HideInInspector] private VideoPlayer player;

        private enum Phase { Charging, Celebration, Blackout, StartingVideo, Video, EndingVideo, Fallback, Finished }
        private readonly LaunchingControls input = new LaunchingControls();
        private readonly bool[] confirmed = new bool[3];
        private Phase phase;
        private double phaseStarted, lastFrameAt;
        private long lastFrame;
        private RenderTexture trailerSurface;
        private AudioSource trailerSound;
        private bool playRequested, running;
        private string trailerError;

        // Called by the editor menu and by the shipped scene on its first Play.
        public void EnsureSetup()
        {
            if (GetComponent<Camera>() == null)
            {
                var camera = gameObject.AddComponent<Camera>();
                camera.cullingMask = 0;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
            }
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            if (view == null || !view.Exists)
            {
                Font sans = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (titleFont == null) titleFont = Resources.Load<Font>("DaengSaka/MainMenu/LiberationSerif-Regular");
                view = LaunchingView.Build(transform, titleFont != null ? titleFont : sans, sans, pixelMaterial);
            }
            if (music == null) music = CreateAudio("Musik seremoni");
            if (effects == null) effects = CreateAudio("Efek suara");
            if (player == null) player = CreateVideoPlayer();

            if (victory == null) victory = BattleSound("Victory");
            if (confirmation == null || confirmation.Length != 3) Array.Resize(ref confirmation, 3);
            if (confirmation[0] == null) confirmation[0] = BattleSound("LegacyOfBarani_Hit");
            if (confirmation[1] == null) confirmation[1] = BattleSound("BattleAura_Hit");
            if (confirmation[2] == null) confirmation[2] = BattleSound("SanctuaryBloom_Hit");
        }

        public void UseDefaults(AudioClip clip, Material sprites)
        {
            if (ceremonyMusic == null) ceremonyMusic = clip;
            if (pixelMaterial == null) pixelMaterial = sprites;
        }

        private static AudioClip BattleSound(string key) => Resources.Load<AudioClip>("DaengSaka/SFX/Battle/" + key);

        private AudioSource CreateAudio(string objectName)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private VideoPlayer CreateVideoPlayer()
        {
            AudioSource sound = CreateAudio("Video Player");
            var video = sound.gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = false;
            video.waitForFirstFrame = true;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            return video;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            EnsureSetup();
            view.LoadArt();
            InputSystem.onDeviceChange += input.DeviceChanged;
            running = true;
            ResetCeremony();
        }

        private void OnDisable()
        {
            if (!running) return;
            running = false;
            InputSystem.onDeviceChange -= input.DeviceChanged;
            if (music != null) music.Stop();
            if (effects != null) effects.Stop();
            StopTrailer();
        }

        private void OnApplicationFocus(bool value) => input.SetFocus(value);

        private void ResetCeremony()
        {
            // A new native player on rehearsal reset keeps late decoder callbacks on the old instance.
            if (trailerSound != null) StopTrailer();
            if (player == null) player = CreateVideoPlayer();
            trailerSound = player.GetComponent<AudioSource>();
            trailerSound.mute = true;
            trailerSound.volume = trailerVolume;
            playRequested = false;
            trailerError = null;

            input.Reset(Application.isFocused);
            Array.Clear(confirmed, 0, confirmed.Length);
            phase = Phase.Charging;
            phaseStarted = lastFrameAt = 0d;
            lastFrame = -1;
            view.Reset();
            effects.Stop();
            effects.volume = effectsVolume;
            music.Stop();
            music.clip = ceremonyMusic;
            music.loop = true;
            music.volume = musicVolume;
            if (music.clip != null) music.Play();

            if (officialTrailer == null) return;
            try
            {
                float width = Mathf.Max(1f, officialTrailer.width);
                float height = Mathf.Max(1f, officialTrailer.height);
                float fit = Mathf.Min(1f, Mathf.Min(1920f / width, 1080f / height));
                trailerSurface = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(width * fit)),
                    Mathf.Max(1, Mathf.RoundToInt(height * fit)), 0, RenderTextureFormat.ARGB32);
                trailerSurface.name = "Daeng Saka — Trailer";
                if (!trailerSurface.Create())
                {
                    trailerError = "RenderTexture trailer gagal dibuat.";
                    return;
                }
                player.source = VideoSource.VideoClip;
                player.clip = officialTrailer;
                player.targetTexture = trailerSurface;
                player.aspectRatio = VideoAspectRatio.Stretch;
                player.controlledAudioTrackCount = (ushort)(officialTrailer.audioTrackCount > 0 ? 1 : 0);
                if (player.controlledAudioTrackCount != 0)
                {
                    player.EnableAudioTrack(0, true);
                    player.SetTargetAudioSource(0, trailerSound);
                }
                player.errorReceived += OnVideoError;
                player.loopPointReached += OnVideoEnded;
                player.frameReady += OnFirstFrame;
                player.Prepare();
            }
            catch (Exception problem)
            {
                trailerError = problem.Message;
            }
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            input.Sample(now, Application.isFocused, phase == Phase.Charging);
            if (input.ResetButton.Ready)
            {
                ResetCeremony();
                return;
            }

            if (phase == Phase.Charging) UpdateCharging(now);
            float age = (float)(now - phaseStarted);
            if (phase <= Phase.Blackout)
                view.Draw(input.Heroes, now, Time.unscaledDeltaTime, phase == Phase.Celebration, age);
            switch (phase)
            {
                case Phase.Celebration:
                    if (age >= CelebrationSeconds)
                    {
                        phase = Phase.Blackout;
                        phaseStarted += CelebrationSeconds;
                        FadeCeremony(now);
                    }
                    break;
                case Phase.Blackout:
                    FadeCeremony(now);
                    break;
                case Phase.StartingVideo:
                    StartVideoWhenReady(now);
                    break;
                case Phase.Video:
                    if (player.frame != lastFrame)
                    {
                        lastFrameAt = now;
                        lastFrame = player.frame;
                    }
                    if (now - lastFrameAt >= VideoTimeout) FailVideo("Frame video berhenti.");
                    break;
                case Phase.EndingVideo:
                    float fade = Mathf.SmoothStep(0f, 1f, age / TrailerEndSeconds);
                    view.SetBlack(fade);
                    trailerSound.volume = trailerVolume * (1f - fade);
                    if (age >= TrailerEndSeconds)
                    {
                        phase = Phase.Finished;
                        StopTrailer();
#if !UNITY_EDITOR
                        Application.Quit();
#endif
                    }
                    break;
            }
        }

        private void UpdateCharging(double now)
        {
            bool emergency = input.OverrideButton.Ready;
            int ready = 0;
            for (int i = 0; i < input.Heroes.Length; i++)
            {
                LaunchingControls.Hold hero = input.Heroes[i];
                if (emergency) hero.Complete(now);
                if (!hero.Ready) continue;
                ready++;
                if (confirmed[i]) continue;
                confirmed[i] = true;
                if (!emergency && confirmation[i] != null) effects.PlayOneShot(confirmation[i], .55f);
            }
            bool allReady = ready == input.Heroes.Length;
            view.Footer(ready, allReady);
            if (!allReady) return;
            phase = Phase.Celebration;
            phaseStarted = now;
            if (victory != null) effects.PlayOneShot(victory, .8f);
        }

        private void FadeCeremony(double now)
        {
            float age = (float)(now - phaseStarted);
            float fade = Mathf.SmoothStep(0f, 1f, age / BlackoutSeconds);
            view.SetBlack(fade);
            music.volume = musicVolume * (1f - fade);
            effects.volume = effectsVolume * (1f - fade);
            if (age < BlackoutSeconds) return;
            music.Stop();
            effects.Stop();
            phase = Phase.StartingVideo;
            phaseStarted = now;
            StartVideoWhenReady(now);
        }

        private void StartVideoWhenReady(double now)
        {
            if (officialTrailer == null) FailVideo("Official Trailer masih kosong.");
            else if (trailerError != null) FailVideo(trailerError);
            else if (now - phaseStarted >= VideoTimeout) FailVideo("Trailer melewati batas waktu persiapan atau pemutaran.");
            else if (!playRequested && player.isPrepared)
            {
                playRequested = true;
                player.sendFrameReadyEvents = true;
                trailerSound.mute = false;
                try { player.Play(); }
                catch (Exception problem) { FailVideo(problem.Message); }
            }
        }

        private void OnFirstFrame(VideoPlayer sender, long frame)
        {
            if (sender != player || phase != Phase.StartingVideo || !playRequested) return;
            sender.sendFrameReadyEvents = false;
            phase = Phase.Video;
            lastFrameAt = Time.unscaledTimeAsDouble;
            lastFrame = frame;
            float aspect = officialTrailer.height > 0 ? (float)officialTrailer.width / officialTrailer.height : 16f / 9f;
            view.ShowVideo(trailerSurface, aspect);
        }

        private void OnVideoEnded(VideoPlayer sender)
        {
            if (sender != player || (phase != Phase.Video && (phase != Phase.StartingVideo || !playRequested))) return;
            phase = Phase.EndingVideo;
            phaseStarted = Time.unscaledTimeAsDouble;
        }

        private void OnVideoError(VideoPlayer sender, string message)
        {
            if (sender != player) return;
            trailerError = message;
            if (phase == Phase.StartingVideo || phase == Phase.Video) FailVideo(message);
        }

        private void FailVideo(string reason)
        {
            phase = Phase.Fallback;
            view.TrailerFailed(officialTrailer == null);
            StopTrailer();
            Debug.LogError("[Daeng Saka Launching] " + reason, this);
        }

        private void StopTrailer()
        {
            VideoPlayer retiring = player;
            player = null;
            if (retiring != null)
            {
                retiring.errorReceived -= OnVideoError;
                retiring.loopPointReached -= OnVideoEnded;
                retiring.frameReady -= OnFirstFrame;
                retiring.sendFrameReadyEvents = false;
                if (trailerSound != null) trailerSound.mute = true;
                retiring.Stop();
                retiring.targetTexture = null;
                retiring.gameObject.SetActive(false);
                Destroy(retiring.gameObject);
            }
            trailerSound = null;
            if (trailerSurface == null) return;
            trailerSurface.Release();
            Destroy(trailerSurface);
            trailerSurface = null;
        }
    }
}
