using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmVisualPolishSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Visual Polish";
        private const string UndoName = "Set Up Visual Polish";
        private const string Scenes = "Assets/GamecommMentor/Scenes_GamecommMentor/";
        private const string Art = "Assets/GamecommMentor/Art/VisualPolish/";
        private const string Generated = "Assets/GamecommMentor/VisualPolish/Generated/";
        private const string Capital = "IbuKota";
        private const string Village = "DesaSorelia";
        private const string Cave = "GoaLembanyung";
        private const string RootName = "Daeng Saka - Petualangan Hangat";

        private sealed class Look
        {
            public string Sky, Equator, Ground, Horizon, Sun;
            public float SunIntensity, Reflection, Exposure, Contrast, Saturation, Temperature, Bloom, Vignette;
            public Vector2 FogDistances;
            public Vector4 Shadows, Midtones, Highlights;
        }

        private static readonly Look CapitalLook = new Look
        {
            Sky = "#BBC9DD", Equator = "#A6927B", Ground = "#5F5041",
            Horizon = "#C6C3AF", Sun = "#FFE2B8", SunIntensity = 1.23f, Reflection = .70f,
            FogDistances = new Vector2(27f, 136f), Exposure = .10f, Contrast = 8f, Saturation = 7f,
            Temperature = 8f, Bloom = .10f, Vignette = .12f,
            Shadows = new Vector4(.94f, 1f, 1.10f, .025f), Midtones = new Vector4(1.04f, 1.01f, .96f, 0f),
            Highlights = new Vector4(1.06f, 1.02f, .94f, 0f)
        };

        private static readonly Look VillageLook = new Look
        {
            Sky = "#B5C6D2", Equator = "#A5A18A", Ground = "#675841",
            Horizon = "#BBC3AD", Sun = "#FFE4B8", SunIntensity = 1.14f, Reflection = .63f,
            FogDistances = new Vector2(23f, 112f), Exposure = .11f, Contrast = 7f, Saturation = 8f,
            Temperature = 9f, Bloom = .12f, Vignette = .13f,
            Shadows = new Vector4(.94f, 1.02f, 1.08f, .035f), Midtones = new Vector4(1.02f, 1.02f, .96f, 0f),
            Highlights = new Vector4(1.07f, 1.025f, .94f, 0f)
        };

        private static readonly Look CaveLook = new Look
        {
            Sky = "#97B0C5", Equator = "#6E8B9C", Ground = "#61556D",
            Horizon = "#3C505D", Sun = "#D6E5FF", SunIntensity = .63f, Reflection = .52f,
            FogDistances = new Vector2(13f, 69f), Exposure = .15f, Contrast = 9f, Saturation = 5f,
            Temperature = -5f, Bloom = .27f, Vignette = .18f,
            Shadows = new Vector4(.91f, 1.02f, 1.14f, .055f), Midtones = new Vector4(.98f, 1.01f, 1.04f, .025f),
            Highlights = new Vector4(1.065f, 1.015f, .94f, 0f)
        };

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Scenes + Capital + ".unity") ||
            CanInstall(Scenes + Village + ".unity") || CanInstall(Scenes + Cave + ".unity");

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
                Transform root = Install(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = root.gameObject;
                Debug.Log($"[Daeng Saka 1E.41B] Petualangan Hangat siap di {scene.name}. Simpan scene.", root);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static Transform Install(Scene scene)
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                throw new InvalidOperationException("Visual Polish memerlukan render pipeline URP proyek.");

            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Camera camera = Require<Camera>(RequireChild(world, "Saka - Orbit Camera"));
            Transform level = RequireRoot(scene, scene.name == Capital ? "LevelDesain" : "LevelDesign");
            Look look = scene.name == Capital ? CapitalLook : scene.name == Village ? VillageLook : CaveLook;
            string folder = Generated + scene.name + "/";
            if (!AssetDatabase.IsValidFolder(folder.TrimEnd('/')))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            Transform root = Array.Find(scene.GetRootGameObjects(), item => item.name == RootName)?.transform;
            if (root == null)
            {
                var host = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(host, scene);
                Undo.RegisterCreatedObjectUndo(host, UndoName);
                root = host.transform;
            }

            // Keep the world profiles outside the default mask used by the additive battle camera.
            const int volumeLayer = 1; // TransparentFX, a built-in GameObject layer.
            Volume volume = SetVolume(root, "Pewarnaan area", volumeLayer, 20f,
                GetProfile(folder + "Petualangan Hangat.asset", profile => ConfigureLook(profile, look)));
            Undo.RecordObject(volume, UndoName);
            volume.weight = 1f;
            ConfigureCamera(camera, volumeLayer);

            if (scene.name == Capital) PolishCapitalFloor(level, folder);
            if (scene.name == Village)
            {
                var quest = Require<GmMainQuestProgress>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
                PolishVillageGround(Require<Terrain>(RequireChild(level, "Terrain")), folder);
                var invasion = SetVolume(root, "Saat Sorelia diserang", volumeLayer, 21f,
                    GetProfile(folder + "Saat Invasi.asset", ConfigureInvasion));
                Undo.RecordObject(invasion, UndoName);
                invasion.weight = 1f;
                var recovery = GetOrAdd<GmSoreliaLightRecovery>(invasion.transform);
                Undo.RecordObject(recovery, UndoName);
                recovery.Configure(invasion, quest);
            }
            PopulateAtmosphere(root, level, world, folder, scene.name);

            Light sun = ConfigureSun(scene, root, look);
            Material sky = scene.name == Cave ? RenderSettings.skybox : CreateSky(folder, look, scene.name == Village);
            AssetDatabase.SaveAssets();
            ApplyEnvironment(look, sun, sky);
            return root;
        }

        private static void ConfigureCamera(Camera camera, int volumeLayer)
        {
            var data = GetOrAdd<UniversalAdditionalCameraData>(camera.transform);
            Undo.RecordObject(data, UndoName);
            Undo.RecordObject(camera, UndoName);
            data.renderPostProcessing = true;
            data.volumeLayerMask = data.volumeLayerMask.value | (1 << volumeLayer);
            camera.allowHDR = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }

        private static Volume SetVolume(Transform parent, string name, int layer, float priority, VolumeProfile profile)
        {
            Transform host = VisualChild(parent, name);
            Undo.RecordObject(host.gameObject, UndoName);
            host.gameObject.layer = layer;
            var volume = GetOrAdd<Volume>(host);
            Undo.RecordObject(volume, UndoName);
            volume.isGlobal = true;
            volume.priority = priority;
            volume.sharedProfile = profile;
            return volume;
        }

        private static VolumeProfile GetProfile(string path, Action<VolumeProfile> configure)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                configure(profile);
                AssetDatabase.CreateAsset(profile, path);
                foreach (VolumeComponent component in profile.components)
                {
                    component.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    AssetDatabase.AddObjectToAsset(component, profile);
                }
                EditorUtility.SetDirty(profile);
                return profile;
            }
            catch
            {
                if (AssetDatabase.Contains(profile)) AssetDatabase.DeleteAsset(path);
                else Object.DestroyImmediate(profile);
                throw;
            }
        }

        private static void ConfigureLook(VolumeProfile profile, Look look)
        {
            var grade = profile.Add<ColorAdjustments>();
            grade.postExposure.Override(look.Exposure);
            grade.contrast.Override(look.Contrast);
            grade.saturation.Override(look.Saturation);
            profile.Add<WhiteBalance>().temperature.Override(look.Temperature);

            var levels = profile.Add<ShadowsMidtonesHighlights>();
            levels.shadows.Override(look.Shadows);
            levels.midtones.Override(look.Midtones);
            levels.highlights.Override(look.Highlights);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.14f);
            bloom.intensity.Override(look.Bloom);
            bloom.scatter.Override(.58f);
            bloom.highQualityFiltering.Override(false);
            bloom.maxIterations.Override(4);

            var vignette = profile.Add<Vignette>();
            vignette.color.Override(ColorFrom("#25323E"));
            vignette.intensity.Override(look.Vignette);
            vignette.smoothness.Override(.61f);
        }

        private static void ConfigureInvasion(VolumeProfile profile)
        {
            var grade = profile.Add<ColorAdjustments>();
            grade.postExposure.Override(-.10f);
            grade.contrast.Override(9f);
            grade.saturation.Override(-6f);
            profile.Add<WhiteBalance>().temperature.Override(1f);
        }

        private static Light ConfigureSun(Scene scene, Transform root, Look look)
        {
            Light sun = RenderSettings.sun;
            if (sun == null || sun.gameObject.scene != scene || sun.type != LightType.Directional || !sun.isActiveAndEnabled)
                sun = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Light>())
                    .FirstOrDefault(light => light.type == LightType.Directional && light.isActiveAndEnabled);
            if (sun == null)
            {
                sun = GetOrAdd<Light>(VisualChild(root, "Matahari"));
                sun.type = LightType.Directional;
            }

            Undo.RecordObject(sun, UndoName);
            Undo.RecordObject(sun.transform, UndoName);
            sun.transform.rotation = Quaternion.Euler(scene.name == Village ? 42f : scene.name == Capital ? 37f : 50f, -32f, 0f);
            sun.useColorTemperature = false;
            sun.color = ColorFrom(look.Sun);
            sun.intensity = look.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = scene.name == Cave ? .70f : .80f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sun);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sun.transform);
            return sun;
        }

        private static Material CreateSky(string folder, Look look, bool village)
        {
            return GetMaterial(folder + "Langit.mat", Shader.Find("Skybox/Procedural"), material =>
            {
                material.SetFloat("_SunSize", .028f);
                material.SetFloat("_AtmosphereThickness", village ? 1.18f : 1.07f);
                material.SetColor("_SkyTint", ColorFrom(village ? "#889BA9" : "#879CB0"));
                material.SetColor("_GroundColor", ColorFrom(look.Horizon));
                material.SetFloat("_Exposure", .94f);
            });
        }

        private static void ApplyEnvironment(Look look, Light sun, Material sky)
        {
            RenderSettings.sun = sun;
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ColorFrom(look.Sky);
            RenderSettings.ambientEquatorColor = ColorFrom(look.Equator);
            RenderSettings.ambientGroundColor = ColorFrom(look.Ground);
            RenderSettings.reflectionIntensity = look.Reflection;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ColorFrom(look.Horizon);
            RenderSettings.fogStartDistance = look.FogDistances.x;
            RenderSettings.fogEndDistance = look.FogDistances.y;
        }

        private static Transform VisualChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var host = new GameObject(name);
            SceneManager.MoveGameObjectToScene(host, parent.gameObject.scene);
            host.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(host, UndoName);
            return host.transform;
        }

        private static Color ColorFrom(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
            throw new ArgumentException($"Warna visual tidak valid: {hex}");
        }

        private static Material GetMaterial(string path, Shader shader, Action<Material> configure)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (shader == null) throw new InvalidOperationException($"Shader belum tersedia untuk {path}.");
            var material = new Material(shader);
            configure(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
