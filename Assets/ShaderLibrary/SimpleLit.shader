Shader "Corn/SimpleLit"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _DiffuseFactor("DiffuseFactor", Float) = 1
        _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularFactor("SpecularFactor", Float) = 1
        _Smoothness("Smoothness", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass 
        {
            name "GBufferPass"
            Tags { "LightMode" = "Deferred"}
            
            Stencil
            {
                Ref 32
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/ShaderLibrary/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SpecColor;
                float _DiffuseFactor;
                float _SpecularFactor;
                float _Smoothness;
            CBUFFER_END

            v2f_PNW vert(vin_PN input)
            {
                v2f_PNW o;
                o.worldPos = mul(unity_ObjectToWorld, input.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
                o.normal = UnityObjectToWorldNormal(input.normal);
                return o;
            }

            fout_gbuffer frag(v2f_PNW input)
            {
                fout_gbuffer o = (fout_gbuffer)0;
                o.color = float4(_BaseColor.xyz, rcp(_DiffuseFactor));
                o.normal = float4(input.normal, _SpecularFactor);
                o.specular = float4(_SpecColor.xyz, _Smoothness);
                o.lightProbe = float4((float3)ShadeSH9(half4((half3)input.normal.xyz, 1)), 1);
                o.pos = input.worldPos;
                return o;
            }
            ENDHLSL
    }
        Pass
        {
            name "ShadowPass"
            Tags { "LightMode" = "Shadow"}
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/ShaderLibrary/Common.hlsl"

            float4x4 _LightMatrices[2];
            float _WarpFactor;

            v2f_P vert(vin_P input)
            {
                v2f_P o;
                o.vertex = mul(_LightMatrices[0], mul(unity_ObjectToWorld, input.vertex));
                o.vertex.xy = pow(abs(o.vertex.xy), float2(_WarpFactor, _WarpFactor))*sign(o.vertex.xy);
                o.vertex = mul(_LightMatrices[1], o.vertex);
                return o;
            }
            float4 frag(v2f_P input) : SV_TARGET
            {
                return (float4)0;
            }
            ENDHLSL
        }
    }
}
