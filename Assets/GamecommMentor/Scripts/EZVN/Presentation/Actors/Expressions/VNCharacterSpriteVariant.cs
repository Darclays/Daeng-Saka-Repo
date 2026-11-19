using System;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    [Serializable]
    public sealed class VNCharacterSpriteVariant
    {
        [Tooltip("Stable logical key used by dialogue and save data, for example: open_eyes_i.")]
        [SerializeField]
        private string variantKey;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite sprite;

        [Tooltip("Manual entries are protected from scanner overwrite. Scanner entries may be identified and cleaned separately.")]
        [SerializeField]
        private VNCharacterVariantAuthoringOrigin
            authoringOrigin =
                VNCharacterVariantAuthoringOrigin.Manual;

        [Tooltip("Editor-only diagnostic path recorded when this entry was created by the folder scanner.")]
        [SerializeField]
        private string scannerSourceAssetPath;

        [Tooltip("Optional old names or alternate authoring names that should still resolve to this variant.")]
        [SerializeField]
        private string[] aliases =
            Array.Empty<string>();

        public string VariantKey =>
            VNCharacterVisualKeyUtility.NormalizeKey(
                variantKey);

        public string DisplayName =>
            !string.IsNullOrWhiteSpace(
                displayName)
                ? displayName.Trim()
                : VNCharacterVisualKeyUtility
                    .CreateDisplayName(
                        VariantKey);

        public Sprite Sprite =>
            sprite;

        public VNCharacterVariantAuthoringOrigin
            AuthoringOrigin =>
                authoringOrigin;

        public string ScannerSourceAssetPath =>
            scannerSourceAssetPath ??
            string.Empty;

        public string[] Aliases =>
            aliases;

        public bool IsManual =>
            authoringOrigin ==
            VNCharacterVariantAuthoringOrigin.Manual;

        public bool IsScannerCreated =>
            authoringOrigin ==
            VNCharacterVariantAuthoringOrigin.Scanner;

        public bool MatchesKey(
            string value)
        {
            var normalized =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        value);

            if (string.IsNullOrEmpty(
                    normalized))
            {
                return false;
            }

            if (string.Equals(
                    VariantKey,
                    normalized,
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (aliases == null)
            {
                return false;
            }

            for (var i = 0;
                 i < aliases.Length;
                 i++)
            {
                if (string.Equals(
                        VNCharacterVisualKeyUtility
                            .NormalizeKey(
                                aliases[i]),
                        normalized,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetAuthoringData(
            string key,
            string label,
            Sprite spriteReference)
        {
            SetManualAuthoringData(
                key,
                label,
                spriteReference);
        }

        public void SetManualAuthoringData(
            string key,
            string label,
            Sprite spriteReference)
        {
            SetAuthoringDataInternal(
                key,
                label,
                spriteReference,
                VNCharacterVariantAuthoringOrigin.Manual,
                string.Empty);
        }

        public void SetScannerAuthoringData(
            string key,
            string label,
            Sprite spriteReference,
            string sourceAssetPath)
        {
            SetAuthoringDataInternal(
                key,
                label,
                spriteReference,
                VNCharacterVariantAuthoringOrigin.Scanner,
                sourceAssetPath);
        }

        public void ProtectAsManual()
        {
            authoringOrigin =
                VNCharacterVariantAuthoringOrigin.Manual;

            scannerSourceAssetPath =
                string.Empty;
        }

        public void SetSprite(
            Sprite spriteReference)
        {
            sprite =
                spriteReference;
        }

        public void Sanitize()
        {
            variantKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        variantKey);

            displayName =
                VNCharacterVisualKeyUtility
                    .SanitizeLabel(
                        displayName);

            scannerSourceAssetPath =
                string.IsNullOrWhiteSpace(
                    scannerSourceAssetPath)
                    ? string.Empty
                    : scannerSourceAssetPath
                        .Trim()
                        .Replace(
                            "\\",
                            "/");

            if (aliases == null)
            {
                aliases =
                    Array.Empty<string>();
                return;
            }

            for (var i = 0;
                 i < aliases.Length;
                 i++)
            {
                aliases[i] =
                    VNCharacterVisualKeyUtility
                        .NormalizeKey(
                            aliases[i]);
            }
        }

        private void SetAuthoringDataInternal(
            string key,
            string label,
            Sprite spriteReference,
            VNCharacterVariantAuthoringOrigin
                origin,
            string sourceAssetPath)
        {
            variantKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        key);

            displayName =
                !string.IsNullOrWhiteSpace(
                    label)
                    ? label.Trim()
                    : VNCharacterVisualKeyUtility
                        .CreateDisplayName(
                            variantKey);

            sprite =
                spriteReference;

            authoringOrigin =
                origin;

            scannerSourceAssetPath =
                origin ==
                    VNCharacterVariantAuthoringOrigin
                        .Scanner
                    ? sourceAssetPath ?? string.Empty
                    : string.Empty;

            Sanitize();
        }
    }
}
