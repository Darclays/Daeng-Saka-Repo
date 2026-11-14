using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmP01SpritePreparation
    {
        private const int Cell = 640;
        private const string SourceRoot = "Assets/Asset2D_Pixel/MainCharacter/MC_Saka/";
        internal const string DataRoot = "Assets/GamecommMentor/Data/Character/P01";

        private sealed class SheetPlan
        {
            public string Key;
            public string Path;
            public string BaseName;
            public int Count;
            public int Width;
            public Vector2 Pivot;
        }

        internal static Dictionary<string, Sprite[]> Prepare()
        {
            // Validate every source before changing any importer.
            List<SheetPlan> plans = BuildPlans();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string backup = Path.Combine(projectRoot, "Library/GamecommMentor/P01",
                "Backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_ffff", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(backup);
            foreach (SheetPlan plan in plans)
            {
                string meta = Path.Combine(projectRoot, plan.Path + ".meta");
                if (File.Exists(meta)) File.Copy(meta, Path.Combine(backup, plan.BaseName + ".png.meta"));
            }

            var result = new Dictionary<string, Sprite[]>();
            var report = new StringBuilder();
            report.AppendLine("Daeng Saka 1A.1B - source preparation");
            report.AppendLine("PNG images are unchanged. Import metadata is prepared for the exploration prototype.");
            report.AppendLine("Original metadata backup: " + backup);
            report.AppendLine("Pixels Per Unit: 100. Point filter, no mipmaps, no compression.");
            report.AppendLine("Pivots are a first-pass foot anchor per clip; validate visually in Unity.");
            foreach (SheetPlan plan in plans)
            {
                TextureImporter importer = AssetImporter.GetAtPath(plan.Path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing texture importer: " + plan.Path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                int nativeLimit = Mathf.NextPowerOfTwo(Mathf.Max(plan.Width, Cell));
                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, nativeLimit);
                TextureImporterPlatformSettings windows = importer.GetPlatformTextureSettings("Standalone");
                if (windows.overridden)
                {
                    windows.maxTextureSize = Mathf.Max(windows.maxTextureSize, nativeLimit);
                    windows.textureCompression = TextureImporterCompression.Uncompressed;
                    windows.format = TextureImporterFormat.RGBA32;
                    importer.SetPlatformTextureSettings(windows);
                }

                var factories = new SpriteDataProviderFactories();
                factories.Init();
                ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                if (provider == null) throw new InvalidOperationException("2D Sprite data provider is unavailable.");
                provider.InitSpriteEditorDataProvider();
                SpriteRect[] previous = provider.GetSpriteRects();
                var rects = new List<SpriteRect>();
                var names = new HashSet<string>();
                for (int i = 0; i < plan.Count; i++)
                {
                    string name = plan.BaseName + "_" + i;
                    names.Add(name);
                    SpriteRect old = previous.FirstOrDefault(r => r.name == name);
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        spriteID = old != null ? old.spriteID : GUID.Generate(),
                        rect = new Rect(i * Cell, 0, Cell, Cell),
                        alignment = SpriteAlignment.Custom,
                        pivot = plan.Pivot,
                        border = old != null ? old.border : Vector4.zero
                    });
                }
                // Preserve additional sprites that the developer may have authored locally.
                rects.AddRange(previous.Where(r => !names.Contains(r.name)));
                provider.SetSpriteRects(rects.ToArray());
                ISpriteNameFileIdDataProvider ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                if (ids == null) throw new InvalidOperationException("Sprite name/ID provider is unavailable.");
                var pairs = ids.GetNameFileIdPairs().Where(p => !names.Contains(p.name)).ToList();
                pairs.AddRange(rects.Where(r => names.Contains(r.name))
                    .Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                ids.SetNameFileIdPairs(pairs);
                provider.Apply();
                importer.SaveAndReimport();

                Sprite[] available = AssetDatabase.LoadAllAssetsAtPath(plan.Path).OfType<Sprite>().ToArray();
                var frames = new Sprite[plan.Count];
                for (int i = 0; i < frames.Length; i++)
                {
                    string expected = plan.BaseName + "_" + i;
                    frames[i] = available.FirstOrDefault(s => s.name == expected);
                    if (frames[i] == null) throw new InvalidOperationException("Missing prepared frame: " + expected);
                }
                result.Add(plan.Key, frames);
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}: {1} frames, 640x640; pivot ({2:0.0000}, {3:0.0000}); texture limit {4}.",
                    plan.Key, plan.Count, plan.Pivot.x, plan.Pivot.y, importer.maxTextureSize));
            }
            EnsureFolder(DataRoot);
            string reportPath = DataRoot + "/P01_Preparation_Report.txt";
            File.WriteAllText(Path.Combine(projectRoot, reportPath), report.ToString());
            AssetDatabase.ImportAsset(reportPath);
            Debug.Log("[Daeng Saka 1A.1B] Sprite preparation complete. Metadata backup: " + backup);
            return result;
        }

        private static List<SheetPlan> BuildPlans()
        {
            var plans = new List<SheetPlan>();
            foreach (string action in new[] { "Idle", "Walking" })
            foreach (string direction in new[] { "Front", "Back", "Left", "Right" })
            {
                string baseName = "MC_Saka_" + action + "_" + direction;
                string path = SourceRoot + "MC_Saka_" + action + "/" + baseName + ".png";
                int count = action == "Walking" ? 6 : (direction == "Front" || direction == "Back" ? 5 : 3);
                string absolute = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
                if (!File.Exists(absolute)) throw new FileNotFoundException("Missing Saka source image.", path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(absolute)))
                        throw new InvalidOperationException("Cannot read PNG source: " + path);
                    if (texture.width != count * Cell || texture.height != Cell)
                        throw new InvalidOperationException(string.Format(
                            "Unexpected layout in {0}: expected {1}x640, found {2}x{3}. Setup stops before import changes.",
                            path, count * Cell, texture.width, texture.height));
                    if (!(AssetImporter.GetAtPath(path) is TextureImporter))
                        throw new InvalidOperationException("Import the source image into Unity first: " + path);
                    plans.Add(new SheetPlan
                    {
                        Key = action + "_" + direction,
                        Path = path,
                        BaseName = baseName,
                        Count = count,
                        Width = texture.width,
                        Pivot = MeasureFootAnchor(texture.GetPixels32(), texture.width, count)
                    });
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            return plans;
        }

        private static Vector2 MeasureFootAnchor(Color32[] pixels, int width, int count)
        {
            var centers = new List<float>();
            int baseline = Cell;
            for (int frame = 0; frame < count; frame++)
            {
                bool found = false;
                for (int y = 0; y < Cell && !found; y++)
                {
                    int minX = Cell, maxX = -1;
                    for (int x = 0; x < Cell; x++)
                    {
                        if (pixels[y * width + frame * Cell + x].a < 32) continue;
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                    }
                    if (maxX < 0) continue;
                    baseline = Mathf.Min(baseline, y);
                    centers.Add((minX + maxX + 1) * 0.5f);
                    found = true;
                }
                if (!found) throw new InvalidOperationException("A Saka animation frame is entirely transparent.");
            }
            // One fixed pivot for the entire clip preserves the authored stride/bob.
            centers.Sort();
            int middle = centers.Count / 2;
            float center = centers.Count % 2 == 0
                ? (centers[middle - 1] + centers[middle]) * 0.5f : centers[middle];
            return new Vector2(Mathf.Round(center) / Cell, (float)baseline / Cell);
        }

        internal static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string child = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = child;
            }
        }
    }
}
