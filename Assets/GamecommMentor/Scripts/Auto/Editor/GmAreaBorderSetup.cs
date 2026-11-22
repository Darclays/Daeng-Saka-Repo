using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmAreaBorderSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Area Borders";
        private const string UndoName = "Set Up Area Borders";
        private const string Scenes = "Assets/GamecommMentor/Scenes_GamecommMentor/";
        private const string Capital = Scenes + "IbuKota.unity";
        private const string Village = Scenes + "DesaSorelia.unity";
        private const string Cave = Scenes + "GoaLembanyung.unity";
        private const string BorderArt = "Assets/GamecommMentor/Art/2D/AreaBorders/";
        private const string VillageStart = "PortalPoint_StartRespawnDesaSoralia";
        private const string VillageExit = "PortalPoint_GoGoaLembayung";

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Capital) || CanInstall(Village) || CanInstall(Cave);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanSetUp()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Transform installed = InstallInScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = installed.gameObject;
                Debug.Log($"[Daeng Saka 1E.38B] Border siap di {scene.name}. Simpan scene.", installed);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static Transform InstallInScene(Scene scene)
        {
            var art = new Art();
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform flow = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            Transform player = RequireChild(world, "Saka");
            var body = Require<CharacterController>(player);
            var controls = Require<GmControlGate>(player);
            var route = Require<GmWorldScene>(flow);
            var progress = Require<GmMainQuestProgress>(flow);
            var journal = Require<GmQuestJournal>(RequireChild(world, "Quest Encounter Hint"));
            var portal = Require<GmWorldPortal>(RequireChild(world, "Portal"));
            Transform start = RequireChild(world, "Spawn - Start");

            if (scene.path == Capital)
            {
                var camera = Require<GmOrbitFollowCamera>(RequireChild(world, "Saka - Orbit Camera"));
                GmCityArchitectureSetup.Install(scene, world, body, camera);
                var layout = GmCapitalRouteSetup.CreateLayout(scene, body);
                Transform cityWalls = RequireChild(RequireRoot(scene, "LevelDesain"), "LingkunganKerajaan");
                Transform gate = cityWalls.GetComponentsInChildren<Transform>()
                    .Where(item => item.name.StartsWith("3D_IbuKota_Kerajaan_Gapura", StringComparison.Ordinal))
                    .OrderBy(item => (item.position - layout.PortalGuide.position).sqrMagnitude).FirstOrDefault();
                if (gate == null) throw new InvalidOperationException("Gapura menuju Desa Sorelia belum ditemukan.");

                Vector3 outward = Heading(gate.forward);
                if (Vector3.Dot(outward, gate.position - start.position) < 0f) outward = -outward;
                Vector3 atGate = new Vector3(gate.position.x, portal.transform.position.y, gate.position.z);
                InstallBorder(portal, atGate, outward, 7.4f, controls, art);
                Wire(portal, route, progress, journal, Village, GmQuestArea.DesaSorelia, GmWorldScene.DefaultEntry,
                    GmMainQuestStep.KunjungiPenempaBesi, "Selesaikan persiapan di Kerajaan");

                Transform arrival = Child(world, "Spawn - Dari Sorelia");
                Undo.RecordObject(arrival, UndoName);
                layout.Place(arrival, atGate - outward * 3.4f, -outward);
                Undo.RecordObject(route, UndoName);
                route.SetEntry(new GmWorldScene.Entry(GmWorldScene.VillageReturnEntry, arrival));
                PrefabUtility.RecordPrefabInstancePropertyModifications(route);
                GmCapitalDialogueSetup.InstallInScene(scene);
            }
            else if (scene.path == Village)
            {
                var layout = new GmWorldSceneLayout(scene, VillageStart, VillageExit, body);
                Transform fromCave = RequireChild(world, "Spawn - Dari Goa");
                Vector3 towardCave = Heading(portal.transform.position - fromCave.position);
                InstallBorder(portal, portal.transform.position, towardCave, 23.5f, controls, art);
                Wire(portal, route, progress, journal, Cave, GmQuestArea.GoaLembayung, GmWorldScene.DefaultEntry,
                    GmMainQuestStep.BicaraDenganNelayan, "Bicara dengan Nelayan setelah battle");

                Transform returnPoint = Child(world, "Portal - Ke Kerajaan");
                var back = GetOrAdd<GmWorldPortal>(returnPoint);
                Undo.RecordObject(returnPoint, UndoName);
                layout.Place(returnPoint, layout.StartGuide.position - layout.IntoLevel * .75f, -layout.IntoLevel, start.position);
                InstallBorder(back, returnPoint.position, -layout.IntoLevel, 11.8f, controls, art);
                Wire(back, route, progress, journal, Capital, GmQuestArea.Kerajaan, GmWorldScene.VillageReturnEntry,
                    null, null, returning: true);
            }
            else
            {
                Vector3 outward = -Heading(start.forward);
                InstallBorder(portal, portal.transform.position, outward, 20.6f, controls, art);
                Wire(portal, route, progress, journal, Village, GmQuestArea.DesaSorelia, GmWorldScene.CaveReturnEntry);
            }
            return portal.transform;
        }

        private static Vector3 Heading(Vector3 direction)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            return horizontal.sqrMagnitude > .0001f ? horizontal.normalized : Vector3.forward;
        }

        private static void Wire(GmWorldPortal portal, GmWorldScene world, GmMainQuestProgress progress,
            GmQuestJournal journal, string destination, GmQuestArea area, string entry,
            GmMainQuestStep? prerequisite = null, string locked = null, bool returning = false)
        {
            Undo.RecordObject(portal, UndoName);
            portal.Configure(world, destination, entry, progress, prerequisite, locked);
            portal.SetPresentation(GmQuestCatalog.AreaName(area), "Pindah area");
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);

            Undo.RecordObject(journal, UndoName);
            journal.SetTravelPortal(portal, area, returning);
            PrefabUtility.RecordPrefabInstancePropertyModifications(journal);
        }

        private static void InstallBorder(GmWorldPortal portal, Vector3 position, Vector3 forward, float width,
            GmControlGate controls, Art art)
        {
            Transform point = portal.transform;
            Undo.RecordObject(point, UndoName);
            point.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            point.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            SetBox(GetOrAdd<BoxCollider>(point), new Vector3(width, 4.4f, 1.2f), sensor: true);
            var barrier = GetOrAdd<BoxCollider>(Child(point, "Batas Jalur"));
            SetBox(barrier, new Vector3(width, 4.4f, .18f), sensor: false);

            var weave = SetSprite(point, "Pita Anyaman", art.Weave, art.Material, 1.04f, width, .42f, 0);
            var crest = SetSprite(point, "Lambang Jalur", art.Crest, art.Material, 1.04f, .74f, .74f, 1);
            var border = GetOrAdd<GmWorldBorder>(point);
            Undo.RecordObject(border, UndoName);
            border.Configure(portal, controls, barrier, weave, crest);
            PrefabUtility.RecordPrefabInstancePropertyModifications(border);
            Undo.RecordObject(portal, UndoName);
            portal.SetBorder(border);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
        }

        private static void SetBox(BoxCollider box, Vector3 size, bool sensor)
        {
            Undo.RecordObject(box, UndoName);
            box.enabled = true;
            box.center = Vector3.up * 1.38f;
            box.size = size;
            box.isTrigger = sensor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(box);
        }

        private static SpriteRenderer SetSprite(Transform owner, string name, Sprite sprite, Material material, float height,
            float width, float sizeY, int order)
        {
            Transform visual = Child(owner, name);
            Undo.RecordObject(visual, UndoName);
            visual.localPosition = new Vector3(0f, height, -.04f);
            visual.localScale = new Vector3(width / sprite.bounds.size.x, sizeY / sprite.bounds.size.y, 1f);
            var renderer = GetOrAdd<SpriteRenderer>(visual);
            Undo.RecordObject(renderer, UndoName);
            renderer.sprite = sprite;
            if (material != null) renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            return renderer;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var node = new GameObject(name);
            SceneManager.MoveGameObjectToScene(node, parent.gameObject.scene);
            node.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(node, UndoName);
            return node.transform;
        }

        private sealed class Art
        {
            public readonly Sprite Weave = LoadSprite("Pita_Anyaman");
            public readonly Sprite Crest = LoadSprite("Lambang_Jalur");
            public readonly Material Material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/GamecommMentor/Art/2D/DialogueExamples/NPC_Unlit.mat");

            private static Sprite LoadSprite(string name)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BorderArt + name + ".png");
                if (sprite == null) throw new InvalidOperationException($"Sprite {name} perlu diimpor sebelum setup border.");
                return sprite;
            }
        }
    }
}
