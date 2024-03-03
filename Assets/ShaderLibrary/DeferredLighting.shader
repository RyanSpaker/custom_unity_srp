Shader "Hidden/DeferredLighting"
{
    SubShader
    {
        

        Pass
        {
            name "DeferredLighting"
            Cull Off
            ZWrite Off
            ZTest Always
            ZClip On
            Stencil
            {
                Ref 0
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Assets/ShaderLibrary/Common.hlsl"
            
            float4 _RTHandleScale;
            
            v2f_deferred vert(vin_deferred input)
            {
                v2f_deferred o;
                o.vertex = float4(input.vertex.xy*3, 3, 3);
                o.uv = input.uv * _RTHandleScale.xy;
                return o;
            }

            sampler2D _GBufferAlbedo;//color, diffusefactor
            sampler2D _GBufferNormal;//normal, specular factor
            sampler2D _GBufferSpecular;//specular color, smoothness
            sampler2D _GBufferLightProbe;//light probe stuff like ambiant light, idk
            sampler2D _GBufferWorldPos;//world position of the pixel
            sampler2D _CameraDepthAttachment2;
            sampler2D _ShadowMap;
            float4 _MainLightDirection;
            float4 _MainLightColor;
            float _FogStart;

            float4x4 _LightMatrices[2];
            float _WarpFactor;

            float4 frag(v2f_deferred i) : SV_Target
            {
                float4 normal = tex2D(_GBufferNormal, i.uv);
                float4 albedo = tex2D(_GBufferAlbedo, i.uv);
                float4 specular = tex2D(_GBufferSpecular, i.uv);
                float4 lightProbe = tex2D(_GBufferLightProbe, i.uv);
                float4 worldPos = tex2D(_GBufferWorldPos, i.uv);
                float depth = tex2D(_CameraDepthAttachment2, i.uv).r; depth = LinearEyeDepth(depth);
                float3 viewVec = normalize(_WorldSpaceCameraPos.xyz-worldPos.xyz);

                float diffuseIntensity = max(dot(_MainLightDirection.xyz, normalize(normal.xyz)) * albedo.w - albedo.w + 1, 0.0);
                float3 diffuse = _MainLightColor*diffuseIntensity*albedo.rgb;

                float smoothness = exp2(10 * specular.w + 1);
                float specularIntensity = normal.w*pow(saturate(dot(normalize(normal.xyz), normalize(_MainLightDirection.xyz + viewVec.xyz))), smoothness);
                float3 specularLight = _MainLightColor * specular.xyz * specularIntensity;

                float3 ambient = lightProbe.xyz * albedo.xyz;

                float3 final = (diffuse + specularLight + ambient);

                //float fogFactor = max((depth - _FogStart) * unity_FogParams.x, 0);
                //fogFactor = exp2(fogFactor * -fogFactor);
                //final = lerp(skybox.xyz, final, fogFactor);

                float4 shadowUV = mul(_LightMatrices[0], worldPos);
                shadowUV.xy = pow(abs(shadowUV.xy), float2(_WarpFactor, _WarpFactor)) * sign(shadowUV.xy);
                shadowUV = mul(_LightMatrices[1], shadowUV);
                shadowUV.xy = shadowUV.xy / shadowUV.w * 0.5 + 0.5;
                float lightDepth = tex2D(_ShadowMap, shadowUV.xy* _RTHandleScale.xy).r;

                return float4(lightDepth, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
