using System.Collections.Generic;

namespace UnityEngine.Rendering.Corn
{
    [CreateAssetMenu(menuName = "Rendering/Passes/PostProcessStack")]
    public class PostProcessStack : CornPass
    {
    }
}
/*
    internal class PostProcessPass : CornPass
    {
        private Material tonemap;
        private int toneMapper;
        private Material bloom;
        private int blurSteps, additiveUpSteps;
        private float blurFactor;
        private Vector4 bloomParams;
        private float bloomIntensity, LDmax, Cmax;
        private bool tonemapEnabled, bloomEnabled;
        public PostProcessPass(CornRPAsset settings)
        {
            try 
            {
                tonemap = new Material(Shader.Find("Hidden/Tonemapper"));
            }
            catch { }
            try
            {
                bloom = new Material(Shader.Find("Hidden/Bloom"));
            }
            catch { }
            toneMapper = settings.toneMapper;
            blurSteps = settings.blurStepCount;
            blurFactor = settings.blurFactor;
            float knee = settings.bloomThreshold * settings.softThreshold;
            bloomParams = new Vector4
            (
                settings.bloomThreshold,
                settings.bloomThreshold - knee,
                knee*2f,
                0.25f / (knee + 0.00001f)
            );
            bloomIntensity = settings.intensity;
            LDmax = settings.Ldmax; Cmax = settings.Cmax;
            tonemapEnabled = settings.tonemapEnabled;
            bloomEnabled = settings.bloomEnabled;
            additiveUpSteps = settings.additiveUpSteps;
        }
        public bool NotReadyToRenderTonemap() 
        {
            if (tonemap == null) return true;
            if (tonemap.shader.passCount <= toneMapper) return true;
            if (toneMapper == 0) return true;
            if (!tonemapEnabled) return true;
            return false;
        }
        public bool NotReadyToRenderBloom()
        {
            if (bloom == null) return true;
            if (!bloomEnabled) return true;
            return false;
        }
        public override void SetGlobalVariables(CommandBuffer cmd)
        {
            cmd.SetGlobalVector("_BloomScaleFactor", new Vector4(blurFactor, blurFactor, 0, 0));
            cmd.SetGlobalVector("_BloomInvScaleFactor", new Vector4(1f/blurFactor, 1f/blurFactor, 0, 0));
            cmd.SetGlobalVector("_BloomParams", bloomParams);
            cmd.SetGlobalFloat("_BloomIntensity", bloomIntensity);
            cmd.SetGlobalFloat("_Ldmax", LDmax);
            cmd.SetGlobalFloat("_Cmax", Cmax);
        }
        internal override List<(RenderEvent, RenderFunc)> GetRenderFunctions()
        {
            return new() 
            {
                (RenderEvent.Tonemapping, RenderTonemap),
                (RenderEvent.Bloom, RenderBloom),
            };
        }
        public void RenderBloom(ScriptableRenderContext context, CornRTHandles handles, Camera camera, CornLights lights) 
        {
            if (NotReadyToRenderBloom()) return;
            CommandBuffer cmd = CommandBufferPool.Get(name: "Bloom");
            //Copy over and clip dim pixels
            cmd.Blit(handles.GetCurrentRenderTarget(), handles.cameraColorBiTargets[0], bloom, 0);
            //downScale
            int i = 0;
            for (; i < blurSteps; i++) 
            {
                cmd.Blit(handles.cameraColorBiTargets[i], handles.cameraColorBiTargets[i+1], bloom, 1);
            }
            //upScale
            i = blurSteps;
            for (; i > blurSteps - additiveUpSteps; i--)
            {
                cmd.Blit(handles.cameraColorBiTargets[i], handles.cameraColorBiTargets[i-1], bloom, 2);
            }
            for (; i > 0; i--) 
            {
                cmd.Blit(handles.cameraColorBiTargets[i], handles.cameraColorBiTargets[i - 1], bloom, 3);
            }
            //Copy back to main targets
            cmd.Blit(handles.cameraColorBiTargets[0], handles.GetCurrentRenderTarget(), bloom, 4);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public void RenderTonemap(ScriptableRenderContext context, CornRTHandles handles, Camera camera, CornLights lights)
        {
            if (NotReadyToRenderTonemap()) return;
            CommandBuffer cmd = CommandBufferPool.Get(name: "ToneMap");
            CoreUtils.SetRenderTarget(cmd, handles.cameraOtherTargets[0], clearFlag: ClearFlag.Color);
            cmd.SetGlobalFloat("_LuminanceScale", handles.GetInverseAreaScale());
            cmd.Blit(handles.GetCurrentRenderTarget(), handles.GetGrayTarget(), tonemap, 0);
            cmd.Blit(handles.GetCurrentRenderTarget(), handles.GetNextRenderTarget(), tonemap, toneMapper);
            handles.IncreaseCurrentRenderTarget();
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
    
    
    */