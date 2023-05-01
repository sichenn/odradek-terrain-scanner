Shader "Replacement Pass" 
{
	Properties
	{
		[HideInInspector]_ID("ID", float) = 0.0
	}

	SubShader
	{
		Pass 
		{
			Name "ID"
			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing

			#pragma vertex IDPassVertex
			#pragma fragment IDPassFragment

			#include "ShaderLibrary/Replacement.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Normal Depth"
			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing

			#pragma vertex NormalDepthPassVertex
			#pragma fragment NormalDepthPassFragment

			#include "ShaderLibrary/Replacement.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "Depth"
			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing

			#pragma vertex DepthPassVertex
			#pragma fragment DepthPassFragment

			#include "ShaderLibrary/Replacement.hlsl"

			ENDHLSL
		}
	}
}