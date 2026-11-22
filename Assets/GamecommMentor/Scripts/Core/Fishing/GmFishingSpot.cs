using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    // Project the rod attachment after the exploration billboard (execution order 200).
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class GmFishingSpot : GmInteractable
    {
        public const string ArtPath = "Assets/GamecommMentor/Resources/DaengSaka/Fishing/SakaFishing.png";
        private const int CatchStep = 1;
        private const float CoverSeconds = .14f, RevealSeconds = .16f;
        private enum Stage { Approach, RevealFishing, Fishing, CoverExit, RevealWorld }

        [SerializeField] private GmQuestJournal journal;
        [SerializeField] private Transform castPoint;
        [SerializeField] private Texture2D sakaAtlas;
        [SerializeField] private Camera worldCamera;
        private readonly System.Random random = new System.Random();
        private GmInteractor player, poseOwner;
        private GmSakaMotor motor;
        private GmFishingRound round;
        private GmFishingView view;
        private AudioSource feedbackAudio;
        private GmFishingPose pose;
        private GmFishingShot shot;
        private Stage stage;
        private float fade, approachTime;
        private bool added, questCompleted;
        private bool focused = true, paused;
        private int openedFrame;

        public void Configure(GmQuestJournal questJournal, Transform waterPoint, Texture2D atlas, Camera camera)
        {
            journal = questJournal;
            castPoint = waterPoint;
            sakaAtlas = atlas;
            worldCamera = camera;
            SetPresentation("Sungai Sorelia", "Mancing");
        }

        private int Completed => journal.CompletedSideSteps(GmQuestCatalog.Fishing.Id);

        public override bool CanInteract(GmInteractor requester) =>
            base.CanInteract(requester) && round == null && castPoint != null && journal != null &&
            journal.isActiveAndEnabled && requester.gameObject.scene == gameObject.scene &&
            SceneManager.GetActiveScene() == gameObject.scene && requester.ControlGate != null &&
            requester.ControlGate.CanExplore && Completed >= CatchStep;

        public override bool TryInteract(GmInteractor requester)
        {
            if (!CanInteract(requester)) return false;
            Camera camera = worldCamera != null && worldCamera.isActiveAndEnabled ? worldCamera : Camera.main;
            if (camera == null || camera.gameObject.scene != gameObject.scene) return false;
            player = requester;
            round = new GmFishingRound(random);
            added = questCompleted = false;
            openedFrame = Time.frameCount;
            stage = Stage.Approach;
            fade = approachTime = 0f;
            player.ControlGate.Block(this);
            try
            {
                view ??= new GmFishingView((RectTransform)journal.transform);
                if (poseOwner != player)
                {
                    pose?.Dispose();
                    pose = null;
                    poseOwner = player;
                    Transform visual = player.transform.Find("Visual");
                    SpriteRenderer source = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                    if (source == null) source = player.GetComponentInChildren<SpriteRenderer>();
                    if (source != null && sakaAtlas != null) pose = new GmFishingPose(source, sakaAtlas);
                }
                shot = new GmFishingShot(camera, player.transform, transform, castPoint);
                motor = player.GetComponent<GmSakaMotor>();
                if (motor != null) motor.BeginInteractionStep(this, shot.Stand);
                return true;
            }
            catch (Exception error)
            {
                Close();
                Debug.LogException(error, this);
                return false;
            }
        }

        private void Update()
        {
            if (round == null) return;
            if (player == null || !player.isActiveAndEnabled || journal == null || !journal.isActiveAndEnabled ||
                castPoint == null || shot.Camera == null || !shot.Camera.isActiveAndEnabled ||
                SceneManager.GetActiveScene() != gameObject.scene)
            {
                Close();
                return;
            }
            if (!focused || paused || Time.frameCount == openedFrame) return;

            bool close = GmGamepadInput.PressedBack();
            bool action = !close && GmGamepadInput.TakeSubmit();
            bool held = GmGamepadInput.SubmitHeld;
#if ENABLE_INPUT_SYSTEM
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                action |= keys.spaceKey.wasPressedThisFrame;
                held |= keys.spaceKey.isPressed;
            }
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                bool pressed = mouse.leftButton.wasPressedThisFrame;
                close |= pressed && view.HitExit(mouse.position.ReadValue());
                action |= pressed;
                held |= mouse.leftButton.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            action = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            held = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
            close |= Input.GetMouseButtonDown(0) && view.HitExit(Input.mousePosition);
#endif
            if (close) RequestExit();
            if (stage == Stage.Fishing) Step(action, held, Time.unscaledDeltaTime);
            else TickTransition(Mathf.Min(Time.unscaledDeltaTime, .05f));
        }

        private void TickTransition(float dt)
        {
            switch (stage)
            {
                case Stage.Approach:
                    approachTime += dt;
                    Vector3 offset = Vector3.ProjectOnPlane(shot.Stand - player.transform.position, Vector3.up);
                    if (motor != null && offset.sqrMagnitude > .45f * .45f && approachTime < .85f) return;
                    if (!FadeTo(1f, dt)) return;
                    if (motor != null) motor.EndInteractionStep(this);
                    // A blocked or interrupted approach uses the actual grounded position.
                    shot.Enter();
                    pose?.Show();
                    view.Show();
                    Draw();
                    stage = Stage.RevealFishing;
                    break;
                case Stage.RevealFishing:
                    if (FadeTo(0f, dt)) stage = Stage.Fishing;
                    break;
                case Stage.CoverExit:
                    if (!FadeTo(1f, dt)) return;
                    RestoreWorld();
                    stage = Stage.RevealWorld;
                    break;
                case Stage.RevealWorld:
                    if (FadeTo(0f, dt)) Close();
                    break;
            }
        }

        private bool FadeTo(float target, float dt)
        {
            float duration = target > 0f ? CoverSeconds : RevealSeconds;
            fade = Mathf.MoveTowards(fade, target, dt / duration);
            view.SetFade(fade);
            return fade == target;
        }

        private void RequestExit()
        {
            if (stage == Stage.Approach)
            {
                if (motor != null) motor.EndInteractionStep(this);
                stage = Stage.RevealWorld;
            }
            else if (stage == Stage.Fishing || stage == Stage.RevealFishing) stage = Stage.CoverExit;
        }

        private void Step(bool action, bool held, float delta)
        {
            if (action) round.Press(Completed == CatchStep);
            GmFishingPhase before = round.Phase;
            round.Tick(delta, held);
            if (before == GmFishingPhase.Waiting && round.Phase == GmFishingPhase.Bite)
                GmGameplayAudio.Play(gameObject, ref feedbackAudio, GmGameplayAudio.FishingBite);
            else if (before == GmFishingPhase.Reeling && round.Phase == GmFishingPhase.Caught)
            {
                added = round.TryCollect(GmPartyRun.Current.Inventory);
                questCompleted = added && journal.TryCompleteSideStep(GmQuestCatalog.Fishing.Id, CatchStep);
                if (added) GmGameplayAudio.Play(gameObject, ref feedbackAudio, GmGameplayAudio.FishingCatch);
            }
            Draw();
        }

        private void Draw()
        {
            pose?.Draw(round.Phase, round.Elapsed);
            view.Render(round, Completed == CatchStep, added, questCompleted);
        }

        private void LateUpdate()
        {
            if (round == null || player == null || shot.Camera == null ||
                stage == Stage.Approach || stage == Stage.RevealWorld) return;
            pose?.FaceCamera(shot.Camera);
            Vector3 tip = pose != null ? pose.RodTip : player.transform.position + Vector3.up * .8f;
            view.DrawLine(shot.Camera, tip, shot.Float, round);
        }

        private void RestoreWorld()
        {
            if (motor != null) motor.EndInteractionStep(this);
            shot?.Restore();
            pose?.Hide();
            view?.Hide();
        }

        private void Close()
        {
            RestoreWorld();
            view?.SetFade(0f);
            if (player != null)
            {
                GmControlGate gate = player.ControlGate;
                if (gate != null) gate.Unblock(this);
                player.SuppressInteraction();
            }
            shot = null;
            round = null;
            motor = null;
            player = null;
        }

        private void OnDisable() => Close();
        private void OnDestroy() { Close(); pose?.Dispose(); view?.Dispose(); }
        private void OnApplicationFocus(bool hasFocus) => focused = hasFocus;
        private void OnApplicationPause(bool isPaused) => paused = isPaused;
    }
}
