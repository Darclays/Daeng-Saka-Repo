using System;
using UnityEditor;
using UnityEngine;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmVisualPolishSetup
    {
        // Authored TerrainData referenced by both Terrain and TerrainCollider in DesaSorelia.
        private const string SoreliaTerrainGuid = "4f9ac4e7e43dd724e88646ec9d22c2b4";

        private static Texture2D Texture(string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name + ".png");
            if (texture == null) throw new InvalidOperationException($"Tekstur visual '{name}' belum diimpor.");
            return texture;
        }

        private static void PolishCapitalFloor(Transform level, string folder)
        {
            Texture2D stone = Texture("Batu_Pelataran");
            Texture2D normal = Texture("Batu_Pelataran_Normal");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            SetPaving(RequireChild(level, "GroundKota"), folder + "Batu Kota.mat",
                lit, stone, normal, new Vector2(7.2f, 5.6f));
            SetPaving(RequireChild(level, "Ground Kerajaan"), folder + "Batu Pelataran Istana.mat",
                lit, stone, normal, new Vector2(8.8f, 7f));
        }

        private static void SetPaving(Transform ground, string path, Shader lit, Texture2D stone, Texture2D normal,
            Vector2 metersPerRepeat)
        {
            var renderer = Require<MeshRenderer>(ground);
            Mesh mesh = Require<MeshFilter>(ground).sharedMesh;
            if (mesh == null) throw new InvalidOperationException($"Mesh lantai '{ground.name}' belum tersedia.");
            Vector3 size = mesh.bounds.size;
            Vector2 repeat = new Vector2(
                ground.TransformVector(Vector3.right * size.x).magnitude / metersPerRepeat.x,
                ground.TransformVector(Vector3.forward * size.z).magnitude / metersPerRepeat.y);
            Material material = GetMaterial(path, lit, created =>
            {
                created.SetTexture("_BaseMap", stone);
                created.SetTextureScale("_BaseMap", repeat);
                created.SetTexture("_BumpMap", normal);
                created.SetFloat("_BumpScale", .52f);
                created.EnableKeyword("_NORMALMAP");
                created.SetFloat("_Smoothness", .16f);
            });
            Undo.RecordObject(renderer, UndoName);
            renderer.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        private static void PolishVillageGround(Terrain terrain, string folder)
        {
            var collider = Require<TerrainCollider>(terrain.transform);
            TerrainData rendered = terrain.terrainData;
            TerrainData collision = collider.terrainData;
            bool renderedCopy = IsVillagePolishCopy(rendered, folder);
            bool collisionCopy = IsVillagePolishCopy(collision, folder);
            if (!renderedCopy && !collisionCopy)
            {
                if (rendered == null || rendered != collision)
                    throw new InvalidOperationException("Terrain dan Terrain Collider Sorelia harus menunjuk data yang sama.");
                return;
            }

            // Keep an authored reference from either component when one is still available.
            TerrainData authored = renderedCopy ? null : rendered;
            if (authored == null && !collisionCopy) authored = collision;
            if (authored == null)
            {
                string path = AssetDatabase.GUIDToAssetPath(SoreliaTerrainGuid);
                authored = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            }
            if (authored == null || IsVillagePolishCopy(authored, folder))
                throw new InvalidOperationException("Terrain asli Sorelia tidak ditemukan. Periksa Assets/New Terrain 1.asset beserta file .meta di proyek.");

            Undo.RecordObject(terrain, UndoName);
            Undo.RecordObject(collider, UndoName);
            terrain.terrainData = authored;
            collider.terrainData = authored;
            terrain.Flush();
            PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            Debug.Log($"[Daeng Saka 1E.41B R2] Jalur Sorelia dipulihkan dari {AssetDatabase.GetAssetPath(authored)}.", terrain);
        }

        private static bool IsVillagePolishCopy(TerrainData data, string folder)
        {
            return data != null && AssetDatabase.GetAssetPath(data)
                .StartsWith(folder + "Terrain - Sorelia", StringComparison.Ordinal);
        }

        private static void PolishVillageWater(Transform level, string folder)
        {
            var renderer = level.Find("Water")?.GetComponent<MeshRenderer>();
            Material source = renderer != null ? renderer.sharedMaterial : null;
            if (source == null || !source.HasProperty("_ShallowColor") || !source.HasProperty("_DeepColor") ||
                !source.HasProperty("_WaterDepth") || !source.HasProperty("_WaveAmptitude"))
            {
                Debug.LogWarning("[Daeng Saka] Visual air Sorelia memerlukan material SimpleWater pada LevelDesign/Water.", level);
                return;
            }

            string path = folder + "Air - Siang Tropis.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                // SimpleWater uses this multiplier to blend shallow and deep colors by scene depth.
                material = new Material(source);
                material.SetColor("_ShallowColor", ColorFrom("#71928C"));
                material.SetColor("_DeepColor", ColorFrom("#355D60"));
                material.SetFloat("_WaterDepth", .65f);
                material.SetFloat("_WaveAmptitude", .045f);
                AssetDatabase.CreateAsset(material, path);
            }

            if (renderer.sharedMaterial == material) return;
            Undo.RecordObject(renderer, UndoName);
            renderer.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }
}
