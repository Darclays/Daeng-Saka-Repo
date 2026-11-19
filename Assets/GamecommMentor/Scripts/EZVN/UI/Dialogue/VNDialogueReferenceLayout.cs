using TMPro;
using UnityEngine;

namespace ProjectVN.UI.Dialogue
{
    /// <summary>Presentation-only reference pixels. No dialogue/input ownership.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-300)]
    public sealed class VNDialogueReferenceLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform referenceFrame;
        [SerializeField] private RectTransform readingArea;
        [SerializeField] private RectTransform quickControls;
        [SerializeField] private RectTransform choicePanel;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private RectTransform bottomGradient;
        [SerializeField] private VNDialogueReferenceLayout readingLayout;
        [SerializeField, Min(1f)] private float referenceHeight = 1080f;
        [SerializeField, Min(1f)] private float maximumReadingWidth = 1560f;
        [SerializeField, Min(0f)] private float sideMargin = 180f;
        [SerializeField, Min(1f)] private float maximumChoiceWidth = 960f;
        private string measuredText;
        private TMP_FontAsset measuredFont;
        private float measuredWidth = -1f;
        private float measuredSize;
        private float measuredSpacing;
        private float measuredHeight;

        public RectTransform ReferenceFrame => referenceFrame;
        public float ReferenceWidth { get; private set; } = 1920f;
        public float ReadingReferenceHeight { get; private set; } = 188f;

        public void Configure(RectTransform frame, RectTransform reading,
            RectTransform controls, RectTransform choices, TMP_Text text = null,
            RectTransform gradient = null)
        {
            referenceFrame = frame;
            readingArea = reading;
            quickControls = controls;
            choicePanel = choices;
            dialogueText = text;
            bottomGradient = gradient;
            RefreshLayout();
        }

        public void BindReadingLayout(VNDialogueReferenceLayout source)
        {
            readingLayout = source;
            RefreshLayout();
        }

        private void OnEnable() { RefreshLayout(); }
        private void LateUpdate() { RefreshLayout(); }
        private void OnValidate() { measuredWidth = -1f; }

        public void RefreshLayout()
        {
            var root = transform as RectTransform;
            if (root == null || referenceFrame == null || referenceFrame == root ||
                root.rect.height <= 0.01f || root.rect.width <= 0.01f)
                return;

            var scale = root.rect.height / Mathf.Max(1f, referenceHeight);
            ReferenceWidth = root.rect.width / scale;
            var frameSize = new Vector2(ReferenceWidth, referenceHeight);
            if ((referenceFrame.sizeDelta - frameSize).sqrMagnitude > 0.001f)
                referenceFrame.sizeDelta = frameSize;
            var frameScale = new Vector3(scale, scale, 1f);
            if ((referenceFrame.localScale - frameScale).sqrMagnitude > 0.000001f)
                referenceFrame.localScale = frameScale;

            var readableWidth = Mathf.Min(maximumReadingWidth,
                Mathf.Max(320f, ReferenceWidth - sideMargin * 2f));
            SetWidth(readingArea, readableWidth);
            SetWidth(choicePanel, Mathf.Min(maximumChoiceWidth, readableWidth));
            if (readingArea != null)
            {
                var textHeight = MeasureText(Mathf.Max(64f, readableWidth - 64f));
                ReadingReferenceHeight = Mathf.Max(188f, textHeight + 48f);
                if (Mathf.Abs(readingArea.sizeDelta.y - ReadingReferenceHeight) > 0.01f)
                    readingArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        ReadingReferenceHeight);
                var gradientHeight = Mathf.Max(389f, ReadingReferenceHeight + 201f);
                if (bottomGradient != null && Mathf.Abs(bottomGradient.sizeDelta.y - gradientHeight) > 0.01f)
                    bottomGradient.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gradientHeight);
            }
            if (choicePanel != null)
            {
                var bottom = readingLayout != null ?
                    76f + readingLayout.ReadingReferenceHeight + 22f : 286f;
                var position = new Vector2(0f, bottom);
                if ((choicePanel.anchoredPosition - position).sqrMagnitude > 0.001f)
                    choicePanel.anchoredPosition = position;
            }

            if (quickControls != null)
            {
                var right = Mathf.Max(sideMargin,
                    (ReferenceWidth - maximumReadingWidth) * 0.5f);
                var position = new Vector2(-right, 22f);
                if ((quickControls.anchoredPosition - position).sqrMagnitude > 0.001f)
                    quickControls.anchoredPosition = position;
            }
        }

        private static void SetWidth(RectTransform target, float width)
        {
            if (target != null && Mathf.Abs(target.sizeDelta.x - width) > 0.01f)
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private float MeasureText(float width)
        {
            if (dialogueText == null || dialogueText.font == null)
                return 0f;
            if (measuredText != dialogueText.text || measuredFont != dialogueText.font ||
                Mathf.Abs(measuredWidth - width) > 0.01f || measuredSize != dialogueText.fontSize ||
                measuredSpacing != dialogueText.lineSpacing)
            {
                measuredHeight = dialogueText.GetPreferredValues(dialogueText.text, width, Mathf.Infinity).y;
                measuredText = dialogueText.text;
                measuredFont = dialogueText.font;
                measuredWidth = width;
                measuredSize = dialogueText.fontSize;
                measuredSpacing = dialogueText.lineSpacing;
            }
            return measuredHeight;
        }
    }
}
