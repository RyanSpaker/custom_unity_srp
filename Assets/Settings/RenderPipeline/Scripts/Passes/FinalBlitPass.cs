using System.Collections.Generic;

namespace UnityEngine.Rendering.Corn
{
    [CreateAssetMenu(menuName = "Rendering/Passes/FinalBlitPass")]
    public class FinalBlitPass : CornPass
    {
        internal void FinalBlit(ref ScriptableRenderContext context, ref CornRTHandles handles, ref RenderInfo renderInfo)
        {
            SetRenderTarget(RenderTarget.Main, ClearFlag.None, ref handles, ref context);
            if (renderInfo.cam.cameraType == CameraType.SceneView) 
            {
                context.DrawGizmos(renderInfo.cam, GizmoSubset.PreImageEffects);
                context.DrawGizmos(renderInfo.cam, GizmoSubset.PostImageEffects);
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "FinalBlit");
            cmd.Blit(
                handles.GetCurrentRender(CornRTHandles.RTGroup.Color),
                renderInfo.cam.activeTexture,
                handles.GetRTScale(),
                Vector2.zero
            );
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        internal override List<(RenderEvent, RenderFunc)> GetRenderFunctions()
        {
            return new List<(RenderEvent, RenderFunc)>()
            {
                (RenderEvent.FinalBlit, FinalBlit)
            };
        }
    }
}
