#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

TEXTURE2D_X(_MainTex);
TEXTURE2D(_EmissiveTex);
SAMPLER(sampler_EmissiveTex);
float _TextureTiling;
float2 _EdgeDetectionNearFadeRange;
float2 _ScanLineFarFadeRange;
float2 _SobelDepthRange;
float _FarthestLineIntensity;
float _EdgeGlowDistance;
float _DarkenFactor;
uint _OutlineThickness;
float _OutlineIntensity;
float _Distance;
float _Width;
float _ArcAngle; // = cos(deg2Rad * arcAngle * 0.5)
float3 _ScannerPos;
float4 _Tint;
float2 _Forward;
float _SideEdgeGradient;
float4x4 unity_CameraInvProjection;
float _GenericRenderDebugScalar;
float4 _GenericRenderDebugVector;

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 vertex: SV_POSITION;
    float3 normal: NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes v)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
    o.texcoord = GetFullScreenTriangleTexCoord(v.vertexID);

    return o;
}

float SobelSampleDepth(float3 pixelOffset, float2 uv)
{
    float center = LinearEyeDepth(SampleCameraDepth(uv), _ZBufferParams);
    float left = LinearEyeDepth(SampleCameraDepth(uv - pixelOffset.xz), _ZBufferParams);
    float right = LinearEyeDepth(SampleCameraDepth(uv + pixelOffset.xz), _ZBufferParams);
    float up = LinearEyeDepth(SampleCameraDepth(uv + pixelOffset.zy), _ZBufferParams);
    float down = LinearEyeDepth(SampleCameraDepth(uv - pixelOffset.zy), _ZBufferParams);

    return  (abs(left - center) +
            abs(right - center) +
            abs(up - center) +
            abs(down - center)) * 0.25;
}

float SobelSampleDepth(float3 pixelOffset, float2 uv, float center)
{
    float left = LinearEyeDepth(SampleCameraDepth(uv - pixelOffset.xz), _ZBufferParams);
    float right = LinearEyeDepth(SampleCameraDepth(uv + pixelOffset.xz), _ZBufferParams);
    float up = LinearEyeDepth(SampleCameraDepth(uv + pixelOffset.zy), _ZBufferParams);
    float down = LinearEyeDepth(SampleCameraDepth(uv - pixelOffset.zy), _ZBufferParams);

    return  (abs(left - center) +
            abs(right - center) +
            abs(up - center) +
            abs(down - center)) * 0.25;
}

float inverseLerp(float a, float b, float value)
{
    return (value - a) / (b - a);
}

float remap(float minA, float toA, float fromB, float toB, float value)
{
    return fromB + (value - minA) * (toB - fromB) / (toA - minA);
}

float Level(float value, float min, float max)
{
    return remap(min, max, 0.0, 1.0, value);
}

float Contrast(float mask, float contrast)
{
    contrast *= 0.5;
    mask = remap(contrast, 1 - contrast, 0.0, 1.0, mask);
    return mask;
}

half4 Frag(Varyings i) : SV_Target
{
    half4 col = LOAD_TEXTURE2D_X(_MainTex, i.texcoord * _ScreenSize.xy);

    // sample view depth01
    float zBuffer = SampleCameraDepth(i.texcoord.xy);
    float depth01 = Linear01Depth(zBuffer, _ZBufferParams);

    PositionInputs posInput = GetPositionInput(i.vertex.xy, _ScreenSize.zw, zBuffer, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    // World-space position code from CameraMotionVectors.shader
    float4 curClipPos = mul(UNITY_MATRIX_UNJITTERED_VP, posInput.positionWS);
    float2 positionCS = curClipPos.xy / curClipPos.w;
    half3 worldPos = _WorldSpaceCameraPos + posInput.positionWS;
    
    float3 scannerSpacePos = worldPos - _ScannerPos;
    float3 offset = float3(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y, 0) * _OutlineThickness;
    
    // scan field
    float dist = distance(worldPos, _ScannerPos);
    float distXZPlane = distance(worldPos.xz, _ScannerPos.xz);
    float2 dir = normalize((worldPos - _ScannerPos).xz);

    float angle01 = inverseLerp(_ArcAngle, 1.0, dot(_Forward, dir));
    

    half4 scannerCol = 0;
    // clip pixels outside the arc angle and the camera far plane
    if (dist < _Distance && dist > (_Distance - _Width - _ScanLineFarFadeRange.y)
        && depth01 < 1 && angle01 >= 0.0)
    {
        float edgeGradient = saturate(inverseLerp(1.0, 1.0 - _SideEdgeGradient, 1.0 - angle01));

        float farEndDarkenFactor = saturate(1 - (_Distance - dist) / _Width);
        float farFadeFactor = saturate(inverseLerp(_Distance - _ScanLineFarFadeRange.y, _Distance - _ScanLineFarFadeRange.x, distXZPlane));
        float nearFadeFactor = saturate(inverseLerp(_EdgeDetectionNearFadeRange.x, _EdgeDetectionNearFadeRange.y, distXZPlane));
        float blendMask = farFadeFactor * nearFadeFactor;

        // Edge Glow
        float rim = saturate(inverseLerp(_Distance - _Width * _EdgeGlowDistance, _Distance,  dist));
        scannerCol.rgb = lerp(scannerCol.rgb, _Tint.rgb, rim);

        // Darken
        col.rgb = lerp(col.rgb, col.rgb * (1.0 - _DarkenFactor * _Tint.a * farEndDarkenFactor), edgeGradient);

        // Scan Line
        float2 packedTexUV = float2(
            atan2(scannerSpacePos.x, scannerSpacePos.z) * 0.5,
            dist * _TextureTiling);
        float scanLineMask = SAMPLE_TEXTURE2D(_EmissiveTex, sampler_EmissiveTex, packedTexUV).x;
        float scanLineFactor = scanLineMask * _OutlineIntensity;
        float rowDistance = 0.33 / _TextureTiling; // assume there are 3 rows in the scan line texture
        float furthestLineMask = step((int(_Distance / rowDistance) - 1) * rowDistance, distXZPlane);
        scanLineFactor *= lerp(1.0, _FarthestLineIntensity, furthestLineMask);

        // Silhouette
        float sobelDepth = SobelSampleDepth(offset, i.texcoord.xy);
        float edgeDetectionFactor = saturate(Level(sobelDepth, _SobelDepthRange.x, _SobelDepthRange.y));
        

        scannerCol.rgb += lerp(_Tint.rgb, 1.0.xxx, furthestLineMask) * scanLineFactor * blendMask; // scan line only
        scannerCol.rgb += _Tint.rgb * max(scanLineFactor, edgeDetectionFactor) * blendMask;

        scannerCol *= edgeGradient;
    }


    return col + scannerCol * _Tint.a;
}