//相机渲染相关库
#ifndef CUSTOM_CAMERA_RENDERER_PASSES_INCLUDED
#define CUSTOM_CAMERA_RENDERER_PASSES_INCLUDED

TEXTURE2D(_SourceTexture);

struct Varyings {
	float4 positionCS : SV_POSITION;
	float2 screenUV : VAR_SCREEN_UV;
};

Varyings DefaultPassVertex(uint vertexID : SV_VertexID) {
	//使用顶点标识Id生成固定的顶点位置和UV坐标。其中三角形顶点坐标为分别为(-1,-1)，(-1,3)，（3,-1）,要使可见的 UV 坐标覆盖0到1的范围，则对应的UV坐标为(0,0)，（0,2）（2,0）
	Varyings output;
	output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
	output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
	if (_ProjectionParams.x < 0.0) {
		output.screenUV.y = 1.0 - output.screenUV.y;
	}
	return output;
}
//采样源纹理
float4 CopyPassFragment(Varyings input) : SV_TARGET{
	return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, input.screenUV, 0);
}
float CopyDepthPassFragment(Varyings input) : SV_DEPTH{
	return SAMPLE_DEPTH_TEXTURE_LOD(_SourceTexture, sampler_point_clamp, input.screenUV, 0);
}
#endif