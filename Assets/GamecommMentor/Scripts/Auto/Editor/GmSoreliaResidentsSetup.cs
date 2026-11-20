using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmSoreliaResidentsSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Sorelia Residents";
        private const string UndoName = "Set Up Sorelia Residents";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string GuardMarker = "NPCPlaces_RoyalGuard";
        private const string BoySource = NpcArt + "NPC_Commoner/NPC_Commoner_AnakL/" +
            "NPC_Commoner_AnakL_Idle/NPC_Commoner_AnakL_Idle_Front.png";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => GmAmbientNpcPlacement.CanInstall(Village);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (CanInstall()) Run(UndoName, InstallInScene);
        }

        private static Transform InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            var rescue = Require<GmSoreliaRescue>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            Transform after = RequireRoot(scene, "NPCPlaces_AfterInvade");
            Transform family = RequireChild(world, "Keluarga Nelayan");
            Transform safe = RequireChild(RequireChild(family, "Posisi Keluarga"), "Setelah Ditolong");
            Transform father = RequireChild(world, "Nelayan (Uji)");
            var renderer = Require<SpriteRenderer>(RequireChild(father, "Visual"));
            var body = Require<CharacterController>(RequireChild(world, "Saka"));
            var layout = new GmWorldSceneLayout(scene, "PortalPoint_StartRespawnDesaSoralia",
                "PortalPoint_GoGoaLembayung", body);

            var reserved = new List<Vector3>
            {
                RequireChild(world, "Spawn - Start").position,
                RequireChild(world, "Portal").position,
                RequireChild(safe, "Ayah").position,
                RequireChild(safe, "Ibu").position,
                RequireChild(safe, "Anak").position
            };
            foreach (string name in new[] { "Spawn - Dari Goa", "Fishing Spot" })
            {
                Transform point = world.Find(name);
                if (point != null) reserved.Add(point.position);
            }

            Transform[] guards = Enumerable.Range(0, after.childCount).Select(after.GetChild)
                .Where(marker => marker.name == GuardMarker ||
                    marker.name.StartsWith(GuardMarker + " (", StringComparison.Ordinal))
                .OrderBy(marker => marker.position.x).ThenBy(marker => marker.position.z).ToArray();
            Vector3 adults = RequireChild(after, "NPCPlaces_Commoner_IbuBapak").position;
            Vector3 child = RequireChild(after, "NPCPlaces_Commoner_Anak").position;
            Vector3 spacing = Vector3.Cross(Vector3.up, layout.IntoLevel) * 1.1f;
            var guardArt = PrepareGuard();
            var motherArt = PrepareCommonerMother();
            var fatherArt = PrepareCommonerFather();
            var boyArt = GmDialogueNpcSprites.Prepare(BoySource, "Sorelia_AnakWarga");

            var residents = new GmAmbientNpcPlacement(world, "Warga Sorelia Setelah Ditolong", layout, renderer,
                reserved.ToArray(), UndoName);
            GameObject group = residents.Group.gameObject;
            Undo.RecordObject(group, UndoName);
            group.SetActive(true);
            try
            {
                residents.Add("Warga Ibu", adults + spacing, motherArt, 1.65f, 0.22f);
                residents.Add("Warga Bapak", adults - spacing, fatherArt, 1.85f, 0.23f);
                residents.Add("Anak Warga", child, boyArt, 1.50f, 0.18f);
                for (int i = 0; i < guards.Length; i++)
                    residents.Add($"Penjaga {i + 1}", guards[i].position, guardArt, 1.65f, 0.24f);
            }
            finally
            {
                group.SetActive(false);
            }
            Undo.RecordObject(rescue, UndoName);
            rescue.SetPostRescueResidents(group);
            return residents.Group;
        }
    }
}
