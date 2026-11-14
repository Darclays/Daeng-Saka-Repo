using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmEncounterSetup
    {
        private const string SceneFolder = "Assets/GamecommMentor/Scenes_GamecommMentor";
        private const string ArtFolder = "Assets/GamecommMentor/Art/3D/EncounterExamples";

        [MenuItem("GamecommMentor/Daeng Saka/Create Encounter Test Scene")]
        private static void CreateExample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            try
            {
                Scene source = SceneManager.GetActiveScene();
                GmEncounterFlow[] existing = InScene<GmEncounterFlow>(source).ToArray();
                if (existing.Length > 0)
                {
                    if (existing.Length != 1 || string.IsNullOrEmpty(source.path) ||
                        AssetDatabase.LoadAssetAtPath<SceneAsset>(existing[0].BattleScenePath) == null)
                        throw new InvalidOperationException("The current scene has an incomplete encounter setup.");
                    EnableScenes(source.path, existing[0].BattleScenePath);
                    Selection.activeGameObject = existing[0].gameObject;
                    Debug.Log("[Daeng Saka 1A.3B-R1] This scene already has its encounter test.");
                    return;
                }
                if (InScene<GmSakaMotor>(source).Count(motor => motor.isActiveAndEnabled) != 1)
                    throw new InvalidOperationException("Open the accepted orbit scene with one active Saka before creating the encounter test.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
                bool canRestorePreviousSetup = !source.isDirty &&
                    previousSetup.All(scene => !string.IsNullOrEmpty(scene.path));
                GmDialogueExampleData.EnsureFolder(SceneFolder);
                string explorationPath = AssetDatabase.GenerateUniqueAssetPath(SceneFolder + "/Exploration_Encounter_Test.unity");
                string battlePath = AssetDatabase.GenerateUniqueAssetPath(SceneFolder + "/Battle_Encounter_Test.unity");
                if (!EditorSceneManager.SaveScene(source, explorationPath, true))
                    throw new InvalidOperationException("Could not save the exploration test copy.");

                GameObject enemy;
                try
                {
                    CreateBattleScene(battlePath);
                    Scene exploration = EditorSceneManager.OpenScene(explorationPath, OpenSceneMode.Single);
                    GmSakaMotor hero = InScene<GmSakaMotor>(exploration).Single(motor => motor.isActiveAndEnabled);
                    GameObject flowObject = new GameObject("Encounter Flow");
                    GmEncounterFlow flow = flowObject.AddComponent<GmEncounterFlow>();
                    flow.SetBattleScene(battlePath);
                    enemy = CreateEnemy(exploration, hero, flow);

                    if (!EditorSceneManager.SaveScene(exploration))
                        throw new InvalidOperationException("Could not save the encounter setup.");
                }
                catch
                {
                    if (canRestorePreviousSetup)
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                        AssetDatabase.DeleteAsset(explorationPath);
                    }
                    else
                    {
                        // Preserve the snapshot when the source had unsaved changes.
                        EditorSceneManager.OpenScene(explorationPath, OpenSceneMode.Single);
                    }
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(battlePath) != null)
                        AssetDatabase.DeleteAsset(battlePath);
                    throw;
                }
                EnableScenes(explorationPath, battlePath);
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = enemy;
                Debug.Log("[Daeng Saka 1A.3B-R1] Play the exploration test and touch the orange enemy. " +
                    "Exploration: " + explorationPath + " | Battle: " + battlePath, enemy);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Daeng Saka Encounter", "Setup berhenti. Periksa pesan pertama di Console.", "OK");
            }
        }

        private static void CreateBattleScene(string path)
        {
            Scene battle = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!battle.IsValid() || SceneManager.GetActiveScene() != battle)
                throw new InvalidOperationException("Could not create the battle test scene.");

            GameObject root = new GameObject("Battle Root", typeof(GmEncounterBattleRoot));
            GmEncounterTestPanel panel = root.AddComponent<GmEncounterTestPanel>();
            GameObject cameraObject = new GameObject("Battle Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.042f, 0.075f, 0.105f);
            camera.cullingMask = 0;

            GameObject canvasObject = new GameObject("Battle Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform card = MakeRect("Encounter Card", canvasObject.transform,
                new Vector2(580f, 300f), Vector2.zero);
            Image background = card.gameObject.AddComponent<Image>();
            background.color = new Color(0.089f, 0.16f, 0.218f);
            background.raycastTarget = false;
            MakeText("Title", card, font, "Battle Uji", 36, new Vector2(480f, 52f), new Vector2(0f, 94f));
            Text enemyName = MakeText("Enemy", card, font, "Musuh Uji", 24,
                new Vector2(480f, 42f), new Vector2(0f, 35f));
            Text hint = MakeText("Hint", card, font, "Pilih hasil untuk menguji kembali ke eksplorasi.", 17,
                new Vector2(510f, 34f), new Vector2(0f, -14f));
            hint.color = new Color(0.76f, 0.83f, 0.88f);
            Button retreat = MakeButton("Mundur", card, font, new Vector2(-120f, -88f),
                new Color(0.21f, 0.31f, 0.40f));
            Button victory = MakeButton("Uji Menang", card, font, new Vector2(120f, -88f),
                new Color(0.12f, 0.40f, 0.35f));
            panel.SetView(enemyName, retreat, victory);

            GameObject eventObject = new GameObject("Battle EventSystem", typeof(EventSystem));
            eventObject.transform.SetParent(root.transform, false);
            eventObject.GetComponent<EventSystem>().firstSelectedGameObject = retreat.gameObject;
#if ENABLE_INPUT_SYSTEM
            eventObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#elif ENABLE_LEGACY_INPUT_MANAGER
            eventObject.AddComponent<StandaloneInputModule>();
#endif
            // The encounter flow activates this root after suspending exploration.
            root.SetActive(false);
            if (!EditorSceneManager.SaveScene(battle, path))
                throw new InvalidOperationException("Could not save the battle test scene.");
        }

        private static GameObject CreateEnemy(Scene scene, GmSakaMotor hero, GmEncounterFlow flow)
        {
            Camera camera = InScene<Camera>(scene).FirstOrDefault(view =>
                view.isActiveAndEnabled && view.CompareTag("MainCamera"));
            Vector3 right = camera != null
                ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up) : Vector3.right;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            GameObject enemy = new GameObject("Musuh Uji");
            enemy.transform.position = hero.transform.position - right.normalized * 3.2f + Vector3.up * 0.8f;
            GmEnemyEncounter encounter = enemy.AddComponent<GmEnemyEncounter>();
            encounter.Configure(flow, "enemy-test-01", "Musuh Uji");
            SphereCollider trigger = enemy.GetComponent<SphereCollider>();
            trigger.radius = 0.55f;
            trigger.isTrigger = true;
            Rigidbody body = enemy.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Orange Marker";
            visual.transform.SetParent(enemy.transform, false);
            visual.transform.localScale = new Vector3(0.9f, 0.8f, 0.9f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = EnemyMaterial();
            return enemy;
        }

        private static Material EnemyMaterial()
        {
            GmDialogueExampleData.EnsureFolder(ArtFolder);
            string path = ArtFolder + "/Encounter_Enemy_Orange.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No shader available for the encounter marker.");
            Material material = new Material(shader) { color = new Color(0.92f, 0.34f, 0.09f) };
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(path));
            return material;
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Text MakeText(string name, Transform parent, Font font, string value,
            int size, Vector2 dimensions, Vector2 position)
        {
            RectTransform rect = MakeRect(name, parent, dimensions, position);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(string label, Transform parent, Font font, Vector2 position, Color color)
        {
            Vector2 size = new Vector2(210f, 54f);
            RectTransform rect = MakeRect(label, parent, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            MakeText("Label", rect, font, label, 21, size, Vector2.zero);
            return button;
        }

        private static void EnableScenes(params string[] paths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;
            foreach (string path in paths)
            {
                int index = scenes.FindIndex(scene => scene.path == path);
                if (index >= 0 && scenes[index].enabled) continue;
                var entry = new EditorBuildSettingsScene(path, true);
                if (index < 0) scenes.Add(entry);
                else scenes[index] = entry;
                changed = true;
            }
            if (changed) EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static IEnumerable<T> InScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
