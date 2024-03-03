using System.Collections.Generic;
using System.Linq;
using static UnityEditor.Rendering.CameraUI;

namespace UnityEngine.Rendering.Corn
{
    [CreateAssetMenu(menuName = "Rendering/Passes/ShadowPass")]
    public class ShadowPass : CornPass
    {
        public float ShadowDistance = 10f;
        public float LightDistanceBehindCam = 100f;
        public float WarpFactor = 2f;
        private Matrix4x4 LightMatrixStageOne;
        private Matrix4x4 LightMatrixStageTwo;
        private Vector3[] temp;
        internal override void HandleInit(ref Dictionary<CornRTHandles.RTGroup, List<RTHandle>> Targets, ref Dictionary<CornRTHandles.RTGroup, (int, int)> Locations)
        {
            base.HandleInit(ref Targets, ref Locations);
            Targets.Add(CornRTHandles.RTGroup.Shadow, new List<RTHandle>() {
                CornRTHandles.Alloc("_ShadowMap", Experimental.Rendering.GraphicsFormat.D32_SFloat, DepthBits.Depth32, filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp)
            });
            Locations.Add(CornRTHandles.RTGroup.Shadow, (0, 1));
        }
        internal override void SetPerCameraShaderConstants(ref CommandBuffer cmd, ref RenderInfo renderInfo)
        {
            base.SetPerCameraShaderConstants(ref cmd, ref renderInfo);
            if (renderInfo.cam.cameraType == CameraType.Game)
            {
                CalculateLightProjection(ref renderInfo.cam, renderInfo.mainLight.Item1.light.transform.rotation);
            }
            cmd.SetGlobalMatrixArray("_LightMatrices", new Matrix4x4[2] { LightMatrixStageOne, LightMatrixStageTwo });
            cmd.SetGlobalFloat("_WarpFactor", 1f / WarpFactor);
        }
        internal void ShadowRender(ref ScriptableRenderContext context, ref CornRTHandles handles, ref RenderInfo renderInfo)
        {
            if (renderInfo.cam.cameraType == CameraType.Game)
            {
                CommandBuffer cmd = CommandBufferPool.Get(name: "ShadowPass");
                SetRenderTarget(RenderTarget.ShadowMap, ClearFlag.Depth, ref handles, ref cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                var settings = new DrawingSettings(new ShaderTagId("Shadow"), new SortingSettings(renderInfo.cam));
                context.DrawRenderers(renderInfo.cullingResults, ref settings, ref renderInfo.filteringSettings);
            }
        }
        internal override List<(RenderEvent, RenderFunc)> GetRenderFunctions()
        {
            return new List<(RenderEvent, RenderFunc)>()
            {
                (RenderEvent.ShadowPass, ShadowRender)
            };
        }
        private void CalculateLightProjection(ref Camera cam, Quaternion lightRotation)
        {
            //During Shader Code we use stage one, then pow warp the x and y, then use stage two to get clip coords
            Vector3[] points = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), ShadowDistance, Camera.MonoOrStereoscopicEye.Mono, points);
            Vector3[] points2 = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, points2);
            Transform t = cam.transform;
            //World Space Points
            points = points.Concat(points2).Select(s => t.TransformPoint(s)).ToArray();

            //Stage One Transformation
            LightMatrixStageOne =
                Matrix4x4.Translate(Vector3.forward * LightDistanceBehindCam / ShadowDistance) *
                (Matrix4x4.Scale(Vector3.one / ShadowDistance) *
                (Matrix4x4.Rotate(Quaternion.Inverse(lightRotation)) *
                Matrix4x4.Translate(-cam.transform.position)));

            //Light Direction Space and warp
            points = points.Select(s =>
            {
                Vector3 intermediate = (Vector3)(LightMatrixStageOne * new Vector4(s.x, s.y, s.z, 1f));
                intermediate.x = Mathf.Sign(intermediate.x) * Mathf.Pow(Mathf.Abs(intermediate.x), 1f / WarpFactor);
                intermediate.y = Mathf.Sign(intermediate.y) * Mathf.Pow(Mathf.Abs(intermediate.y), 1f / WarpFactor);
                return intermediate;
            }).ToArray();


