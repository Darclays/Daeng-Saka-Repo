using System;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [CreateAssetMenu(menuName = "GamecommMentor/Daeng Saka/Dialogue/Daftar Dialog VN")]
    public sealed class GmVnDialogueCatalog : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Dialogue/DaftarDialogVN";

        [Serializable]
        private struct Entry
        {
            [Tooltip("Judul lengkap Conversation di database Pixel Crushers.")]
            public string judul;
            public GmVnDialoguePilot profil;
        }

        [SerializeField] private Entry[] percakapan = Array.Empty<Entry>();

        internal static GmVnDialoguePilot Resolve(string title)
        {
            var catalog = Resources.Load<GmVnDialogueCatalog>(ResourcePath);
            if (catalog == null)
            {
                // Projects with the original pilot can keep their existing fishing setup.
                return title == GmFishingQuestDialogue.LoanConversation
                    ? Resources.Load<GmVnDialoguePilot>(GmVnDialoguePilot.ResourcePath) : null;
            }

            if (catalog.percakapan == null || string.IsNullOrEmpty(title)) return null;
            foreach (var entry in catalog.percakapan)
                if (string.Equals(entry.judul, title, StringComparison.Ordinal)) return entry.profil;
            return null;
        }
    }
}
