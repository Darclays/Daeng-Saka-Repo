using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectVN.UI.Dialogue
{
    /// <summary>
    /// Batch 10C.3 - Next Indicator Behavior.
    /// Controls blink/pulse/show/hide for the custom VN next indicator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VNNextIndicatorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text indicatorText;
        [SerializeField] private Graphic indicatorGraphic;

        [Header("Visibility")]
        [SerializeField] private bool visibleOnStart = false;
        [SerializeField] private float showFadeSpeed = 14f;
        [SerializeField] private float hideFadeSpeed = 18f;

        [Header("Blink")]
        [SerializeField] private bool useBlink = true;
        [SerializeField] private float blinkSpeed = 3.2f;
        [SerializeField] private float minBlinkAlpha = 0.35f;
        [SerializeField] private float maxBlinkAlpha = 1f;

        [Header("Pulse")]
        [SerializeField] private bool useScalePulse = true;
        [SerializeField] private float pulseSpeed = 3.2f;
        [SerializeField] private float minScale = 0.95f;
        [SerializeField] private float maxScale = 1.05f;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = false;

        private bool _targetVisible;
        private float _baseAlpha = 1f;
        private Vector3 _baseScale;

        public bool TargetVisible => _targetVisible;
        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

        private void Reset()
        {
            AutoAssignReferences();
        }

        private void Awake()
        {
            AutoAssignReferences();
            _baseScale = transform.localScale;

            if (indicatorText != null)
            {
                _baseAlpha = indicatorText.color.a;
            }
            else if (indicatorGraphic != null)
            {
                _baseAlpha = indicatorGraphic.color.a;
            }

            SetVisible(visibleOnStart, true);
        }

        private void Update()
        {
            if (canvasGroup == null)
            {
                return;
            }

            var targetAlpha = _targetVisible ? 1f : 0f;
            var speed = _targetVisible ? showFadeSpeed : hideFadeSpeed;

            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                speed * Time.unscaledDeltaTime);

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (_targetVisible && canvasGroup.alpha > 0.01f)
            {
                ApplyBlinkAndPulse();
            }
            else
            {
                RestoreVisuals();
            }
        }

        public void SetVisible(bool visible)
        {
            SetVisible(visible, false);
        }

        public void SetVisible(bool visible, bool instant)
        {
            AutoAssignReferences();

            if (_targetVisible != visible && logStateChanges)
            {
                Debug.Log($"[VNNextIndicatorController] Visible: {visible}", this);
            }

            _targetVisible = visible;

            if (canvasGroup == null)
            {
                return;
            }

            if (instant)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (!visible && instant)
            {
                RestoreVisuals();
            }
        }

        [ContextMenu("ProjectVN Next Indicator/Show")]
        public void Show()
        {
            SetVisible(true);
        }

        [ContextMenu("ProjectVN Next Indicator/Hide")]
        public void Hide()
        {
            SetVisible(false);
        }

        [ContextMenu("ProjectVN Next Indicator/Auto Assign References")]
        public void AutoAssignReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (indicatorText == null)
            {
                indicatorText = GetComponent<TMP_Text>();
            }

            if (indicatorGraphic == null)
            {
                indicatorGraphic = GetComponent<Graphic>();
            }
        }

        private void ApplyBlinkAndPulse()
        {
            var t = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f;
            var blinkAlpha = useBlink ? Mathf.Lerp(minBlinkAlpha, maxBlinkAlpha, t) : maxBlinkAlpha;

            ApplyAlpha(_baseAlpha * blinkAlpha);

            if (useScalePulse)
            {
                var scaleT = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
                var scale = Mathf.Lerp(minScale, maxScale, scaleT);
                transform.localScale = _baseScale * scale;
            }
        }

        private void RestoreVisuals()
        {
            ApplyAlpha(_baseAlpha);
            transform.localScale = _baseScale == Vector3.zero ? Vector3.one : _baseScale;
        }

        private void ApplyAlpha(float alpha)
        {
            if (indicatorText != null)
            {
                var color = indicatorText.color;
                color.a = alpha;
                indicatorText.color = color;
            }
            else if (indicatorGraphic != null)
            {
                var color = indicatorGraphic.color;
                color.a = alpha;
                indicatorGraphic.color = color;
            }
        }
    }
}
