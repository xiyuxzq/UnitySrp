#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

//顶点函数输入结构体
struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float2 lightMapUV : TEXCOORD1;
};
//片元函数输入结构体
struct Varyings {
	float4 positionCS_SS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
};

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varyings MetaPassVertex(Attributes input) {
	Varyings output;
	input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
	output.positionCS_SS = TransformWorldToHClip(input.positionOS);
	output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET{
	InputConfig config = GetInputConfig(input.positionCS_SS,input.baseUV);
	float4 base = GetBase(config);
	Surface surface;
	ZERO_INITIALIZE(Surface, surface);
	surface.color = base.rgb;
	surface.metallic = GetMetallic(config);
	surface.smoothness = GetSmoothness(config);
	BRDF brdf = GetBRDF(surface);
	float4 meta =0.0;
	//若标记了X分量，则需要漫反射率
	if (unity_MetaFragmentControl.x) {
		meta = float4(brdf.diffuse, 1.0);
		meta.rgb += brdf.specular * brdf.roughness * 0.5;
		meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
	}
	//若标记了Y分量，则返回自发光的颜色
	else if (unity_MetaFragmentControl.y) {
		meta = float4(GetEmission(config), 1.0);
	}
	return meta;
}

#endif