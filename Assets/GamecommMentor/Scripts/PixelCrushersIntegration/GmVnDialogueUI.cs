using System;
using PixelCrushers.DialogueSystem;
using ProjectVN.Preferences;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmVnDialogueUI : AbstractDialogueUI
    {
        private enum BackdropMode { Clear, Dim, DimWithBlur }

        private const string BackdropKey = "GamecommMentor.DaengSaka.Dialogue.Backdrop";
        private DialogueSystemController owner;
        private AbstractDialogueUI stock;
        private GmVnDialoguePilot cast;
        private string conversationTitle;
        private GmVnDialogueStage stage;
        private GmDialogueWorldBlur worldBlur;
        private Subtitle current;
        private readonly GmVnTextReveal reveal = new GmVnTextReveal();
        private VNTextSpeedMode speed;
        private BackdropMode backdropMode;
        private bool canContinue, showingResponses;
        private int shownFrame, acceptedFrame = -1;
        private int completedFrame = -1, focusFrame = -1;

        public override AbstractUIRoot uiRootControls => stock.uiRootControls;
        public override AbstractDialogueUIControls dialogueControls => stock.dialogueControls;
        public override AbstractUIQTEControls qteControls => stock.qteControls;
        public override AbstractUIAlertControls alertControls => stock.alertControls;

        internal static GmVnDialogueUI ForConversation(string title, DialogueSystemController controller,
            ref GmVnDialogueUI cached)
        {
            var settings = GmVnDialogueCatalog.PresentationFor(title, controller.masterDatabase);
            if (settings == null || !settings.Enabled) return null;
            if (cached != null && controller.dialogueUI == cached)
            {
                cached.cast = settings;
                cached.conversationTitle = title;
                cached.SetWorldFraming(false);
                return cached;
            }
            var stockUI = controller.dialogueUI as AbstractDialogueUI;
            if (stockUI == null) return null;

            if (cached == null)
            {
                GameObject host = null;
                try
                {
                    var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) return null;
                    host = new GameObject("Daeng Saka - Tampilan VN", typeof(RectTransform),
                        typeof(Canvas), typeof(GraphicRaycaster));
                    if (host.scene != controller.gameObject.scene)
                        SceneManager.MoveGameObjectToScene(host, controller.gameObject.scene);
                    var canvas = host.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 72;
                    var ui = host.AddComponent<GmVnDialogueUI>();
                    ui.owner = controller;
                    ui.stock = stockUI;
                    ui.stage = new GmVnDialogueStage((RectTransform)host.transform, font,
                        ui.OnContinueConversation, ui.ChangeSpeed, ui.ChangeBackdrop);
                    cached = ui;
                }
                catch (Exception exception)
                {
                    if (host != null)
                    {
                        host.SetActive(false);
                        Destroy(host);
                    }
                    Debug.LogWarning("[Daeng Saka] Memakai tampilan dialog bawaan. " + exception.Message, controller);
                    return null;
                }
            }
            cached.stock = stockUI;
            cached.cast = settings;
            cached.conversationTitle = title;
            cached.SetWorldFraming(false);
            return cached;
        }

        internal void SetWorldFraming(bool value)
        {
            stage.SetWorldFraming(value, cast.UsesSpeakerPortraits);
            if (!value) SetWorldBackdropCamera(null);
            else ApplyBackdrop();
            if (current != null) stage.Show(cast, conversationTitle, current, reveal);
        }

        internal void SetWorldBackdropCamera(Camera camera)
        {
            worldBlur?.Dispose();
            worldBlur = camera != null && HasBackdropOptions
                ? GmDialogueWorldBlur.TryCreate(camera) : null;
            ApplyBackdrop();
        }

        internal void SetCameraFade(float alpha) => stage.SetCameraFade(alpha);

        // The stock UI owns its own initialization, alerts, QTEs and response controls.
        public override void Start() { }
        public override void Open()
        {
            if (!isOpen) GmGameplayAudio.Ui(GmUiSound.Open);
            ClearLine();
            speed = GmVnReadingSpeed.Load();
            stage.SetSpeed(speed);
            isOpen = true;
            backdropMode = PlayerPrefs.GetInt(BackdropKey, 0) switch
            {
                (int)BackdropMode.Dim => BackdropMode.Dim,
                (int)BackdropMode.DimWithBlur => BackdropMode.DimWithBlur,
                _ => BackdropMode.Clear
            };
            ApplyBackdrop();
        }

        public override void Close()
        {
            isOpen = false;
            SetWorldBackdropCamera(null);
            HideResponses();
            ClearLine();
        }

        private void OnDisable() => EndIfActive();

        private void OnApplicationFocus(bool focused)
        {
            if (focused) focusFrame = Time.frameCount;
        }

        internal void EndIfActive()
        {
            if (owner != null && owner.isActiveAndEnabled && owner.isConversationActive &&
                owner.conversationView?.dialogueUI == this) owner.StopConversation();
            Close();
        }

        internal void RestoreAfterFailedStart()
        {
            // Pixel Crushers can reject the first graph node after selecting the override.
            if (owner != null && !owner.isConversationActive && stock != null && owner.dialogueUI == this)
                owner.dialogueUI = stock;
            Close();
        }

        private void ClearLine()
        {
            current = null;
            canContinue = false;
            reveal.Begin(string.Empty, speed);
            stage?.Hide();
        }

        public override void ShowSubtitle(Subtitle subtitle)
        {
            if (!isOpen || subtitle == null || string.IsNullOrWhiteSpace(subtitle.formattedText?.text)) return;
            current = subtitle;
            shownFrame = Time.frameCount;
            completedFrame = -1;
            canContinue = false;
            reveal.Begin(subtitle.formattedText.text, speed, cast.RevealMode);
            stage.Show(cast, conversationTitle, subtitle, reveal);
            stage.SetAction(reveal.IsWriting, false);
        }

        public override void HideSubtitle(Subtitle subtitle)
        {
            if (current == null || subtitle != current) return;
            current = null;
            canContinue = false;
            stage.EndSubtitle();
        }

        public override void ShowContinueButton(Subtitle subtitle) => SetContinue(subtitle, true);
        public override void HideContinueButton(Subtitle subtitle) => SetContinue(subtitle, false);

        private void SetContinue(Subtitle subtitle, bool value)
        {
            if (!isOpen || current == null || current != subtitle) return;
            canContinue = value;
            stage.SetAction(reveal.IsWriting, value);
        }

        public override void Update()
        {
            if (!isOpen || !Application.isFocused || Time.frameCount == focusFrame)
                return;
            if (showingResponses)
            {
                if (GmGamepadInput.UsingGamepad && stock != null) GmMenuButton.FocusWithin(stock.transform);
                return;
            }
            stage.Tick(Time.unscaledDeltaTime);
            if (current == null) return;
            if (HasBackdropOptions && GmGamepadInput.NextTabPressed) ChangeBackdrop();
            else if (GmGamepadInput.PreviousTabPressed) ChangeSpeed();
            else if (GmGamepadInput.TakeSubmit()) OnContinueConversation();
            else ReadKeyboard();
            if (current == null || Time.frameCount == shownFrame || !reveal.Tick(Time.unscaledDeltaTime, speed)) return;
            stage.RefreshText();
            if (!reveal.IsWriting)
            {
                completedFrame = Time.frameCount;
                stage.SetAction(false, canContinue);
            }
        }

        private void ReadKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (HasBackdropOptions && keyboard.bKey.wasPressedThisFrame) ChangeBackdrop();
                else if (keyboard.tKey.wasPressedThisFrame) ChangeSpeed();
                else if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) OnContinueConversation();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (HasBackdropOptions && UnityEngine.Input.GetKeyDown(KeyCode.B)) ChangeBackdrop();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.T)) ChangeSpeed();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
                OnContinueConversation();
