#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

//顶点函数输入结构体
struct Attributes {
	float3 positionOS : POSITION;
	float4 color : COLOR;
#if defined(_FLIPBOOK_BLENDING)
	float4 baseUV : TEXCOORD0;
	float flipbookBlend : TEXCOORD1;
#else
	float2 baseUV : TEXCOORD0;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
//片元函数输入结构体
struct Varyings {
	float4 positionCS_SS : SV_POSITION;
#if defined(_VERTEX_COLORS)
	float4 color : VAR_COLOR;
#endif
	float2 baseUV : VAR_BASE_UV;
#if defined(_FLIPBOOK_BLENDING)
	float3 flipbookUVB : VAR_FLIPBOOK;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//顶点函数
Varyings UnlitPassVertex(Attributes input){
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	//使UnlitPassVertex输出位置和索引,并复制索引
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(positionWS);
#if defined(_VERTEX_COLORS)
	output.color = input.color;
#endif
	//计算缩放和偏移后的UV坐标
	output.baseUV.xy = TransformBaseUV(input.baseUV.xy);
#if defined(_FLIPBOOK_BLENDING)
	output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
	output.flipbookUVB.z = input.flipbookBlend;
#endif
	return output;
}
//片元函数
float4 UnlitPassFragment (Varyings input) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS,input.baseUV);
#if defined(_VERTEX_COLORS)
	config.color = input.color;
#endif
#if defined(_FLIPBOOK_BLENDING)
	config.flipbookUVB = input.flipbookUVB;
	config.flipbookBlending = true;
#endif
#if defined(_NEAR_FADE)
	config.nearFade = true;
#endif
#if defined(_SOFT_PARTICLES)
	config.softParticles = true;
#endif
	float4 base = GetBase(config);
#if defined(_CLIPPING)
	//透明度低于阈值的片元进行舍弃
	clip(base.a - GetCutoff(config));
#endif
#if defined(_DISTORTION)
	float2 distortion = GetDistortion(config) * base.a;
	base.rgb = lerp(GetBufferColor(config.fragment, distortion).rgb, base.rgb,saturate(base.a - GetDistortionBlend(config)));
#endif
	return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