            float extraRotation = GetEigenRotation(points);
            float minDist = 10000f;
            float closest = 0f;
            for (int i = 0; i < 4; i++)
            {
                float cur = Vector2.SignedAngle((Vector2)(points[i] - points[(i + 1) % 4]), Vector2.right);
                if (Mathf.Abs(cur - extraRotation) < minDist) { minDist = Mathf.Abs(cur - extraRotation); closest = cur; }
                cur = Vector2.SignedAngle((Vector2)(points[(i + 1) % 4] - points[i]), Vector2.right);
                if (Mathf.Abs(cur - extraRotation) < minDist) { minDist = Mathf.Abs(cur - extraRotation); closest = cur; }
                cur = Vector2.SignedAngle((Vector2)(points[4 + (i + 1) % 4] - points[4 + i]), Vector2.right);
                if (Mathf.Abs(cur - extraRotation) < minDist) { minDist = Mathf.Abs(cur - extraRotation); closest = cur; }
                cur = Vector2.SignedAngle((Vector2)(points[4 + i] - points[4 + (i + 1) % 4]), Vector2.right);
                if (Mathf.Abs(cur - extraRotation) < minDist) { minDist = Mathf.Abs(cur - extraRotation); closest = cur; }
                cur = Vector2.SignedAngle((Vector2)(points[i] - points[4 + i]), Vector2.right);
                if (Mathf.Abs(cur - extraRotation) < minDist) { minDist = Mathf.Abs(cur - extraRotation); closest = cur; }
                cur = Vector2.SignedAngle((Vector2)(points[4 + i] - points[i]), Vector2.right);
                if (Mathf.Abs(cur - extraRotation) < minDist) { minDist = Mathf.Abs(cur - extraRotation); closest = cur; }
            }
            Matrix4x4 extra = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, closest));

            //Final Space before orth projection
            points = points.Select(s => (Vector3)(extra * new Vector4(s.x, s.y, s.z, 1f))).ToArray();

            Vector2 X = new Vector2(points[0].x, points[0].x);
            Vector2 Y = new Vector2(points[0].y, points[0].y);
            Vector2 Z = new Vector2(points[0].z, points[0].z);
            foreach (Vector3 point in points)
            {
                X.x = Mathf.Min(X.x, point.x);
                X.y = Mathf.Max(X.y, point.x);
                Y.x = Mathf.Min(Y.x, point.y);
                Y.y = Mathf.Max(Y.y, point.y);
                Z.x = Mathf.Min(Z.x, point.z);
                Z.y = Mathf.Max(Z.y, point.z);
            }

            Matrix4x4 projection = Matrix4x4.Ortho(X.x, X.y, Y.x, Y.y, Z.x, Z.y);
            projection.m02 *= -1f;
            projection.m12 *= -1f;
            projection.m22 *= -1f;
            projection.m32 *= -1f;

            LightMatrixStageTwo = GL.GetGPUProjectionMatrix(projection, true) * extra;
            if (cam.cameraType == CameraType.Game) {
                temp = points.Select(s => (Vector3)(GL.GetGPUProjectionMatrix(projection, true) * new Vector4(s.x, s.y, s.z, 1f))).ToArray();
            }
        }
        private float GetEigenRotation(Vector3[] points) 
        {
            Vector2 means = points.Aggregate(Vector2.zero, (acc, val) => new Vector2(acc.x + val.x, acc.y + val.y));
            means /= points.Length;
            float[] covMatrix = new float[4];
            covMatrix[1] = points.Aggregate(0f, (acc, next) => { return acc + (next.y - means.y) * (next.x - means.x); }) / ((float)points.Length - 1f);
            covMatrix[2] = covMatrix[1];
            covMatrix[0] = points.Aggregate(0f, (acc, next) => { return acc + (next.x - means.x) * (next.x - means.x); }) / ((float)points.Length - 1f);
            covMatrix[3] = points.Aggregate(0f, (acc, next) => { return acc + (next.y - means.y) * (next.y - means.y); }) / ((float)points.Length - 1f);
            float eval1 = (covMatrix[0] + covMatrix[3] + Mathf.Sqrt(Mathf.Pow(covMatrix[0] + covMatrix[3], 2) - 4f * (covMatrix[0] * covMatrix[3] - covMatrix[1] * covMatrix[2]))) / 2f;
            float eval2 = (covMatrix[0] + covMatrix[3] - Mathf.Sqrt(Mathf.Pow(covMatrix[0] + covMatrix[3], 2) - 4f * (covMatrix[0] * covMatrix[3] - covMatrix[1] * covMatrix[2]))) / 2f;
            float eval = Mathf.Max(eval1, eval2);
            Vector2 evec = new Vector2(covMatrix[1] / (eval - covMatrix[0]), 1f).normalized;
            return Vector2.SignedAngle(evec, Vector2.right);
        }
        private float[] CalculateEigenValues(float[] matrix) 
        {
            float A = -1f;
            float B = matrix[0] + matrix[4] + matrix[8];
            float C = (matrix[1] * matrix[3] + matrix[2] * matrix[6] + matrix[5] * matrix[7]) -
                (matrix[4] * matrix[8] + matrix[0] * matrix[8] + matrix[0] * matrix[4]);
            float D = matrix[1] * matrix[5] * matrix[6] + matrix[2] * matrix[3] * matrix[7] + matrix[0] * matrix[4] * matrix[8] -
                (matrix[0] * matrix[5] * matrix[7] + matrix[1] * matrix[3] * matrix[8] + matrix[2] * matrix[6] * matrix[8]);
            return SolveCubic(A, B, C, D);
        }
        private float[] SolveCubic(float a, float b, float c, float d) 
        {
            float c1 = b / a;
            float c2 = c / a;
            float c3 = d / a;
            float p = c2 / 3f - c1 * c1 / 9f;
            float q = c1 * c1 * c1 / 27f - c1 * c2 / 6f + c3 / 2f;
            float discriminant = p * p * p + q * q;
            if (discriminant > 0f) 
            {
                float t = Mathf.Pow(-q + Mathf.Sqrt(discriminant), 1f / 3f) + Mathf.Pow(-q - Mathf.Sqrt(discriminant), 1f / 3f);
                return new float[1] {
                    t-c1/3f
                };
            }
            if (discriminant == 0f)
            {
                float t1 = 2f * Mathf.Pow(-q, 1f / 3f);
                float t2 = -Mathf.Pow(-q, 1f / 3f);
                return new float[2] {
                    t1-c1/3f,
                    t2-c1/3f
                };
            }
            else
            {
                float angle = Mathf.Acos(-q / Mathf.Sqrt(-p * p * p));
                float t1 = 2f * Mathf.Sqrt(-p) * Mathf.Cos((angle + 2f * Mathf.PI) / 3f);
                float t2 = 2f * Mathf.Sqrt(-p) * Mathf.Cos(angle / 3f);
                float t3 = 2f * Mathf.Sqrt(-p) * Mathf.Cos((angle - 2f * Mathf.PI) / 3f);
                return new float[3] {
                    t1-c1/3f,
                    t2-c1/3f,
                    t3-c1/3f
                };
            }
        }
    }
}
/*
 protected float GetEigenRotation(Vector2[] points) 
        {
            Vector2 means = points.Aggregate(Vector2.zero, (acc, next) => { return acc + next; }) / ((float)points.Length);
            float[] covMatrix = new float[4];
            covMatrix[1] = points.Aggregate(0f, (acc, next) => { return acc + (next.y - means.y) * (next.x - means.x); }) / ((float)points.Length - 1f);
            covMatrix[2] = covMatrix[1];
            covMatrix[0] = points.Aggregate(0f, (acc, next) => { return acc + (next.x - means.x) * (next.x - means.x); }) / ((float)points.Length - 1f);
            covMatrix[3] = points.Aggregate(0f, (acc, next) => { return acc + (next.y - means.y) * (next.y - means.y); }) / ((float)points.Length - 1f);
            float eval1 = (covMatrix[0] + covMatrix[3] + Mathf.Sqrt(Mathf.Pow(covMatrix[0] + covMatrix[3], 2) - 4f * (covMatrix[0] * covMatrix[3] - covMatrix[1] * covMatrix[2]))) / 2f;
            float eval2 = (covMatrix[0] + covMatrix[3] - Mathf.Sqrt(Mathf.Pow(covMatrix[0] + covMatrix[3], 2) - 4f * (covMatrix[0] * covMatrix[3] - covMatrix[1] * covMatrix[2]))) / 2f;
            float eval = Mathf.Max(eval1, eval2);
            Vector2 evec = new Vector2((eval - covMatrix[3]) / covMatrix[2], 1f).normalized;
            return Vector2.SignedAngle(evec, Vector2.right);
        }
 */
