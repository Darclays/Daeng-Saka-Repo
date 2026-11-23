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
        private const string GoaCutsceneMenu = "GamecommMentor/Daeng Saka/Set Up Goa Arrival Cutscene";
        private const string GoaCutsceneUndo = "Set Up Goa Arrival Cutscene";
        private const string GoaScene = "Assets/GamecommMentor/Scenes_GamecommMentor/GoaLembanyung.unity";
        private const string GoaRoar = "Assets/GamecommMentor/Audio/Cutscenes/GoaAuman.wav";
        private const string GoaMouthPoppo = "Poppo - Mulut Goa";
        private const string GoaMouthPoppoMarker = "EC_Poppo_MulutGoa";

        [MenuItem(GoaCutsceneMenu, true)]
        private static bool CanSetUpGoa() => CanInstall(GoaScene);

        [MenuItem(GoaCutsceneMenu)]
        private static void SetUpGoa()
        {
            if (!CanSetUpGoa()) return;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(GoaCutsceneUndo);
            try
            {
                var intro = InstallGoa(SceneManager.GetActiveScene());
                EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = intro.gameObject;
                Debug.Log("[Daeng Saka 1E.16B R2] Poppo di mulut goa dan cutscene siap. Simpan GoaLembanyung.", intro);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
            }
        }

        private static GmGoaArrivalIntro InstallGoa(Scene scene)
        {
            GmCaveRouteSetup.InstallInScene(scene);
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform root = Child(world, "Cutscene - Masuk Goa Lembayung");
            var director = Component<GmSoreliaRescueCutscene>(root);
            var intro = Component<GmGoaArrivalIntro>(root);
            Transform cameraObject = RequireChild(world, "Saka - Orbit Camera");
            var camera = Require<Camera>(cameraObject);
            EnsureGoaListener(scene, cameraObject);

            Transform enemy = RequireChild(world, "Musuh Goa");
            var enemyArt = enemy.GetComponentInChildren<SpriteRenderer>(true);
            if (enemyArt == null || enemyArt.sprite == null)
                throw new InvalidOperationException("Visual Musuh Goa belum siap di GoaLembanyung.");
            var terrain = Require<TerrainCollider>(RequireChild(RequireRoot(scene, "LevelDesign"), "Terrain"));
            Transform mouth = SetUpGoaMouthPoppo(RequireRoot(scene, "NPCPlaces"), world, terrain, enemyArt, camera);
            Undo.RecordObject(intro, GoaCutsceneUndo);
            intro.SetMouthPoppo(mouth);

            bool validSequence = director.IsConfigured;
            if (validSequence && intro.IsConfigured) return intro;

            Transform hero = RequireChild(world, "Saka");
            Transform arrival = RequireChild(world, "Spawn - Start");
            Transform guide = world.Find(GmVisualPolishSetup.CaveTestGuide);
            var orbit = Require<GmOrbitFollowCamera>(cameraObject);
            var hud = Require<Canvas>(RequireChild(world, "Quest Encounter Hint"));
            var quest = Require<GmMainQuestProgress>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            var heroArt = hero.GetComponentInChildren<SpriteRenderer>(true);
            if (heroArt == null) throw new InvalidOperationException("Visual Saka belum siap di GoaLembanyung.");

            Transform cast = Child(root, "Aktor Cutscene");
            var actors = CreateCast(cast, heroArt, "Saka", "Daung", "Pacca");
            RemoveLegacyGoaPoppo(root, cast);

            Shot[] shots = BuildGoaArrivalShots(root, terrain, arrival, enemy, actors);
            if (validSequence && director.Shots.Count == shots.Length)
            {
                for (int i = 0; i < 4; i++) shots[i] = director.Shots[i];
            }
            Transform overlay = MakeOverlay(root, out Image fade, out Text caption);
            SpriteRenderer[] hidden = new[] { hero, guide }.Where(actor => actor != null)
                .SelectMany(actor => actor.GetComponentsInChildren<SpriteRenderer>(true)).Distinct().ToArray();
            Undo.RecordObject(director, GoaCutsceneUndo);
            director.Configure(camera, orbit, terrain, hud, overlay.gameObject, fade, caption,
                actors.Values.ToArray(), hidden, shots);
            intro.Configure(Require<GmSakaMotor>(hero), quest, arrival, director, enemyArt.transform);
            if (!director.IsConfigured)
                throw new InvalidOperationException("Ada kamera, aktor, atau jalur cutscene Goa yang belum siap.");
            return intro;
        }

        private static Transform SetUpGoaMouthPoppo(Transform places, Transform world, TerrainCollider terrain,
            SpriteRenderer reference, Camera camera)
        {
            // The former encounter position is visible past the party in the final arrival shot.
            Vector3 defaultPosition = Ground(terrain, new Vector3(13.59f, 1.22f, 51.69f));
            Transform marker = Mark(places, GoaMouthPoppoMarker, defaultPosition);
            Transform poppo = Child(world, GoaMouthPoppo);
            Undo.RecordObject(poppo, GoaCutsceneUndo);
            poppo.SetPositionAndRotation(marker.position, marker.rotation);

            Transform visual = Child(poppo, "Visual");
            Undo.RecordObject(visual, GoaCutsceneUndo);
            visual.localPosition = Vector3.zero;
            visual.localScale = reference.transform.localScale;
            var sprite = Component<SpriteRenderer>(visual);
            Undo.RecordObject(sprite, GoaCutsceneUndo);
            sprite.sprite = reference.sprite;
            sprite.sharedMaterial = reference.sharedMaterial;
            sprite.sortingLayerID = reference.sortingLayerID;
            sprite.sortingOrder = reference.sortingOrder;
            sprite.spriteSortPoint = reference.spriteSortPoint;
            sprite.color = reference.color;
            sprite.flipX = reference.flipX;
            var billboard = Component<GmSpriteBillboard>(visual);
            Undo.RecordObject(billboard, GoaCutsceneUndo);
            billboard.SetCamera(camera);
            Undo.RecordObject(visual.gameObject, GoaCutsceneUndo);
            visual.gameObject.SetActive(false);
            return visual;
        }

        private static void RemoveLegacyGoaPoppo(Transform root, Transform cast)
        {
            Transform oldClosingTrack = root.Find("02.2 - Mengamati Pintu Goa")?.Find("Poppo");
            foreach (Transform legacy in new[] { cast.Find("Poppo"), root.Find("02.1 - Poppo di Lorong"), oldClosingTrack })
                if (legacy != null) Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        private static void EnsureGoaListener(Scene scene, Transform cameraObject)
        {
            if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AudioListener>())
                .Any(listener => listener.enabled)) return;

            var listener = Component<AudioListener>(cameraObject);
            Undo.RecordObject(listener, GoaCutsceneUndo);
            listener.enabled = true;
        }

        private static Shot[] BuildGoaArrivalShots(Transform root, TerrainCollider terrain, Transform arrival,
            Transform enemy, Dictionary<string, GmCutsceneSprite> actors)
        {
            Vector3 forward = Flat(arrival.forward).normalized;
            Vector3 side = Vector3.Cross(forward, Vector3.up);
            Vector3 Road(float distance, float lane) => Ground(terrain, arrival.position + forward * distance + side * lane);
            Vector3 ApproachEye(float distance, float lane) => Road(distance, lane) + Vector3.up * 3.02f;
            Vector3 ApproachLook(float distance) => Road(distance, .44f) + Vector3.up * .92f;

            Transform opening = Child(root, "01.1 - Jalan ke Goa");
            Transform enter = Child(root, "01.2 - Rombongan Tiba");
            Transform listen = Child(root, "01.3 - Auman dari Dalam");
            Transform advance = Child(root, "01.4 - Menuju Mulut Goa");
            Transform ready = Child(root, "02.2 - Mengamati Pintu Goa");
            Transform faceCave = Mark(listen, "Arah Mulut Goa", Road(16.7f, .3f));

            ActorCue Cue(Transform panel, string key, Vector3 start, Vector3 end,
                float delay = 0f, float duration = 0f)
            {
                Transform track = Child(panel, actors[key].gameObject.name);
                Transform from = Mark(track, "Awal", start);
                Transform to = duration > 0f ? Mark(track, "Akhir", end) : from;
                return new ActorCue(actors[key], from, to, faceCave, delay, duration, true);
            }

            var team = new[]
            {
                (Key: "Saka", Start: -1.64f, Halt: 2.84f, End: 6.36f, Lane: .22f, EndLane: .22f, Delay: 0f),
                (Key: "Daung", Start: -2.33f, Halt: 1.45f, End: 5.05f, Lane: .91f, EndLane: .91f, Delay: .12f),
                (Key: "Pacca", Start: -3.02f, Halt: .32f, End: 3.76f, Lane: -.66f, EndLane: -2.36f, Delay: .17f),
            };
            var walking = team.Select(actor => Cue(enter, actor.Key, Road(actor.Start, actor.Lane),
                Road(actor.Halt, actor.Lane), actor.Delay, 2.34f)).ToArray();
            var listening = team.Select(actor => Cue(listen, actor.Key, Road(actor.Halt, actor.Lane),
                Road(actor.Halt, actor.Lane))).ToArray();
            var approaching = team.Select(actor => Cue(advance, actor.Key, Road(actor.Halt, actor.Lane),
                Road(actor.End, actor.EndLane), actor.Delay, 2.02f)).ToArray();
            var waiting = team.Select(actor => Cue(ready, actor.Key, Road(actor.End, actor.EndLane),
                Road(actor.End, actor.EndLane))).ToArray();

            Transform wideFrom = CameraMark(opening, "Kamera Awal", ApproachEye(3.86f, -4.13f), ApproachLook(3.73f));
            Transform wideTo = CameraMark(opening, "Kamera Akhir", ApproachEye(3.70f, -4.15f), ApproachLook(3.60f));
            Transform teamTo = CameraMark(enter, "Kamera Akhir", ApproachEye(2.91f, -4.56f), ApproachLook(2.96f));
            Transform walkTo = CameraMark(advance, "Kamera Akhir", Road(6.12f, -5.75f) + Vector3.up * 2.66f, ApproachLook(6.12f));
            Vector3 overShoulder = Road(-.93f, -1.78f) + Vector3.up * 1.96f;
            Vector3 shoulderFocus = Road(12.1f, -.20f) + Vector3.up * .87f;

            var sound = Component<AudioSource>(Mark(listen, "Sumber Auman", Road(13.6f, 0f) + Vector3.up));
            var roar = AssetDatabase.LoadAssetAtPath<AudioClip>(GoaRoar);
            if (roar == null) throw new InvalidOperationException("GoaAuman.wav belum tersedia. Impor folder Audio dari patch.");
            Undo.RecordObject(sound, GoaCutsceneUndo);
            sound.clip = roar;
            sound.playOnAwake = false;
            sound.spatialBlend = .73f;
            sound.dopplerLevel = 0f;
            sound.rolloffMode = AudioRolloffMode.Linear;
            sound.minDistance = 3f;
            sound.maxDistance = 32f;
            sound.volume = .78f;

            return new[]
            {
                new Shot(wideFrom, wideTo, .85f, 54f, Array.Empty<ActorCue>()),
                new Shot(wideTo, teamTo, 2.68f, 54f, walking, "Saka   ·   Akhirnya kita sampai di pintu goa.", 1.03f),
                new Shot(teamTo, teamTo, 1.94f, 54f, listening, "[Auman dari dalam goa]", .14f, sound),
                new Shot(teamTo, walkTo, 2.23f, 54f, approaching),
                BuildGoaPoppoReveal(terrain, enemy),
                new Shot(CameraMark(ready, "Kamera Awal", overShoulder, shoulderFocus),
                    CameraMark(ready, "Kamera Akhir", overShoulder + forward * .36f, shoulderFocus), 2.03f, 53f, waiting),
            };
        }

        private static Shot BuildGoaPoppoReveal(TerrainCollider terrain, Transform enemy)
        {
            Transform rig = Child(enemy, "Kamera Cutscene Goa");
            Vector3 front = Flat(enemy.forward).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, front);
            Vector3 eye = Ground(terrain, enemy.position + front * 5.7f + side * .95f) + Vector3.up * 2.28f;
            Vector3 focus = enemy.position + Vector3.up * (GmGoaArrivalIntro.PreviewHeight + .48f);
            return new Shot(CameraMark(rig, "Kamera Awal", eye, focus),
                CameraMark(rig, "Kamera Akhir", eye - front * .74f, focus), 1.67f, 49f, Array.Empty<ActorCue>());
        }
    }
}
