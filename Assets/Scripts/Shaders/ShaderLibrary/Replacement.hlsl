#ifndef UTILITY_PASS_INCLUDED
#define UTILITY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerFrame)
float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(PerInstance)
UNITY_DEFINE_INSTANCED_PROP(float, _ID)
UNITY_INSTANCING_BUFFER_END(PerInstance)

// -------- ID Begin--------
struct VertexInputSimple {
	float4 pos : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutputSimple {
	float4 clipPos : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutputSimple IDPassVertex(VertexInputSimple input) 
{
	VertexOutputSimple output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	return output;
}

float4 IDPassFragment(VertexOutputSimple input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	//return float4(0, 1, 0, 1);
	return UNITY_ACCESS_INSTANCED_PROP(PerInstance, _ID);
}
// -------- ID End --------

// -------- Normal Depth Begin--------
struct VertexInputFull {
	float4 pos : POSITION;
	float4 normal : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutputFull {
	float4 clipPos : SV_POSITION;
	float3 normal : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutputFull NormalDepthPassVertex(VertexInputFull input) {
	VertexOutputFull output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	float3 worldNormal = mul(input.normal, (float3x3)unity_WorldToObject);
	output.clipPos = mul(unity_MatrixVP, worldPos);
	output.normal = normalize(worldNormal);
	return output;
}

float4 NormalDepthPassFragment(VertexOutputFull input) : SV_TARGET{
	UNITY_SETUP_INSTANCE_ID(input);
	return float4(normalize(input.normal), input.clipPos.z);
}
// -------- Normal Depth End --------

// -------- Depth Begin --------
VertexOutputSimple DepthPassVertex(VertexInputSimple input)
{
	VertexOutputSimple output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	return output;
}

float4 DepthPassFragment(VertexOutputSimple input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	return input.clipPos.z;
}
// -------- Depth End --------

#endif // UTILITY_PASS_INCLUDED