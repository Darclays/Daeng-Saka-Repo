using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using ActorCue = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.ActorCue;
using Shot = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.Shot;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmSoreliaRescueCutsceneSetup
    {
        private const string CapitalIntroMenu = "GamecommMentor/Daeng Saka/Set Up Ibu Kota Intro Cutscene";
        private const string CapitalIntroUndo = "Set Up Ibu Kota Intro Cutscene";
        private const string CapitalScene = "Assets/GamecommMentor/Scenes_GamecommMentor/IbuKota.unity";
        private const int CapitalIntroLayoutVersion = 3;
        private const float EntranceSeconds = 9.4f;
        private const float EntranceWalkSeconds = 8.8f;

        [MenuItem(CapitalIntroMenu, true)]
        private static bool CanSetUpCapitalIntro() => CanInstall(CapitalScene);

        [MenuItem(CapitalIntroMenu)]
        private static void SetUpCapitalIntro()
        {
            if (!CanSetUpCapitalIntro()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(CapitalIntroUndo);
            try
            {
                var intro = InstallCapitalIntro(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = intro.gameObject;
                Debug.Log("[Daeng Saka 1E.19B R3] Tur Ibu Kota terdiri dari delapan shot. Simpan IbuKota.", intro);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static GmCapitalIntro InstallCapitalIntro(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            var quest = Require<GmMainQuestProgress>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            var player = Require<GmSakaMotor>(RequireChild(world, "Saka"));
            var playerSprites = player.GetComponentsInChildren<SpriteRenderer>(true);
            if (playerSprites.Length == 0) throw new InvalidOperationException("Sprite Saka belum tersedia di IbuKota.");
            var body = Require<CharacterController>(player.transform);
            var route = GmCapitalRouteSetup.CreateLayout(scene, body);
            var hidden = playerSprites.Concat(route.StartGuide.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
            Transform spawn = RequireChild(world, "Spawn - Start");

            Transform cameraObject = RequireChild(world, "Saka - Orbit Camera");
            var camera = Require<Camera>(cameraObject);
            var orbit = Require<GmOrbitFollowCamera>(cameraObject);
            if (orbit.Target != player.transform)
                throw new InvalidOperationException("Kamera orbit IbuKota belum mengikuti Saka.");
            var hud = Require<Canvas>(RequireChild(world, "Quest Encounter Hint"));
            Transform level = RequireRoot(scene, "LevelDesain");
            var floor = Require<MeshCollider>(RequireChild(level, "GroundKota"));
            Transform city = RequireChild(level, "LingkunganKerajaan");
            Transform gate = RequireChild(city, "3D_IbuKota_Kerajaan_Gapura");
            Transform arrivalGate = RequireChild(city, GmCapitalRouteSetup.ArrivalGateName);
            Transform stairs = RequireChild(city, "3D_IbuKota_Kerajaan_Tangga");
            Transform palace = RequireChild(city, "3D_IbuKota_Kerajaan_Istana");
            Transform statue = RequireChild(level, "3D_IbuKota_Kerajaan_Patung");
            Transform houses = RequireChild(RequireChild(level, "Rumah"), "3D_IbuKota_Rumah_3 (2)");
            Transform tenants = RequireChild(level, "Tenant");
            Transform smith = RequireChild(tenants, "Asset3D_IbuKota_Blacksmith");
            Transform workshop = RequireChild(smith, "Asset3D_IbuKota_Blacksmith_Bangunan");
            Transform emptyStall = RequireChild(tenants, "3D_Ibukota_Tenant_1 (3)");

            Undo.RecordObject(spawn, CapitalIntroUndo);
            bool bodyEnabled = body.enabled;
            body.enabled = false;
            try
            {
                route.Place(spawn, GmCapitalRouteSetup.StartPosition(route), spawn.forward);
            }
            finally
            {
                body.enabled = bodyEnabled;
                Physics.SyncTransforms();
            }

            Transform root = Child(world, "Cutscene - Pembukaan Ibu Kota");
            var director = Component<GmSoreliaRescueCutscene>(root);
            var intro = Component<GmCapitalIntro>(root);
            bool upgradeFirstTour = intro.LayoutVersion < 1;
            var overlay = MakeOverlay(root, out var fade, out var caption);
            ActorCue[] noCues = Array.Empty<ActorCue>();
            var cast = CreateCast(Child(root, "Aktor Cutscene"), playerSprites[0], "Saka", "Daung", "Pacca");

            Vector3 At(Transform landmark, Vector3 offset) =>
                level.TransformPoint(level.InverseTransformPoint(landmark.position) + offset);

            Transform Key(Transform shot, string name, Transform landmark, Vector3 eye, Vector3 focus,
                bool revise = false)
            {
                Vector3 point = At(landmark, eye);
                Vector3 target = At(landmark, focus);
                Transform existing = revise ? shot.Find(name) : null;
                if (existing == null) return CameraMark(shot, name, point, target);

                Undo.RecordObject(existing, CapitalIntroUndo);
                existing.SetPositionAndRotation(point, Quaternion.LookRotation(target - point, Vector3.up));
                return existing;
            }

            Shot Move(string name, Transform landmark, float duration, float lens,
                Vector3 eyeFrom, Vector3 lookFrom, Vector3 eyeTo, Vector3 lookTo, bool revise = false)
            {
                Transform shot = Child(root, name);
                var from = Key(shot, "Kamera Awal", landmark, eyeFrom, lookFrom, revise);
                var to = Key(shot, "Kamera Akhir", landmark, eyeTo, lookTo, revise);
                return new Shot(from, to, duration, lens, noCues);
            }

            Transform entrance = Child(root, "00A - Tim Masuk Ibu Kota");
            Transform arrivalEnd = Child(spawn, "Cutscene - Akhir Kedatangan");
            Transform entranceFrom = Key(entrance, "Kamera Awal", arrivalGate,
                new Vector3(.20f, 2.74f, -9.4f), new Vector3(.05f, .82f, -1.35f));
            Transform entranceTo = Key(arrivalEnd, "Kamera Akhir", spawn,
                new Vector3(.18f, 2.60f, -4.85f), new Vector3(.10f, .72f, 3.2f));

            ActorCue EnterCity(string name, float lane, float startZ, Transform destination)
            {
                Transform from = Mark(entrance, name + " - Awal", At(arrivalGate, new Vector3(lane, .12f, startZ)));
                return new ActorCue(cast[name], from, destination, null, 0f, EntranceWalkSeconds, followGround: true);
            }

            var teamEntry = new Shot(entranceFrom, entranceTo, EntranceSeconds, 64f, new[]
            {
                EnterCity("Saka", -.02f, -4.55f, spawn),
                EnterCity("Daung", -1.10f, -5.63f,
                    Mark(arrivalEnd, "Daung", At(spawn, new Vector3(-1.08f, 0f, -1.08f)))),
                EnterCity("Pacca", 1.14f, -5.86f,
                    Mark(arrivalEnd, "Pacca", At(spawn, new Vector3(1.16f, 0f, -1.31f))))
            });

            Transform smithStreet = Child(root, "00B - Jalan ke Bengkel");
            Transform streetFrom = Key(smithStreet, "Kamera Awal", emptyStall,
                new Vector3(-6.98f, 3.43f, -6.01f), new Vector3(.02f, 2.33f, -4.67f));
            Transform streetTo = Key(smithStreet, "Kamera Akhir", workshop,
                new Vector3(-12.57f, 3.43f, 6.69f), new Vector3(-1.77f, 2.50f, 7.39f));
            var smithTour = new Shot(streetFrom, streetTo, 8.4f, 60f, noCues);

            Shot wide = Move("01 - Gapura dan Pasar", gate, 4.4f, 54f,
                new Vector3(8.1f, 9.14f, 30f), new Vector3(.3f, .3f, 2f),
                new Vector3(4.1f, 8.84f, 29.4f), new Vector3(-2.7f, .3f, 2.3f), revise: upgradeFirstTour);
            Shot arrival = Move("01B - Gerbang Kedatangan", arrivalGate, 3.5f, 60f,
                new Vector3(1.14f, 2.61f, 13.13f), new Vector3(-1.06f, 3.96f, 1.83f),
                new Vector3(-1.06f, 2.91f, 11.28f), new Vector3(-1.96f, 4.06f, 1.75f));
            Shot square = Move("02 - Patung dan Rumah", statue, 4.3f, 58f,
                new Vector3(7.3f, 6.15f, 17.8f), new Vector3(-.4f, 4.1f, -3f),
                new Vector3(3.57f, 6f, 17.13f), new Vector3(-4.08f, 4.25f, -3.67f), revise: upgradeFirstTour);
            Shot neighborhood = Move("02B - Jalan Permukiman", houses, 3.5f, 54f,
                new Vector3(9.53f, 3.78f, -16.17f), new Vector3(-2.47f, 3.08f, -1.87f),
                new Vector3(6.33f, 3.98f, -15.17f), new Vector3(-5.27f, 3.48f, -1.87f));

            Transform guardShot = Child(root, "04 - Tangga dan Penjaga");
            Transform approach = Key(guardShot, "Kamera Awal", stairs,
                new Vector3(0f, 4f, 17.7f), new Vector3(-.08f, 4.15f, -1.5f));
            Transform handoff = Key(guardShot, "Kamera Akhir", stairs,
                new Vector3(0f, 3.58f, 12.98f), new Vector3(-.1f, 4.65f, -1.5f));
            Transform palaceShot = Child(root, "05 - Fasad Istana");
            Transform palaceView = Key(palaceShot, "Kamera Akhir", palace,
                new Vector3(-.16f, -.27f, 36.16f), new Vector3(-.1f, 14.63f, 10f));
            const float palaceLens = 52f;
            var shots = new[]
            {
                teamEntry, smithTour, wide, arrival, square, neighborhood,
                new Shot(approach, handoff, 2.2f, palaceLens, noCues),
                // Continue from the end of the stair shot before tilting up to the palace.
                new Shot(handoff, palaceView, 2.65f, palaceLens, noCues)
            };

            Undo.RecordObject(director, CapitalIntroUndo);
            director.Configure(camera, orbit, floor, hud, overlay.gameObject, fade, caption,
                cast.Values.ToArray(), hidden, shots);
            if (!director.IsConfigured)
                throw new InvalidOperationException("Kamera atau overlay pembukaan IbuKota belum siap.");
            Undo.RecordObject(intro, CapitalIntroUndo);
            intro.Configure(player, quest, director, CapitalIntroLayoutVersion);
            return intro;
        }
    }
}
