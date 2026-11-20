using System;
using ProjectVN.Presentation.Actors;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [CreateAssetMenu(menuName = "GamecommMentor/Daeng Saka/Dialogue/Arahan Ekspresi VN")]
    public sealed class GmVnPortraitCues : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Dialogue/ArahanEkspresiVN";

        [Serializable]
        private struct LineCue
        {
            [Tooltip("Judul lengkap Conversation dari Pixel Crushers.")]
            public string percakapan;
            [Min(1)]
            [Tooltip("ID Dialogue Entry yang mendapat ekspresi, sesuai Dialogue Editor.")]
            public int idEntri;
            [Tooltip("Actor Key pada VNActorVisualProfile pemeran di panggung.")]
            public string aktor;
            [Tooltip("Expression Key pada Character Expression Library aktor.")]
            public string ekspresi;
            [Tooltip("Opsional. Variant Key pada ekspresi tersebut.")]
            public string varian;
            [Tooltip("Sprite netral prototipe yang menjadi pasangan pose sementara.")]
            public Sprite netralSementara;
            [Tooltip("Pose prototipe yang dipakai selama sprite netralnya masih cocok.")]
            public Sprite poseSementara;
        }

        [SerializeField] private LineCue[] arahan = Array.Empty<LineCue>();

        internal Sprite Resolve(string conversation, int entryId, VNActorVisualProfile actor, Sprite defaultSprite)
        {
            if (arahan == null || string.IsNullOrWhiteSpace(conversation) || entryId < 1 || actor == null)
                return defaultSprite;

            string actorKey = actor.ActorKey;
            if (string.IsNullOrEmpty(actorKey)) return defaultSprite;

            foreach (var cue in arahan)
            {
                if (cue.idEntri != entryId || !string.Equals(cue.percakapan, conversation, StringComparison.Ordinal) ||
                    !string.Equals(cue.aktor, actorKey, StringComparison.OrdinalIgnoreCase)) continue;

                if (!string.IsNullOrWhiteSpace(cue.ekspresi) &&
                    actor.TryResolveExpressionSprite(cue.ekspresi, cue.varian, out var resolved) &&
                    (resolved.fallbackLevel == VNCharacterVisualFallbackLevel.ExactVariant ||
                     resolved.fallbackLevel == VNCharacterVisualFallbackLevel.RequestedExpressionDefault))
                    return resolved.sprite;

                if (cue.poseSementara != null && cue.netralSementara != null && cue.netralSementara == defaultSprite)
                    return cue.poseSementara;
                return defaultSprite;
            }
            return defaultSprite;
        }
    }
}
