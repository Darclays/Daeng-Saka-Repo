using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmOrbitSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Create Horizontal Orbit Test Scene";
        private const string SceneRoot = "Assets/GamecommMentor/Scenes_GamecommMentor";
        private const string CameraRoot = "Assets/GamecommMentor/Prefabs/Core/Camera";
        private const string AnimationRoot = "Assets/GamecommMentor/Data/Character/OrbitExamples";
        private const string PlayerPrefab = "Assets/GamecommMentor/Prefabs/Core/Dialogue/Saka_Interaction.prefab";
        private const string FixedCameraPrefab = "Assets/GamecommMentor/Prefabs/Core/P01/FixedFollowCamera_P01.prefab";

        [MenuItem(Menu)]
        private static void CreateExample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            try
            {
                Scene source = SceneManager.GetActiveScene();
                GmOrbitExampleRoot existing = InScene<GmOrbitExampleRoot>(source).FirstOrDefault();
                if (existing != null)
                {
                    Selection.activeGameObject = existing.gameObject;
                    Debug.Log("[Daeng Saka 1A.1B-R1] This scene already contains the orbit test. Existing tuning was preserved.");
                    return;
                }
                int heroCount = InScene<GmSakaMotor>(source).Length;
                if (heroCount > 1)
                    throw new InvalidOperationException("Keep one GamecommMentor Saka motor in the active scene before creating its orbit test.");
                GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
                GameObject cameraAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FixedCameraPrefab);
                if (playerAsset == null || cameraAsset == null)
                    throw new InvalidOperationException("Complete the accepted exploration and NPC dialogue setup first. Their Saka_Interaction and camera prefabs are required.");
                ValidateDirectionMapping();
                Vector3 spawn = new Vector3(0f, 0.1f, 0f);
                if (Selection.activeTransform != null && Selection.activeTransform.gameObject.scene == source)
                    spawn = Selection.activeTransform.position;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                GmDialogueExampleData.EnsureFolder(SceneRoot);
                string path = AssetDatabase.GenerateUniqueAssetPath(SceneRoot + "/Exploration_Horizontal_Orbit_Test.unity");
                if (!EditorSceneManager.SaveScene(source, path, true))
                    throw new InvalidOperationException("Cannot save a copy of the active scene.");
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                EditorUtility.DisplayProgressBar("Daeng Saka - Horizontal Orbit", "Preparing camera and directional sprites...", 0.3f);

                GmSakaMotor hero = InScene<GmSakaMotor>(scene).SingleOrDefault();
                bool addedPlayer = hero == null;
                if (addedPlayer)
                {
                    GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset, scene);
                    player.name = "Saka";
                    player.transform.position = spawn;
                    Persist(player.transform);
                    hero = player.GetComponent<GmSakaMotor>();
                }
                Camera[] mainCameras = InScene<Camera>(scene)
                    .Where(c => c.enabled && c.gameObject.activeInHierarchy && c.CompareTag("MainCamera")).ToArray();
                Camera camera = mainCameras.Length == 1 &&
                    (mainCameras[0].GetComponent<GmFixedFollowCamera>() != null || mainCameras[0].GetComponent<GmOrbitFollowCamera>() != null)
                    ? mainCameras[0] : null;
                if (camera == null)
                {
                    // Only the test copy is affected. Do not reuse an unknown student camera controller.
                    foreach (Camera other in InScene<Camera>(scene)) { other.enabled = false; Persist(other); }
                    foreach (AudioListener listener in InScene<AudioListener>(scene)) { listener.enabled = false; Persist(listener); }
                    GameObject cameraObject = (GameObject)PrefabUtility.InstantiatePrefab(cameraAsset, scene);
                    cameraObject.name = "Main Camera - Horizontal Orbit";
                    camera = cameraObject.GetComponent<Camera>();
                }
                GmOrbitFollowCamera orbit = ConfigureCamera(camera, hero);
                var ignored = new List<Transform>();
                foreach (GmSpriteBillboard billboard in InScene<GmSpriteBillboard>(scene))
                {
                    billboard.SetCamera(camera);
                    Persist(billboard);
                }
                foreach (GmDialogueExampleRoot example in InScene<GmDialogueExampleRoot>(scene))
                {
                    foreach (GmInteractable npc in example.GetComponentsInChildren<GmInteractable>(true))
                        ignored.Add(npc.transform);
                    PrepareExampleNpcViews(example, camera);
                }
                // These four invisible walls belong only to the accepted sandbox, not the actual level.
                foreach (Transform item in InScene<Transform>(scene))
                    if (item.parent != null && item.parent.name == "P01_Test_Area" &&
                        new[] { "Boundary_North", "Boundary_South", "Boundary_East", "Boundary_West" }.Contains(item.name))
                        ignored.Add(item);
                orbit.SetIgnoredRoots(ignored.ToArray());
                Persist(orbit);
                GameObject marker = new GameObject("Horizontal Orbit Test");
                marker.AddComponent<GmOrbitExampleRoot>();
                if (InScene<GmDialogueExampleRoot>(scene).Length > 0)
                    CreateTestWall(marker.transform, hero.transform.position, camera.transform.eulerAngles.y);
                GetCameraPrefab(camera, orbit);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save the orbit test scene.");
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = hero.gameObject;
                Debug.Log("[Daeng Saka 1A.1B-R1] Ready: " + path +
                    "\nWASD: move. Hold right mouse + move horizontally: orbit. Escape: release mouse." +
                    (addedPlayer ? "\nA Saka rig was added. Place it on a safe floor before Play; disable old player rigs in this test copy if present." : "") +
                    "\nThe source scene and source prefabs were preserved.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Daeng Saka - Horizontal Orbit", "Setup stopped. Read the first Console error before continuing.", "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static GmOrbitFollowCamera ConfigureCamera(Camera camera, GmSakaMotor hero)
        {
            GmOrbitFollowCamera orbit = camera.GetComponent<GmOrbitFollowCamera>();
            bool newOrbit = orbit == null;
            GmFixedFollowCamera fixedFollow = camera.GetComponent<GmFixedFollowCamera>();
            float height = 0.8f;
            float heading = camera.transform.eulerAngles.y;
            float pitch = 30.4f;
            float distance = 9.28f;
            if (fixedFollow != null)
            {
                if (newOrbit)
                {
                    fixedFollow.SetTarget(hero.transform);
                    height = new SerializedObject(fixedFollow).FindProperty("focusHeight").floatValue;
                    Vector3 offset = camera.transform.position - (hero.transform.position + Vector3.up * height);
                    distance = offset.magnitude;
                    pitch = Mathf.Atan2(offset.y, new Vector2(offset.x, offset.z).magnitude) * Mathf.Rad2Deg;
                    heading = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
                }
                fixedFollow.enabled = false;
                Persist(fixedFollow);
            }
            if (newOrbit) orbit = camera.gameObject.AddComponent<GmOrbitFollowCamera>();
            orbit.SetTarget(hero.transform, hero.GetComponent<GmControlGate>());
            if (newOrbit) orbit.ConfigureView(heading, pitch, distance, height);
            orbit.enabled = true;
            camera.orthographic = false;
            hero.SetCamera(camera);
            GmInteractor interactor = hero.GetComponent<GmInteractor>();
            if (interactor != null) { interactor.SetCamera(camera); Persist(interactor); }
            Persist(hero);
            Persist(camera);
            Persist(camera.transform);
            Persist(orbit);
            return orbit;
        }

        private static void PrepareExampleNpcViews(GmDialogueExampleRoot root, Camera camera)
        {
            foreach (GmInteractable npc in root.GetComponentsInChildren<GmInteractable>(true))
            {
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(npc.gameObject);
                string path = AssetDatabase.GetAssetPath(prefab);
                string label, frontSource;
                if (path == "Assets/GamecommMentor/Prefabs/Core/Dialogue/NPC_Nelayan.prefab")
                { label = "Nelayan"; frontSource = GmDialogueNpcSprites.FisherSource; }
                else if (path == "Assets/GamecommMentor/Prefabs/Core/Dialogue/NPC_Pedagang.prefab")
                { label = "Pedagang"; frontSource = GmDialogueNpcSprites.MerchantSource; }
                else continue;
                Animator animator = npc.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.GetComponent<SpriteRenderer>() == null) continue;
                string controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                if (controllerPath != "Assets/GamecommMentor/Data/Character/DialogueExamples/" + label + "_Idle.controller" &&
                    controllerPath != AnimationRoot + "/" + label + "_Directional.controller")
                {
                    Debug.LogWarning("[Daeng Saka] Preserved the customized NPC controller: " + npc.name);
                    continue;
                }
                animator.runtimeAnimatorController = GetDirectionalController(frontSource, label);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                GmNpcDirectionalAnimator driver = animator.GetComponent<GmNpcDirectionalAnimator>();
                if (driver == null) driver = animator.gameObject.AddComponent<GmNpcDirectionalAnimator>();
                driver.Configure(npc.transform, camera);
                Persist(driver);
                Persist(animator);
            }
        }

        private static AnimatorController GetDirectionalController(string frontSource, string label)
        {
            GmDialogueExampleData.EnsureFolder(AnimationRoot);
            string path = AnimationRoot + "/" + label + "_Directional.controller";
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            GmDialogueNpcSprites.Prepared front = GmDialogueNpcSprites.Prepare(frontSource, label);
            GmDialogueNpcSprites.Prepared back = GmDialogueNpcSprites.Prepare(frontSource.Replace("_Front.png", "_Back.png"), label + "_Orbit_Back");
            GmDialogueNpcSprites.Prepared side = GmDialogueNpcSprites.Prepare(frontSource.Replace("_Front.png", "_Side.png"), label + "_Orbit_Side");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (controller == null) throw new InvalidOperationException("Cannot create directional NPC controller.");
            controller.AddParameter(new AnimatorControllerParameter
                { name = "FacingX", type = AnimatorControllerParameterType.Float, defaultFloat = 0f });
            controller.AddParameter(new AnimatorControllerParameter
                { name = "FacingY", type = AnimatorControllerParameterType.Float, defaultFloat = -1f });
            BlendTree tree;
            AnimatorState state = controller.CreateBlendTreeInController("Idle", out tree);
            state.writeDefaultValues = false;
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            tree.blendParameter = "FacingX";
            tree.blendParameterY = "FacingY";
            tree.useAutomaticThresholds = false;
            tree.AddChild(front.Controller.animationClips.Single(), Vector2.down);
            tree.AddChild(back.Controller.animationClips.Single(), Vector2.up);
            tree.AddChild(side.Controller.animationClips.Single(), Vector2.left);
            tree.AddChild(side.Controller.animationClips.Single(), Vector2.right);
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void GetCameraPrefab(Camera source, GmOrbitFollowCamera orbit)
        {
            GmDialogueExampleData.EnsureFolder(CameraRoot);
            string path = CameraRoot + "/Horizontal_Orbit_Camera.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = new GameObject("Horizontal_Orbit_Camera");
            try
            {
                root.tag = "MainCamera";
                Camera camera = root.AddComponent<Camera>();
                EditorUtility.CopySerialized(source, camera);
                root.AddComponent<AudioListener>();
                GmOrbitFollowCamera follow = root.AddComponent<GmOrbitFollowCamera>();
                EditorUtility.CopySerialized(orbit, follow);
                follow.SetTarget(null, null);
                follow.SetIgnoredRoots(new Transform[0]);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new InvalidOperationException("Cannot save the orbit camera prefab.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateTestWall(Transform root, Vector3 playerPosition, float heading)
        {
            string art = "Assets/GamecommMentor/Art/3D/CameraExamples";
            GmDialogueExampleData.EnsureFolder(art);
            string path = art + "/Camera_Test_Wall.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("The camera test wall needs the project's URP shader.");
                material = new Material(shader) { name = "Camera_Test_Wall" };
                material.SetColor("_BaseColor", new Color(0.38f, 0.27f, 0.19f));
                AssetDatabase.CreateAsset(material, path);
            }
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Camera Test Wall";
            wall.transform.SetParent(root, false);
            Quaternion rotation = Quaternion.Euler(0f, heading, 0f);
            wall.transform.position = playerPosition + rotation * Vector3.right * 4f + Vector3.up * 1.5f;
            wall.transform.rotation = rotation;
            wall.transform.localScale = new Vector3(0.35f, 3f, 5f);
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static T[] InScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static void ValidateDirectionMapping()
        {
            // Small, deterministic checks of the actual runtime helper, executed by Unity at setup.
            GameObject probe = new GameObject("Orbit Direction Check") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera view = probe.AddComponent<Camera>();
                view.enabled = false;
                Vector3[] headings = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
                Vector2[,] expected = {
                    { Vector2.up, Vector2.left, Vector2.down, Vector2.right },
                    { Vector2.right, Vector2.up, Vector2.left, Vector2.down },
                    { Vector2.down, Vector2.right, Vector2.up, Vector2.left },
                    { Vector2.left, Vector2.down, Vector2.right, Vector2.up }
                };
                for (int h = 0; h < headings.Length; h++)
                for (int yaw = 0; yaw < 4; yaw++)
                {
                    view.transform.rotation = Quaternion.Euler(30f, yaw * 90f, 0f);
                    if (GmCardinalFacing.FromWorld(headings[h], view, Vector2.zero, 0f) != expected[h, yaw])
                        throw new InvalidOperationException("Camera-relative sprite direction check failed.");
                }
                view.transform.rotation = Quaternion.identity;
                if (GmCardinalFacing.FromWorld(Quaternion.Euler(0f, 46f, 0f) * Vector3.forward, view, Vector2.up) != Vector2.up ||
                    GmCardinalFacing.FromWorld(Quaternion.Euler(0f, 50f, 0f) * Vector3.forward, view, Vector2.up) != Vector2.right ||
                    GmCardinalFacing.FromWorld(Quaternion.Euler(0f, 44f, 0f) * Vector3.forward, view, Vector2.right) != Vector2.right ||
                    GmCardinalFacing.FromWorld(Quaternion.Euler(0f, 40f, 0f) * Vector3.forward, view, Vector2.right) != Vector2.up)
                    throw new InvalidOperationException("Sprite direction boundary check failed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        private static void Persist(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
