using System;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    [Serializable]
    public sealed class VNCharacterExpressionResolveResult
    {
        public bool success;
        public string requestedExpressionKey;
        public string requestedVariantKey;
        public string resolvedExpressionKey;
        public string resolvedVariantKey;
        public Sprite sprite;
        public VNCharacterVisualFallbackLevel fallbackLevel;
        public string message;

        public override string ToString()
        {
            return
                $"Success={success}, Requested='{requestedExpressionKey}/{requestedVariantKey}', Resolved='{resolvedExpressionKey}/{resolvedVariantKey}', Fallback={fallbackLevel}, Sprite='{(sprite != null ? sprite.name : "<null>")}', Message='{message}'";
        }
    }
}
