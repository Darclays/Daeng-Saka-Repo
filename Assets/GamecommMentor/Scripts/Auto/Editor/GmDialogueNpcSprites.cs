using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmDialogueNpcSprites
    {
        private const int Cell = 640;
        private const string ArtRoot = "Assets/GamecommMentor/Art/2D/DialogueExamples";
        private const string AnimationRoot = "Assets/GamecommMentor/Data/Character/DialogueExamples";
        internal const string FisherSource = "Assets/Asset2D_Pixel/NPC/NPC_Nelayan/NPC_Nelayan_AnakL/NPC_Nelayan_AnakL_Idle/NPC_Nelayan_AnakL_Idle_Front.png";
        internal const string MerchantSource = "Assets/Asset2D_Pixel/NPC/NPC_Merchant/NPC_Merchant_Idle/NPC_Merchant_Idle_Front.png";

        internal sealed class Prepared
        {
            internal Sprite First;
            internal AnimatorController Controller;
            internal float Height;
        }

        internal static Prepared Prepare(string source, string label)
        {
            GmDialogueExampleData.EnsureFolder(ArtRoot);
            GmDialogueExampleData.EnsureFolder(AnimationRoot);
            string path = ArtRoot + "/" + Path.GetFileName(source);
            string existingControllerPath = AnimationRoot + "/" + label + "_Idle.controller";
            AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(existingControllerPath);
            if (existingController != null)
            {
                Sprite first = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .FirstOrDefault(s => s.name == label + "_Idle_0");
                if (first == null) throw new InvalidOperationException("The existing NPC example was edited. Check its first sprite: " + path);
                return new Prepared { First = first, Controller = existingController, Height = first.bounds.size.y };
            }
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null && !AssetDatabase.CopyAsset(source, path))
                throw new InvalidOperationException("Cannot copy NPC source texture: " + source);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("NPC texture importer is missing: " + path);

            var frames = new List<Rect>();
            var footCenters = new List<float>();
            int lowestFoot = Cell;
            int maxHeight = 0;
            int imageWidth;
            Texture2D raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string project = Directory.GetParent(Application.dataPath).FullName;
                if (!raw.LoadImage(File.ReadAllBytes(Path.Combine(project, path))))
                    throw new InvalidOperationException("Cannot read NPC texture: " + path);
                if (raw.width % Cell != 0 || raw.height % Cell != 0)
                    throw new InvalidOperationException("NPC example requires a grid of 640x640 cells: " + path);
                imageWidth = raw.width;
                Color32[] pixels = raw.GetPixels32();
                for (int row = 0; row < raw.height / Cell; row++)
                for (int col = 0; col < raw.width / Cell; col++)
                {
                    int originX = col * Cell;
                    int originY = raw.height - (row + 1) * Cell;
                    int minY = Cell, maxY = -1, footMinX = Cell, footMaxX = -1;
                    for (int y = 0; y < Cell; y++)
                    for (int x = 0; x < Cell; x++)
                    {
                        if (pixels[(originY + y) * raw.width + originX + x].a < 32) continue;
                        if (y < minY) { minY = y; footMinX = x; footMaxX = x; }
                        else if (y == minY) { footMinX = Mathf.Min(footMinX, x); footMaxX = Mathf.Max(footMaxX, x); }
                        maxY = Mathf.Max(maxY, y);
                    }
                    if (maxY < 0) continue;
                    frames.Add(new Rect(originX, originY, Cell, Cell));
                    lowestFoot = Mathf.Min(lowestFoot, minY);
                    maxHeight = Mathf.Max(maxHeight, maxY - minY + 1);
                    footCenters.Add((footMinX + footMaxX + 1) * 0.5f);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            if (frames.Count == 0) throw new InvalidOperationException("NPC example texture is empty: " + path);
            footCenters.Sort();
            int middle = footCenters.Count / 2;
            float footX = footCenters.Count % 2 == 0
                ? (footCenters[middle - 1] + footCenters[middle]) * 0.5f : footCenters[middle];

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 200f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, Mathf.NextPowerOfTwo(imageWidth));
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("2D Sprite data provider is missing.");
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] existing = provider.GetSpriteRects();
            var rects = new List<SpriteRect>();
            for (int i = 0; i < frames.Count; i++)
            {
                string name = label + "_Idle_" + i;
                SpriteRect previous = existing.FirstOrDefault(r => r.name == name);
                rects.Add(new SpriteRect
                {
                    name = name, rect = frames[i], alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(Mathf.Round(footX) / Cell, (float)lowestFoot / Cell),
                    spriteID = previous != null ? previous.spriteID : GUID.Generate()
                });
            }
            // This file is a generated example copy; the original NPC source is never reimported.
            provider.SetSpriteRects(rects.ToArray());
            ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names == null) throw new InvalidOperationException("Sprite name/ID provider is missing.");
            names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Sprite[] sprites = rects.Select(r => all.FirstOrDefault(s => s.name == r.name)).ToArray();
            if (sprites.Any(s => s == null)) throw new InvalidOperationException("NPC sprite preparation failed: " + path);

            string clipPath = AnimationRoot + "/" + label + "_Idle.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = label + "_Idle", frameRate = 5f };
                var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i / 5f, value = sprites[i % sprites.Length] };
                AnimationUtility.SetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            string controllerPath = AnimationRoot + "/" + label + "_Idle.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
            return new Prepared { First = sprites[0], Controller = controller, Height = maxHeight / 200f };
        }
    }
}
