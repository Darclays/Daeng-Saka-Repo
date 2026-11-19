using UnityEditor;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmEquipmentPlaytest
    {
        private const string GrantWeapons = "GamecommMentor/Daeng Saka/Playtest/Tambah Senjata Tempaan";

        [MenuItem(GrantWeapons, true)]
        private static bool CanGrant() => Application.isPlaying;

        [MenuItem(GrantWeapons)]
        private static void Grant()
        {
            if (!CanGrant()) return;
            GmPartyEquipment equipment = GmPartyRun.Current.Equipment;
            int added = 0;
            foreach (var definition in equipment.Definitions)
                if (!definition.IsStarter && equipment.TryGrant(definition.Id)) added++;
            Debug.Log($"[Daeng Saka] {added} senjata tempaan masuk ke Tas. Lihat Perlengkapan saat eksplorasi.");
        }
    }
}
