//阴影投射器
#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

//顶点函数输入结构体
struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
//片元函数输入结构体
struct Varyings {
	float4 positionCS_SS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;
//顶点函数
Varyings ShadowCasterPassVertex(Attributes input){
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	//使UnlitPassVertex输出位置和索引,并复制索引
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(positionWS);
	if (_ShadowPancaking) {
	    #if UNITY_REVERSED_Z
		    output.positionCS_SS.z = min(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
	    #else
		    output.positionCS_SS.z = max(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
	#endif
	}
	
	//计算缩放和偏移后的UV坐标
	output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}
//片元函数
void ShadowCasterPassFragment(Varyings input) {
	UNITY_SETUP_INSTANCE_ID(input);
	InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
	ClipLOD(config.fragment, unity_LODFade.x);
	float4 base = GetBase(config);
#if defined(_SHADOWS_CLIP)
	//透明度低于阈值的片元进行舍弃
	clip(base.a - GetCutoff(config));
#elif defined(_SHADOWS_DITHER)
	//计算抖动值
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(base.a - dither);
#endif
	
}

#endif
