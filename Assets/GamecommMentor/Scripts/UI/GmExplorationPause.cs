using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(-220)]
    [DisallowMultipleComponent]
    internal sealed class GmExplorationPause : MonoBehaviour
    {
        private GmInteractor player;
        private GmControlDialog dialog;
        private GameObject host;

        private void Awake() => player = GetComponent<GmInteractor>();

        private void Update()
        {
            if (player == null || !player.isActiveAndEnabled || gameObject.scene != SceneManager.GetActiveScene())
            {
                Close();
                return;
            }
            if (dialog?.IsOpen == true)
            {
                if (GmGamepadInput.PressedMenu() || GmGamepadInput.PressedBack()) Close();
                else dialog.RestoreFocus();
            }
            else if (player.ControlGate.CanExplore && GmGamepadInput.PressedMenu())
            {
                dialog ??= CreateDialog();
                player.ControlGate.Block(this);
                dialog.Show("Jeda", "Eksplorasi dijeda.", "Lanjutkan");
            }
        }

        private GmControlDialog CreateDialog()
        {
            host = new GameObject("Menu jeda eksplorasi", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 65;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            return new GmControlDialog((RectTransform)host.transform,
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), Close, Close);
        }

        private void Close()
        {
            if (dialog?.IsOpen != true) return;
            dialog.Hide();
            if (player != null)
            {
                player.ControlGate.Unblock(this);
                player.SuppressInteraction();
            }
            GmMenuButton.Focus(null);
        }

        private void OnDisable() => Close();
        private void OnDestroy() { if (host != null) Destroy(host); }
    }
}
