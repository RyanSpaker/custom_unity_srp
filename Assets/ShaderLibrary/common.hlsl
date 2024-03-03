#include "UnityCG.cginc"
#include "Assets/ShaderLibrary/random.hlsl"

struct vin_P
{
    float4 vertex : POSITION;
};

struct vin_PN
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct vin_PNI
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct vin_PU
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct vin_deferred
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f_P
{
    float4 vertex : SV_POSITION;
};

struct v2f_skybox
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 viewVec : TEXCOORD1;
};

struct v2f_PN
{
    float4 vertex : SV_POSITION;
    float3 normal : TEXCOORD1;
};

struct v2f_PNW
{
    float4 vertex : SV_POSITION;
    float3 normal : TEXCOORD1;
    float4 worldPos : TEXCOORD2;
};

struct v2f_PU
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f_deferred
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct fout_color
{
    float4 color : SV_Target;
};

struct fout_gbuffer
{
    float4 color : SV_Target0;
    float4 normal : SV_Target1;
    float4 specular : SV_Target2;
    float4 lightProbe : SV_Target3;
    float4 pos : SV_Target4;
};

float3 ComputeViewSpacePosition(float4 positionCS)
{
    float4 positionWS = mul(unity_CameraInvProjection, positionCS);
    return positionWS.xyz;
}

void Remap(float origMin, float origMax, float outMin, float outMax, out float value)
{
    float perc = (value - origMin) / (origMax - origMin);
    value = perc * (outMax - outMin) + outMin;
}

v2f_PU vert_post_process(vin_PU input)
{
    v2f_PU o;
    o.vertex = UnityObjectToClipPos(input.vertex);
    o.uv = input.uv;
    return o;
}