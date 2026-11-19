using System;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    public enum VNActorFacingPreference
    {
        Unspecified = 0,
        FaceLeft = 1,
        FaceRight = 2
    }

    [CreateAssetMenu(
        fileName = "ACT_NewActor",
        menuName = "ProjectVN/Actors/Actor Visual Profile")]
    public sealed class VNActorVisualProfile : ScriptableObject
    {
        [Header("Stable ProjectVN Identity")]
        [Tooltip("Stable lowercase internal key, for example: linda, nenek, bobby.")]
        [SerializeField] private string actorKey;

        [Header("Pixel Crushers Mapping")]
        [SerializeField] private int pixelActorId;
        [SerializeField] private string pixelActorName;
        [SerializeField] private string[] aliases;

        [Header("Display")]
        [SerializeField] private string displayNameOverride;
        [SerializeField] private bool isProtagonist;
        [SerializeField] private bool hasStageVisual = true;

        [Header("Visual Assets")]
        [SerializeField] private GameObject stageVisualPrefab;
        [SerializeField] private Sprite defaultStageSprite;
        [SerializeField] private Sprite defaultPortraitSprite;

        [Header("Character Expressions")]
        [Tooltip("Optional data-driven expression and sprite-variant library for this actor.")]
        [SerializeField]
        private VNCharacterExpressionLibrary
            characterExpressionLibrary;

        [Header("Future Stage Defaults")]
        [SerializeField] private string preferredAnchorHint;
        [Min(0.01f)]
        [SerializeField] private float defaultScale = 1f;
        [SerializeField] private Vector2 defaultLocalOffset;
        [SerializeField] private VNActorFacingPreference defaultFacing;
        [SerializeField] private int defaultDepthBias;

        public string ActorKey => VNActorIdentity.NormalizeKey(actorKey);
        public int PixelActorId => pixelActorId;
        public string PixelActorName => Sanitize(pixelActorName);
        public string DisplayNameOverride => Sanitize(displayNameOverride);
        public string[] Aliases => aliases;
        public bool IsProtagonist => isProtagonist;
        public bool HasStageVisual => hasStageVisual;
        public GameObject StageVisualPrefab => stageVisualPrefab;
        public Sprite DefaultStageSprite => defaultStageSprite;
        public Sprite DefaultPortraitSprite => defaultPortraitSprite;

        public VNCharacterExpressionLibrary
            CharacterExpressionLibrary =>
                characterExpressionLibrary;

        public bool HasCharacterExpressionLibrary =>
            characterExpressionLibrary != null;

        public string PreferredAnchorHint => Sanitize(preferredAnchorHint);
        public float DefaultScale => Mathf.Max(0.01f, defaultScale);
        public Vector2 DefaultLocalOffset => defaultLocalOffset;
        public VNActorFacingPreference DefaultFacing => defaultFacing;
        public int DefaultDepthBias => defaultDepthBias;

        public string GetResolvedDisplayName(VNActorIdentity identity)
        {
            if (!string.IsNullOrWhiteSpace(DisplayNameOverride))
            {
                return DisplayNameOverride;
            }

            if (!string.IsNullOrWhiteSpace(identity.DisplayName))
            {
                return identity.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(identity.PixelActorName))
            {
                return identity.PixelActorName;
            }

            return PixelActorName;
        }

        public bool TryResolveDefaultStageSprite(
            out VNCharacterExpressionResolveResult
                result)
        {
            if (characterExpressionLibrary != null &&
                characterExpressionLibrary
                    .TryResolveDefaultSprite(
                        out result))
            {
                return true;
            }

            result =
                new VNCharacterExpressionResolveResult
                {
                    success =
                        defaultStageSprite != null,

                    requestedExpressionKey =
                        string.Empty,

                    requestedVariantKey =
                        string.Empty,

                    resolvedExpressionKey =
                        string.Empty,

                    resolvedVariantKey =
                        string.Empty,

                    sprite =
                        defaultStageSprite,

                    fallbackLevel =
                        defaultStageSprite != null
                            ? VNCharacterVisualFallbackLevel
                                .LegacyDefaultSprite
                            : VNCharacterVisualFallbackLevel
                                .Unresolved,

                    message =
                        defaultStageSprite != null
                            ? "Legacy Default Stage Sprite resolved."
                            : "No expression-library default or legacy Default Stage Sprite is available."
                };

            return result.success;
        }

        public bool TryResolveExpressionSprite(
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
                return TryResolveDefaultStageSprite(
                    out result);
            }

            if (characterExpressionLibrary != null &&
                characterExpressionLibrary.TryResolveSprite(
                    expressionKey,
                    variantKey,
                    out result))
            {
                return true;
            }

            result =
                new VNCharacterExpressionResolveResult
                {
                    success =
                        defaultStageSprite != null,

                    requestedExpressionKey =
                        VNCharacterVisualKeyUtility
                            .NormalizeKey(
                                expressionKey),

                    requestedVariantKey =
                        VNCharacterVisualKeyUtility
                            .NormalizeKey(
                                variantKey),

                    resolvedExpressionKey =
                        string.Empty,

                    resolvedVariantKey =
                        string.Empty,

                    sprite =
                        defaultStageSprite,

                    fallbackLevel =
                        defaultStageSprite != null
                            ? VNCharacterVisualFallbackLevel
                                .LegacyDefaultSprite
                            : VNCharacterVisualFallbackLevel
                                .Unresolved,

                    message =
                        defaultStageSprite != null
                            ? "Expression library did not resolve the request. Legacy Default Stage Sprite was used."
                            : "No expression library sprite or legacy Default Stage Sprite is available."
                };

            return result.success;
        }

        [ContextMenu(
            "ProjectVN Actors/Validate Visual Profile")]
        public void ValidateVisualProfile()
        {
            var issueCount = 0;

            if (string.IsNullOrEmpty(
                    ActorKey))
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNActorVisualProfile)}] Actor Key is empty.",
                    this);
            }

            if (characterExpressionLibrary != null)
            {
                if (!string.IsNullOrEmpty(
                        characterExpressionLibrary
                            .ActorKey) &&
                    !string.Equals(
                        ActorKey,
                        characterExpressionLibrary
                            .ActorKey,
                        StringComparison.Ordinal))
                {
                    issueCount++;

                    Debug.LogWarning(
                        $"[{nameof(VNActorVisualProfile)}] Expression Library actor key '{characterExpressionLibrary.ActorKey}' does not match profile actor key '{ActorKey}'.",
                        this);
                }

                characterExpressionLibrary
                    .ValidateAndLog();
            }
            else if (hasStageVisual &&
                     defaultStageSprite == null &&
                     stageVisualPrefab == null)
            {
                issueCount++;

                Debug.LogWarning(
                    $"[{nameof(VNActorVisualProfile)}] Actor '{ActorKey}' has a stage visual but no Expression Library, Default Stage Sprite, or Stage Visual Prefab.",
                    this);
            }

            if (issueCount == 0)
            {
                Debug.Log(
                    $"[{nameof(VNActorVisualProfile)}] Validation PASS. Actor='{ActorKey}', ExpressionLibrary='{(characterExpressionLibrary != null ? characterExpressionLibrary.name : "<none>")}'.",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(VNActorVisualProfile)}] Validation found {issueCount} profile-level issue(s). Actor='{ActorKey}'.",
                    this);
            }
        }

        public bool HasAlias(string value)
        {
            var normalized = VNActorIdentity.NormalizeLookup(value);

            if (string.IsNullOrEmpty(normalized) || aliases == null)
            {
                return false;
            }

            for (var i = 0; i < aliases.Length; i++)
            {
                if (VNActorIdentity.NormalizeLookup(aliases[i]) == normalized)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            actorKey = VNActorIdentity.NormalizeKey(actorKey);
            pixelActorName = Sanitize(pixelActorName);
            displayNameOverride = Sanitize(displayNameOverride);
            preferredAnchorHint = Sanitize(preferredAnchorHint);
            defaultScale = Mathf.Max(0.01f, defaultScale);

            if (aliases == null)
            {
                aliases = Array.Empty<string>();
                return;
            }

            for (var i = 0; i < aliases.Length; i++)
            {
                aliases[i] = Sanitize(aliases[i]);
            }
        }
#endif

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
