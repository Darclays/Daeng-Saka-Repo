using System;
using System.Collections.Generic;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
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
    internal static class GmDialogueSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Create NPC Dialogue Test Scene";
        private const string ResumeKey = "GamecommMentor.Dialogue.Setup.Resume";
        private const string CoreRoot = "Assets/GamecommMentor/Prefabs/Core/Dialogue";
        private const string UiRoot = "Assets/GamecommMentor/Prefabs/UI/Dialogue";
        private const string SceneRoot = "Assets/GamecommMentor/Scenes_GamecommMentor";
        private const string BasicUi = "Assets/Plugins/Pixel Crushers/Dialogue System/Prefabs/Standard UI Prefabs/Templates/Basic/Basic Standard Dialogue UI.prefab";
        private const string SpriteShader = "Assets/GamecommMentor/Art/2D/P01/GmSpriteUnlit.shader";

        [MenuItem(Menu)]
        private static void CreateExample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            try
            {
                Scene source = SceneManager.GetActiveScene();
                GmDialogueExampleRoot marker = InScene<GmDialogueExampleRoot>(source).FirstOrDefault();
                if (marker != null)
                {
                    Selection.activeGameObject = marker.gameObject;
                    Debug.Log("[Daeng Saka 1A.2B] This scene already contains the dialogue example. Existing setup was preserved.");
                    return;
                }
                GmSakaMotor[] heroes = InScene<GmSakaMotor>(source);
                if (heroes.Length != 1)
                    throw new InvalidOperationException("Open the exploration scene that passed 1A.1B. It must contain exactly one Saka motor.");
                if (InScene<DialogueSystemController>(source).Length > 1)
                    throw new InvalidOperationException("The active scene contains multiple Dialogue Managers. Keep one before creating the dialogue example.");
                GameObject basicUi = AssetDatabase.LoadAssetAtPath<GameObject>(BasicUi);
                Shader spriteShader = AssetDatabase.LoadAssetAtPath<Shader>(SpriteShader);
                if (basicUi == null || spriteShader == null)
                    throw new InvalidOperationException("The Pixel Crushers Basic Standard Dialogue UI and the approved exploration shader must be installed.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (!EnsurePixelCrushersInput()) return;

                GmDialogueExampleData.EnsureFolder(CoreRoot);
                GmDialogueExampleData.EnsureFolder(UiRoot);
                GmDialogueExampleData.EnsureFolder(SceneRoot);
                string path = AssetDatabase.GenerateUniqueAssetPath(SceneRoot + "/Exploration_Dialogue_Test.unity");
                if (!EditorSceneManager.SaveScene(source, path, true))
                    throw new InvalidOperationException("Cannot save a copy of the exploration scene.");
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                EditorUtility.DisplayProgressBar("Daeng Saka - NPC Dialogue Setup", "Preparing dialogue examples...", 0.2f);
                DialogueDatabase database = GmDialogueExampleData.GetOrCreate();
                GameObject uiPrefab = GetDialogueUi(basicUi);
                GameObject managerPrefab = GetManagerPrefab(database, uiPrefab);
                GmDialogueNpcSprites.Prepared fisher = GmDialogueNpcSprites.Prepare(GmDialogueNpcSprites.FisherSource, "Nelayan");
                GmDialogueNpcSprites.Prepared merchant = GmDialogueNpcSprites.Prepare(GmDialogueNpcSprites.MerchantSource, "Pedagang");
                Material material = GetNpcMaterial(spriteShader);
                GameObject fisherPrefab = GetNpcPrefab("Nelayan", GmDialogueExampleData.NelayanConversation, fisher, material);
                GameObject merchantPrefab = GetNpcPrefab("Pedagang", GmDialogueExampleData.MerchantConversation, merchant, material);
                GameObject promptPrefab = GetPromptPrefab();
                CreatePlayerVariant();

                GmSakaMotor hero = InScene<GmSakaMotor>(scene).Single();
                GmInteractor interactor = PreparePlayer(hero.gameObject);
                Camera camera = InScene<Camera>(scene).FirstOrDefault(c => c.CompareTag("MainCamera"));
                if (camera == null) throw new InvalidOperationException("The exploration scene needs its MainCamera.");
                interactor.SetCamera(camera);
                Persist(interactor);
                GameObject root = new GameObject("NPC Dialogue Example");
                root.AddComponent<GmDialogueExampleRoot>();

                DialogueSystemController manager = InScene<DialogueSystemController>(scene).FirstOrDefault();
                if (manager == null)
                    manager = ((GameObject)PrefabUtility.InstantiatePrefab(managerPrefab, scene)).GetComponent<DialogueSystemController>();
                if (!manager.gameObject.activeInHierarchy || !manager.enabled)
                    throw new InvalidOperationException("Enable the scene's Dialogue Manager before testing.");
                GmPixelCrushersDialogueBridge bridge = manager.GetComponent<GmPixelCrushersDialogueBridge>();
                if (bridge == null) bridge = manager.gameObject.AddComponent<GmPixelCrushersDialogueBridge>();
                if (manager.GetComponent<GmPixelCrushersInputBridge>() == null)
                    manager.gameObject.AddComponent<GmPixelCrushersInputBridge>();
                bridge.SetPlayer(interactor);
                bridge.SetAdditionalDatabase(manager.initialDatabase == database ? null : database);
                Persist(bridge);

                PlaceNpc(fisherPrefab, root.transform, new Vector3(-1.05f, 0.025f, -1.4f));
                PlaceNpc(merchantPrefab, root.transform, new Vector3(1.05f, 0.025f, -1.4f));
                GameObject prompt = (GameObject)PrefabUtility.InstantiatePrefab(promptPrefab, scene);
                prompt.transform.SetParent(root.transform, false);
                GmInteractionPrompt promptView = prompt.GetComponent<GmInteractionPrompt>();
                promptView.SetInteractor(interactor);
                Persist(promptView);
                CreateSightBlocker(root.transform);
                EnsureEventSystem(scene, root.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the dialogue test scene.");
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = root;
                Debug.Log("[Daeng Saka 1A.2B] Ready: " + path +
                    "\nPlay: approach and face an NPC, press E. Use the dialogue's Continue button or Space, then choose a reply." +
                    "\nThe source exploration scene and its existing prefab settings were preserved.");
            }
            catch (Exception exception)
            {
                SessionState.SetBool(ResumeKey, false);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Daeng Saka - NPC Dialogue Setup", "Setup stopped. Read the first Console error before continuing.", "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static bool EnsurePixelCrushersInput()
        {
#if ENABLE_INPUT_SYSTEM && !USE_NEW_INPUT
            SessionState.SetBool(ResumeKey, true);
            NamedBuildTarget active = NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            AddNewInputSymbol(NamedBuildTarget.Standalone);
            if (active.TargetName != NamedBuildTarget.Standalone.TargetName) AddNewInputSymbol(active);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log("[Daeng Saka 1A.2B] Enabling Pixel Crushers' new Input System support. Setup resumes automatically after scripts reload.");
            return false;
#else
            return true;
#endif
        }

        private static void AddNewInputSymbol(NamedBuildTarget target)
        {
            var symbols = new HashSet<string>(PlayerSettings.GetScriptingDefineSymbols(target)
                .Split(';').Select(s => s.Trim()).Where(s => s.Length > 0));
            if (symbols.Add("USE_NEW_INPUT"))
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols.OrderBy(s => s)));
        }

        [DidReloadScripts]
        private static void ResumeAfterReload()
        {
            if (!SessionState.GetBool(ResumeKey, false)) return;
            SessionState.SetBool(ResumeKey, false);
            EditorApplication.delayCall += CreateExample;
        }

        private static T[] InScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static GmInteractor PreparePlayer(GameObject root)
        {
            if (root.GetComponent<GmInteractInput>() == null) root.AddComponent<GmInteractInput>();
            GmInteractor interactor = root.GetComponent<GmInteractor>();
            if (interactor == null) interactor = root.AddComponent<GmInteractor>();
            DialogueActor actor = root.GetComponent<DialogueActor>();
            if (actor == null) actor = root.AddComponent<DialogueActor>();
            if (string.IsNullOrEmpty(actor.actor)) actor.actor = "Saka";
            Persist(actor);
            return interactor;
        }

        private static void CreatePlayerVariant()
        {
            string path = CoreRoot + "/Saka_Interaction.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GamecommMentor/Prefabs/Core/P01/Saka_Exploration_P01.prefab");
            if (source == null) return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                PreparePlayer(instance);
                SavePrefab(instance, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static GameObject GetDialogueUi(GameObject basic)
        {
            string path = UiRoot + "/Exploration_Dialogue_UI.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(basic);
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                {
                    text.font = font;
                    Persist(text);
                }
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.name.IndexOf("Continue", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Text text = button.GetComponentInChildren<Text>(true);
                    if (text != null) { text.text = "Lanjut"; Persist(text); }
                }
                return SavePrefab(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject GetManagerPrefab(DialogueDatabase database, GameObject ui)
        {
            string path = CoreRoot + "/Dialogue_Manager.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject("Dialogue Manager");
            try
            {
                DialogueSystemController controller = root.AddComponent<DialogueSystemController>();
                controller.initialDatabase = database;
                controller.dontDestroyOnLoad = false;
                controller.allowOnlyOneInstance = true;
                controller.warmUpConversationController = DialogueSystemController.WarmUpMode.Off;
                controller.displaySettings.dialogueUI = ui;
                controller.displaySettings.defaultCanvas = AddCanvas(root.transform, "Dialogue Canvas", 50).GetComponent<Canvas>();
                controller.displaySettings.subtitleSettings.continueButton = DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;
                controller.displaySettings.subtitleSettings.requireContinueOnLastLine = true;
                controller.displaySettings.subtitleSettings.showPCSubtitlesDuringLine = false;
                controller.displaySettings.subtitleSettings.skipPCSubtitleAfterResponseMenu = true;
                controller.displaySettings.inputSettings.alwaysForceResponseMenu = true;
                PixelCrushers.InputDeviceManager input = root.AddComponent<PixelCrushers.InputDeviceManager>();
                input.singleton = false;
                input.inputDevice = PixelCrushers.InputDevice.Mouse;
                input.controlCursorState = false;
                root.AddComponent<GmPixelCrushersInputBridge>();
                root.AddComponent<GmPixelCrushersDialogueBridge>();
                return SavePrefab(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Material GetNpcMaterial(Shader shader)
        {
            string root = "Assets/GamecommMentor/Art/2D/DialogueExamples";
            string path = root + "/NPC_Unlit.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Material material = new Material(shader) { name = "NPC_Unlit" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject GetNpcPrefab(string name, string conversation,
            GmDialogueNpcSprites.Prepared sprites, Material material)
        {
            string path = CoreRoot + "/NPC_" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject(name);
            try
            {
                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.height = Mathf.Max(1f, sprites.Height);
                collider.radius = 0.24f;
                collider.center = Vector3.up * collider.height * 0.5f;
                DialogueActor actor = root.AddComponent<DialogueActor>();
                actor.actor = name;
                GmPixelCrushersConversation interaction = root.AddComponent<GmPixelCrushersConversation>();
                interaction.SetPresentation(name, "Bicara");
                interaction.SetConversation(conversation);
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites.First;
                renderer.sharedMaterial = material;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                Animator animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = sprites.Controller;
                animator.applyRootMotion = false;
                visual.AddComponent<GmSpriteBillboard>();
                return SavePrefab(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void PlaceNpc(GameObject prefab, Transform parent, Vector3 position)
        {
            GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            npc.transform.SetParent(parent, false);
            npc.transform.position = position;
            Persist(npc.transform);
        }

        private static GameObject GetPromptPrefab()
        {
            string path = UiRoot + "/Interaction_Prompt.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = AddCanvas(null, "Interaction Prompt", 40);
            try
            {
                CanvasGroup group = root.AddComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
                GameObject panel = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 34f);
                rect.sizeDelta = new Vector2(480f, 44f);
                Image background = panel.GetComponent<Image>();
                background.color = new Color(0.035f, 0.05f, 0.065f, 0.9f);
                background.raycastTarget = false;
                GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(panel.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12f, 4f);
                textRect.offsetMax = new Vector2(-12f, -4f);
                Text text = textObject.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                root.AddComponent<GmInteractionPrompt>().SetView(panel, text);
                panel.SetActive(false);
                return SavePrefab(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject AddCanvas(Transform parent, string name, int order)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) root.transform.SetParent(parent, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static void CreateSightBlocker(Transform root)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Interaction Sight Blocker";
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(0f, 0.5f, -0.25f);
            wall.transform.localScale = new Vector3(3.8f, 1f, 0.3f);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            string path = "Assets/GamecommMentor/Art/2D/DialogueExamples/Sight_Blocker.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "Sight_Blocker" };
                material.SetColor("_BaseColor", new Color(0.29f, 0.34f, 0.38f));
                AssetDatabase.CreateAsset(material, path);
            }
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void EnsureEventSystem(Scene scene, Transform parent)
        {
            EventSystem[] systems = InScene<EventSystem>(scene);
            EventSystem selected = systems.FirstOrDefault(s => s.enabled && s.gameObject.activeInHierarchy);
            if (selected == null)
            {
                GameObject root = new GameObject("EventSystem");
                root.transform.SetParent(parent, false);
                selected = root.AddComponent<EventSystem>();
            }
            foreach (EventSystem system in systems)
                if (system != selected) { system.enabled = false; Persist(system); }
#if ENABLE_INPUT_SYSTEM
            foreach (BaseInputModule module in selected.GetComponents<BaseInputModule>())
                if (!(module is InputSystemUIInputModule)) { module.enabled = false; Persist(module); }
            InputSystemUIInputModule input = selected.GetComponent<InputSystemUIInputModule>();
            if (input == null) { input = selected.gameObject.AddComponent<InputSystemUIInputModule>(); input.AssignDefaultActions(); }
            input.enabled = true;
            Persist(input);
#else
            if (selected.GetComponent<StandaloneInputModule>() == null) selected.gameObject.AddComponent<StandaloneInputModule>();
#endif
            Persist(selected);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null) throw new InvalidOperationException("Cannot save prefab: " + path);
            return saved;
        }

        private static void Persist(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value))
                PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
