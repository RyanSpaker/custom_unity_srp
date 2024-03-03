Shader "Hidden/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "Filter"
            HLSLPROGRAM
            #include "Assets/ShaderLibrary/common.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _BloomParams;

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv;
                return o;
            }
            
            float3 prefilter(float3 col) 
            {
                float b = dot(col.rgb, float3(0.299f, 0.587f, 0.114f));
                float soft = b - _BloomParams.y;
                soft = clamp(soft, 0, _BloomParams.z);
                soft *= soft * _BloomParams.w;
                float c = max(soft, b - _BloomParams.x);
                c /= max(b, 0.00001);
                return  col*c;
            }

            float4 frag(v2f_PU input) : SV_Target
            {
                float4 col = tex2D(_MainTex, input.uv);
                return float4(prefilter(col.rgb), 1);
            }
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #include "Assets/ShaderLibrary/common.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BloomScaleFactor;

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv*_BloomScaleFactor.xy;
                return o;
            }

            float3 Sample(float2 uv) {
                return tex2D(_MainTex, uv).rgb;
            }

            float3 SampleBox(float2 uv) {
                float4 o = _MainTex_TexelSize.xyxy * float2(-1, 1).xxyy;
                float3 s = Sample(uv + o.xy) + Sample(uv + o.zy) + Sample(uv + o.xw) + Sample(uv + o.zw);
                return s * 0.25f;
            }

            float4 frag (v2f_PU input) : SV_Target
            {
                float4 col = float4(SampleBox(input.uv), 1);
                return col;
            }
            ENDHLSL
        }
        Pass
        {
            Blend One One
            HLSLPROGRAM
            #include "Assets/ShaderLibrary/common.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BloomInvScaleFactor;

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv * _BloomInvScaleFactor.xy;
                return o;
            }

            float3 Sample(float2 uv) {
                return tex2D(_MainTex, uv).rgb;
            }

            float3 SampleBox(float2 uv, float delta) {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
                float3 s = Sample(uv + o.xy) + Sample(uv + o.zy) + Sample(uv + o.xw) + Sample(uv + o.zw);
                return s * 0.25f;
            }

            float4 frag(v2f_PU input) : SV_Target
            {
                float4 col = float4(SampleBox(input.uv, 0.5), 1);
                return col;
            }
            ENDHLSL
        }
        Pass
        {
            Blend One Zero
            HLSLPROGRAM
            #include "Assets/ShaderLibrary/common.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BloomInvScaleFactor;

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv * _BloomInvScaleFactor.xy;
                return o;
            }

            float3 Sample(float2 uv) {
                return tex2D(_MainTex, uv).rgb;
            }

            float3 SampleBox(float2 uv, float delta) {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
                float3 s = Sample(uv + o.xy) + Sample(uv + o.zy) + Sample(uv + o.xw) + Sample(uv + o.zw);
                return s * 0.25f;
            }

            float4 frag(v2f_PU input) : SV_Target
            {
                float4 col = float4(SampleBox(input.uv, 0.5), 1);
                return col;
            }
            ENDHLSL
        }
        Pass
        {
            Blend One One
            HLSLPROGRAM
            #include "Assets/ShaderLibrary/common.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float _BloomIntensity;

            v2f_PU vert(vin_PU input)
            {
                v2f_PU o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.uv = input.uv;
                return o;
            }

            float4 frag(v2f_PU input) : SV_Target
            {
                float4 col = tex2D(_MainTex, input.uv);
                return float4(_BloomIntensity*col.rgb, 1);
            }
            ENDHLSL
        }
    }
}
