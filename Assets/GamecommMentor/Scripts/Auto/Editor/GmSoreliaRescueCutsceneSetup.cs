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
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Sorelia Rescue Cutscene";
        private const string UndoName = "Set Up Sorelia Rescue Cutscene";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string Art = "Assets/GamecommMentor/Art/2D/Cutscenes/Sorelia/";

        [MenuItem(Menu, true)]
        private static bool CanRun() => CanInstall(Village);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanRun()) return;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                var director = Install(SceneManager.GetActiveScene());
                EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = director.gameObject;
                Debug.Log("[Daeng Saka 1E.14B R1] Jalur cutscene dan akses teras siap. Simpan DesaSorelia.", director);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
            }
        }

        private static GmSoreliaRescueCutscene Install(Scene scene)
        {
            var rescue = Require<GmSoreliaRescue>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform root = Child(world, "Cutscene - Sorelia Setelah Invasi");
            var director = Component<GmSoreliaRescueCutscene>(root);
            Transform level = RequireRoot(scene, "LevelDesign");
            Transform houses = RequireChild(level, "Rumah");
            var terrain = Require<TerrainCollider>(RequireChild(level, "Terrain"));
            var (timber, red) = SetUpHouseAccess(houses, terrain);
            if (!director.IsConfigured)
            {
                Transform player = RequireChild(world, "Saka");
                var reference = player.GetComponentInChildren<SpriteRenderer>(true);
                if (reference == null) throw new InvalidOperationException("Visual Saka belum tersedia di DesaSorelia.");
                Transform cameraObject = RequireChild(world, "Saka - Orbit Camera");
                var camera = Require<Camera>(cameraObject);
                var orbit = Require<GmOrbitFollowCamera>(cameraObject);
                var hud = Require<Canvas>(RequireChild(world, "Quest Encounter Hint"));
                var actors = CreateCast(Child(root, "Aktor Cutscene"), reference);
                Transform poppo = RequireChild(world, "Poppo (Uji)");
                var opening = BuildTerraceShots(root, timber, red, terrain);
                Shot[] sequence = new[] { opening.Wide, opening.Close }.Concat(
                    BuildStreetShots(root, terrain, timber.House, red.House, poppo.position, actors)).ToArray();
                var overlay = MakeOverlay(root, out Image fade, out Text subtitle);
                SpriteRenderer[] hidden = new[] { player, RequireChild(world, "Nelayan (Uji)"),
                    RequireChild(world, "Keluarga Nelayan"), RequireChild(world, "Warga Sorelia Setelah Ditolong") }
                    .SelectMany(actor => actor.GetComponentsInChildren<SpriteRenderer>(true)).Distinct().ToArray();

                Undo.RecordObject(director, UndoName);
                director.Configure(camera, orbit, terrain, hud, overlay.gameObject, fade, subtitle,
                    actors.Values.ToArray(), hidden, sequence);
                director.SetTerraceShots(opening.Wide, opening.Close);
                if (!director.IsConfigured)
                    throw new InvalidOperationException("Ada kamera, aktor, atau jalur cutscene Sorelia yang belum siap.");
            }
            else if (!director.TerraceShotsInstalled)
            {
                var opening = BuildTerraceShots(root, timber, red, terrain);
                Undo.RecordObject(director, UndoName);
                director.SetTerraceShots(opening.Wide, opening.Close);
            }
            Undo.RecordObject(rescue, UndoName);
            rescue.SetVictoryCutscene(director);
            return director;
        }

        private static Dictionary<string, GmCutsceneSprite> CreateCast(Transform parent, SpriteRenderer reference)
        {
            var cast = new Dictionary<string, GmCutsceneSprite>();
            foreach (var entry in new (string ArtKey, string Label, bool Hero, bool Standing)[]
            {
                ("Saka", "Saka", true, false),
                ("Daung", "Daung", true, false),
                ("Pacca", "Pacca", true, false),
                ("NelayanAyah", "Bapak Nelayan", false, false),
                ("NelayanIbu", "Ibu Nelayan", false, false),
                ("NelayanAnak", "Anak Nelayan", false, false),
                ("CommonerBapak", "Bapak Commoner", false, false),
                ("CommonerIbu", "Ibu Commoner", false, false),
                ("CommonerAnakL", "AnakL Commoner", false, false),
                ("CommonerAnakP", "AnakP Commoner", false, false),
                ("RoyalGuard", "Royal Guard", false, true),
            })
            {
                string path = Art + entry.ArtKey + ".png";
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                GmCutsceneSprite.Directions idle = ReadDirections(sprites, "Idle", entry.Hero, entry.Standing, path);
                GmCutsceneSprite.Directions walk = entry.Standing ? default :
                    ReadDirections(sprites, "Walk", entry.Hero, false, path);
                Transform actor = Child(parent, entry.Label);
                var renderer = Component<SpriteRenderer>(actor);
                var visual = Component<GmCutsceneSprite>(actor);
                Undo.RecordObject(renderer, UndoName);
                renderer.sharedMaterial = reference.sharedMaterial;
                renderer.sortingLayerID = reference.sortingLayerID;
                renderer.sortingOrder = reference.sortingOrder;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                Undo.RecordObject(visual, UndoName);
                visual.Configure(renderer, idle, walk);
                Undo.RecordObject(actor.gameObject, UndoName);
                actor.gameObject.SetActive(false);
                cast.Add(entry.ArtKey, visual);
            }
            return cast;
        }

        private static GmCutsceneSprite.Directions ReadDirections(Sprite[] sprites, string motion,
            bool hero, bool standing, string path)
        {
            Sprite[] Frames(string direction)
            {
                string prefix = motion + "_" + direction + "_";
                Sprite[] found = sprites.Where(s => s.name.StartsWith(prefix, StringComparison.Ordinal))
                    .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
                if (found.Length == 0) throw new InvalidOperationException($"Frame {prefix} belum terbaca di '{path}'.");
                return found;
            }

            var result = new GmCutsceneSprite.Directions { front = Frames("Front") };
            if (standing) return result;
            result.back = Frames("Back");
            result.right = Frames(hero ? "Right" : "Side");
            result.mirrorLeft = !hero;
            if (hero) result.left = Frames("Left");
            return result;
        }

        private static Shot[] BuildStreetShots(Transform root, TerrainCollider terrain, Transform timber, Transform red,
            Vector3 poppo, Dictionary<string, GmCutsceneSprite> actors)
        {
            Vector3 front = Flat(timber.forward + red.forward).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, front);
            Transform panel1 = Child(root, "Panel 1 - Terima Kasih");
            Transform panel2 = Child(root, "Panel 2 - Warga Berpencar");

            ActorCue Cue(Transform panel, string key, Vector3 from, Vector3 to, Transform focus,
                float wait, float duration)
            {
                Transform track = Child(panel, actors[key].gameObject.name);
                Transform first = Mark(track, "Awal", from);
                Transform last = duration > 0f ? Mark(track, "Akhir", to) : first;
                return new ActorCue(actors[key], first, last, focus, wait, duration, true);
            }

            Vector3 road = Ground(terrain, poppo - side * 5.90f - front * .80f);
            Vector3 Road(float x, float z) => Ground(terrain, road + side * x + front * z);
            Transform partyFocus = Mark(panel1, "Arah ke Tim", Road(2f, .02f));
            Transform residentsFocus = Mark(panel1, "Arah ke Nelayan", Road(-1.5f, .05f));
            Vector3 greetEye = road + front * 7.4f - side * 1.9f + Vector3.up * 3.55f;
            Vector3 greetFocus = road - front * .45f + Vector3.up * .90f;
            Vector3 greetEnd = greetEye - front * .4f;
            Transform greetStartMark = CameraMark(panel1, "Kamera Awal", greetEye, greetFocus);
            Transform greetEndMark = CameraMark(panel1, "Kamera Akhir", greetEnd, greetFocus);
            Transform leaveEnd = CameraMark(panel2, "Kamera Akhir",
                greetEnd + front * .85f - side * .52f + Vector3.up * .42f, greetFocus);

            var team = new[]
            {
                (Key: "Saka", X: 1.19f, Z: .16f, Delay: .05f),
                (Key: "Daung", X: 2.58f, Z: -.56f, Delay: .15f),
                (Key: "Pacca", X: 3.83f, Z: .43f, Delay: .23f),
            };
            var villagers = new[]
            {
                (Key: "NelayanAyah", X: -1.27f, Z: .11f, ExitX: .55f, ExitZ: 2.58f, Delay: .15f),
                (Key: "NelayanIbu", X: -2.62f, Z: -.82f, ExitX: -1.15f, ExitZ: -2.84f, Delay: .18f),
                (Key: "NelayanAnak", X: -2.87f, Z: .95f, ExitX: -5.05f, ExitZ: 1.2f, Delay: .28f),
                (Key: "CommonerBapak", X: -4.34f, Z: -.7f, ExitX: -6.9f, ExitZ: -.55f, Delay: .26f),
                (Key: "CommonerIbu", X: -3.88f, Z: -1.98f, ExitX: -5.55f, ExitZ: -2.9f, Delay: .35f),
                (Key: "CommonerAnakL", X: -4.9f, Z: .56f, ExitX: -7.4f, ExitZ: .65f, Delay: .10f),
                (Key: "CommonerAnakP", X: -1.53f, Z: -2.11f, ExitX: -3.55f, ExitZ: -3f, Delay: .37f),
            };
            var greet = new List<ActorCue>();
            var disperse = new List<ActorCue>();
            foreach (var actor in team)
            {
                Vector3 spot = Road(actor.X, actor.Z);
                greet.Add(Cue(panel1, actor.Key, Road(actor.X + 1.65f, actor.Z), spot,
                    residentsFocus, actor.Delay, 1.24f));
                disperse.Add(Cue(panel2, actor.Key, spot, spot, residentsFocus, 0f, 0f));
            }
            foreach (var actor in villagers)
            {
                Vector3 spot = Road(actor.X, actor.Z);
                greet.Add(Cue(panel1, actor.Key, Road(actor.X - 1.15f, actor.Z - .38f), spot,
                    partyFocus, actor.Delay, 1.12f));
                disperse.Add(Cue(panel2, actor.Key, spot, Road(actor.ExitX, actor.ExitZ), null,
                    actor.Delay, 1.9f));
            }
            Vector3 guard = Road(.72f, -3.35f);
            greet.Add(Cue(panel1, "RoyalGuard", guard, guard, residentsFocus, 0f, 0f));
            disperse.Add(Cue(panel2, "RoyalGuard", guard, guard, residentsFocus, 0f, 0f));

            return new[]
            {
                new Shot(greetStartMark, greetEndMark, 3.90f, 51f, greet.ToArray(),
                    "Nelayan   ·   Terima kasih, Nak.", 1.58f),
                new Shot(greetEndMark, leaveEnd, 2.62f, 51f, disperse.ToArray()),
            };
        }

        private static Vector3 Ground(TerrainCollider terrain, Vector3 point)
        {
            var ray = new Ray(point + Vector3.up * 5f, Vector3.down);
            if (terrain.Raycast(ray, out RaycastHit ground, 12f)) point.y = ground.point.y + .025f;
            return point;
        }

        private static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

        private static Transform CameraMark(Transform parent, string name, Vector3 point, Vector3 focus) =>
            Mark(parent, name, point, Quaternion.LookRotation(focus - point, Vector3.up));

        private static Transform Mark(Transform parent, string name, Vector3 point, Quaternion? rotation = null)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            Transform marker = Child(parent, name);
            Undo.RecordObject(marker, UndoName);
            marker.SetPositionAndRotation(point, rotation ?? Quaternion.identity);
            return marker;
        }

        private static Transform MakeOverlay(Transform parent, out Image fade, out Text caption)
        {
            Transform overlay = Child(parent, "Overlay Cutscene", true);
            var canvas = Component<Canvas>(overlay);
            var scaler = Component<CanvasScaler>(overlay);
            Undo.RecordObject(canvas, UndoName);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 185;
            Undo.RecordObject(scaler, UndoName);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            Image Backdrop(string name, Vector2 min, Vector2 max)
            {
                RectTransform rect = PlaceRect(overlay, name, min, max);
                var image = Component<Image>(rect);
                Undo.RecordObject(image, UndoName);
                image.color = Color.black;
                image.raycastTarget = false;
                return image;
            }

            Text Label(string name, Vector2 min, Vector2 max, int size, TextAnchor align)
            {
                RectTransform rect = PlaceRect(overlay, name, min, max);
                var label = Component<Text>(rect);
                Undo.RecordObject(label, UndoName);
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = size;
                label.color = Color.white;
                label.alignment = align;
                label.raycastTarget = false;
                return label;
            }

            Backdrop("Bilah Atas", new Vector2(0f, .92f), Vector2.one);
            Backdrop("Bilah Bawah", Vector2.zero, new Vector2(1f, .08f));
            caption = Label("Subtitle", new Vector2(.17f, .005f), new Vector2(.83f, .076f), 25, TextAnchor.MiddleCenter);
            Label("Lewati", new Vector2(.78f, .93f), new Vector2(.978f, .99f), 18, TextAnchor.MiddleRight)
                .text = "[Esc] Lewati";
            fade = Backdrop("Fade", Vector2.zero, Vector2.one);
            Undo.RecordObject(overlay.gameObject, UndoName);
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        private static RectTransform PlaceRect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)Child(parent, name, true);
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Transform Child(Transform parent, string name, bool rectTransform = false)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            GameObject entry = rectTransform ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            SceneManager.MoveGameObjectToScene(entry, parent.gameObject.scene);
            entry.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(entry, UndoName);
            return entry.transform;
        }

        private static T Component<T>(Transform owner) where T : Component
        {
            if (owner.TryGetComponent(out T existing)) return existing;
            T added = Undo.AddComponent<T>(owner.gameObject);
            if (added == null) throw new InvalidOperationException($"Gagal menambahkan {typeof(T).Name} ke '{owner.name}'.");
            return added;
        }
    }
}
