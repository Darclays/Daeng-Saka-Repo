using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCapitalResidentsSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Ibu Kota Residents";
        private const string UndoName = "Set Up Ibu Kota Residents";
        private const string Capital = "Assets/GamecommMentor/Scenes_GamecommMentor/IbuKota.unity";
        private const string AcoSource = "Assets/Asset2D_Pixel/SupportCharacter/SC_DaengAco/" +
            "SC_DaengAco_Idle/SC_DaengAco_Idle_Front.png";
        private const string JamuSource = NpcArt + "NPC_TukangJamu/NPC_TukangJamu_Idle/NPC_TukangJamu_Idle_Front.png";
        private const string FisherSource = NpcArt + "NPC_Nelayan/NPC_Nelayan_Ayah/" +
            "NPC_Nelayan_Ayah_Idle/NPC_Nelayan_Ayah_Idle_Front.png";
        private const string GirlSource = NpcArt + "NPC_Commoner/NPC_Commoner_AnakP/" +
            "NPC_Commoner_AnakP_Idle/NPC_Commoner_AnakP_Idle_Front.png";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => GmAmbientNpcPlacement.CanInstall(Capital);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (CanInstall()) Run(UndoName, InstallInScene);
        }

        private static Transform InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform places = RequireRoot(scene, "NPC_Places");
            Transform merchant = RequireChild(world, "Merchant");
            var renderer = Require<SpriteRenderer>(RequireChild(merchant, "Visual"));
            var body = Require<CharacterController>(RequireChild(world, "Saka"));
            var layout = new GmWorldSceneLayout(scene, "PortalPoint_StartRespawnPointIbuKota",
                "PortalPoint_GoDesaSoralia", body, "GroundKota", "Ground Kerajaan");
            var reserved = new[]
            {
                RequireChild(world, "Spawn - Start").position,
                RequireChild(world, "Pemandu Kerajaan (Uji)").position,
                merchant.position,
                RequireChild(world, "Penempa Besi").position,
                RequireChild(world, "Portal").position
            };

            var guard = PrepareGuard();
            var mother = PrepareCommonerMother();
            var father = PrepareCommonerFather();
            var girl = GmDialogueNpcSprites.Prepare(GirlSource, "IbuKota_AnakWarga");
            var jamu = GmDialogueNpcSprites.Prepare(JamuSource, "IbuKota_TukangJamu");
            var fisher = GmDialogueNpcSprites.Prepare(FisherSource, "Sorelia_Nelayan_Ayah");
            var aco = GmDialogueNpcSprites.Prepare(AcoSource, "IbuKota_DaengAco");
            var residents = new GmAmbientNpcPlacement(world, "Warga Ibu Kota", layout, renderer, reserved, UndoName);
            void Add(string name, string marker, GmDialogueNpcSprites.Prepared art, float height, float radius)
            {
                if (residents.Group.Find(name) != null) return;

                Transform point = places.Find(marker);
                if (point == null)
                {
                    string alternateName = marker.Replace("NPCPlace_", "NPCPlaces_");
                    point = System.Array.Find(places.GetComponentsInChildren<Transform>(true), candidate =>
                        candidate != places &&
                        (string.Equals(candidate.name.Trim(), marker, System.StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(candidate.name.Trim(), alternateName, System.StringComparison.OrdinalIgnoreCase)));
                }

                if (point != null)
                {
                    residents.Add(name, point.position, art, height, radius);
                    return;
                }

                Vector3? fallback = marker switch
                {
                    "NPCPlace_RoyalGuard" => new Vector3(10.27f, 1.1099993f, -12.658998f),
                    "NPCPlace_RoyalGuard (1)" => new Vector3(18.521f, 1.1099993f, -12.658998f),
                    _ => (Vector3?)null
                };

                if (fallback.HasValue)
                {
                    residents.Add(name, places.TransformPoint(fallback.Value), art, height, radius);
                    Debug.LogWarning($"[Daeng Saka] Marker '{marker}' belum ditemukan. {name} memakai titik cadangan.", places);
                    return;
                }

                Debug.LogWarning($"[Daeng Saka] Marker '{marker}' belum ditemukan. Setup melanjutkan ke NPC berikutnya.", places);
            }

            Add("Penjaga Gerbang", "NPCPlace_RoyalGuard_DepanKota", guard, 1.65f, 0.24f);
            Add("Penjaga Jalur Sorelia", "NPCPlace_RoyalGuard_BelakangKota", guard, 1.65f, 0.24f);
            Add("Warga Ibu", "NPCPlace_Commoner_Ibu", mother, 1.65f, 0.22f);
            Add("Anak Warga", "NPCPlace_Commoner_AnakP", girl, 1.40f, 0.18f);
            Add("Penjaga Kerajaan 1", "NPCPlace_RoyalGuard", guard, 1.65f, 0.24f);
            Add("Penjaga Kerajaan 2", "NPCPlace_RoyalGuard (1)", guard, 1.65f, 0.24f);
            Add("Warga Bapak", "NPCPlace_Commoner_Bapak", father, 1.85f, 0.23f);
            Add("Nelayan Bapak", "NPCPlace_Nelayan_Bapak", fisher, 1.65f, 0.24f);
            Add("Tukang Jamu", "NPCPlace_TukangJamu", jamu, 1.70f, 0.23f);
            Add("Daeng Aco", "NPCPlace_DaengAco", aco, 1.90f, 0.24f);
            return residents.Group;
        }
    }
}
