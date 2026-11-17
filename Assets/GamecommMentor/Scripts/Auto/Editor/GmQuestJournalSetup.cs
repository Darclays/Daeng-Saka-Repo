using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmQuestJournalSetup
    {
        private const string InstallMenu = "GamecommMentor/Daeng Saka/Install Quest Journal";
        private const string PreviewMenu = "GamecommMentor/Daeng Saka/Preview Fishing Quest";
        private const string SceneFolder = "Assets/GamecommMentor/Scenes_GamecommMentor/";
        private const string Capital = SceneFolder + "IbuKota.unity";
        private const string Village = SceneFolder + "DesaSorelia.unity";
        private const string Cave = SceneFolder + "GoaLembanyung.unity";
        private const string WorldRoot = "Daeng Saka - World Gameplay";
        private const string FlowRoot = "Daeng Saka - Encounter Flow";

        private sealed class SceneSetup
        {
            public Scene Scene;
            public GmQuestArea Area;
            public GmQuestArea Destination;
            public GmMainQuestEncounterHint Hint;
            public GmMainQuestProgress Progress;
            public GmInteractor Player;
            public GmWorldPortal Portal;
            public GmQuestLandmark[] Landmarks;
            public Rect Bounds;
        }

        [MenuItem(InstallMenu, true)]
        private static bool CanInstall() =>
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        [MenuItem(InstallMenu)]
        private static void Install()
        {
            if (!CanInstall() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in new[] { Capital, Village, Cave })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) continue;
                Debug.LogError("[Daeng Saka 1C.5B] Scene tidak ditemukan: " + path);
                return;
            }

            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                SceneSetup[] setups =
                {
                    ReadScene(EditorSceneManager.OpenScene(Capital, OpenSceneMode.Single),
                        GmQuestArea.Kerajaan, GmQuestArea.DesaSorelia),
                    ReadScene(EditorSceneManager.OpenScene(Village, OpenSceneMode.Additive),
                        GmQuestArea.DesaSorelia, GmQuestArea.GoaLembayung),
                    ReadScene(EditorSceneManager.OpenScene(Cave, OpenSceneMode.Additive),
                        GmQuestArea.GoaLembayung, GmQuestArea.DesaSorelia)
                };
                foreach (SceneSetup setup in setups)
                {
                    if (!AttachJournal(setup)) continue;
                    EditorSceneManager.MarkSceneDirty(setup.Scene);
                    if (!EditorSceneManager.SaveScene(setup.Scene))
                        throw new InvalidOperationException("Gagal menyimpan " + setup.Scene.path);
                }

                Scene capital = EditorSceneManager.OpenScene(Capital, OpenSceneMode.Single);
                Selection.activeGameObject = RequireChild(RequireRoot(capital, WorldRoot), "Quest Encounter Hint").gameObject;
                Debug.Log("[Daeng Saka 1C.5B] Jurnal dan peta terpasang di Kerajaan, Desa Sorelia, dan Goa Lembayung. Mulai Play dari IbuKota.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (previous.Length != 0 && previous.All(setup => !string.IsNullOrEmpty(setup.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static SceneSetup ReadScene(Scene scene, GmQuestArea area, GmQuestArea destination)
        {
            Transform world = RequireRoot(scene, WorldRoot);
            Transform flow = RequireRoot(scene, FlowRoot);
            var player = RequireComponent<GmInteractor>(RequireChild(world, "Saka"));
            RequireComponent<GmControlGate>(player.transform);
            var hint = RequireComponent<GmMainQuestEncounterHint>(RequireChild(world, "Quest Encounter Hint"));
            var quest = RequireComponent<GmMainQuestProgress>(flow);
            var portal = RequireComponent<GmWorldPortal>(RequireChild(world, "Portal"));
            Transform start = RequireChild(world, "Spawn - Start");
            var eventSystem = RequireComponent<EventSystem>(RequireChild(world, "EventSystem"));
            if (!eventSystem.GetComponents<BaseInputModule>().Any(module => module.enabled))
                throw new InvalidOperationException("UI input module belum aktif di " + scene.name);

            string guideName = area switch
            {
                GmQuestArea.Kerajaan => "Pemandu Kerajaan (Uji)",
                GmQuestArea.DesaSorelia => "Nelayan (Uji)",
                _ => "Pemandu Goa (Uji)"
            };
            string guideLabel = area switch
            {
                GmQuestArea.Kerajaan => "Pemandu Kerajaan",
                GmQuestArea.DesaSorelia => "Nelayan",
                _ => "Pemandu Goa"
            };
            string guideDescription = area switch
            {
                GmQuestArea.Kerajaan => "Bicara dengan pemandu untuk dialog persiapan, Merchant, dan Penempa Besi.",
                GmQuestArea.DesaSorelia => "Nelayan yang keluarganya perlu ditolong.",
                _ => "Temui pemandu di pintu masuk Goa Lembayung."
            };
            string destinationName = GmQuestCatalog.AreaName(destination);
            var landmarks = new List<GmQuestLandmark>
            {
                new GmQuestLandmark(GmQuestCatalog.Exit, "Portal " + destinationName,
                    "Jalur menuju " + destinationName + ".", portal.transform),
                new GmQuestLandmark(GmQuestCatalog.Guide, guideLabel, guideDescription, RequireChild(world, guideName))
            };
            if (area == GmQuestArea.DesaSorelia)
            {
                landmarks.Add(new GmQuestLandmark(GmQuestCatalog.Encounter, "Poppo",
                    "Poppo yang mengepung keluarga Nelayan.", RequireChild(world, "Poppo (Uji)")));
                Transform water = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(point => point.name == "Water");
                if (water != null)
                    landmarks.Add(new GmQuestLandmark("river", "Sungai Sorelia", "Sungai di Desa Sorelia.", water));
                Transform fishingSpot = world.Find("Fishing Spot");
                if (fishingSpot != null)
                    landmarks.Add(new GmQuestLandmark(GmQuestCatalog.FishingSpot, "Spot mancing",
                        "Tepi sungai tempat Saka bisa memancing.", fishingSpot));
            }
            if (area == GmQuestArea.GoaLembayung)
            {
                landmarks.Add(new GmQuestLandmark(GmQuestCatalog.Encounter, "Musuh Goa",
                    "Musuh yang menghalangi jalan di dalam goa.", RequireChild(world, "Musuh Goa")));
                landmarks.Add(new GmQuestLandmark(GmQuestCatalog.Pakande, "Nenek Pakande",
                    "Posisi Nenek Pakande di dalam goa.", RequireChild(world, "Nenek Pakande")));
            }

            return new SceneSetup
            {
                Scene = scene, Area = area, Destination = destination, Hint = hint, Progress = quest,
                Player = player, Portal = portal, Landmarks = landmarks.ToArray(),
                Bounds = AreaBounds(scene, start.position, landmarks)
            };
        }

        private static Rect AreaBounds(Scene scene, Vector3 start, List<GmQuestLandmark> landmarks)
        {
            var bounds = new Bounds(start, Vector3.zero);
            foreach (GmQuestLandmark landmark in landmarks) bounds.Encapsulate(landmark.Point.position);
            foreach (Terrain terrain in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>()))
            {
                if (terrain.terrainData == null) continue;
                Vector3 size = terrain.terrainData.size;
                bounds.Encapsulate(terrain.transform.position);
                bounds.Encapsulate(terrain.transform.TransformPoint(size));
            }
            Vector3 center = bounds.center;
            float width = Mathf.Max(20f, bounds.size.x + 8f);
            float height = Mathf.Max(20f, bounds.size.z + 8f);
            return new Rect(center.x - width * .5f, center.z - height * .5f, width, height);
        }

        private static bool AttachJournal(SceneSetup setup)
        {
            var journal = setup.Hint.GetComponent<GmQuestJournal>();
            if (journal != null)
            {
                using (var data = new SerializedObject(journal))
                {
                    if (data.FindProperty("player").objectReferenceValue == setup.Player &&
                        data.FindProperty("mainQuest").objectReferenceValue == setup.Progress &&
                        data.FindProperty("exit").objectReferenceValue == setup.Portal &&
                        data.FindProperty("landmarks").arraySize != 0)
                        return false;
                }
            }
            else journal = Undo.AddComponent<GmQuestJournal>(setup.Hint.gameObject);

            Undo.RecordObject(journal, "Configure Quest Journal");
            journal.Configure(setup.Progress, setup.Player, setup.Area, setup.Portal, setup.Destination,
                setup.Bounds, setup.Landmarks);
            EditorUtility.SetDirty(journal);
            return true;
        }

        [MenuItem(PreviewMenu, true)]
        private static bool CanPreview() =>
            EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Village;

        [MenuItem(PreviewMenu)]
        private static void PreviewFishing()
        {
            if (!CanPreview()) return;
            Scene scene = SceneManager.GetActiveScene();
            var progress = RequireComponent<GmMainQuestProgress>(RequireRoot(scene, FlowRoot));
            if (!progress.HasCompleted(GmMainQuestStep.BicaraDenganNelayan))
            {
                Debug.Log("[Daeng Saka] Selesaikan percakapan dengan Nelayan setelah battle Poppo untuk menguji quest sampingan.");
                return;
            }
            Transform hint = RequireChild(RequireRoot(scene, WorldRoot), "Quest Encounter Hint");
            var journal = RequireComponent<GmQuestJournal>(hint);
            GmSideQuestDefinition fishing = GmQuestCatalog.Fishing;
            if (journal.TryStartSideQuest(fishing))
            {
                Debug.Log("[Daeng Saka] Preview Memancing di Sorelia aktif. Buka Jurnal untuk menguji pelacakan.");
                return;
            }
            int step = journal.CompletedSideSteps(fishing.Id);
            if (journal.TryCompleteSideStep(fishing.Id, step))
                Debug.Log("[Daeng Saka] Preview selesai: " + fishing.Goals[step].Text);
        }

        private static Transform RequireRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().SingleOrDefault(root => root.name == name)?.transform ??
            throw new InvalidOperationException($"'{name}' belum ada di {scene.name}. Pasang world dari 1C.4B-R1 dahulu.");

        private static Transform RequireChild(Transform parent, string name) =>
            parent.Find(name) ?? throw new InvalidOperationException($"'{name}' belum ada di bawah '{parent.name}'.");

        private static T RequireComponent<T>(Transform owner) where T : Component =>
            owner.TryGetComponent(out T component) ? component :
                throw new InvalidOperationException($"'{owner.name}' memerlukan {typeof(T).Name}.");
    }
}
