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
            [Tooltip("Untuk interaksi quest utama, ambil shot pemain dan NPC yang diajak bicara.")]
            public bool kameraQuestUtama;
        }

        [SerializeField] private Entry[] percakapan = Array.Empty<Entry>();

        internal static GmVnDialoguePilot Resolve(string title) => Resolve(title, out _);

        internal static GmVnDialoguePilot Resolve(string title, out bool frameWorldPair)
        {
            frameWorldPair = false;
            var catalog = Resources.Load<GmVnDialogueCatalog>(ResourcePath);
            if (catalog == null)
            {
                // Projects with the original pilot can keep their existing fishing setup.
                return title == GmFishingQuestDialogue.LoanConversation
                    ? Resources.Load<GmVnDialoguePilot>(GmVnDialoguePilot.ResourcePath) : null;
            }

            if (catalog.percakapan == null || string.IsNullOrEmpty(title)) return null;
            foreach (var entry in catalog.percakapan)
            {
                if (!string.Equals(entry.judul, title, StringComparison.Ordinal)) continue;
                frameWorldPair = entry.kameraQuestUtama;
                return entry.profil;
            }
            return null;
        }
    }
}
