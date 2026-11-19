using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    [CreateAssetMenu(
        fileName = "VIS_NewCharacter_Expressions",
        menuName = "ProjectVN/Actors/Character Expression Library")]
    public sealed class VNCharacterExpressionLibrary :
        ScriptableObject
    {
        [Header("Owner")]
        [Tooltip("Stable ProjectVN actor key. This should match the owning ACT profile.")]
        [SerializeField]
        private string actorKey;

        [Header("Defaults")]
        [Tooltip("Expression used when a requested expression cannot be resolved.")]
        [SerializeField]
        private string defaultExpressionKey;

        [Header("Expression Library")]
        [SerializeField]
        private List<VNCharacterExpressionDefinition>
            expressions =
                new List<VNCharacterExpressionDefinition>();

        [Header("Folder Scanner")]
        [SerializeField]
        private VNCharacterSpriteImportSettings
            importSettings =
                new VNCharacterSpriteImportSettings();

        [Header("Diagnostics")]
        [SerializeField]
        private bool warnOnRuntimeFallback = true;

        private readonly HashSet<string>
            _warnedFallbackRequests =
                new HashSet<string>(
                    StringComparer.Ordinal);

        public string ActorKey =>
            VNActorIdentity.NormalizeKey(
                actorKey);

        public string DefaultExpressionKey =>
            VNCharacterVisualKeyUtility
                .NormalizeKey(
                    defaultExpressionKey);

        public IReadOnlyList<VNCharacterExpressionDefinition>
            Expressions =>
                expressions;

        public VNCharacterSpriteImportSettings
            ImportSettings =>
                importSettings;

        public int ExpressionCount =>
            expressions != null
                ? expressions.Count
                : 0;

        public bool TryGetExpression(
            string expressionKey,
            out VNCharacterExpressionDefinition
                expression)
        {
            expression = null;

            if (expressions == null ||
                expressions.Count == 0)
            {
                return false;
            }

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                var candidate =
                    expressions[i];

                if (candidate != null &&
                    candidate.MatchesKey(
                        expressionKey))
                {
                    expression = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetDefaultExpression(
            out VNCharacterExpressionDefinition
                expression)
        {
            if (!string.IsNullOrEmpty(
                    DefaultExpressionKey) &&
                TryGetExpression(
                    DefaultExpressionKey,
                    out expression))
            {
                return true;
            }

            expression =
                GetFirstValidExpression();

            return expression != null;
        }

        public bool TryResolveDefaultSprite(
            out VNCharacterExpressionResolveResult
                result)
        {
            result =
                new VNCharacterExpressionResolveResult
                {
                    requestedExpressionKey =
                        string.Empty,

                    requestedVariantKey =
                        string.Empty,

                    fallbackLevel =
                        VNCharacterVisualFallbackLevel
                            .Unresolved,

                    message =
                        string.Empty
                };

            if (!TryGetDefaultExpression(
                    out var defaultExpression) ||
                !defaultExpression
                    .TryGetDefaultVariant(
                        out var defaultVariant) ||
                defaultVariant == null ||
                defaultVariant.Sprite == null)
            {
                result.success = false;
                result.message =
                    "The character expression library has no valid default expression Sprite.";
                return false;
            }

            CompleteResult(
                result,
                defaultExpression,
                defaultVariant,
                VNCharacterVisualFallbackLevel
                    .CharacterDefaultExpression,
                "Character default expression resolved.");

            return true;
        }

        public bool TryResolveSprite(
            string expressionKey,
            string variantKey,
            out VNCharacterExpressionResolveResult
                result)
        {
            if (string.IsNullOrWhiteSpace(
                    expressionKey) &&
                string.IsNullOrWhiteSpace(
                    variantKey))
            {
                return TryResolveDefaultSprite(
                    out result);
            }

            result =
                new VNCharacterExpressionResolveResult
                {
                    requestedExpressionKey =
                        VNCharacterVisualKeyUtility
                            .NormalizeKey(
                                expressionKey),

                    requestedVariantKey =
                        VNCharacterVisualKeyUtility
                            .NormalizeKey(
                                variantKey),

                    fallbackLevel =
                        VNCharacterVisualFallbackLevel
                            .Unresolved,

                    message =
                        string.Empty
                };

            if (TryGetExpression(
                    expressionKey,
                    out var requestedExpression))
            {
                if (!string.IsNullOrEmpty(
                        result.requestedVariantKey) &&
                    requestedExpression
                        .TryGetVariant(
                            result.requestedVariantKey,
                            out var exactVariant) &&
                    exactVariant.Sprite != null)
                {
                    CompleteResult(
                        result,
                        requestedExpression,
                        exactVariant,
                        VNCharacterVisualFallbackLevel
                            .ExactVariant,
                        "Exact expression variant resolved.");

                    return true;
                }

                if (requestedExpression
                        .TryGetDefaultVariant(
                            out var expressionDefault) &&
                    expressionDefault.Sprite != null)
                {
                    CompleteResult(
                        result,
                        requestedExpression,
                        expressionDefault,
                        VNCharacterVisualFallbackLevel
                            .RequestedExpressionDefault,
                        "Requested expression resolved through its default variant.");

                    WarnFallbackOnce(
                        result);

                    return true;
                }
            }

            if (TryGetDefaultExpression(
                    out var defaultExpression) &&
                defaultExpression
                    .TryGetDefaultVariant(
                        out var characterDefault) &&
                characterDefault.Sprite != null)
            {
                CompleteResult(
                    result,
                    defaultExpression,
                    characterDefault,
                    VNCharacterVisualFallbackLevel
                        .CharacterDefaultExpression,
                    "Request resolved through the character default expression.");

                WarnFallbackOnce(
                    result);

                return true;
            }

            result.success = false;
            result.message =
                "No valid expression sprite could be resolved.";

            WarnFallbackOnce(
                result);

            return false;
        }

        public VNCharacterExpressionDefinition
            GetOrCreateExpression(
                string expressionKey,
                string displayName = "")
        {
            expressions ??=
                new List<VNCharacterExpressionDefinition>();

            if (TryGetExpression(
                    expressionKey,
                    out var existing))
            {
                return existing;
            }

            var created =
                new VNCharacterExpressionDefinition();

            created.Configure(
                expressionKey,
                displayName);

            expressions.Add(
                created);

            EnsureDefaultExpression();

            return created;
        }

        public VNCharacterSpriteVariant
            AddOrUpdateVariant(
                string expressionKey,
                string expressionDisplayName,
                string variantKey,
                string variantDisplayName,
                Sprite sprite)
        {
            return AddOrUpdateManualVariant(
                expressionKey,
                expressionDisplayName,
                variantKey,
                variantDisplayName,
                sprite);
        }

        public VNCharacterSpriteVariant
            AddOrUpdateManualVariant(
                string expressionKey,
                string expressionDisplayName,
                string variantKey,
                string variantDisplayName,
                Sprite sprite)
        {
            var expression =
                GetOrCreateExpression(
                    expressionKey,
                    expressionDisplayName);

            var variant =
                expression
                    .AddOrUpdateManualVariant(
                        variantKey,
                        variantDisplayName,
                        sprite);

            EnsureDefaultExpression();

            return variant;
        }

        public bool TryAddMissingScannerVariant(
            string expressionKey,
            string expressionDisplayName,
            string variantKey,
            string variantDisplayName,
            Sprite sprite,
            string sourceAssetPath,
            out VNCharacterSpriteVariant variant)
        {
            variant = null;

            if (sprite == null ||
                ContainsSprite(
                    sprite,
                    out _,
                    out _))
            {
                return false;
            }

            var expression =
                GetOrCreateExpression(
                    expressionKey,
                    expressionDisplayName);

            var added =
                expression
                    .TryAddMissingScannerVariant(
                        variantKey,
                        variantDisplayName,
                        sprite,
                        sourceAssetPath,
                        out variant);

            EnsureDefaultExpression();

            return added;
        }

        public bool ContainsSprite(
            Sprite sprite,
            out VNCharacterExpressionDefinition
                expression,
            out VNCharacterSpriteVariant variant)
        {
            expression = null;
            variant = null;

            if (sprite == null ||
                expressions == null)
            {
                return false;
            }

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                var candidateExpression =
                    expressions[i];

                if (candidateExpression != null &&
                    candidateExpression
                        .TryGetVariantBySprite(
                            sprite,
                            out var candidateVariant))
                {
                    expression =
                        candidateExpression;

                    variant =
                        candidateVariant;

                    return true;
                }
            }

            return false;
        }

        public bool RemoveExpressionAt(
            int index)
        {
            if (expressions == null ||
                index < 0 ||
                index >= expressions.Count)
            {
                return false;
            }

            expressions.RemoveAt(
                index);

            EnsureDefaultExpression();

            return true;
        }

        public int RemoveScannerVariants()
        {
            if (expressions == null ||
                expressions.Count == 0)
            {
                return 0;
            }

            var removed = 0;

            for (var i =
                     expressions.Count - 1;
                 i >= 0;
                 i--)
            {
                var expression =
                    expressions[i];

                if (expression == null)
                {
                    continue;
                }

                removed +=
                    expression
                        .RemoveScannerVariants();

                if (expression.VariantCount == 0)
                {
                    expressions.RemoveAt(
                        i);
                }
            }

            EnsureDefaultExpression();

            return removed;
        }

        public void ClearExpressions()
        {
            expressions ??=
                new List<VNCharacterExpressionDefinition>();

            expressions.Clear();
            defaultExpressionKey =
                string.Empty;

            _warnedFallbackRequests.Clear();
        }

        public void ConfigureForActor(
            string ownerActorKey,
            string suggestedFilenamePrefix)
        {
            actorKey =
                VNActorIdentity.NormalizeKey(
                    ownerActorKey);

            importSettings ??=
                new VNCharacterSpriteImportSettings();

            if (string.IsNullOrWhiteSpace(
                    importSettings.FilenamePrefix))
            {
                importSettings.ConfigurePrefix(
                    suggestedFilenamePrefix);
            }

            SanitizeAndSort();
        }

        public string[] GetExpressionKeys()
        {
            if (expressions == null)
            {
                return Array.Empty<string>();
            }

            var keys =
                new List<string>();

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                var expression =
                    expressions[i];

                if (expression == null ||
                    string.IsNullOrEmpty(
                        expression.ExpressionKey))
                {
                    continue;
                }

                keys.Add(
                    expression.ExpressionKey);
            }

            return keys.ToArray();
        }

        public string[] GetVariantKeys(
            string expressionKey)
        {
            if (!TryGetExpression(
                    expressionKey,
                    out var expression) ||
                expression.Variants == null)
            {
                return Array.Empty<string>();
            }

            var keys =
                new List<string>();

            for (var i = 0;
                 i < expression.Variants.Count;
                 i++)
            {
                var variant =
                    expression.Variants[i];

                if (variant == null ||
                    string.IsNullOrEmpty(
                        variant.VariantKey))
                {
                    continue;
                }

                keys.Add(
                    variant.VariantKey);
            }

            return keys.ToArray();
        }

        [ContextMenu(
            "ProjectVN Character Visuals/Normalize and Sort")]
        public void SanitizeAndSort()
        {
            actorKey =
                VNActorIdentity.NormalizeKey(
                    actorKey);

            defaultExpressionKey =
                VNCharacterVisualKeyUtility
                    .NormalizeKey(
                        defaultExpressionKey);

            expressions ??=
                new List<VNCharacterExpressionDefinition>();

            importSettings ??=
                new VNCharacterSpriteImportSettings();

            importSettings.Sanitize();

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                expressions[i]?.Sanitize();
                expressions[i]?.SortVariants();
            }

            expressions.Sort(
                CompareExpressions);

            EnsureDefaultExpression();

            _warnedFallbackRequests.Clear();
        }

        [ContextMenu(
            "ProjectVN Character Visuals/Validate Library")]
        public void ValidateAndLog()
        {
            SanitizeAndSort();

            var issueCount = 0;
            var expressionKeys =
                new HashSet<string>(
                    StringComparer.Ordinal);

            if (string.IsNullOrEmpty(
                    ActorKey))
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Actor Key is empty.",
                    this);
            }

            if (expressions == null ||
                expressions.Count == 0)
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] No expressions are configured.",
                    this);
            }
            else
            {
                for (var i = 0;
                     i < expressions.Count;
                     i++)
                {
                    ValidateExpression(
                        expressions[i],
                        i,
                        expressionKeys,
                        ref issueCount);
                }
            }

            if (!string.IsNullOrEmpty(
                    DefaultExpressionKey) &&
                !TryGetExpression(
                    DefaultExpressionKey,
                    out _))
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Default Expression '{DefaultExpressionKey}' does not exist.",
                    this);
            }

            if (issueCount == 0)
            {
                Debug.Log(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Validation PASS. Actor='{ActorKey}', Expressions={ExpressionCount}, Variants={GetTotalVariantCount()}.",
                    this);
                return;
            }

            Debug.LogWarning(
                $"[{nameof(VNCharacterExpressionLibrary)}] Validation found {issueCount} issue(s). Actor='{ActorKey}'.",
                this);
        }

        [ContextMenu(
            "ProjectVN Character Visuals/Log Library Summary")]
        public void LogLibrarySummary()
        {
            SanitizeAndSort();

            Debug.Log(
                $"[{nameof(VNCharacterExpressionLibrary)}] Actor='{ActorKey}', DefaultExpression='{DefaultExpressionKey}', Expressions={ExpressionCount}, Variants={GetTotalVariantCount()}, ImportPrefix='{ImportSettings.FilenamePrefix}'.",
                this);

            if (expressions == null)
            {
                return;
            }

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                var expression =
                    expressions[i];

                if (expression == null)
                {
                    continue;
                }

                Debug.Log(
                    $"[{nameof(VNCharacterExpressionLibrary)}] [{i}] Expression='{expression.ExpressionKey}', DefaultVariant='{expression.DefaultVariantKey}', Variants={expression.VariantCount}.",
                    this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SanitizeAndSort();
        }
#endif

        private void EnsureDefaultExpression()
        {
            if (!string.IsNullOrEmpty(
                    DefaultExpressionKey) &&
                TryGetExpression(
                    DefaultExpressionKey,
                    out _))
            {
                return;
            }

            var first =
                GetFirstValidExpression();

            defaultExpressionKey =
                first != null
                    ? first.ExpressionKey
                    : string.Empty;
        }

        private VNCharacterExpressionDefinition
            GetFirstValidExpression()
        {
            if (expressions == null)
            {
                return null;
            }

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                if (expressions[i] != null)
                {
                    return expressions[i];
                }
            }

            return null;
        }

        private static void CompleteResult(
            VNCharacterExpressionResolveResult result,
            VNCharacterExpressionDefinition expression,
            VNCharacterSpriteVariant variant,
            VNCharacterVisualFallbackLevel fallback,
            string message)
        {
            result.success = true;
            result.resolvedExpressionKey =
                expression.ExpressionKey;
            result.resolvedVariantKey =
                variant.VariantKey;
            result.sprite =
                variant.Sprite;
            result.fallbackLevel =
                fallback;
            result.message =
                message ?? string.Empty;
        }

        private void WarnFallbackOnce(
            VNCharacterExpressionResolveResult result)
        {
            if (!warnOnRuntimeFallback ||
                result == null ||
                result.fallbackLevel ==
                    VNCharacterVisualFallbackLevel
                        .ExactVariant)
            {
                return;
            }

            var warningKey =
                $"{result.requestedExpressionKey}|{result.requestedVariantKey}|{result.fallbackLevel}";

            if (!_warnedFallbackRequests.Add(
                    warningKey))
            {
                return;
            }

            Debug.LogWarning(
                $"[{nameof(VNCharacterExpressionLibrary)}] Visual fallback for Actor='{ActorKey}'. {result}",
                this);
        }

        private void ValidateExpression(
            VNCharacterExpressionDefinition expression,
            int index,
            HashSet<string> expressionKeys,
            ref int issueCount)
        {
            if (expression == null)
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Expressions[{index}] is null.",
                    this);
                return;
            }

            if (string.IsNullOrEmpty(
                    expression.ExpressionKey))
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Expressions[{index}] has an empty key.",
                    this);
            }
            else if (!expressionKeys.Add(
                         expression.ExpressionKey))
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Duplicate expression key '{expression.ExpressionKey}'.",
                    this);
            }

            var variantKeys =
                new HashSet<string>(
                    StringComparer.Ordinal);

            if (expression.Variants == null ||
                expression.Variants.Count == 0)
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Expression '{expression.ExpressionKey}' has no variants.",
                    this);
                return;
            }

            for (var variantIndex = 0;
                 variantIndex <
                     expression.Variants.Count;
                 variantIndex++)
            {
                var variant =
                    expression.Variants[
                        variantIndex];

                if (variant == null)
                {
                    issueCount++;

                    Debug.LogWarning(
                        $"[{nameof(VNCharacterExpressionLibrary)}] Expression '{expression.ExpressionKey}' variant [{variantIndex}] is null.",
                        this);
                    continue;
                }

                if (string.IsNullOrEmpty(
                        variant.VariantKey))
                {
                    issueCount++;

                    Debug.LogWarning(
                        $"[{nameof(VNCharacterExpressionLibrary)}] Expression '{expression.ExpressionKey}' has a variant with an empty key.",
                        this);
                }
                else if (!variantKeys.Add(
                             variant.VariantKey))
                {
                    issueCount++;

                    Debug.LogWarning(
                        $"[{nameof(VNCharacterExpressionLibrary)}] Duplicate variant key '{expression.ExpressionKey}/{variant.VariantKey}'.",
                        this);
                }

                if (variant.Sprite == null)
                {
                    issueCount++;

                    Debug.LogWarning(
                        $"[{nameof(VNCharacterExpressionLibrary)}] Variant '{expression.ExpressionKey}/{variant.VariantKey}' has no Sprite.",
                        this);
                }
            }

            if (!string.IsNullOrEmpty(
                    expression.DefaultVariantKey) &&
                !expression.TryGetVariant(
                    expression.DefaultVariantKey,
                    out _))
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNCharacterExpressionLibrary)}] Expression '{expression.ExpressionKey}' default variant '{expression.DefaultVariantKey}' does not exist.",
                    this);
            }
        }

        private int GetTotalVariantCount()
        {
            if (expressions == null)
            {
                return 0;
            }

            var count = 0;

            for (var i = 0;
                 i < expressions.Count;
                 i++)
            {
                count +=
                    expressions[i] != null
                        ? expressions[i]
                            .VariantCount
                        : 0;
            }

            return count;
        }

        private static int CompareExpressions(
            VNCharacterExpressionDefinition left,
            VNCharacterExpressionDefinition right)
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
                left.ExpressionKey,
                right.ExpressionKey,
                StringComparison.Ordinal);
        }
    }
}
