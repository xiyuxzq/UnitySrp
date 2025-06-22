using System;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 相机组件的扩展配置项
/// </summary>
[Serializable]
public class CameraSettings
{
    //是否拷贝深度和颜色
    public bool copyDepth = true;

    public bool copyColor = true;
    //设置相机的Rendering Layer Mask来限制相机的渲染
    [RenderingLayerMaskField]
	public int renderingLayerMask = -1;

	public bool maskLights = false;
    //渲染缩放模式设置：分别为继承、叠加相乘、覆盖
    public enum RenderScaleMode
    {
        Inherit,
        Multiply,
        Override
    }

    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;
    public const float renderScaleMin = 0.1f, renderScaleMax = 2f;
    //渲染缩放比例
    [Range(renderScaleMin, renderScaleMax)]
    public float renderScale = 1f;

    public bool overridePostFX = false;

	public PostFXSettings postFXSettings = default;
    //存储源和目标的混合模式
    [Serializable]
	public struct FinalBlendMode
	{

		public BlendMode source, destination;
	}

	public FinalBlendMode finalBlendMode = new FinalBlendMode
	{
		source = BlendMode.One,
		destination = BlendMode.Zero
	};

    public float GetRenderScale(float scale)
    {
        return renderScaleMode == RenderScaleMode.Inherit ? scale :
            renderScaleMode == RenderScaleMode.Override ? renderScale : scale * renderScale;
    }
}