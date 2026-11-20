using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmDialogueWorldBlur : IDisposable
    {
        private const string MaterialPath = "DaengSaka/Dialogue/LatarDialog";
        private readonly Camera camera;
        private readonly BlurPass pass;

        internal bool Enabled { get; private set; }

        private GmDialogueWorldBlur(Camera camera, Material material)
        {
            this.camera = camera;
            pass = new BlurPass(material);
        }

        internal static GmDialogueWorldBlur TryCreate(Camera camera)
        {
            if (camera == null || GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                return null;
            var material = Resources.Load<Material>(MaterialPath);
            return material != null && material.shader != null && material.shader.isSupported
                ? new GmDialogueWorldBlur(camera, material) : null;
        }

        internal void SetEnabled(bool value)
        {
            if (Enabled == value) return;
            Enabled = value;
            if (value) RenderPipelineManager.beginCameraRendering += EnqueuePass;
            else RenderPipelineManager.beginCameraRendering -= EnqueuePass;
        }

        public void Dispose() => SetEnabled(false);

        private void EnqueuePass(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (renderingCamera != camera || GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                return;
            renderingCamera.GetUniversalAdditionalCameraData().scriptableRenderer?.EnqueuePass(pass);
        }

        private sealed class BlurPass : ScriptableRenderPass
        {
            private readonly Material material;

            internal BlurPass(Material material)
            {
                this.material = material;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resources = frameData.Get<UniversalResourceData>();
                // The dialogue borrows Daeng Saka's standalone world camera.
                if (cameraData.renderType != CameraRenderType.Base || !cameraData.resolveFinalTarget ||
                    resources.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resources.activeColorTexture;
                if (!source.IsValid()) return;
                var desc = graph.GetTextureDesc(source);
                desc.name = "Daeng Saka - Dialogue Blur";
                desc.depthBufferBits = DepthBits.None;
                desc.msaaSamples = MSAASamples.None;
                desc.bindTextureMS = false;
                desc.clearBuffer = false;

                TextureHandle blurred = graph.CreateTexture(desc);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, blurred, material, 0);
                graph.AddBlitPass(parameters, passName: desc.name);
                resources.cameraColor = blurred;
            }
        }
    }
}
