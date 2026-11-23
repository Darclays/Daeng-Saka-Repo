using System;
using PixelCrushers.DialogueSystem;
using ProjectVN.Presentation.Actors;
using UnityEngine;
using DialogueCharacterInfo = PixelCrushers.DialogueSystem.CharacterInfo;

namespace GamecommMentor.DaengSaka
{
    [CreateAssetMenu(menuName = "GamecommMentor/Daeng Saka/Dialogue/Daftar Dialog VN")]
    public sealed class GmVnDialogueCatalog : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Dialogue/DaftarDialogVN";
        private const string PortraitFolder = "DaengSaka/Dialogue";
        private const string DefaultProfilePath = PortraitFolder + "/ProfilDialogDemo";
        private static VNActorVisualProfile[] sharedProfiles;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedProfiles() => sharedProfiles = null;

        internal static GmVnDialoguePilot Resolve(string title) => Resolve(title, out _);

        internal static GmVnDialoguePilot Resolve(string title, out bool frameWorldPair) =>
            Resolve(title, out frameWorldPair, out _);

        private static GmVnDialoguePilot Resolve(string title, out bool frameWorldPair, out bool registered)
        {
            frameWorldPair = registered = false;
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
                registered = true;
                frameWorldPair = entry.kameraQuestUtama;
                return entry.profil;
            }
            return null;
        }

        internal static GmVnDialoguePilot PresentationFor(string title, DialogueDatabase database)
        {
            var configured = Resolve(title, out _, out bool registered);
            if (configured != null || registered || title?.StartsWith("DaengSaka/", StringComparison.Ordinal) != true)
                return configured;

            var fallback = Resources.Load<GmVnDialoguePilot>(DefaultProfilePath);
            var conversation = database?.GetConversation(title);
            if (fallback == null || !fallback.UsesSpeakerPortraits || conversation?.dialogueEntries == null)
                return null;

            foreach (var line in conversation.dialogueEntries)
            {
                if (string.IsNullOrWhiteSpace(line.DialogueText)) continue;
                var actor = database.GetActor(line.ActorID);
                if (actor == null) continue;
                if (actor.spritePortrait != null || actor.portrait != null) return fallback;
                if (SharedSpeaker(actor.id, actor.Name) != null) return fallback;
            }
            return null;
        }

        internal static VNActorVisualProfile SharedSpeaker(DialogueCharacterInfo info) => info == null ? null :
            SharedSpeaker(info.id, string.IsNullOrWhiteSpace(info.nameInDatabase) ? info.Name : info.nameInDatabase);

        private static VNActorVisualProfile SharedSpeaker(int id, string name)
        {
            // The demo cast chooses the preferred art where an older pilot uses the same actor name.
            var demoActor = Resources.Load<GmVnDialoguePilot>(DefaultProfilePath)?.Speaker(id, name);
            if (HasPortrait(demoActor)) return demoActor;
            sharedProfiles ??= Resources.LoadAll<VNActorVisualProfile>(PortraitFolder);
            foreach (var profile in sharedProfiles)
                if (GmVnDialoguePilot.Matches(profile, id, name) && HasPortrait(profile)) return profile;
            return null;
        }

        private static bool HasPortrait(VNActorVisualProfile profile) => profile != null && profile.HasStageVisual &&
            (profile.DefaultPortraitSprite != null || profile.TryResolveDefaultStageSprite(out _));
    }
}
