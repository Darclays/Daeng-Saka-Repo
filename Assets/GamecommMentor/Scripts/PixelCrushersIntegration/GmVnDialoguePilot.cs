using System;
using ProjectVN.Presentation.Actors;
using UnityEngine;
using UnityEngine.Serialization;
using DialogueCharacterInfo = PixelCrushers.DialogueSystem.CharacterInfo;

namespace GamecommMentor.DaengSaka
{
    public enum GmVnRevealMode { PerKata, PerHuruf }

    [CreateAssetMenu(menuName = "GamecommMentor/Daeng Saka/Dialogue/Profil Percakapan VN")]
    public sealed class GmVnDialoguePilot : ScriptableObject
    {
        internal const string ResourcePath = "DaengSaka/Dialogue/PilotPinjamAlatPancing";

        [Tooltip("Aktifkan tampilan VN untuk percakapan yang memakai profil ini.")]
        [SerializeField] private bool gunakanTampilanVN = true;
        [SerializeField] private VNActorVisualProfile aktorKiri;
        [SerializeField] private VNActorVisualProfile aktorKanan;
        [Tooltip("Atur cara munculnya ucapan dalam percakapan ini.")]
        [SerializeField] private GmVnRevealMode animasiTeks = GmVnRevealMode.PerKata;
        [FormerlySerializedAs("portraitSaatShotDunia")]
        [Tooltip("Daftar profil untuk menampilkan satu portrait pembicara aktif.")]
        [SerializeField] private VNActorVisualProfile[] portraitPembicara = Array.Empty<VNActorVisualProfile>();

        internal bool Enabled => gunakanTampilanVN;
        internal VNActorVisualProfile Left => aktorKiri;
        internal VNActorVisualProfile Right => aktorKanan;
        internal GmVnRevealMode RevealMode => animasiTeks;
        internal bool UsesSpeakerPortraits => portraitPembicara != null && portraitPembicara.Length > 0;

        internal VNActorVisualProfile Speaker(DialogueCharacterInfo info)
        {
            if (info == null) return null;
            return Speaker(info.id, string.IsNullOrWhiteSpace(info.nameInDatabase) ? info.Name : info.nameInDatabase);
        }

        internal VNActorVisualProfile Speaker(int id, string name)
        {
            if (UsesSpeakerPortraits)
            {
                foreach (var profile in portraitPembicara)
                    if (Matches(profile, id, name)) return profile;
            }
            if (Matches(aktorKiri, id, name)) return aktorKiri;
            return Matches(aktorKanan, id, name) ? aktorKanan : null;
        }

        internal static bool Matches(VNActorVisualProfile profile, int id, string key)
        {
            if (profile == null) return false;
            if (profile.PixelActorId > 0 && profile.PixelActorId == id) return true;
            if (string.IsNullOrWhiteSpace(key)) return false;
            key = key.Trim();
            if (string.Equals(profile.PixelActorName, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.ActorKey, key, StringComparison.OrdinalIgnoreCase)) return true;
            return profile.Aliases != null && Array.Exists(profile.Aliases,
                alias => string.Equals(alias, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
