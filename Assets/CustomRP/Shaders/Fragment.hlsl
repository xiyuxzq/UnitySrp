//片元数据相关库
#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraColorTexture);

float4 _CameraBufferSize;

struct Fragment {
	//屏幕空间位置
	float2 positionSS;
	//屏幕空间UV坐标
	float2 screenUV;
	//片元深度
	float depth;
	//深度缓冲
	float bufferDepth;
};
//得到片元数据
Fragment GetFragment (float4 positionSS) {
	Fragment f;
	f.positionSS = positionSS.xy;
	f.screenUV = f.positionSS * _CameraBufferSize.xy;// / _ScreenParams.xy;
	f.depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
	f.bufferDepth =SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV, 0);
	f.bufferDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) : LinearEyeDepth(f.bufferDepth, _ZBufferParams);
	return f;
}
//采样相机颜色纹理
float4 GetBufferColor(Fragment fragment, float2 uvOffset = float2(0.0, 0.0)) {
	float2 uv = fragment.screenUV + uvOffset;
	return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_CameraColorTexture, uv,0);
}

#endif