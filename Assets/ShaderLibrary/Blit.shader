Shader "Custom/Blit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite On ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/ShaderLibrary/Common.hlsl"

            v2f_PU vert (vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv;
                return o;
            }

            sampler2D _MainTex;

            float frag (v2f_PU i) : SV_Depth
            {
                return tex2D(_MainTex, i.uv).r;
            }
            ENDHLSL
        }
    }
}
