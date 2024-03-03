using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

namespace UnityEngine.Rendering.Corn
{
    [CreateAssetMenu(menuName = "Rendering/CornRenderPipelineAsset")]
    public class CornRPAsset : RenderPipelineAsset
    {
        [Header("Pipeline Settings")]
        public Vector2Int MinimumResolution = new Vector2Int(1920, 1080);
        public CameraRenderer SceneViewRenderer;
        public CameraRenderer PreviewRenderer;
        protected override RenderPipeline CreatePipeline(){ return new CornRP(this); }
    }
    
    
    internal class CornRP : RenderPipeline
    {
        public readonly CornRPAsset settings;
        public CornRTHandles handles;
        private readonly List<CornPass> renderPasses;
        private readonly List<RenderFunc> renderFunctions;

        public CornRP(CornRPAsset settings)
        {
            this.settings = settings;
            renderPasses = settings.GetPasses();
            handles = new CornRTHandles(settings.MinimumResolution, ref renderPasses);
            renderFunctions = renderPasses.SelectMany(x => x.GetRenderFunctions()).OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            GlobalSetup(ref context, ref cameras);
            foreach (Camera cam in cameras) 
            {
                switch (cam.cameraType)
                {
                    case CameraType.Game:
                        //cam.gameObject.GetComponent<CameraRenderer>().Render()
                        break;
                }
            }
            for (int i = 0; i < cameras.Length; i++) 
            {
                Camera cam = cameras[i];
                RenderInfo renderInfo = PerCameraSetup(ref context, ref cam);
                if (settings.B_IsDeferred(cam.cameraType)) 
                {
                    for (int j = 0; j < renderFunctions.Count; j++) { renderFunctions[j](ref context, ref handles, ref renderInfo); }
                }
            }
            context.Submit();
        }
        protected void GlobalSetup(ref ScriptableRenderContext context, ref Camera[] cameras) 
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "Set Global Shader Varaibles");
            handles.SetGlobalVariables(ref cmd, ref cameras);
            for (int i = 0; i < renderPasses.Count; i++) { renderPasses[i].SetGlobalShaderConstants(ref cmd); }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        protected RenderInfo PerCameraSetup(ref ScriptableRenderContext context, ref Camera camera)
        {
            RenderInfo info = new RenderInfo();
            info.cam = camera; 
            for (int i = 0; i < renderPasses.Count; i++) {renderPasses[i].PreRenderComputation(ref context, ref info);}

            handles.SetReferenceSize(camera);

            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
            context.SetupCameraProperties(camera);

            CommandBuffer cmd = CommandBufferPool.Get(name: "Set Per Camera Shader Varaibles");
            cmd.SetGlobalMatrix("UNITY_MATRIX_IV", camera.cameraToWorldMatrix);
            cmd.SetGlobalMatrix("UNITY_MATRIX_IP", Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(camera.projectionMatrix, true)));
            handles.SetPerCameraVariables(ref cmd, ref camera);

