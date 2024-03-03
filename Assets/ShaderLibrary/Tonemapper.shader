Shader "Hidden/Tonemapper"
{
    Properties{
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_post_process
            #pragma fragment frag

            #include "Assets/ShaderLibrary/Common.hlsl"
            
            sampler2D _MainTex;

            float luminance(float3 color) {
                return dot(color, float3(0.299f, 0.587f, 0.114f));
            }
            
            float frag(v2f_PU input) : SV_Target
            {
                float4 col = tex2D(_MainTex, input.uv);
                return luminance(col.rgb);
            }
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/ShaderLibrary/Common.hlsl"

            sampler2D _MainTex, _LuminanceTex;
            float _Ldmax, _Cmax, _LuminanceScale;

            float luminance(float3 color) {
                return dot(color, float3(0.299f, 0.587f, 0.114f));
            }

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv;
                return o;
            }

            float4 frag(v2f_PU input) : SV_Target
            {
                float3 col = tex2D(_MainTex, input.uv).rgb;
                float Lin = luminance(col);
                float Lavg = tex2Dlod(_LuminanceTex, float4(input.uv.x, input.uv.y, 0, 10)).r* _LuminanceScale;

                float logLrw = log10(Lavg) + 0.84;
                float alphaRw = 0.4 * logLrw + 2.92;
                float betaRw = -0.4 * logLrw * logLrw - 2.584 * logLrw + 2.0208;
                float Lwd = _Ldmax / sqrt(_Cmax);
                float logLd = log10(Lwd) + 0.84;
                float alphaD = 0.4 * logLd + 2.92;
                float betaD = -0.4 * logLd * logLd - 2.584 * logLd + 2.0208;
                float Lout = pow(Lin, alphaRw / alphaD) / _Ldmax * pow(10.0, (betaRw - betaD) / alphaD) - (1.0 / _Cmax);

                float3 Cout = col / Lin * Lout;

                return float4(saturate(Cout), 1);
            }
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/ShaderLibrary/Common.hlsl"

            sampler2D _MainTex;

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv;
                return o;
            }

            float4 frag(v2f_PU input) : SV_Target
            {
                float3 col = tex2D(_MainTex, input.uv).rgb;

                float3 Cout = (col * (2.51 * col + 0.03)) / (col * (2.43 * col + 0.59) + 0.14);

                return float4(saturate(Cout), 1);
            }
            ENDHLSL
        }
    }
}
