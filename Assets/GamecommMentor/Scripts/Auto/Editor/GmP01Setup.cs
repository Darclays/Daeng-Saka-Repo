using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmP01Setup
    {
        private const string PrefabRoot = "Assets/GamecommMentor/Prefabs/Core/P01";
        private const string MaterialRoot = "Assets/GamecommMentor/Art/2D/P01";
        private const string SceneRoot = "Assets/GamecommMentor/Scenes_GamecommMentor";

        [MenuItem("GamecommMentor/Daeng Saka/Create Exploration Test Scene")]
        private static void CreateTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Daeng Saka 1A.1B] Stop Play Mode before creating the test scene.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Daeng Saka - Exploration Setup", "Preparing Saka source sprites...", 0.1f);
                Dictionary<string, Sprite[]> sprites = GmP01SpritePreparation.Prepare();
                GmP01SpritePreparation.EnsureFolder(PrefabRoot);
                GmP01SpritePreparation.EnsureFolder(MaterialRoot);
                GmP01SpritePreparation.EnsureFolder(SceneRoot);
                Shader spriteShader = AssetDatabase.LoadAssetAtPath<Shader>(MaterialRoot + "/GmSpriteUnlit.shader");
                Shader worldShader = Shader.Find("Universal Render Pipeline/Lit");
                if (spriteShader == null || worldShader == null)
                    throw new InvalidOperationException("Exploration setup requires the supplied sprite shader and the project's URP package.");

                AnimatorController controller = CreateController(sprites);
                Material spriteMaterial = GetMaterial("Saka_Unlit", spriteShader, Color.white);
                Material groundMaterial = GetMaterial("Ground", worldShader, new Color(0.18f, 0.29f, 0.27f));
                Material obstacleMaterial = GetMaterial("Obstacles", worldShader, new Color(0.49f, 0.47f, 0.38f));

                EditorUtility.DisplayProgressBar("Daeng Saka - Exploration Setup", "Creating test scene and prefabs...", 0.7f);
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject playerPrefab = CreatePlayerPrefab(controller, sprites["Idle_Front"][0], spriteMaterial);
                GameObject cameraPrefab = CreateCameraPrefab();
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                player.name = "Saka";
                player.transform.position = new Vector3(0f, 0.05f, -4f);
                GameObject cameraObject = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab, scene);
                Camera camera = cameraObject.GetComponent<Camera>();
                GmFixedFollowCamera follow = cameraObject.GetComponent<GmFixedFollowCamera>();
                follow.SetTarget(player.transform);
                GmSakaMotor motor = player.GetComponent<GmSakaMotor>();
                motor.SetCamera(camera);
                GmSpriteBillboard billboard = player.GetComponentInChildren<GmSpriteBillboard>();
                billboard.SetCamera(camera);
                billboard.transform.rotation = camera.transform.rotation;
                PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(billboard);
                PrefabUtility.RecordPrefabInstancePropertyModifications(billboard.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(follow);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cameraObject.transform);
                CreateWorld(groundMaterial, obstacleMaterial);
                string path = AssetDatabase.GenerateUniqueAssetPath(SceneRoot + "/P01_Exploration_Test.unity");
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("Unity could not save the exploration test scene.");
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = player;
                Debug.Log("[Daeng Saka 1A.1B] Ready: " + path +
                    "\nPress Play, focus the Game view, then use WASD or arrow keys." +
                    "\nSprite source metadata was prepared; see Data/Character/P01/P01_Preparation_Report.txt.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Daeng Saka - Exploration Setup", "Setup stopped. Read the Console error before continuing.", "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static AnimatorController CreateController(Dictionary<string, Sprite[]> sprites)
        {
            string root = GmP01SpritePreparation.DataRoot;
            string path = root + "/Saka_Exploration_P01.controller";
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            var motions = new Dictionary<string, AnimationClip>();
            foreach (KeyValuePair<string, Sprite[]> pair in sprites)
            {
                string clipPath = root + "/Saka_" + pair.Key + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    float fps = pair.Key.StartsWith("Idle", StringComparison.Ordinal) ? 6f : 10f;
                    clip = new AnimationClip { name = "Saka_" + pair.Key, frameRate = fps };
                    var keys = new ObjectReferenceKeyframe[pair.Value.Length + 1];
                    for (int i = 0; i < keys.Length; i++)
                        keys[i] = new ObjectReferenceKeyframe
                        {
                            time = i / fps,
                            value = pair.Value[i % pair.Value.Length]
                        };
                    EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                    AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    AssetDatabase.CreateAsset(clip, clipPath);
                }
                motions.Add(pair.Key, clip);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (controller == null) throw new InvalidOperationException("Cannot create Saka Animator Controller.");
            controller.AddParameter(new AnimatorControllerParameter
                { name = "FacingX", type = AnimatorControllerParameterType.Float, defaultFloat = 0f });
            controller.AddParameter(new AnimatorControllerParameter
                { name = "FacingY", type = AnimatorControllerParameterType.Float, defaultFloat = -1f });
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            BlendTree idleTree, walkTree;
            AnimatorState idle = controller.CreateBlendTreeInController("Idle", out idleTree);
            AnimatorState walk = controller.CreateBlendTreeInController("Walk", out walkTree);
            ConfigureTree(idleTree, "Idle", motions);
            ConfigureTree(walkTree, "Walking", motions);
            idle.writeDefaultValues = false;
            walk.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = idle;
            AnimatorStateTransition toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
            AnimatorStateTransition toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureTree(BlendTree tree, string action, Dictionary<string, AnimationClip> clips)
        {
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            tree.blendParameter = "FacingX";
            tree.blendParameterY = "FacingY";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips[action + "_Front"], Vector2.down);
            tree.AddChild(clips[action + "_Back"], Vector2.up);
            tree.AddChild(clips[action + "_Left"], Vector2.left);
            tree.AddChild(clips[action + "_Right"], Vector2.right);
        }

        private static GameObject CreatePlayerPrefab(AnimatorController controller, Sprite initialSprite, Material material)
        {
            string path = PrefabRoot + "/Saka_Exploration_P01.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject("Saka_Exploration_P01");
            try
            {
                root.tag = "Player";
                CharacterController body = root.AddComponent<CharacterController>();
                body.height = 1.7f;
                body.center = new Vector3(0f, 0.85f, 0f);
                body.radius = 0.24f;
                body.skinWidth = 0.025f;
                body.stepOffset = 0.25f;
                body.slopeLimit = 45f;
                body.minMoveDistance = 0f;
                root.AddComponent<GmMoveInput>();
                root.AddComponent<GmControlGate>();
                GmSakaMotor motor = root.AddComponent<GmSakaMotor>();
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * 0.5f;
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = initialSprite;
                renderer.sharedMaterial = material;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Animator animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                visual.AddComponent<GmAnimatorDriver>().SetMotor(motor);
                visual.AddComponent<GmSpriteBillboard>();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new InvalidOperationException("Cannot save Saka prefab.");
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateCameraPrefab()
        {
            string path = PrefabRoot + "/FixedFollowCamera_P01.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject("FixedFollowCamera_P01");
            try
            {
                root.tag = "MainCamera";
                Camera camera = root.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.065f, 0.09f, 0.12f);
                camera.fieldOfView = 40f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                root.AddComponent<AudioListener>();
                root.AddComponent<GmFixedFollowCamera>();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new InvalidOperationException("Cannot save camera prefab.");
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Material GetMaterial(string name, Shader shader, Color color)
        {
            string path = MaterialRoot + "/P01_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = "P01_" + name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateWorld(Material ground, Material obstacles)
        {
            GameObject area = new GameObject("P01_Test_Area");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.SetParent(area.transform, false);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            floor.GetComponent<Renderer>().sharedMaterial = ground;
            CreateBox(area.transform, "Obstacle_A", new Vector3(-2f, 0.75f, 0f),
                new Vector3(2f, 1.5f, 2f), obstacles, true);
            CreateBox(area.transform, "Obstacle_B", new Vector3(2.5f, 0.45f, 2f),
                new Vector3(1.5f, 0.9f, 3f), obstacles, true);
            // Invisible boundary colliders keep the follow camera clear of perimeter walls.
            CreateBox(area.transform, "Boundary_North", new Vector3(0f, 1.5f, 10f),
                new Vector3(21f, 3f, 0.4f), obstacles, false);
            CreateBox(area.transform, "Boundary_South", new Vector3(0f, 1.5f, -10f),
                new Vector3(21f, 3f, 0.4f), obstacles, false);
            CreateBox(area.transform, "Boundary_East", new Vector3(10f, 1.5f, 0f),
                new Vector3(0.4f, 3f, 21f), obstacles, false);
            CreateBox(area.transform, "Boundary_West", new Vector3(-10f, 1.5f, 0f),
                new Vector3(0.4f, 3f, 21f), obstacles, false);
            GameObject lightObject = new GameObject("Sun");
            lightObject.transform.SetParent(area.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
        }

        private static void CreateBox(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, bool visible)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = visible;
        }
    }
}
