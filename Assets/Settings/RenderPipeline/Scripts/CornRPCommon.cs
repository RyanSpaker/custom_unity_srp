namespace UnityEngine.Rendering.Corn
{
    public static class Globals
    {
        public static Mesh FULL_SCREEN_QUAD = new()
        {
            indexFormat = IndexFormat.UInt16,
            vertices = new Vector3[4]
                {
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f, -1.0f, 0.0f)
                },
            triangles = new int[6] { 0, 1, 2, 2, 3, 0 },
            uv = new Vector2[4]
                {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
                }
        };
    }
    
    internal struct RenderInfo
    {
        public Camera cam;
        public (VisibleLight, int) mainLight;
        public CullingResults cullingResults;
        public FilteringSettings filteringSettings;
        public DrawingSettings drawingSettings;
        public bool cullIsValued;
    }
    
    internal delegate void RenderFunc(ref ScriptableRenderContext context, ref CornRTHandles handles, ref RenderInfo renderInfo);
    
    public enum RenderEvent
    {
        ShadowPass,
        GBuffer,
        DeferredLighting,
        SkyPass,
        FinalBlit
    }
    
    public enum RenderTarget
    {
        Main,
        MainAndDepth,
        GBufferAndDepth,
        ShadowMap
    }
}
