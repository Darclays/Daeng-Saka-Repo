using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using ActorCue = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.ActorCue;
using Shot = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.Shot;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmSoreliaRescueCutsceneSetup
    {
        private const string InvasionMenu = "GamecommMentor/Daeng Saka/Set Up Sorelia Invasion Cutscene";
        private const string InvasionUndo = "Set Up Sorelia Invasion Cutscene";
        private const string InvasionGuardGroup = "Penjagaan Sorelia";
        private const string InvasionGuard = "Penjaga Jalan";

        [MenuItem(InvasionMenu, true)]
        private static bool CanSetUpInvasion() => CanInstall(Village);

        [MenuItem(InvasionMenu)]
        private static void SetUpInvasion()
        {
            if (!CanSetUpInvasion()) return;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(InvasionUndo);
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                var intro = InstallInvasion(scene);
                InstallInvasionHandoff(scene, intro.transform);
                EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = intro.gameObject;
                Debug.Log("[Daeng Saka 1E.15B R1] Cutscene, penjaga, dan transisi battle Poppo siap. Simpan DesaSorelia.", intro);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
            }
        }

        private static GmSoreliaInvasionIntro InstallInvasion(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform root = Child(world, "Cutscene - Sorelia Saat Invasi");
            var director = Component<GmSoreliaRescueCutscene>(root);
            var intro = Component<GmSoreliaInvasionIntro>(root);
            if (director.IsConfigured && intro.IsConfigured) return intro;

            var player = Require<GmSakaMotor>(RequireChild(world, "Saka"));
            Transform arrival = RequireChild(world, "Spawn - Start");
            var quest = Require<GmMainQuestProgress>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            Transform level = RequireRoot(scene, "LevelDesign");
            var terrain = Require<TerrainCollider>(RequireChild(level, "Terrain"));
            Transform cameraObject = RequireChild(world, "Saka - Orbit Camera");
            var camera = Require<Camera>(cameraObject);
            var orbit = Require<GmOrbitFollowCamera>(cameraObject);
            var hud = Require<Canvas>(RequireChild(world, "Quest Encounter Hint"));
            Transform poppo = RequireChild(world, "Poppo (Uji)");
            Transform family = RequireChild(world, "Keluarga Nelayan");
            Transform danger = RequireChild(RequireChild(family, "Posisi Keluarga"), "Saat Diserang");
            Transform mother = RequireChild(danger, "Ibu");
            var heroArt = player.GetComponentInChildren<SpriteRenderer>(true);
            var poppoArt = poppo.GetComponentInChildren<SpriteRenderer>(true);
            if (heroArt == null || poppoArt == null || poppoArt.sprite == null)
                throw new InvalidOperationException("Visual Saka atau Poppo belum tersedia di DesaSorelia.");

            Transform cast = Child(root, "Aktor Cutscene");
            var actors = CreateCast(cast, heroArt, "Saka", "Daung", "Pacca", "CommonerAnakL",
                "NelayanIbu", "NelayanAnak", "RoyalGuard");
            actors.Add("Poppo", CreateActor(cast, "Poppo", poppoArt,
                new GmCutsceneSprite.Directions { front = new[] { poppoArt.sprite } }, default));
            Undo.RecordObject(actors["Poppo"].transform, InvasionUndo);
            actors["Poppo"].transform.localScale = poppoArt.transform.lossyScale;

            Shot[] shots = BuildInvasionShots(root, terrain, arrival, mother.position, poppo.position, actors);
            Transform overlay = MakeOverlay(root, out Image fade, out Text caption);
            SpriteRenderer[] hidden = new[] { player.transform, RequireChild(world, "Nelayan (Uji)"), family, poppo }
                .SelectMany(actor => actor.GetComponentsInChildren<SpriteRenderer>(true)).Distinct().ToArray();
            Undo.RecordObject(director, InvasionUndo);
            director.Configure(camera, orbit, terrain, hud, overlay.gameObject, fade, caption,
                actors.Values.ToArray(), hidden, shots);
            Undo.RecordObject(intro, InvasionUndo);
            intro.Configure(player, arrival, quest, director);
            if (!director.IsConfigured)
                throw new InvalidOperationException("Ada kamera, aktor, atau jalur cutscene invasi yang belum siap.");
            return intro;
        }

        private static void InstallInvasionHandoff(Scene scene, Transform intro)
        {
            Transform world = intro.parent;
            Transform player = RequireChild(world, "Saka");
            Transform poppo = RequireChild(world, "Poppo (Uji)");
            var poppoArt = Require<SpriteRenderer>(RequireChild(poppo, "Visual"));
            var rescue = Require<GmSoreliaRescue>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            Undo.RecordObject(rescue, InvasionUndo);
            rescue.SetEncounterHandoff(Require<GmSakaMotor>(player), poppoArt.transform);

            Transform guard = world.Find(InvasionGuardGroup)?.Find(InvasionGuard);
            if (guard == null)
            {
                Transform panel = RequireChild(intro, "Panel 5 - Poppo Menghadang");
                Transform spot = RequireChild(RequireChild(panel, "Royal Guard"), "Awal");
                var layout = new GmWorldSceneLayout(scene, "PortalPoint_StartRespawnDesaSoralia",
                    "PortalPoint_GoGoaLembayung", Require<CharacterController>(player));
                var father = RequireChild(world, "Nelayan (Uji)");
                var residents = new GmAmbientNpcPlacement(world, InvasionGuardGroup, layout,
                    Require<SpriteRenderer>(RequireChild(father, "Visual")),
                    new[] { father.position, poppo.position }, InvasionUndo);
                residents.Add(InvasionGuard, spot.position, PrepareGuard(), 1.65f, .24f);
                guard = RequireChild(residents.Group, InvasionGuard);
            }

            var guardArt = Require<SpriteRenderer>(RequireChild(guard, "Visual"));
            var opening = Require<GmSoreliaRescueCutscene>(intro);
            Undo.RecordObject(opening, InvasionUndo);
            opening.HideDuringCutscene(guardArt);
            var ending = world.Find("Cutscene - Sorelia Setelah Invasi")?.GetComponent<GmSoreliaRescueCutscene>();
            if (ending != null)
            {
                Undo.RecordObject(ending, InvasionUndo);
                ending.HideDuringCutscene(guardArt);
            }
        }

        private static Shot[] BuildInvasionShots(Transform root, TerrainCollider terrain, Transform arrival,
            Vector3 mother, Vector3 poppo, Dictionary<string, GmCutsceneSprite> actors)
        {
            Vector3 forward = Flat(arrival.forward).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 Road(float distance, float lane) => Ground(terrain, arrival.position + forward * distance + side * lane);

            ActorCue Cue(Transform panel, string key, Vector3 start, Vector3 end, Transform face = null,
                float delay = 0f, float duration = 0f)
            {
                Transform track = Child(panel, actors[key].gameObject.name);
                Transform from = Mark(track, "Awal", start);
                Transform to = duration > 0f ? Mark(track, "Akhir", end) : from;
                return new ActorCue(actors[key], from, to, face, delay, duration, true);
            }

            var team = new[]
            {
                (Key: "Saka", Start: .55f, Stop: 3.8f, Lane: 0f, Delay: 0f),
                (Key: "Daung", Start: -.35f, Stop: 2.62f, Lane: .81f, Delay: .08f),
                (Key: "Pacca", Start: -1.35f, Stop: 1.56f, Lane: -.57f, Delay: .17f),
            };
            Transform enter = Child(root, "Panel 1 - Masuk Desa");
            Transform warning = Child(root, "Panel 2 - Peringatan Anak Warga");
            Transform glimpse = Child(root, "Panel 3 - Ancaman di Ujung Jalan");
            Transform follow = Child(root, "Panel 4 - Rombongan Mendekat");
            Transform menace = Child(root, "Panel 5 - Poppo Menghadang");
            Vector3 childStops = Road(5.4f, .19f);
            Transform childFocus = Mark(warning, "Arah ke Anak Warga", childStops);
            Transform sakaFocus = Mark(warning, "Arah ke Saka", Road(3.8f, 0f));

            ActorCue[] walkingTeam = team.Select(member => Cue(enter, member.Key,
                Road(member.Start, member.Lane), Road(member.Stop, member.Lane), childFocus,
                member.Delay, 1.72f)).ToArray();
            ActorCue[] warningCast = team.Select(member => Cue(warning, member.Key,
                Road(member.Stop, member.Lane), Road(member.Stop, member.Lane), childFocus)).Append(
                Cue(warning, "CommonerAnakL", Road(8.8f, .38f), childStops, sakaFocus, .08f, 1.16f)).ToArray();

            Vector3 child = Ground(terrain, mother - forward * 1.85f + side * 2.7f);
            Vector3 guard = Ground(terrain, Vector3.Lerp(mother, poppo, .56f) - forward * .54f);
            Vector3 threatFocus = Vector3.Lerp(mother, poppo, .57f) + Vector3.up * .95f;
            Transform facePoppo = Mark(glimpse, "Arah ke Poppo", Ground(terrain, poppo));
            Transform faceGuard = Mark(glimpse, "Arah ke Pengawal", guard);

            ActorCue[] Threat(Transform panel) => new[]
            {
                Cue(panel, "NelayanIbu", mother, mother, facePoppo),
                Cue(panel, "NelayanAnak", child, child, facePoppo),
                Cue(panel, "RoyalGuard", guard, guard, facePoppo),
                Cue(panel, "Poppo", poppo, poppo, faceGuard),
            };

            ActorCue[] followCast = new[]
            {
                Cue(follow, "Saka", Road(20.8f, .05f), Road(23f, .05f), facePoppo, 0f, 1.9f),
                Cue(follow, "Daung", Road(19.62f, 1.36f), Road(21.82f, 1.36f), facePoppo, .08f, 1.9f),
                Cue(follow, "Pacca", Road(18.56f, -.41f), Road(20.76f, -.41f), facePoppo, .17f, 1.9f),
                Cue(follow, "CommonerAnakL", Road(17.48f, -.89f), Road(19.49f, -.89f), facePoppo, .33f, 1.69f),
            }.Concat(Threat(follow)).ToArray();

            Vector3 EnterEye(float distance) => Road(distance, -3.45f) + Vector3.up * 2.55f;
            Vector3 EnterLook(float distance) => Road(distance, .09f) + Vector3.up * .95f;
            Vector3 warningLook = Road(4.65f, .14f) + Vector3.up * .92f;
            Vector3 peekEye = Road(25.35f, -.15f) + Vector3.up * 2.04f;
            Vector3 followLook = Road(27f, 1f) - Vector3.up * .18f;
            Vector3 revealEye = poppo + new Vector3(9.25f, 0f, -2.19f);

            return new[]
            {
                new Shot(CameraMark(enter, "Kamera Awal", EnterEye(-.1f), EnterLook(.35f)),
                    CameraMark(enter, "Kamera Akhir", EnterEye(2.3f), EnterLook(2.72f)), 2.12f, 52f, walkingTeam),
                new Shot(CameraMark(warning, "Kamera", Road(4.54f, -4.1f) + Vector3.up * 2.72f, warningLook),
                    CameraMark(warning, "Kamera Akhir", Road(4.6f, -3.88f) + Vector3.up * 2.62f, warningLook),
                    3.74f, 58f, warningCast, "Anak Warga   ·   Tolong! Ada Poppo yang menyerang desa!", .9f),
                new Shot(CameraMark(glimpse, "Kamera Awal", peekEye, threatFocus),
                    CameraMark(glimpse, "Kamera Akhir", peekEye + forward * 2.1f, threatFocus), 1.52f, 40f, Threat(glimpse)),
                new Shot(CameraMark(follow, "Kamera Awal", Road(13.2f, 2.8f) + Vector3.up * 2.06f, followLook),
                    CameraMark(follow, "Kamera Akhir", Road(13.7f, 2.75f) + Vector3.up * 1.98f, followLook), 2.38f, 48f, followCast),
                new Shot(CameraMark(menace, "Kamera Awal", Ground(terrain, revealEye) + Vector3.up * 2.32f, threatFocus),
                    CameraMark(menace, "Kamera Akhir", Ground(terrain, revealEye + new Vector3(-1.6f, 0f, .09f)) + Vector3.up * 2.12f, threatFocus),
                    2.28f, 50f, Threat(menace), "Saka   ·   Ayo bantu mereka!", .54f),
            };
        }
    }
}
