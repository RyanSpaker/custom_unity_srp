using System.Collections.Generic;

namespace UnityEngine.Rendering.Corn
{
    [CreateAssetMenu(menuName = "Rendering/Passes/SkyRenderPass")]
    public class SkyRenderPass : CornPass
    {
        public Material SkyBoxMaterial;
        internal void RenderSky(ref ScriptableRenderContext context, ref CornRTHandles handles, ref RenderInfo renderInfo) {
            if (SkyBoxMaterial == null) {return;}
            CommandBuffer cmd = CommandBufferPool.Get(name: "SkyBox");
            SetRenderTarget(RenderTarget.MainAndDepth, ClearFlag.None, ref handles, ref cmd);
            DrawFullScreen(ref SkyBoxMaterial, 0, ref cmd);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        internal override List<(RenderEvent, RenderFunc)> GetRenderFunctions()
        {
            return new List<(RenderEvent, RenderFunc)>()
            {
                (RenderEvent.SkyPass, RenderSky)
            };
        }
    }
}
/*
 internal class SkyRenderPass : CornPass
    {
        struct StarData
        {
            Vector4 position;
            Matrix4x4 rotationMatrix;
            public StarData(Vector4 pos, Matrix4x4 rot) { position = pos; rotationMatrix = rot; }
        };

        private Mesh starMesh;
        private Material starMat;
        private Material skyBoxMat;
        private Mesh fullscreenQuad;
        public ComputeBuffer B_StarData;

        private bool starDataSet;
        private bool disposed;
        private int count;
        private float radius, size, radiusDepth;
        private bool starEnabled, skyboxEnabled;
        public SkyRenderPass(CornRPAsset settings)
        {
            count = settings.starCount;
            radius = settings.starRadius; size = settings.starSize; radiusDepth = settings.starRadiusDepth;

            starMesh = CreateStarMesh();
            starMat = settings.starMaterial;
            B_StarData = new ComputeBuffer(count, 80);
            starDataSet = false;
            disposed = false;
            skyBoxMat = settings.skyBox;
            skyboxEnabled = settings.skyboxEnabled;
            starEnabled = settings.starsEnabled;
            fullscreenQuad = CreateFullscreenQuad();

            starMesh.RecalculateNormals();

            SetStarData();
        }
        public Mesh CreateStarMesh()
        {
            return new()
            {
                indexFormat = IndexFormat.UInt16,
                vertices = new Vector3[4]
                {
                new Vector3(0.5f, Mathf.Sqrt(2)/4f, 0f),
                new Vector3(-0.5f, Mathf.Sqrt(2)/4f, 0f),
                new Vector3(0f, -Mathf.Sqrt(2)/4f, 0.5f),
                new Vector3(0f, -Mathf.Sqrt(2)/4f, -0.5f)
                },
                triangles = new int[12] { 2, 0, 1, 1, 0, 3, 0, 2, 3, 3, 2, 1 }
            };
        }
        public Mesh CreateFullscreenQuad()
        {
            return new()
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
        public override void SetGlobalVariables(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer("_StarPositionBuffer", B_StarData);
        }
        private async void SetStarData()
        {
            StarData[] data = new StarData[count];
            await Task.Run(() => 
            {
                var randomGen = new System.Random();
                float Range(float min, float max) 
                {
                    return (float)randomGen.NextDouble()*(max - min) + min;
                }
                for (int i = 0; i < count; i++)
                {
                    float u = Range(0f, 1f);
                    float sizeRandom = Range(0.9f, 1.1f);
                    float radiusRandom = Mathf.Sqrt(Range(radius/(radius+radiusDepth), 1f)) * (radius+radiusDepth);
                    float theta = Range(0f, 2f * Mathf.PI);

                    Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.Euler(
                        Range(0f, 360f),
                        Range(0f, 360f),
                        Range(0f, 360f)
                    ));
                    data[i] = new StarData(
                        new Vector4(
                            Mathf.Sqrt(1f - u * u) * Mathf.Cos(theta) * radiusRandom,
                            u * radiusRandom,
                            Mathf.Sqrt(1f - u * u) * Mathf.Sin(theta) * radiusRandom,
                            size * sizeRandom), 
                        rot);
                }
            });
            if (!disposed)
            {
                B_StarData.SetData(data);
                starDataSet = true;
            }
        }
        private bool NotReadyToRenderStars() 
        {
            if (starMat == null) return true;
            if (!starDataSet) return true;
            if (!starEnabled) return true;
            return false;
        }
        private bool NotReadyToRenderSkyBox()
        {
            if (skyBoxMat == null) return true;
            if (!skyboxEnabled) return true;
            return false;
        }
        public void RenderStars(ScriptableRenderContext context, CornRTHandles handles, Camera camera, CornLights lights)
        {
            if (NotReadyToRenderStars()) return;

            CommandBuffer cmd = CommandBufferPool.Get(name: "Stars");
            CoreUtils.SetRenderTarget(
                cmd,
                handles.GetGBufferArray(),
                handles.cameraDepthTarget,
                clearFlag: ClearFlag.None
            );
            cmd.DrawMeshInstancedProcedural(starMesh, 0, starMat, 0, count);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public void RenderSkyBox(ScriptableRenderContext context, CornRTHandles handles, Camera camera, CornLights lights) 
        {
            if (NotReadyToRenderSkyBox()) return;
            CommandBuffer cmd = CommandBufferPool.Get(name: "SkyBox");
            CoreUtils.SetRenderTarget(cmd, handles.GetCurrentRenderTarget(), handles.cameraDepthTarget, ClearFlag.None);
            cmd.DrawMesh(fullscreenQuad, Matrix4x4.identity, skyBoxMat, 0, 1);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        internal override List<(RenderEvent, RenderFunc)> GetRenderFunctions()
        {
            return new()
            {
                (RenderEvent.Stars, RenderStars),
                (RenderEvent.SkyBox, RenderSkyBox)
            };
        }
        public override void Dispose()
        {
            base.Dispose();
            disposed = true;
            B_StarData.Release();
        }
    }
 */