/*
                if (cam.tag.Equals("MainCamera") && lights.lights.Length > 0)
                { 
                    Quaternion worldToLocalRotation = Quaternion.Inverse(lights.lights[0].light.transform.rotation);

                    Vector3[] frustumCorners = new Vector3[4];
                    cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 10f, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
                    Vector3[] frustumCorners2 = new Vector3[4];
                    cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners2);

                    Vector2[] points = frustumCorners.Concat(frustumCorners2)
                        .Select(s => (Vector2)(worldToLocalRotation * cam.transform.TransformPoint(s))).ToArray();
                    float extraRotation = GetEigenRotation(points);
                    worldToLocalRotation *= Quaternion.Euler(0f, 0f, extraRotation);
                    points = points.Select(s => (Vector2)(Quaternion.Euler(0f, 0f, extraRotation) * (new Vector3(s.x, s.y, 0f)))).ToArray();

                    Rect bounds = points.Aggregate(Rect.zero, (acc, val) => {
                        acc.xMin = Mathf.Min(acc.xMin, val.x);
                        acc.xMax = Mathf.Max(acc.xMax, val.x);
                        acc.yMin = Mathf.Min(acc.yMin, val.y);
                        acc.yMax = Mathf.Max(acc.yMax, val.y);
                        return acc;
                    });

                    Matrix4x4 worldToLocal = Matrix4x4.TRS(-1f*(Quaternion.Inverse(worldToLocalRotation) * (new Vector3(bounds.center.x, bounds.center.y, -100f))), worldToLocalRotation, Vector3.one);
                    Matrix4x4 localToWorld = worldToLocal.inverse;

                    Vector4 localCamPos = worldToLocal * new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1f);
                    localCamPos.z = 0f;
                    Debug.DrawLine(cam.transform.position, localToWorld * localCamPos, Color.red);
                    Debug.DrawLine(localToWorld * new Vector4(0f, 0f, 0f, 1f), localToWorld * new Vector4(-bounds.width/2f, 0f, 0f, 1f), Color.red);
                    Debug.DrawLine(localToWorld * new Vector4(0f, 0f, 0f, 1f), localToWorld * new Vector4(bounds.width/2f, 0f, 0f, 1f), Color.red);
                    Debug.DrawLine(localToWorld * new Vector4(0f, 0f, 0f, 1f), localToWorld * new Vector4(0f, -bounds.height/2f, 0f, 1f), Color.red);
                    Debug.DrawLine(localToWorld * new Vector4(0f, 0f, 0f, 1f), localToWorld * new Vector4(0f, bounds.height/2f, 0f, 1f), Color.red);
                }*/ 