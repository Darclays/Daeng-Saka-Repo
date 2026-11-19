using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    [Serializable]
    public sealed class VNCharacterExpressionDefinition
    {
        [Tooltip("Stable logical key, for example: idle, mad, sad, shock.")]
        [SerializeField]
        private string expressionKey;

        [SerializeField]
        private string displayName;

        [Tooltip("Variant used when a dialogue line selects this expression without requesting a specific variant.")]
        [SerializeField]
        private string defaultVariantKey;

        [SerializeField]
        private List<VNCharacterSpriteVariant> variants =
            new List<VNCharacterSpriteVariant>();

        [Tooltip("Optional old names or alternate authoring names that should still resolve to this expression.")]
        [SerializeField]
        private string[] aliases =
            Array.Empty<string>();

        public string ExpressionKey =>
            VNCharacterVisualKeyUtility
                .NormalizeKey(
                    expressionKey);

        public string DisplayName =>
            !string.IsNullOrWhiteSpace(
                displayName)
                ? displayName.Trim()
                : VNCharacterVisualKeyUtility
                    .CreateDisplayName(
                        ExpressionKey);

        public string DefaultVariantKey =>
            VNCharacterVisualKeyUtility
                .NormalizeKey(
                    defaultVariantKey);

        public IReadOnlyList<VNCharacterSpriteVariant>
            Variants =>
                variants;

        public int VariantCount =>
            variants != null
                ? variants.Count
                : 0;

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
                    ExpressionKey,
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

        public bool TryGetVariant(
            string variantKey,
            out VNCharacterSpriteVariant variant)
        {
            variant = null;

            if (variants == null ||
                variants.Count == 0)
            {
                return false;
            }

            for (var i = 0;
                 i < variants.Count;
                 i++)
            {
                var candidate =
                    variants[i];

                if (candidate != null &&
                    candidate.MatchesKey(
                        variantKey))
                {
                    variant = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetDefaultVariant(
            out VNCharacterSpriteVariant variant)
        {
            if (!string.IsNullOrEmpty(
                    DefaultVariantKey) &&
                TryGetVariant(
                    DefaultVariantKey,
                    out variant))
            {
                return true;
            }

            variant =
                GetFirstValidVariant();

            return variant != null;
        }

        public VNCharacterSpriteVariant
            AddOrUpdateVariant(
                string variantKey,
                string displayName,
                Sprite sprite)
        {
            return AddOrUpdateManualVariant(
                variantKey,
                displayName,
                sprite);
        }

        public VNCharacterSpriteVariant
            AddOrUpdateManualVariant(
                string variantKey,
                string displayName,
                Sprite sprite)
        {
            variants ??=
                new List<VNCharacterSpriteVariant>();

            if (TryGetVariant(
                    variantKey,
                    out var existing))
            {
                existing.SetManualAuthoringData(
                    variantKey,
                    displayName,
                    sprite);

                EnsureDefaultVariant();

                return existing;
            }

            var created =
                new VNCharacterSpriteVariant();

            created.SetManualAuthoringData(
                variantKey,
                displayName,
                sprite);

            variants.Add(
                created);

            EnsureDefaultVariant();

            return created;
        }

        public bool TryAddMissingScannerVariant(
            string variantKey,
            string displayName,
            Sprite sprite,
            string sourceAssetPath,
            out VNCharacterSpriteVariant variant)
        {
            variant = null;

            variants ??=
                new List<VNCharacterSpriteVariant>();

            if (TryGetVariant(
                    variantKey,
                    out _))
            {
                return false;
            }

            if (TryGetVariantBySprite(
                    sprite,
                    out _))
            {
                return false;
            }

            var created =
                new VNCharacterSpriteVariant();

            created.SetScannerAuthoringData(
                variantKey,
                displayName,
                sprite,
                sourceAssetPath);

            variants.Add(
                created);

            EnsureDefaultVariant();

            variant = created;
            return true;
        }

        public bool TryGetVariantBySprite(
            Sprite sprite,
            out VNCharacterSpriteVariant variant)
        {
            variant = null;

            if (sprite == null ||
                variants == null)
            {
                return false;
            }

            for (var i = 0;
                 i < variants.Count;
                 i++)
            {
                var candidate =
                    variants[i];

                if (candidate != null &&
                    candidate.Sprite == sprite)
                {
                    variant = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool RemoveVariantAt(
            int index)
        {
            if (variants == null ||
                index < 0 ||
                index >= variants.Count)
            {
                return false;
            }

            variants.RemoveAt(
                index);

            EnsureDefaultVariant();

            return true;
        }

        public int RemoveScannerVariants()
        {
            if (variants == null ||
                variants.Count == 0)
            {
                return 0;
            }

            var removed = 0;

            for (var i =
                     variants.Count - 1;
                 i >= 0;
                 i--)
            {
                var variant =
                    variants[i];

                if (variant != null &&
                    variant.IsScannerCreated)
                {
                    variants.RemoveAt(
                        i);

                    removed++;
                }
            }

            EnsureDefaultVariant();

            return removed;
        }

        public void Configure(
            string key,
            string label)
        {
            expressionKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        key);

            displayName =
                !string.IsNullOrWhiteSpace(
                    label)
                    ? label.Trim()
                    : VNCharacterVisualKeyUtility
                        .CreateDisplayName(
                            expressionKey);

            Sanitize();
        }

        public void SetDefaultVariant(
            string variantKey)
        {
            defaultVariantKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        variantKey);

            EnsureDefaultVariant();
        }

        public void SortVariants()
        {
            if (variants == null)
            {
                return;
            }

            variants.Sort(
                CompareVariants);
        }

        public void Sanitize()
        {
            expressionKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        expressionKey);

            displayName =
                VNCharacterVisualKeyUtility
                    .SanitizeLabel(
                        displayName);

            defaultVariantKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        defaultVariantKey);

            variants ??=
                new List<VNCharacterSpriteVariant>();

            for (var i = 0;
                 i < variants.Count;
                 i++)
            {
                variants[i]?.Sanitize();
            }

            if (aliases == null)
            {
                aliases =
                    Array.Empty<string>();
            }
            else
            {
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

            EnsureDefaultVariant();
        }

        private void EnsureDefaultVariant()
        {
            if (!string.IsNullOrEmpty(
                    DefaultVariantKey) &&
                TryGetVariant(
                    DefaultVariantKey,
                    out _))
            {
                return;
            }

            var first =
                GetFirstValidVariant();

            defaultVariantKey =
                first != null
                    ? first.VariantKey
                    : string.Empty;
        }

        private VNCharacterSpriteVariant
            GetFirstValidVariant()
        {
            if (variants == null)
            {
                return null;
            }

            for (var i = 0;
                 i < variants.Count;
                 i++)
            {
                if (variants[i] != null)
                {
                    return variants[i];
                }
            }

            return null;
        }

        private static int CompareVariants(
            VNCharacterSpriteVariant left,
            VNCharacterSpriteVariant right)
        {
            if (ReferenceEquals(
                    left,
                    right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return string.Compare(
                left.VariantKey,
                right.VariantKey,
                StringComparison.Ordinal);
        }
    }
}