#endif
        }

        private bool AcceptsInput => isActiveAndEnabled && isOpen && Application.isFocused && current != null &&
            !showingResponses && Time.frameCount != shownFrame && Time.frameCount != acceptedFrame &&
            Time.frameCount != focusFrame && owner != null && owner.conversationView?.dialogueUI == this;

        public override void OnContinue() => OnContinueConversation();
        public override void OnContinueConversation()
        {
            if (!AcceptsInput) return;
            if (reveal.IsWriting)
            {
                acceptedFrame = Time.frameCount;
                GmGameplayAudio.Ui(GmUiSound.Dialogue);
                CompleteLineText();
                return;
            }
            if (!canContinue || Time.frameCount == completedFrame) return;
            acceptedFrame = Time.frameCount;
            GmGameplayAudio.Ui(GmUiSound.Dialogue);
            SetContinue(current, false);
            owner.conversationView.OnConversationContinue(this);
        }

        private void ChangeSpeed()
        {
            if (!AcceptsInput) return;
            acceptedFrame = Time.frameCount;
            GmGameplayAudio.Ui(GmUiSound.Tab);
            speed = GmVnReadingSpeed.Cycle(speed);
            stage.SetSpeed(speed);
            if (speed == VNTextSpeedMode.Instant && reveal.IsWriting) CompleteLineText();
        }

        private bool HasBackdropOptions => isOpen && cast != null && cast.UsesSpeakerPortraits;

        private BackdropMode EffectiveBackdrop => backdropMode == BackdropMode.DimWithBlur && worldBlur == null
            ? BackdropMode.Dim : backdropMode;

        private void ChangeBackdrop()
        {
            if (!HasBackdropOptions || !AcceptsInput) return;
            acceptedFrame = Time.frameCount;
            GmGameplayAudio.Ui(GmUiSound.Tab);
            backdropMode = EffectiveBackdrop switch
            {
                BackdropMode.Clear => BackdropMode.Dim,
                BackdropMode.Dim when worldBlur != null => BackdropMode.DimWithBlur,
                _ => BackdropMode.Clear
            };
            PlayerPrefs.SetInt(BackdropKey, (int)backdropMode);
            PlayerPrefs.Save();
            ApplyBackdrop();
        }

        private void ApplyBackdrop()
        {
            var mode = EffectiveBackdrop;
            bool available = HasBackdropOptions;
            worldBlur?.SetEnabled(available && mode == BackdropMode.DimWithBlur);
            string caption = mode switch
            {
                BackdropMode.Dim => "Redup   [B]",
                BackdropMode.DimWithBlur => "Redup + blur   [B]",
                _ => "Jernih   [B]"
            };
            stage?.SetBackdropMode(available, mode != BackdropMode.Clear, caption);
        }

        private void CompleteLineText()
        {
            reveal.Complete();
            completedFrame = Time.frameCount;
            stage.RefreshText();
            stage.SetAction(false, canContinue);
        }

        public override void ShowResponses(Subtitle subtitle, Response[] responses, float timeout)
        {
            ClearLine();
            if (!showingResponses)
            {
                showingResponses = true;
                stock.SelectedResponseHandler += OnStockResponse;
                stock.Open();
            }
            stock.ShowResponses(subtitle, responses, timeout);
        }

        private void OnStockResponse(object sender, SelectedResponseEventArgs args)
        {
            var view = owner != null ? owner.conversationView : null;
            if (!showingResponses || view == null || view.dialogueUI != this) return;
            GmGameplayAudio.Ui(GmUiSound.Confirm);
            view.SelectResponse(args);
        }

        public override void HideResponses()
        {
            if (!showingResponses) return;
            showingResponses = false;
            if (stock == null) return;
            stock.SelectedResponseHandler -= OnStockResponse;
            stock.HideResponses();
            stock.Close();
        }

        public override void ShowAlert(string message, float duration) => stock.ShowAlert(message, duration);
        public override void HideAlert() => stock.HideAlert();
        public override void HideAllAlerts() => stock.HideAllAlerts();
        public override void ShowQTEIndicator(int index) => stock.ShowQTEIndicator(index);
        public override void HideQTEIndicator(int index) => stock.HideQTEIndicator(index);
        public override void SetPCPortrait(Sprite portrait, string name) => stock.SetPCPortrait(portrait, name);
        public override void SetActorPortraitSprite(string name, Sprite portrait) => stock.SetActorPortraitSprite(name, portrait);
    }
}
