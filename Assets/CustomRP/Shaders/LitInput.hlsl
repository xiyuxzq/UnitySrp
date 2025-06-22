//受光着色器公用属性和方法库
#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

TEXTURE2D(_EmissionMap);
TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailMap);
TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailMap);

TEXTURE2D(_NormalMap);
//SAMPLER(sampler_NormalMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)

UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)
//输入配置
struct InputConfig {
	Fragment fragment;
	float2 baseUV;
	float2 detailUV;
	bool useMask;
	bool useDetail;
};
//获取输入配置
InputConfig GetInputConfig(float4 positionSS,float2 baseUV, float2 detailUV = 0.0) {
	InputConfig c;
	c.fragment = GetFragment(positionSS);
	c.baseUV = baseUV;
	c.detailUV = detailUV;
	c.useMask = false;
	c.useDetail = false;
	return c;
}
//基础纹理UV转换
float2 TransformBaseUV(float2 baseUV) {
	float4 baseST = INPUT_PROP(_BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}
//细节纹理UV转换
float2 TransformDetailUV (float2 detailUV) {
	float4 detailST = INPUT_PROP(_DetailMap_ST);
	return detailUV * detailST.xy + detailST.zw;
}

//获取细节纹理的采样数据
float4 GetDetail (InputConfig c) {
	if (c.useDetail) {
		float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, c.detailUV);
		return map * 2.0 - 1.0;
	}
	return 0.0;
}
//获取遮罩纹理的采样数据
float4 GetMask (InputConfig c) {
	if (c.useMask) {
		return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, c.baseUV);
	}
	return 1.0;
}
//采样法线并解码法线向量得到原法线方向
float3 GetNormalTS (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, c.baseUV);
	float scale = INPUT_PROP(_NormalScale);
	float3 normal = DecodeNormal(map, scale);
	if (c.useDetail) {
		map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, c.detailUV);
		scale = INPUT_PROP(_DetailNormalScale) * GetMask(c).b;
		float3 detail = DecodeNormal(map, scale);
		normal = BlendNormalRNM(normal, detail);
	}
	return normal;
}
//获取基础纹理的采样数据
float4 GetBase(InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
	float4 color = INPUT_PROP( _BaseColor);
	if (c.useDetail) {
		float detail = GetDetail(c).r * INPUT_PROP(_DetailAlbedo);
		float mask = GetMask(c).b;
		map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
		map.rgb *= map.rgb;
	}
	return map * color;
}


float GetCutoff(InputConfig c) {
	return INPUT_PROP(_Cutoff);
}


float GetMetallic(InputConfig c) {
	float metallic = INPUT_PROP(_Metallic);
	metallic *= GetMask(c).r;
	return metallic;
}

float GetSmoothness(InputConfig c) {
	float smoothness = INPUT_PROP(_Smoothness);
	smoothness *= GetMask(c).a;
	if (c.useDetail) {
		float detail = GetDetail(c).b * INPUT_PROP(_DetailSmoothness);
		float mask = GetMask(c).b;
		smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
	}
	return smoothness;
}
//获取自发光纹理的采样数据
float3 GetEmission (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, c.baseUV);
	float4 color = INPUT_PROP(_EmissionColor);
	return map.rgb * color.rgb;
}

float GetOcclusion (InputConfig c) {
	float strength = INPUT_PROP(_Occlusion);
	float occlusion = GetMask(c).g;
	occlusion = lerp(occlusion, 1.0, strength);
	return occlusion;
}

float GetFresnel (InputConfig c) {
	return INPUT_PROP(_Fresnel);
}
float GetFinalAlpha(float alpha) {
	
	return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}


#endif
