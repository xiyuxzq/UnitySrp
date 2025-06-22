//Unity标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
//相机位置
float3 _WorldSpaceCameraPos;
//这个矩阵包含一些在这里我们不需要的转换信息
real4 unity_WorldTransformParams;

float4 unity_RenderingLayer;
//灯光数据和索引
real4 unity_LightData;
real4 unity_LightIndices[2];

float4 unity_ProbesOcclusion;
float4 unity_SpecCube0_HDR;

float4 unity_LightmapST;
float4 unity_DynamicLightmapST;
//存储光探针数据,它们是代表红色、绿色、蓝光的多项式组件
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;

//LPPV相关数据
float4 unity_ProbeVolumeParams;
float4x4 unity_ProbeVolumeWorldToObject;
float4 unity_ProbeVolumeSizeInv;
float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4 _ProjectionParams;
//正交相机信息
float4 unity_OrthoParams;

float4 _ScreenParams;
float4 _ZBufferParams;
#endif
