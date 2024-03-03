Shader "Corn/Star"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _DiffuseFactor("DiffuseFactor", Float) = 1
        _Brightness("Brightness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            name "GBufferPass"
            Tags { "LightMode" = "Deferred"}

            Stencil
            {
                Ref 16
                Comp GEqual
                Pass Replace
            }
            Conservative False
            
            HLSLPROGRAM

            #include "Assets/ShaderLibrary/common.hlsl"

            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:vertInstancingSetup
            #pragma vertex vert
            #pragma fragment frag

            struct StarData {
                float4 pos;
                float4x4 rot;
            };

            StructuredBuffer<StarData> _StarPositionBuffer;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _DiffuseFactor;
                float _Brightness;
            CBUFFER_END

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            void vertInstancingSetup() {}
            #endif

            v2f_PN vert(vin_PNI input)
            {
                v2f_PN o;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 position = input.vertex;
                #if defined(PROCEDURAL_INSTANCING_ON)
                    //random rotate
                    StarData data = _StarPositionBuffer[unity_InstanceID];
                    position *= data.pos.w;
                    position = mul(data.rot, float4(position, 1)).xyz;
                    position += data.pos.xyz;
                #endif
                o.vertex = mul(UNITY_MATRIX_VP, float4(position, 1));
                o.normal = UnityObjectToWorldNormal(input.normal);
                return o;
            }

            fout_gbuffer frag(v2f_PN input)
            {
                fout_gbuffer o = (fout_gbuffer)0;
                o.color = float4(_BaseColor.xyz*_Brightness, rcp(_DiffuseFactor));
                o.normal = float4(input.normal, 0);
                o.specular = float4(0, 0, 0, 0);
                o.lightProbe = float4(0 ,0, 0, 1);
                return o;
            }
            ENDHLSL
        }
    }
}
