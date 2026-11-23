using System.Collections;
using ProjectVN.Preferences;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmDemoEnding : MonoBehaviour
    {
        private const string Message = "Terima kasih telah memainkan game ini.\nDemo playtest berakhir sampai di sini";
        private const float FadeSeconds = 1f;
        private const float HoldSeconds = 10f;
        private GmInteractor player;
        private GmControlGate gate;

        internal static void Play(GmInteractor player)
        {
            if (player == null) return;
            var root = new GameObject("Penutup Demo", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (root.scene != player.gameObject.scene)
                SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
            root.AddComponent<GmDemoEnding>().Begin(player);
        }

        private void Begin(GmInteractor interactor)
        {
            player = interactor;
            gate = player.ControlGate;
            gate.Block(this);
            player.SuppressInteraction();
            GmMenuButton.Focus(null);

            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            var layer = (RectTransform)transform;
            Image black = Fill(Rect("Layar hitam", layer, 0f, 0f, 1f, 1f), Color.clear);
            black.raycastTarget = true;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text caption = Label(font, layer, "Teks penutup", string.Empty, 36, .08f, .36f, .92f, .64f);
            caption.color = Color.white;
            caption.resizeTextMinSize = 18;
            StartCoroutine(Run(black, caption));
        }

        private IEnumerator Run(Image black, Text caption)
        {
            float fadeElapsed = 0f;
            while (fadeElapsed < FadeSeconds)
            {
                yield return null;
                fadeElapsed += Time.unscaledDeltaTime;
                black.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, fadeElapsed / FadeSeconds));
            }

            const VNTextSpeedMode speed = VNTextSpeedMode.Normal;
            var reveal = new GmVnTextReveal();
            reveal.Begin(Message, speed, GmVnRevealMode.PerHuruf);
            caption.gameObject.AddComponent<GmVnLineFade>().Bind(caption, reveal);
            while (reveal.IsWriting)
            {
                yield return null;
                if (reveal.Tick(Time.unscaledDeltaTime, speed)) caption.SetVerticesDirty();
            }

            float shownFor = 0f;
            while (shownFor < HoldSeconds)
            {
                yield return null;
                shownFor += Time.unscaledDeltaTime;
            }

            if (GmDemoScenes.TryReturnToTitle()) yield break;
            Debug.LogError("[Daeng Saka] Main Menu gagal dimuat setelah penutup demo.", this);
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (gate != null) gate.Unblock(this);
            if (player != null) player.SuppressInteraction();
        }
    }
}
