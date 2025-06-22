//表面属性相关库
#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface {
	//表面位置坐标
	float3 position;
	float3 normal;
	//插值法线
	float3 interpolatedNormal;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	//遮挡数据
	float occlusion;
	float3 viewDirection;
	//表面深度
	float depth;
	//抖动
	float dither;
	//菲涅尔反射强度
	float fresnelStrength;
	//渲染层掩码
	uint renderingLayerMask;
};

#endif
