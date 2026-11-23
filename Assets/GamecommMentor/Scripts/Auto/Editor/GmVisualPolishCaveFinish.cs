using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmVisualPolishSetup
    {
        private const string CaveFinishMenu = "GamecommMentor/Daeng Saka/Finish Goa Lembayung";
        private const string CaveFinishUndo = "Finish Goa Lembayung";
        internal const string CaveMeetingPoint = "Titik Kumpul";
        internal const string CaveTestGuide = "Pemandu Goa (Uji)";

        [MenuItem(CaveFinishMenu, true)]
        private static bool CanFinishCave() => CanInstall(Scenes + Cave + ".unity");

        [MenuItem(CaveFinishMenu)]
        private static void FinishCave()
        {
            if (!CanFinishCave()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(CaveFinishUndo);
            try
            {
                Transform meeting = InstallCaveFinish(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = meeting.gameObject;
                Debug.Log("[Daeng Saka 1E.45B] Goa Lembayung siap. Simpan scene.", meeting);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static Transform InstallCaveFinish(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform flow = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            var sequence = Require<GmPixelCrushersMainQuestDialogue>(flow);
            var journal = Require<GmQuestJournal>(RequireChild(world, "Quest Encounter Hint"));
            var terrain = Require<TerrainCollider>(RequireChild(RequireRoot(scene, "LevelDesign"), "Terrain"));
            Transform legacy = world.Find(CaveTestGuide);
            Transform existing = world.Find(CaveMeetingPoint);
            Vector3 center = (existing ?? legacy ?? RequireChild(world, "Spawn - Start")).position;
            if (legacy == null && existing == null) center = new Vector3(6.35f, center.y, 50.52f);
            Physics.SyncTransforms();
            if (!terrain.Raycast(new Ray(center + Vector3.up * 2f, Vector3.down), out RaycastHit floor, 5f) ||
                floor.normal.y < .74f)
                throw new InvalidOperationException("Pijakan Titik Kumpul belum siap di dekat pintu masuk Goa.");

            Transform root = Install(scene);
            string folder = Generated + Cave + "/";
            Transform meeting = SetCaveMeetingPoint(world, legacy, existing, floor.point, sequence, journal, folder);
            BuildCrystalTrail(root, terrain, folder);
            AssetDatabase.SaveAssets();
            return meeting;
        }

        private static Transform SetCaveMeetingPoint(Transform world, Transform legacy, Transform existing,
            Vector3 ground, GmPixelCrushersMainQuestDialogue sequence, GmQuestJournal journal, string folder)
        {
            Transform meeting = existing ?? VisualChild(world, CaveMeetingPoint);
            if (existing == null)
            {
                Undo.RecordObject(meeting, CaveFinishUndo);
                meeting.SetPositionAndRotation(ground + Vector3.up * .018f, Quaternion.identity);
            }
            var inspect = GetOrAdd<GmPixelCrushersConversation>(meeting);
            Undo.RecordObject(inspect, CaveFinishUndo);
            inspect.SetPresentation(CaveMeetingPoint, "Periksa");
            inspect.SetMainQuestDialogue(sequence);

            var zone = GetOrAdd<SphereCollider>(meeting);
            Undo.RecordObject(zone, CaveFinishUndo);
            zone.isTrigger = true;
            zone.enabled = true;
            zone.center = Vector3.up * .68f;
            zone.radius = 1.65f;

            Undo.RecordObject(sequence, CaveFinishUndo);
            sequence.SetSpeaker(GmMainQuestStep.MasukGuaLembayung, meeting);
            sequence.SetSpeaker(GmMainQuestStep.SelidikiGua, meeting);
            sequence.SetSpeaker(GmMainQuestStep.LawanPakande, meeting);
            Undo.RecordObject(journal, CaveFinishUndo);
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.Guide, CaveMeetingPoint,
                "Titik kumpul Saka dan kawan-kawan di depan lorong Goa Lembayung.", meeting));

            if (legacy != null)
            {
                // This array used to hide the guide during the arrival shot. Keep the other sprite bindings.
                Transform intro = world.Find("Cutscene - Masuk Goa Lembayung");
                var director = intro != null ? intro.GetComponent<GmSoreliaRescueCutscene>() : null;
                if (director != null)
                {
                    using var data = new SerializedObject(director);
                    SerializedProperty sprites = data.FindProperty("explorationSprites");
                    for (int index = sprites.arraySize - 1; index >= 0; index--)
                    {
                        var renderer = sprites.GetArrayElementAtIndex(index).objectReferenceValue as SpriteRenderer;
                        if (renderer == null || !renderer.transform.IsChildOf(legacy)) continue;
                        int length = sprites.arraySize;
                        sprites.DeleteArrayElementAtIndex(index);
                        if (sprites.arraySize == length) sprites.DeleteArrayElementAtIndex(index);
                    }
                    data.ApplyModifiedProperties();
                }
                Undo.DestroyObjectImmediate(legacy.gameObject);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material wood = CaveMaterial(folder, "Kayu Titik Kumpul", shader, "#745441", .28f);
            Material strap = CaveMaterial(folder, "Tali Titik Kumpul", shader, "#BD9C6B", .17f);
            Material cloth = CaveMaterial(folder, "Kain Titik Kumpul", shader, "#A64C36", .26f);
            Material stone = CaveMaterial(folder, "Batu Pijakan", shader, "#44485C", .38f);
            Transform supplies = VisualChild(meeting, "Perbekalan");
            Undo.RecordObject(supplies, CaveFinishUndo);
            supplies.localPosition = new Vector3(0f, .04f, -.57f);
            LanternPart(supplies, "Batu alas", new Vector3(-.38f, .025f, 0f), new Vector3(.86f, .10f, .64f), stone);
            LanternPart(supplies, "Peti", new Vector3(-.39f, .28f, 0f), new Vector3(.72f, .41f, .51f), wood);
            LanternPart(supplies, "Tutup peti", new Vector3(-.39f, .51f, 0f), new Vector3(.76f, .065f, .56f), wood);
            LanternPart(supplies, "Ikat kiri", new Vector3(-.62f, .30f, 0f), new Vector3(.06f, .46f, .545f), strap);
            LanternPart(supplies, "Ikat kanan", new Vector3(-.16f, .30f, 0f), new Vector3(.06f, .46f, .545f), strap);
            LanternPart(supplies, "Tikar gulung", new Vector3(.30f, .15f, .03f), new Vector3(.55f, .22f, .29f), cloth);
            LanternPart(supplies, "Ikat tikar", new Vector3(.29f, .15f, .03f), new Vector3(.065f, .24f, .31f), strap);
            LanternPart(supplies, "Tiang tanda", new Vector3(.68f, .62f, -.14f), new Vector3(.09f, 1.24f, .09f), wood);
            LanternPart(supplies, "Kain tanda", new Vector3(.46f, 1.03f, -.13f), new Vector3(.40f, .22f, .036f), cloth);
            LanternPart(supplies, "Ujung kain", new Vector3(.24f, 1.06f, -.13f), new Vector3(.05f, .16f, .038f), strap);
            return meeting;
        }

        private static void BuildCrystalTrail(Transform root, TerrainCollider terrain, string folder)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Material stone = CaveMaterial(folder, "Batu Pijakan", lit, "#44485C", .38f);
            Material blue = CaveMaterial(folder, "Kristal Biru", lit, "#4B8098", .48f, "#80D3E9", 1.65f);
            Material purple = CaveMaterial(folder, "Kristal Ungu", lit, "#6D558F", .43f, "#BC9BEF", 2.05f);
            Material tip = CaveMaterial(folder, "Kristal Pucat", lit, "#9291C8", .54f, "#D5BEF8", 2.3f);
            var dust = GetMaterial(folder + "Partikel Udara.mat", AssetDatabase.LoadAssetAtPath<Shader>(
                Art + "GmAmbienceParticle.shader"), material => material.SetFloat("_Gain", 1f));
            Transform trail = VisualChild(root, "Kristal Lembayung");
            Mesh shard = GetCrystalMesh(folder);

            // The pockets follow the actual tunnel bends. Each center sits beside the walkable route.
            MakeCrystalPocket(trail, terrain, "Mulut lorong", 27f, 49.90f, .89f, 25f, shard, stone, blue, tip, dust, "#7BBFD9");
            MakeCrystalPocket(trail, terrain, "Tebing lorong tengah", 42.65f, 58.65f, 1.13f, -42f, shard, stone, blue, tip, dust, "#89B6DF");
            MakeCrystalPocket(trail, terrain, "Lekuk selatan", 47f, 27.25f, .84f, 38f, shard, stone, blue, tip, dust, "#83BCD8");
            MakeCrystalPocket(trail, terrain, "Simpang ruang dalam", 69.70f, 61.55f, 1.05f, 72f, shard, stone, purple, tip, dust, "#B4A3E7");
            MakeCrystalPocket(trail, terrain, "Dinding ruang Pakande", 64f, 44.80f, 1.12f, -29f, shard, stone, purple, tip, dust, "#B59AE9");
            MakeCrystalPocket(trail, terrain, "Belakang ruang Pakande", 68f, 48.60f, 1.27f, 108f, shard, stone, purple, tip, dust, "#A68EE0");

            Material wood = CaveMaterial(folder, "Kayu Titik Kumpul", lit, "#745441", .28f);
            Material flame = CaveMaterial(folder, "Kaca Jejak", lit, "#C1844E", .29f, "#FFC27D", 3.3f);
            var embers = GetMaterial(folder + "Bara Lentera.mat", dust.shader, material => material.SetFloat("_Gain", 2.5f));
            PlaceLantern(root, terrain, "Lentera simpang", new Vector3(55.20f, .68f, 58.60f), Vector3.right, Vector3.forward,
                4.4f, stone, wood, flame, embers);
        }

        private static void MakeCrystalPocket(Transform trail, TerrainCollider terrain, string name, float x, float z,
            float height, float angle, Mesh shard, Material stone, Material main, Material tip, Material dust, string lightColor)
        {
            var anchor = new Vector3(x, 2f, z);
            if (!terrain.Raycast(new Ray(anchor + Vector3.up * 2f, Vector3.down), out RaycastHit ground, 8f) ||
                ground.normal.y < .74f || !HasLanternClearance(terrain, ground.point))
            {
                Debug.LogWarning($"[Daeng Saka] Kristal '{name}' dilewati karena pijakannya terhalang.", trail);
                return;
            }
            Transform pocket = VisualChild(trail, name);
            Undo.RecordObject(pocket, CaveFinishUndo);
            pocket.SetPositionAndRotation(ground.point, Quaternion.Euler(0f, angle, 0f));
            Crystal(pocket, "Singkapan", new Vector3(0f, -.025f, 0f), new Vector3(.53f, .24f, .43f),
                Quaternion.Euler(0f, 16f, 0f), shard, stone);
            Crystal(pocket, "Kristal utama", new Vector3(.035f, .08f, .02f), new Vector3(.225f, height, .19f),
                Quaternion.Euler(6f, 24f, -11f), shard, main);
            Crystal(pocket, "Kristal kecil kiri", new Vector3(-.22f, .015f, -.09f), new Vector3(.145f, height * .67f, .15f),
                Quaternion.Euler(-14f, 68f, 25f), shard, tip);
            Crystal(pocket, "Kristal kecil kanan", new Vector3(.22f, .005f, .09f), new Vector3(.13f, height * .48f, .125f),
                Quaternion.Euler(16f, -23f, -29f), shard, main);

            Transform glow = VisualChild(pocket, "Pendar kristal");
            Undo.RecordObject(glow, CaveFinishUndo);
            glow.localPosition = Vector3.up * (height + .14f);
            var light = GetOrAdd<Light>(glow);
            Undo.RecordObject(light, CaveFinishUndo);
            light.type = LightType.Point;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = ColorFrom(lightColor);
            light.useColorTemperature = false;
            light.range = 4.3f + height;
            light.intensity = 2.6f + height * .85f;
            light.shadows = LightShadows.None;

            Drift(pocket, "Serbuk kristal", pocket.position + Vector3.up * (height + .12f),
                new Vector3(2.1f, 1.65f, 1.85f), light.color, dust, 8, 1.18f);
        }

        private static Material CaveMaterial(string folder, string name, Shader shader, string color, float smoothness,
            string emission = null, float gain = 0f) => GetMaterial(folder + name + ".mat", shader, material =>
        {
            material.SetColor("_BaseColor", ColorFrom(color));
            material.SetFloat("_Smoothness", smoothness);
            if (emission == null) return;
            material.SetColor("_EmissionColor", ColorFrom(emission).linear * gain);
            material.EnableKeyword("_EMISSION");
        });

        private static void Crystal(Transform pocket, string name, Vector3 position, Vector3 size,
            Quaternion rotation, Mesh mesh, Material material)
        {
            Transform part = VisualChild(pocket, name);
            Undo.RecordObject(part, CaveFinishUndo);
            part.localPosition = position;
            part.localRotation = rotation;
            part.localScale = size;
            var filter = GetOrAdd<MeshFilter>(part);
            var renderer = GetOrAdd<MeshRenderer>(part);
            Undo.RecordObject(filter, CaveFinishUndo);
            Undo.RecordObject(renderer, CaveFinishUndo);
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh GetCrystalMesh(string folder)
        {
            string path = folder + "Kristal Bersegi.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;

            const int sides = 6;
            var vertices = new Vector3[sides * 7];
            var triangles = new int[sides * 9];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * (Mathf.PI * 2f / sides);
                float nextAngle = (side + 1) * (Mathf.PI * 2f / sides);
                var a = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var b = new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle));
                Vector3 upperA = a * .78f + Vector3.up * .67f;
                Vector3 upperB = b * .78f + Vector3.up * .67f;
                int v = side * 7;
                vertices[v] = a;
                vertices[v + 1] = upperA;
                vertices[v + 2] = upperB;
                vertices[v + 3] = b;
                vertices[v + 4] = upperA;
                vertices[v + 5] = new Vector3(.10f, 1f, -.08f);
                vertices[v + 6] = upperB;
                int i = side * 9;
                triangles[i] = v;
                triangles[i + 1] = v + 1;
                triangles[i + 2] = v + 2;
                triangles[i + 3] = v;
                triangles[i + 4] = v + 2;
                triangles[i + 5] = v + 3;
                triangles[i + 6] = v + 4;
                triangles[i + 7] = v + 5;
                triangles[i + 8] = v + 6;
            }
            mesh = new Mesh { name = "Kristal Bersegi", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
