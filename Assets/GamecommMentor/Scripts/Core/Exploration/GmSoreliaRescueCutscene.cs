using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmSoreliaRescueCutscene : MonoBehaviour
    {
        private const float FadeSeconds = .23f;
        private const float MaxFrameDelta = .10f;

        [Serializable]
        public sealed class ActorCue
        {
            [SerializeField] private GmCutsceneSprite actor;
            [SerializeField] private Transform from, via, viaAfter, to, face;
            [SerializeField, Min(0f)] private float start;
            [SerializeField, Min(0f)] private float walkSeconds;
            [SerializeField] private bool followGround;

            public ActorCue(GmCutsceneSprite sprite, Transform from, Transform to, Transform face,
                float start, float walkSeconds, bool followGround, Transform via = null, Transform viaAfter = null)
            {
                actor = sprite;
                this.from = from;
                this.via = via;
                this.viaAfter = viaAfter;
                this.to = to;
                this.face = face;
                this.start = start;
                this.walkSeconds = walkSeconds;
                this.followGround = followGround;
            }

            internal bool IsReady => actor != null && actor.HasArt && from != null && to != null;

            internal void Enter()
            {
                actor.ResetFacing();
                actor.gameObject.SetActive(true);
            }

            internal void Sample(Camera camera, Collider terrain, float elapsed)
            {
                float t = walkSeconds > 0f ? Mathf.Clamp01((elapsed - start) / walkSeconds) : 1f;
                Vector3 first = from.position;
                Vector3 last = to.position;
                bool moving = elapsed >= start && t < 1f;
                Vector3 heading = last - first;
                float progress = Ease(t);
                Transform firstBend = via != null ? via : viaAfter;
                Vector3 point = firstBend != null
                    ? AlongPath(first, firstBend.position, via != null && viaAfter != null ? viaAfter.position : last,
                        last, progress, out heading)
                    : Vector3.Lerp(first, last, progress);
                if (followGround && terrain != null &&
                    terrain.Raycast(new Ray(point + Vector3.up * 3f, Vector3.down), out RaycastHit hit, 7f))
                    point.y = hit.point.y + .025f;

                if (!moving && face != null) heading = face.position - point;
                actor.Sample(camera, point, heading, moving, Mathf.Max(0f, elapsed - start) + start * .65f);
            }

            private static Vector3 AlongPath(Vector3 start, Vector3 bend, Vector3 exit, Vector3 end,
                float fraction, out Vector3 heading)
            {
                float toBend = Vector3.Distance(start, bend);
                float toExit = Vector3.Distance(bend, exit);
                float toEnd = Vector3.Distance(exit, end);
                float distance = fraction * (toBend + toExit + toEnd);
                if (distance < toBend)
                {
                    heading = bend - start;
                    return Vector3.Lerp(start, bend, distance / toBend);
                }

                distance -= toBend;
                if (toEnd <= .001f || distance < toExit)
                {
                    heading = exit - bend;
                    return Vector3.Lerp(bend, exit, distance / Mathf.Max(.001f, toExit));
                }

                heading = end - exit;
                return Vector3.Lerp(exit, end, (distance - toExit) / toEnd);
            }
        }

        [Serializable]
        public sealed class Shot
        {
            [SerializeField] private Transform from, to;
            [SerializeField, Min(.5f)] private float seconds;
            [SerializeField, Range(25f, 75f)] private float fieldOfView;
            [SerializeField] private ActorCue[] actors;
            [SerializeField] private string cueText;
            [SerializeField, Min(0f)] private float cueAt;
            [SerializeField] private AudioSource cueAudio;
            [SerializeField, Min(0f)] private float shake;

            public Shot(Transform from, Transform to, float seconds, float fieldOfView, ActorCue[] actors,
                string cueText = "", float cueAt = 0f, AudioSource cueAudio = null, float shake = 0f)
            {
                this.from = from;
                this.to = to;
                this.seconds = seconds;
                this.fieldOfView = fieldOfView;
                this.actors = actors;
                this.cueText = cueText;
                this.cueAt = cueAt;
                this.cueAudio = cueAudio;
                this.shake = shake;
            }

            internal float Seconds => seconds;
            internal ActorCue[] Actors => actors;
            internal bool IsReady
            {
                get
                {
                    if (from == null || to == null || seconds < .5f || actors == null)
                        return false;
                    foreach (ActorCue cue in actors)
                        if (cue == null || !cue.IsReady) return false;
                    return true;
                }
            }

            internal string Caption(float time) => time >= cueAt ? cueText : "";
            internal AudioSource AudioAt(float time) => time >= cueAt ? cueAudio : null;

            internal void Sample(Camera camera, Collider terrain, float elapsed)
            {
                float blend = Ease(Mathf.Clamp01(elapsed / seconds));
                camera.fieldOfView = fieldOfView;
                camera.transform.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, blend),
                    Quaternion.Slerp(from.rotation, to.rotation, blend));
                float sinceCue = elapsed - cueAt;
                if (shake > 0f && sinceCue >= 0f && sinceCue < .32f)
                {
                    float envelope = 1f - sinceCue / .32f;
                    camera.transform.position += camera.transform.right *
                        (Mathf.Sin(sinceCue * 62f) * shake * envelope);
                }
                foreach (ActorCue cue in actors) cue.Sample(camera, terrain, elapsed);
            }
        }

        [SerializeField] private Camera view;
        [SerializeField] private GmOrbitFollowCamera orbit;
        [SerializeField] private Collider terrain;
        [SerializeField] private Canvas questHud;
        [SerializeField] private GameObject overlay;
        [SerializeField] private Image fade;
        [SerializeField] private Text caption;
        [SerializeField] private GmCutsceneSprite[] cast = Array.Empty<GmCutsceneSprite>();
        [SerializeField] private SpriteRenderer[] explorationSprites = Array.Empty<SpriteRenderer>();
        [SerializeField] private Shot[] shots = Array.Empty<Shot>();
        [SerializeField, HideInInspector] private bool terraceShotsInstalled;

        private GmControlGate gate;
        private GmInteractor interactor;
        private Text skipLabel;
        private bool started;
        private bool playing;
        private bool holdFinalShot;
        private bool limitFrameDelta;
        private int playStartedFrame;

        internal bool IsPlaying => playing;
        internal bool IsHoldingFinalShot { get; private set; }
        internal bool Completed { get; private set; }
        internal bool WasSkipped { get; private set; }
        private bool[] rendererStates;
        private bool hudWasEnabled;
        private Pose explorationPose;
        private float explorationFov;
        private float shotTime;
        private int shotIndex;
        private (string Text, float Seconds)[] openingCaptions;
        private float openingCaptionDuration;
        private AudioSource playingCue;
        private float playingCueVolume;
        private bool cuePlayedInShot;

        public void Configure(Camera camera, GmOrbitFollowCamera followCamera, Collider floor,
            Canvas hud, GameObject cutsceneOverlay, Image fadeImage, Text subtitle,
            GmCutsceneSprite[] actors, SpriteRenderer[] normalSprites, Shot[] sequence)
        {
            view = camera;
            orbit = followCamera;
            terrain = floor;
            questHud = hud;
            overlay = cutsceneOverlay;
            fade = fadeImage;
            caption = subtitle;
            cast = actors;
            explorationSprites = normalSprites;
            shots = sequence;
        }

        public bool IsConfigured
        {
            get
            {
                if (view == null || orbit == null || questHud == null || overlay == null || fade == null ||
                    caption == null || cast == null || shots == null || shots.Length == 0 ||
                    (cast.Length > 0 && terrain == null))
                    return false;
                foreach (Shot shot in shots)
                    if (shot == null || !shot.IsReady || (cast.Length == 0 && shot.Actors.Length > 0))
                        return false;
                return true;
            }
        }

        public bool TerraceShotsInstalled => terraceShotsInstalled;
        public IReadOnlyList<Shot> Shots => shots;

        public void HideDuringCutscene(SpriteRenderer sprite)
        {
            if (sprite == null || Array.IndexOf(explorationSprites, sprite) >= 0) return;
            int index = explorationSprites.Length;
            Array.Resize(ref explorationSprites, index + 1);
            explorationSprites[index] = sprite;
        }

        public void SetTerraceShots(Shot wide, Shot close)
        {
            shots[0] = wide;
            shots[1] = close;
            terraceShotsInstalled = true;
        }

        internal bool TryPlay(GmSakaMotor player, bool holdFinalShot = false, bool limitFrameDelta = false,
            (string Text, float Seconds)[] openingCaptions = null)
        {
            if (started || !isActiveAndEnabled || player == null || !IsConfigured || orbit.Target != player.transform)
                return false;
            var controls = player.GetComponent<GmControlGate>();
            if (controls == null || !controls.CanExplore || !orbit.TryHoldDialogueView()) return false;

            started = true;
            playing = true;
            this.holdFinalShot = holdFinalShot;
            this.limitFrameDelta = limitFrameDelta;
            this.openingCaptions = openingCaptions;
            openingCaptionDuration = 0f;
            if (openingCaptions != null)
            {
                foreach (var line in openingCaptions) openingCaptionDuration += line.Seconds;
                caption.resizeTextForBestFit = true;
                caption.resizeTextMinSize = Math.Min(18, caption.fontSize);
                caption.resizeTextMaxSize = caption.fontSize;
            }
            playStartedFrame = Time.frameCount;
            gate = controls;
            gate.Block(this);
            interactor = player.GetComponent<GmInteractor>();
            explorationPose = new Pose(view.transform.position, view.transform.rotation);
            explorationFov = view.fieldOfView;
            hudWasEnabled = questHud.enabled;
            questHud.enabled = false;
            rendererStates = new bool[explorationSprites.Length];
            for (int i = 0; i < explorationSprites.Length; i++)
            {
                SpriteRenderer sprite = explorationSprites[i];
                if (sprite == null) continue;
                rendererStates[i] = sprite.enabled;
                sprite.enabled = false;
            }

            skipLabel = overlay.transform.Find("Lewati")?.GetComponent<Text>();
            overlay.SetActive(true);
            UpdateSkipHint();
            EnterShot(0);
            SetFade(1f);
            return true;
        }

        private void Update()
        {
            if (!playing) return;
            UpdateSkipHint();
            if (view == null || orbit == null || gate == null || !gate.isActiveAndEnabled)
            {
                Finish();
                return;
            }
            if (SkipPressed())
            {
                WasSkipped = true;
                Finish(completed: true);
                return;
            }
            // A scene can open on a frame whose elapsed time includes loading the scene.
            if (limitFrameDelta && Time.frameCount == playStartedFrame) return;
            float elapsed = limitFrameDelta ? Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta) : Time.unscaledDeltaTime;
            if (IsHoldingFinalShot)
            {
                shotTime += elapsed;
                shots[shotIndex].Sample(view, terrain, shotTime);
                return;
            }

            shotTime += elapsed;
            while (shotTime >= ShotDuration)
            {
                if (shotIndex == shots.Length - 1)
                {
                    if (holdFinalShot)
                    {
                        Shot final = shots[shotIndex];
                        shotTime = final.Seconds;
                        final.Sample(view, terrain, shotTime);
                        overlay.SetActive(false);
                        IsHoldingFinalShot = true;
                    }
                    else Finish(completed: true);
                    return;
                }
                float remainder = shotTime - ShotDuration;
                EnterShot(shotIndex + 1);
                shotTime = remainder;
            }

            Shot shot = shots[shotIndex];
            shot.Sample(view, terrain, shotTime);
            caption.text = CaptionAt(shot);
            AudioSource cue = shot.AudioAt(shotTime);
            if (!cuePlayedInShot && cue != null)
            {
                StopCue();
                playingCue = cue;
                playingCueVolume = cue.volume;
                cue.volume = playingCueVolume * GmGameOptions.EffectsGain;
                cue.Play();
                cuePlayedInShot = true;
            }
            if (shotIndex == 0) SetFade(1f - shotTime / FadeSeconds);
            else if (shotIndex == shots.Length - 1 && !holdFinalShot)
                SetFade(1f - (ShotDuration - shotTime) / FadeSeconds);
        }

        private float ShotDuration => shots[shotIndex].Seconds + (shotIndex == 0 ? openingCaptionDuration : 0f);

        private string CaptionAt(Shot shot)
        {
            if (shotIndex != 0 || openingCaptions == null || shotTime < shot.Seconds) return shot.Caption(shotTime);

            float elapsed = shotTime - shot.Seconds;
            foreach (var line in openingCaptions)
            {
                if (elapsed < line.Seconds) return line.Text;
                elapsed -= line.Seconds;
            }
            return "";
        }

        private void EnterShot(int index)
        {
            HideCast();
            shotIndex = index;
            shotTime = 0f;
            cuePlayedInShot = false;
            Shot shot = shots[index];
            foreach (ActorCue actor in shot.Actors) actor.Enter();
            shot.Sample(view, terrain, 0f);
            caption.text = shot.Caption(0f);
            SetFade(0f);
        }

        private void HideCast()
        {
            foreach (GmCutsceneSprite actor in cast)
                if (actor != null) actor.gameObject.SetActive(false);
        }

        private void SetFade(float value)
        {
            if (fade != null) fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(value));
        }

        internal bool HoldsViewFor(GmInteractor requester) =>
            IsHoldingFinalShot && requester != null && requester == interactor;

        internal void FinishHeldShot()
        {
            if (IsHoldingFinalShot) Finish(completed: true);
        }

        internal void CancelPlayback() => Finish();

        private void OnDisable() => Finish();

        private void Finish(bool completed = false)
        {
            if (!playing) return;
            playing = false;
            if (!completed && holdFinalShot) started = false;
            IsHoldingFinalShot = false;
            Completed = completed;
            HideCast();
            StopCue();
            if (overlay != null) overlay.SetActive(false);
            for (int i = 0; i < explorationSprites.Length; i++)
                if (explorationSprites[i] != null) explorationSprites[i].enabled = rendererStates[i];
            if (questHud != null) questHud.enabled = hudWasEnabled;
            if (view != null)
            {
                view.fieldOfView = explorationFov;
                view.transform.SetPositionAndRotation(explorationPose.position, explorationPose.rotation);
            }
            if (orbit != null) orbit.ReleaseDialogueView();
            if (interactor != null) interactor.SuppressInteraction();
            if (gate != null) gate.Unblock(this);
            gate = null;
            interactor = null;
        }

        private void StopCue()
        {
            if (playingCue == null) return;
            playingCue.Stop();
            playingCue.volume = playingCueVolume;
            playingCue = null;
        }

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private void UpdateSkipHint()
        {
            if (skipLabel == null) return;
            string text = GmGamepadInput.UsingGamepad ? "[B] Lewati" : "[Esc] Lewati";
            if (skipLabel.text != text) skipLabel.text = text;
        }

        private static bool SkipPressed()
        {
            if (GmGamepadInput.TakeCancel()) return true;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
