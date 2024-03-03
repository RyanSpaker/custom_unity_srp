Shader "Skybox/CornSkybox" {
    Properties
    {
        _GroundColor("Ground Color", Color) = (1, 1, 1, 1)
        _SkyColor("Sky Color", Color) = (1, 1, 1, 1)
        _GroundLevel("Ground Level", Range(0.0, 1.0)) = 0.0
        _RandomLevel("Random Level", Range(0.0, 1.0)) = 0.0
        _Atmosphere("Atmosphere depth", Range(0.0, 2.0)) = 1.0
        _DensityFalloff("Density Falloff", Range(0.0, 10.0)) = 1.0
    }
    SubShader{
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off ZTest Always

        Pass {
            Stencil
            {
                Ref 0
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/ShaderLibrary/common.hlsl"

            float4 _MainLightDirection;
            float4x4 UNITY_MATRIX_IV;

            v2f_skybox vert(vin_PU input)
            {
                v2f_skybox o;
                o.vertex = float4(input.vertex.xyz, 1);
                o.uv = input.uv;
                
                float4 viewPos = float4(rcp(UNITY_MATRIX_P[0][0])*input.vertex.x, rcp(UNITY_MATRIX_P[1][1])*input.vertex.y, 1, 0);
                o.viewVec = normalize(mul(UNITY_MATRIX_IV, normalize(viewPos))).xyz;
                return o;
            }

            float4 _GroundColor;
            float4 _SkyColor;
            float _GroundLevel;
            float _RandomLevel;
            uint _ReferenceWidth;
            uint _ReferenceHeight;
            
            float4 frag(v2f_skybox input) : SV_Target
            {
                float x = (_GroundLevel+normalize(input.viewVec).y) / (1 + _GroundLevel);
                float4 col = lerp(_GroundColor, _SkyColor, saturate(x));
                col += float4(1.0 / 256.0, 1.0 / 256.0, 1.0 / 256.0, 0) * GetBayer8(uint2(input.uv.x * _ReferenceWidth, input.uv.y * _ReferenceHeight)) * _RandomLevel;
                return col;
            }
            ENDHLSL
        }
    }
}