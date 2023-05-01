
Shader "Hidden/TopographicScanHDRP"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
           HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag

                #include "TopographicScanHDRP.hlsl"
        ENDHLSL
        }
    }
}
