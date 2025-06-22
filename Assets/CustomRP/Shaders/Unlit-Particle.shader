Shader "CustomRP/Particles/Unlit"
{
    Properties
    {
	   _BaseMap("Texture", 2D) = "white" {}
	  [HDR]  _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	   //透明度测试的阈值
	   _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
	   [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
	   //使用顶点色
	   [Toggle(_VERTEX_COLORS)] _VertexColors ("Vertex Colors", Float) = 0
	   //使用Flipbook混合
	   [Toggle(_FLIPBOOK_BLENDING)] _FlipbookBlending ("Flipbook Blending", Float) = 0
	   //粒子淡化
	   [Toggle(_NEAR_FADE)] _NearFade ("Near Fade", Float) = 0
		_NearFadeDistance ("Near Fade Distance", Range(0.0, 10.0)) = 1
		_NearFadeRange ("Near Fade Range", Range(0.01, 10.0)) = 1
		//软粒子
		[Toggle(_SOFT_PARTICLES)] _SoftParticles("Soft Particles", Float) = 0
		_SoftParticlesDistance("Soft Particles Distance", Range(0.0, 10.0)) = 0
		_SoftParticlesRange("Soft Particles Range", Range(0.01, 10.0)) = 1
		//扰动效果
		[Toggle(_DISTORTION)] _Distortion("Distortion", Float) = 0
		[NoScaleOffset] _DistortionMap("Distortion Vectors", 2D) = "bumb" {}
		_DistortionStrength("Distortion Strength", Range(0.0, 0.2)) = 0.1
		_DistortionBlend("Distortion Blend", Range(0.0, 1.0)) = 1
	   //设置混合模式
	  [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
	  [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
	  //默认写入深度缓冲区
	  [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {     
        HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "UnlitInput.hlsl"
		ENDHLSL
        Pass
        {
		   //定义混合模式
		   Blend[_SrcBlend][_DstBlend], One OneMinusSrcAlpha
		   //是否写入深度
		   ZWrite[_ZWrite]
           HLSLPROGRAM
		   #pragma target 3.5
		   #pragma shader_feature _CLIPPING
		   #pragma shader_feature _VERTEX_COLORS
		   #pragma shader_feature _FLIPBOOK_BLENDING
		   #pragma shader_feature _SOFT_PARTICLES
		   #pragma shader_feature _DISTORTION
		   #pragma shader_feature _NEAR_FADE
           #pragma multi_compile_instancing
           #pragma vertex UnlitPassVertex
           #pragma fragment UnlitPassFragment
		   //插入相关hlsl代码
           #include"UnlitPass.hlsl"
           ENDHLSL
        }
		Pass
        {
		   Tags {
				"LightMode" = "ShadowCaster"
			}
		    ColorMask 0

            HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
        }
		
    }
		   CustomEditor "CustomShaderGUI"
}
