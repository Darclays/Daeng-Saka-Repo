using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmVisualPolishSetup
    {
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
            TerrainData source = terrain.terrainData;
            var collider = Require<TerrainCollider>(terrain.transform);
            if (source == null || collider.terrainData != source)
                throw new InvalidOperationException("Terrain dan Terrain Collider Sorelia harus menunjuk data yang sama.");

            TerrainLayer[] layers = source.terrainLayers;
            bool changed = false;
            for (int i = 0; i < layers.Length; i++)
            {
                TerrainLayer layer = layers[i];
                if (layer == null) continue;
                string file = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(layer));
                TerrainLayer polished = file switch
                {
                    "Terrain_Grass" => GetTerrainLayer(folder, layer, "Rumput_Sorelia", 5.4f),
                    "Soil" => GetTerrainLayer(folder, layer, "Tanah_Sorelia", 5.2f),
                    "WalkPath" => GetTerrainLayer(folder, layer, "Jalur_Sorelia", 4.6f),
                    _ => layer
                };
                if (polished == layer) continue;
                layers[i] = polished;
                changed = true;
            }
            if (!changed)
            {
                if (!AssetDatabase.GetAssetPath(source).StartsWith(folder, StringComparison.Ordinal))
                    Debug.LogWarning("[Daeng Saka] Terrain Sorelia tidak memakai layer Soil, Terrain_Grass, atau WalkPath.", terrain);
                return;
            }

            // Preserve the same painted weights, heights, holes, and placed terrain instances.
            // Duplicate data once so edits to this scene never repaint the source terrain.
            TerrainData data = source;
            if (!AssetDatabase.GetAssetPath(source).StartsWith(folder, StringComparison.Ordinal))
            {
                data = Object.Instantiate(source);
                data.name = "Terrain - Sorelia";
                AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(folder + data.name + ".asset"));
            }
            Undo.RecordObject(data, UndoName);
            float[,,] paint = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            data.terrainLayers = layers;
            data.SetAlphamaps(0, 0, paint);
            EditorUtility.SetDirty(data);
            Undo.RecordObject(terrain, UndoName);
            Undo.RecordObject(collider, UndoName);
            terrain.terrainData = data;
            collider.terrainData = data;
            terrain.Flush();
            PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }

        private static TerrainLayer GetTerrainLayer(string folder, TerrainLayer source, string art, float tile)
        {
            string path = folder + art + ".terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer != null) return layer;

            Texture2D texture = Texture(art);
            layer = Object.Instantiate(source);
            layer.name = art;
            layer.diffuseTexture = texture;
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = Vector4.one;
            layer.normalMapTexture = null;
            layer.maskMapTexture = null;
            layer.tileSize = new Vector2(tile, tile);
            layer.metallic = 0f;
            layer.smoothness = 0f;
            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }
    }
}
