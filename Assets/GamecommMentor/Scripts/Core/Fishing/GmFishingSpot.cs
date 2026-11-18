using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmFishingSpot : GmInteractable
    {
        public const string ArtPath = "Assets/GamecommMentor/Resources/DaengSaka/Fishing/SakaFishing.png";
        private const int CatchStep = 1;
        [SerializeField] private GmQuestJournal journal;
        [SerializeField] private Transform castPoint;
        [SerializeField] private Texture2D sakaAtlas;
        [SerializeField] private Camera worldCamera;
        private readonly System.Random random = new System.Random();
        private GmInteractor player, poseOwner;
        private GmFishingRound round;
        private GmFishingView view;
        private GmFishingPose pose;
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
            player = requester;
            round = new GmFishingRound(random);
            added = questCompleted = false;
            openedFrame = Time.frameCount;
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
                pose?.Show();
                view.Show();
                Draw();
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
                castPoint == null || SceneManager.GetActiveScene() != gameObject.scene)
            {
                Close();
                return;
            }
            if (!focused || paused || Time.frameCount == openedFrame) return;

            bool action = false, held = false, close = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                action = keys.spaceKey.wasPressedThisFrame;
                held = keys.spaceKey.isPressed;
                close = keys.escapeKey.wasPressedThisFrame;
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
            close = Input.GetKeyDown(KeyCode.Escape) ||
                (Input.GetMouseButtonDown(0) && view.HitExit(Input.mousePosition));
#endif
            if (close) { Close(); return; }
            Step(action, held, Time.unscaledDeltaTime);
        }

        private void Step(bool action, bool held, float delta)
        {
            if (action) round.Press(Completed == CatchStep);
            GmFishingPhase before = round.Phase;
            round.Tick(delta, held);
            if (before == GmFishingPhase.Reeling && round.Phase == GmFishingPhase.Caught)
            {
                added = round.TryCollect(GmPartyRun.Current.Inventory);
                questCompleted = added && journal.TryCompleteSideStep(GmQuestCatalog.Fishing.Id, CatchStep);
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
            if (round == null || player == null || castPoint == null) return;
            Vector3 hand = pose != null ? pose.Hand : player.transform.position + Vector3.up * .85f;
            view.DrawLine(worldCamera != null ? worldCamera : Camera.main, hand, castPoint.position, round);
        }

        private void Close()
        {
            round = null;
            view?.Hide();
            pose?.Hide();
            if (player != null)
            {
                GmControlGate gate = player.ControlGate;
                if (gate != null) gate.Unblock(this);
                player.SuppressInteraction();
            }
            player = null;
        }

        private void OnDisable() => Close();
        private void OnDestroy() { Close(); pose?.Dispose(); view?.Dispose(); }
        private void OnApplicationFocus(bool hasFocus) => focused = hasFocus;
        private void OnApplicationPause(bool isPaused) => paused = isPaused;
    }
}
