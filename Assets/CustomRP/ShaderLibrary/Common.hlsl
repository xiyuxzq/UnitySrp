//公共方法库
#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

//定义一些宏取代常用的转换矩阵
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM


float4x4 unity_MatrixInvV;
float4x4 unity_prev_MatrixM;
float4x4 unity_prev_MatrixIM;



//获取值的平方
float Square (float v) {
	return v * v;
}
//计算两点间距离的平方
float DistanceSquared(float3 pA, float3 pB) {
	return dot(pA - pB, pA - pB);
}



#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);
SAMPLER(sampler_CameraColorTexture);
//根据unity_OrthoParams的W分量是0还是1判断是否使用的是正交相机
bool IsOrthographicCamera () {
	return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear (float rawDepth) {
	//如果使用了反向深度缓冲区，我们还需要反转原始深度
	#if UNITY_REVERSED_Z
		rawDepth = 1.0 - rawDepth;
	#endif
	//得到正确的片元深度
	return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}


#include "Fragment.hlsl"
//解码法线数据，得到原来的法线向量
float3 DecodeNormal (float4 sample, float scale) {
	#if defined(UNITY_NO_DXT5nm)
	    return UnpackNormalRGB(sample, scale);
	#else
	    return UnpackNormalmapRGorAG(sample, scale);
	#endif
}
//将法线从切线空间转换到世界空间
float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS) {
	//构建切线到世界空间的转换矩阵，需要:世界空间的法线，世界空间的切线的XYZ和W分量
	float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}
//使用裁剪 过渡两个LOD级别
void ClipLOD (Fragment fragment, float fade) {
	#if defined(LOD_FADE_CROSSFADE)
	    //获取正常的抖动值
		float dither = InterleavedGradientNoise(fragment.positionSS, 0);
		clip(fade + (fade < 0.0 ? dither : -dither));
	#endif
}


#endif
