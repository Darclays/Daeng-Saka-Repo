using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmVisualPolishSetup
    {
        private static void PopulateAtmosphere(Transform root, Transform level, Transform world, string folder, string area)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Art + "GmAmbienceParticle.shader");
            var dust = GetMaterial(folder + "Partikel Udara.mat", shader, material => material.SetFloat("_Gain", 1f));
            Transform start = RequireChild(world, "Spawn - Start");
            if (area == Capital)
            {
                Vector3 market = (world.Find("Penempa Besi") ?? start).position;
                Drift(root, "Debu pelataran", market + Vector3.up * 2.1f,
                    new Vector3(14f, 3.5f, 10f), ColorFrom("#F5D9A2"), dust, 22, 2.7f);
                return;
            }
            if (area == Village)
            {
                Transform invaded = world.Find("Keluarga Nelayan/Posisi Keluarga/Saat Diserang/Ayah");
                Transform safe = world.Find("Keluarga Nelayan/Posisi Keluarga/Setelah Ditolong/Ayah");
                Vector3 field = Vector3.Lerp((invaded ?? start).position, (safe ?? invaded ?? start).position, .42f);
                Drift(root, "Serbuk halaman nelayan", field + Vector3.up * 1.65f,
                    new Vector3(21f, 2.8f, 11f), ColorFrom("#E9D69B"), dust, 28, 3.2f);
                return;
            }

            var quest = Require<GmMainQuestProgress>(RequireRoot(root.gameObject.scene, "Daeng Saka - Encounter Flow"));
            Transform boss = quest.PakandeEncounter != null ? quest.PakandeEncounter.transform : world.Find("Nenek Pakande");
            Vector3 entrance = start.position;
            Vector3 chamber = boss != null ? boss.position : entrance + start.forward * 22f;
            Vector3 forward = Vector3.ProjectOnPlane(chamber - entrance, Vector3.up).normalized;
            if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(start.forward, Vector3.up).normalized;
            Vector3 entryPocket = entrance + forward * 6.3f;
            Vector3 chamberPocket = chamber - forward * 4.5f;
            Drift(root, "Debu mulut goa", entryPocket + Vector3.up * 2f,
                new Vector3(9f, 3.5f, 8f), ColorFrom("#D1DBDC"), dust, 19, 2.3f);
            Drift(root, "Debu ruang Pakande", chamberPocket + Vector3.up * 2f,
                new Vector3(10f, 3.8f, 8f), ColorFrom("#ADBBD0"), dust, 23, 2.6f);

            TerrainCollider terrain = level.GetComponentInChildren<TerrainCollider>();
            if (terrain == null)
            {
                Debug.LogWarning("[Daeng Saka] Lentera goa memerlukan Terrain Collider.", level);
                return;
            }
            var ember = GetMaterial(folder + "Bara Lentera.mat", shader, material => material.SetFloat("_Gain", 2.5f));
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            var baseMaterial = GetMaterial(folder + "Batu Lentera.mat", lit, material =>
            {
                material.SetColor("_BaseColor", ColorFrom("#514D50"));
                material.SetFloat("_Smoothness", .22f);
            });
            var metal = GetMaterial(folder + "Bingkai Lentera.mat", lit, material =>
            {
                material.SetColor("_BaseColor", ColorFrom("#745139"));
                material.SetFloat("_Metallic", .28f);
                material.SetFloat("_Smoothness", .36f);
            });
            var glass = GetMaterial(folder + "Kaca Lentera.mat", lit, material =>
            {
                Color amber = ColorFrom("#FFAC62");
                material.SetColor("_BaseColor", amber);
                material.SetColor("_EmissionColor", amber.linear * 3.3f);
                material.EnableKeyword("_EMISSION");
            });

            Physics.SyncTransforms();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            PlaceLantern(root, terrain, "Lentera mulut goa", entryPocket, forward, right, 4.1f,
                baseMaterial, metal, glass, ember);
            PlaceLantern(root, terrain, "Lentera ruang Pakande", chamberPocket, forward, -right, 5.6f,
                baseMaterial, metal, glass, ember);
        }

        private static void PlaceLantern(Transform root, TerrainCollider terrain, string name, Vector3 anchor,
            Vector3 forward, Vector3 side, float intensity, Material stone, Material metal, Material glass, Material embers)
        {
            Transform lantern = root.Find(name);
            if (lantern == null)
            {
                Vector3[] candidates =
                {
                    anchor + side * 2.5f,
                    anchor + side * 2.0f - forward * 1.3f,
                    anchor - side * 2.5f,
                    anchor - side * 2.0f - forward * 1.3f
                };
                foreach (Vector3 point in candidates)
                {
                    if (!terrain.Raycast(new Ray(point + Vector3.up * 4f, Vector3.down), out RaycastHit ground, 8f) ||
                        ground.normal.y < .74f || Mathf.Abs(ground.point.y - anchor.y) > 1.25f ||
                        !HasLanternClearance(terrain, ground.point))
                        continue;
                    lantern = VisualChild(root, name);
                    lantern.SetPositionAndRotation(ground.point, Quaternion.LookRotation(forward, Vector3.up));
                    break;
                }
            }
            if (lantern == null)
            {
                Debug.LogWarning($"[Daeng Saka] Tidak ada pijakan tepi jalur untuk '{name}'.", root);
                return;
            }

            LanternPart(lantern, "Alas", new Vector3(0f, .11f, 0f), new Vector3(.50f, .22f, .50f), stone);
            LanternPart(lantern, "Pilar", new Vector3(0f, .52f, 0f), new Vector3(.19f, .67f, .19f), stone);
            LanternPart(lantern, "Dudukan", new Vector3(0f, .87f, 0f), new Vector3(.33f, .10f, .33f), metal);
            LanternPart(lantern, "Nyala", new Vector3(0f, 1.035f, 0f), new Vector3(.22f, .24f, .22f), glass);
            LanternPart(lantern, "Atap", new Vector3(0f, 1.20f, 0f), new Vector3(.37f, .10f, .37f), metal);
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner < 2 ? -.133f : .133f;
                float z = (corner & 1) == 0 ? -.133f : .133f;
                LanternPart(lantern, "Bilah " + (corner + 1), new Vector3(x, 1.035f, z),
                    new Vector3(.035f, .25f, .035f), metal);
            }

            Transform glow = VisualChild(lantern, "Pendar amber");
            Undo.RecordObject(glow, UndoName);
            glow.localPosition = new Vector3(0f, 1.29f, 0f);
            var light = GetOrAdd<Light>(glow);
            Undo.RecordObject(light, UndoName);
            light.type = LightType.Point;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = ColorFrom("#FFB26B");
            light.useColorTemperature = false;
            light.intensity = intensity;
            light.range = 6.3f;
            light.shadows = LightShadows.None;

            Transform spark = VisualChild(lantern, "Bara api");
            Undo.RecordObject(spark, UndoName);
            spark.localPosition = new Vector3(0f, 1.205f, 0f);
            spark.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = Particles(spark, embers, ColorFrom("#FFBF69"), 13, 5.2f, true);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .12f;
            shape.angle = 13f;
        }

        private static bool HasLanternClearance(TerrainCollider terrain, Vector3 bottom)
        {
            foreach (Collider body in Physics.OverlapCapsule(bottom + Vector3.up * .28f,
                         bottom + Vector3.up * 1.40f, .26f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (body != terrain && body.gameObject.scene == terrain.gameObject.scene) return false;
            return true;
        }

        private static void LanternPart(Transform lantern, string name, Vector3 position, Vector3 size, Material material)
        {
            Transform part = lantern.Find(name);
            if (part == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = name;
                Object.DestroyImmediate(cube.GetComponent<Collider>());
                SceneManager.MoveGameObjectToScene(cube, lantern.gameObject.scene);
                part = cube.transform;
                part.SetParent(lantern, false);
                Undo.RegisterCreatedObjectUndo(cube, UndoName);
            }
            Undo.RecordObject(part, UndoName);
            part.localPosition = position;
            part.localScale = size;
            var renderer = Require<MeshRenderer>(part);
            Undo.RecordObject(renderer, UndoName);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Drift(Transform parent, string name, Vector3 center, Vector3 dimensions, Color color,
            Material material, int limit, float rate)
        {
            Transform host = VisualChild(parent, name);
            Undo.RecordObject(host, UndoName);
            host.position = center;
            var ps = Particles(host, material, color, limit, rate, false);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = dimensions;
            shape.randomDirectionAmount = .72f;
        }

        private static ParticleSystem Particles(Transform host, Material material, Color color, int limit, float rate, bool spark)
        {
            var ps = GetOrAdd<ParticleSystem>(host);
            Undo.RecordObject(ps, UndoName);
            var main = ps.main;
            main.duration = spark ? 3f : 8f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.Pause;
            main.maxParticles = limit;
            main.startLifetime = spark ? new ParticleSystem.MinMaxCurve(.68f, 2f) : new ParticleSystem.MinMaxCurve(3.2f, 6.5f);
            main.startSpeed = spark ? new ParticleSystem.MinMaxCurve(.20f, .54f) : new ParticleSystem.MinMaxCurve(.06f, .21f);
            main.startSize = spark ? new ParticleSystem.MinMaxCurve(.021f, .067f) : new ParticleSystem.MinMaxCurve(.035f, .086f);
            main.startColor = color;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(spark ? .65f : .30f, .21f),
                    new GradientAlphaKey(spark ? .35f : .25f, .66f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
            var renderer = GetOrAdd<ParticleSystemRenderer>(host);
            Undo.RecordObject(renderer, UndoName);
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }
    }
}
