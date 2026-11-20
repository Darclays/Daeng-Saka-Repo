using System;
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

            internal void Sample(Camera camera, TerrainCollider terrain, float elapsed)
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

            public Shot(Transform from, Transform to, float seconds, float fieldOfView, ActorCue[] actors,
                string cueText = "", float cueAt = 0f)
            {
                this.from = from;
                this.to = to;
                this.seconds = seconds;
                this.fieldOfView = fieldOfView;
                this.actors = actors;
                this.cueText = cueText;
                this.cueAt = cueAt;
            }

            internal float Seconds => seconds;
            internal ActorCue[] Actors => actors;
            internal bool IsReady
            {
                get
                {
                    if (from == null || to == null || seconds < .5f || actors == null || actors.Length == 0)
                        return false;
                    foreach (ActorCue cue in actors)
                        if (cue == null || !cue.IsReady) return false;
                    return true;
                }
            }

            internal string Caption(float time) => time >= cueAt ? cueText : "";

            internal void Sample(Camera camera, TerrainCollider terrain, float elapsed)
            {
                float blend = Ease(Mathf.Clamp01(elapsed / seconds));
                camera.fieldOfView = fieldOfView;
                camera.transform.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, blend),
                    Quaternion.Slerp(from.rotation, to.rotation, blend));
                foreach (ActorCue cue in actors) cue.Sample(camera, terrain, elapsed);
            }
        }

        [SerializeField] private Camera view;
        [SerializeField] private GmOrbitFollowCamera orbit;
        [SerializeField] private TerrainCollider terrain;
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
        private bool started;
        private bool playing;
        private bool[] rendererStates;
        private bool hudWasEnabled;
        private Pose explorationPose;
        private float explorationFov;
        private float shotTime;
        private int shotIndex;

        public void Configure(Camera camera, GmOrbitFollowCamera followCamera, TerrainCollider floor,
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
                if (view == null || orbit == null || terrain == null || questHud == null ||
                    overlay == null || fade == null || caption == null || cast.Length == 0 || shots.Length != 4)
                    return false;
                foreach (Shot shot in shots)
                    if (shot == null || !shot.IsReady) return false;
                return true;
            }
        }

        public bool TerraceShotsInstalled => terraceShotsInstalled;

        public void SetTerraceShots(Shot wide, Shot close)
        {
            shots[0] = wide;
            shots[1] = close;
            terraceShotsInstalled = true;
        }

        internal bool TryPlay(GmSakaMotor player)
        {
            if (started || !isActiveAndEnabled || player == null || !IsConfigured || orbit.Target != player.transform)
                return false;
            var controls = player.GetComponent<GmControlGate>();
            if (controls == null || !controls.CanExplore || !orbit.TryHoldDialogueView()) return false;

            started = true;
            playing = true;
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

            overlay.SetActive(true);
            EnterShot(0);
            SetFade(1f);
            return true;
        }

        private void Update()
        {
            if (!playing) return;
            if (view == null || orbit == null || gate == null || !gate.isActiveAndEnabled || SkipPressed())
            {
                Finish();
                return;
            }

            shotTime += Time.unscaledDeltaTime;
            while (shotTime >= shots[shotIndex].Seconds)
            {
                if (shotIndex == shots.Length - 1)
                {
                    Finish();
                    return;
                }
                float remainder = shotTime - shots[shotIndex].Seconds;
                EnterShot(shotIndex + 1);
                shotTime = remainder;
            }

            Shot shot = shots[shotIndex];
            shot.Sample(view, terrain, shotTime);
            caption.text = shot.Caption(shotTime);
            if (shotIndex == 0) SetFade(1f - shotTime / FadeSeconds);
            else if (shotIndex == shots.Length - 1) SetFade(1f - (shot.Seconds - shotTime) / FadeSeconds);
        }

        private void EnterShot(int index)
        {
            HideCast();
            shotIndex = index;
            shotTime = 0f;
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

        private void OnDisable() => Finish();

        private void Finish()
        {
            if (!playing) return;
            playing = false;
            HideCast();
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

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private static bool SkipPressed()
        {
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
