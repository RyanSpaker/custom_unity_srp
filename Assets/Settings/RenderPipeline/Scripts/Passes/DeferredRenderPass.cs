using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;


namespace UnityEngine.Rendering.Corn
{
    [CreateAssetMenu(menuName = "Rendering/Passes/DeferredRenderPass")]
    public class DeferredRenderPass : CornPass
    {
        public Shader DeferredBlitShader;
        public RenderEvent DeferredLightingEvent;
        public RenderEvent GBufferRenderEvent;
        public float FogStart = 10f;
        public Material blit;
        private Material DeferredBlitMaterial;
        internal override void PreRenderComputation(ref ScriptableRenderContext context, ref RenderInfo renderInfo)
        {
            base.PreRenderComputation(ref context, ref renderInfo);
            if (!renderInfo.cullIsValued)
            {
                renderInfo.cam.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
                renderInfo.cullingResults = context.Cull(ref cullingParameters);

                int maxdex = 0;
                float max = 0f;
                for (int i = 0; i < renderInfo.cullingResults.visibleLights.Length; i++) {
                    if (renderInfo.cullingResults.visibleLights[i].lightType == LightType.Directional && renderInfo.cullingResults.visibleLights[i].light.intensity > max) 
                    {
                        maxdex = i;
                        max = renderInfo.cullingResults.visibleLights[i].light.intensity;
                    }
                }

                renderInfo.mainLight = (renderInfo.cullingResults.visibleLights[maxdex], maxdex);
                renderInfo.filteringSettings = FilteringSettings.defaultValue;
                renderInfo.drawingSettings = new(new ShaderTagId("Deferred"), new SortingSettings(renderInfo.cam))
                {
                    enableDynamicBatching = true,
                    perObjectData = PerObjectData.LightProbe
                };
            }
        }
        internal override void HandleInit(ref Dictionary<CornRTHandles.RTGroup, List<RTHandle>> Targets, ref Dictionary<CornRTHandles.RTGroup, (int, int)> Locations)
        {
            base.HandleInit(ref Targets, ref Locations);
            Targets.Add(CornRTHandles.RTGroup.GBuffer, new List<RTHandle>() {
                CornRTHandles.Alloc("_GBufferAlbedo", Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None),
                CornRTHandles.Alloc("_GBufferNormal", Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None),
                CornRTHandles.Alloc("_GBufferSpecular", Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None),
                CornRTHandles.Alloc("_GBufferLightProbe", Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None),
                CornRTHandles.Alloc("_GBufferWorldPos", Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None)
            });
            Locations.Add(CornRTHandles.RTGroup.GBuffer, (0, 1));
        }
        internal override void SetPerCameraShaderConstants(ref CommandBuffer cmd, ref RenderInfo renderInfo)
        {
            base.SetPerCameraShaderConstants(ref cmd, ref renderInfo);
            cmd.SetGlobalVector("_MainLightColor", renderInfo.mainLight.Item1.finalColor);
            Vector4 dir = -(renderInfo.mainLight.Item1.localToWorldMatrix * (new Vector4(0, 0, 1, 0))).normalized;
            cmd.SetGlobalVector("_MainLightDirection", dir);
        }
        internal override void SetGlobalShaderConstants(ref CommandBuffer cmd)
        {
            base.SetGlobalShaderConstants(ref cmd);
            cmd.SetGlobalFloat("_FogStart", FogStart);
        }
        internal void RenderLighting(ref ScriptableRenderContext context, ref CornRTHandles handles, ref RenderInfo renderInfo)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "Deferred Lighting");
            cmd.Blit(handles.GetCurrentRender(CornRTHandles.RTGroup.Depth), handles.Targets[CornRTHandles.RTGroup.Depth][1], blit);
            SetRenderTarget(RenderTarget.MainAndDepth, ClearFlag.Color, ref handles, ref cmd);
            DrawFullScreen(ref DeferredBlitMaterial, 0, ref cmd);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        internal void RenderGBuffer(ref ScriptableRenderContext context, ref CornRTHandles handles, ref RenderInfo renderInfo)
        {
            SetRenderTarget(RenderTarget.GBufferAndDepth, ClearFlag.All, ref handles, ref context);
            context.DrawRenderers(renderInfo.cullingResults, ref renderInfo.drawingSettings, ref renderInfo.filteringSettings);
        }
        internal override List<(RenderEvent, RenderFunc)> GetRenderFunctions() 
        {
            return new List<(RenderEvent, RenderFunc)>() 
            {
                (GBufferRenderEvent, RenderGBuffer),
                (DeferredLightingEvent, RenderLighting)
            };
        }
        private void OnValidate()
        {
            if (DeferredBlitShader == null) { DeferredBlitShader = Shader.Find("Hidden/DeferredLighting"); }
        }
        private void OnEnable()
        {
            if (DeferredBlitShader == null) { DeferredBlitShader = Shader.Find("Hidden/DeferredLighting"); }
            DeferredBlitMaterial = new(DeferredBlitShader);
        }
    }
}