            for (int i = 0; i < renderPasses.Count; i++) { renderPasses[i].SetPerCameraShaderConstants(ref cmd, ref info); }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            return info;
        }
        protected override void Dispose(bool disposing)
        {
            handles.Dispose();
            renderPasses.ForEach(x => x.Dispose());
        }
    }
    internal class CornRTHandles
    {
        public enum RTGroup
        {
            Color,
            Depth,
            GBuffer,
            Shadow
        }
        public readonly Dictionary<RTGroup, List<RTHandle>> Targets;
        public readonly Dictionary<RTGroup, (int, int)> Locations;
        public CornRTHandles(Vector2Int refres, ref List<CornPass> passes)
        {
            RTHandles.Initialize(refres.x, refres.y);

            Targets = new Dictionary<RTGroup, List<RTHandle>>
            {
                {
                    RTGroup.Color,
                    new List<RTHandle>() {
                        Alloc("_CameraColorAttachment", GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None),
                        Alloc("_CameraColorAttachment2", GraphicsFormat.R32G32B32A32_SFloat, DepthBits.None)
                    }
                },
                {
                    RTGroup.Depth,
                    new List<RTHandle>() {
                        Alloc("_CameraDepthAttachment", GraphicsFormat.D32_SFloat_S8_UInt, DepthBits.Depth32),
                        Alloc("_CameraDepthAttachment2", GraphicsFormat.D32_SFloat_S8_UInt, DepthBits.Depth32)
                    }
                }
            };

            Locations = new Dictionary<RTGroup, (int, int)>
            {
                {
                    RTGroup.Color,
                    (0, 2)
                },
                { 
                    RTGroup.Depth,
                    (0, 2)
                }
            };

            for (int i = 0; i < passes.Count; i++) { passes[i].HandleInit(ref Targets, ref Locations); }
        }
        public static RTHandle Alloc(string name, GraphicsFormat colorFormat, DepthBits depthBits, bool useMip = false, FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            return RTHandles.Alloc(scaleFactor: Vector2.one, dimension: TextureDimension.Tex2D, name: name, colorFormat: colorFormat, depthBufferBits: depthBits, useMipMap: useMip, filterMode: filterMode, wrapMode: wrapMode);
        }
        public void SetReferenceSize(Camera cam)
        {
            RTHandles.SetReferenceSize(cam.scaledPixelWidth, cam.scaledPixelHeight);
        }
        public void SetGlobalVariables(ref CommandBuffer cmd, ref Camera[] cameras)
        {
            foreach (List<RTHandle> i in Targets.Values) 
            {
                foreach (RTHandle j in i) 
                {
                    cmd.SetGlobalTexture(j.name, j);
                }
            }
        }
        public void SetPerCameraVariables(ref CommandBuffer cmd, ref Camera camera) {
            cmd.SetGlobalVector("_RTHandleScale", RTHandles.rtHandleProperties.rtHandleScale);
            cmd.SetGlobalInt("_ReferenceWidth", RTHandles.rtHandleProperties.currentViewportSize.x);
            cmd.SetGlobalInt("_ReferenceHeight", RTHandles.rtHandleProperties.currentViewportSize.y);
        }
        public Vector2 GetRTScale() 
        {
            return RTHandles.rtHandleProperties.rtHandleScale;
        }
        public RTHandle GetCurrentRender(RTGroup group) 
        {
            return Targets[group][Locations[group].Item1];
        }
        public RenderTargetIdentifier[] GetAllIdentifiers(RTGroup group) 
        {
            return Targets[group].Select(x => (RenderTargetIdentifier)x).ToArray();
        }
        public float GetInverseAreaScale() { return 1f / (RTHandles.rtHandleProperties.rtHandleScale.x*RTHandles.rtHandleProperties.rtHandleScale.y); }
        public void Dispose() 
        {
            Targets.Values.SelectMany(x => x).ToList().ForEach(x => RTHandles.Release(x));
        }
    }
    
    public abstract class CornPass : ScriptableObject
    {
        public bool enabled;
        internal virtual List<(RenderEvent, RenderFunc)> GetRenderFunctions() { return new List<(RenderEvent, RenderFunc)>(); }
        internal virtual void SetGlobalShaderConstants(ref CommandBuffer cmd) { }
        internal virtual void SetPerCameraShaderConstants(ref CommandBuffer cmd, ref RenderInfo renderInfo) { }
        internal virtual void PreRenderComputation(ref ScriptableRenderContext context, ref RenderInfo renderInfo) { }
        internal virtual void HandleInit(ref Dictionary<CornRTHandles.RTGroup, List<RTHandle>> Targets, ref Dictionary<CornRTHandles.RTGroup, (int, int)> Locations) { }
        internal void SetRenderTarget(RenderTarget target, ClearFlag clearFlag, ref CornRTHandles handles, ref ScriptableRenderContext context) 
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "SetTarget");
            SetRenderTarget(target, clearFlag, ref handles, ref cmd);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        internal void SetRenderTarget(RenderTarget target, ClearFlag clearFlag, ref CornRTHandles handles, ref CommandBuffer cmd)
        {
            switch (target)
            {
                case RenderTarget.Main:
                    CoreUtils.SetRenderTarget(cmd, handles.GetCurrentRender(CornRTHandles.RTGroup.Color), clearFlag: clearFlag);
                    break;
                case RenderTarget.MainAndDepth:
                    CoreUtils.SetRenderTarget(cmd, handles.GetCurrentRender(CornRTHandles.RTGroup.Color), handles.GetCurrentRender(CornRTHandles.RTGroup.Depth), clearFlag: clearFlag);
                    break;
                case RenderTarget.GBufferAndDepth:
                    CoreUtils.SetRenderTarget(cmd, handles.GetAllIdentifiers(CornRTHandles.RTGroup.GBuffer), handles.GetCurrentRender(CornRTHandles.RTGroup.Depth), clearFlag: clearFlag);
                    break;
                case RenderTarget.ShadowMap:
                    CoreUtils.SetRenderTarget(cmd, handles.GetCurrentRender(CornRTHandles.RTGroup.Shadow), clearFlag: clearFlag);
                    break;
            }
        }
        internal void DrawFullScreen(ref Material blitMat, int pass, string name, ref ScriptableRenderContext context) 
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: name);
            cmd.DrawMesh(Globals.FULL_SCREEN_QUAD, Matrix4x4.identity, blitMat, 0, pass);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        internal void DrawFullScreen(ref Material blitMat, int pass, ref CommandBuffer cmd)
        {
            cmd.DrawMesh(Globals.FULL_SCREEN_QUAD, Matrix4x4.identity, blitMat, 0, pass);
        }
        public virtual void Dispose() { }
    }
}


/*
 [Header("Fog Settings")]
        public float FogStart = 10;
        [Header("Shadow Settings")]
        public float shadowRadius = 10;
        [Header("SkyBox Settings")]
        public bool skyboxEnabled = true;
        public Material skyBox;
        [Header("Star Settings")]
        public bool starsEnabled = true;
        public float starRadius = 100f;
        public float starRadiusDepth = 10f;
        public float starSize = 1f;
        public int starCount = 100;
        public Material starMaterial;
        [Header("Tonemapper Settings")]
        public bool tonemapEnabled = true;
        public float Ldmax = 1f;
        public float Cmax = 1f;
        public int toneMapper = 1;
        [Header("Bloom Settings")]
        public bool bloomEnabled = true;
        public int blurStepCount = 1;
        public float blurFactor = 1.5f;
        [Range(0, 10)]
        public float bloomThreshold = 1;
        [Range(0, 1)]
        public float softThreshold = 1;
        [Range(0, 10)]
        public float intensity = 1;
        public int additiveUpSteps = 1;
 */
