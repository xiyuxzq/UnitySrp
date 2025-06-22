using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;
/// <summary>
/// 后处理效果管理类
/// </summary>
public partial class PostFXStack
{
	const string bufferName = "Post FX";
	int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
	int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
	int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
	int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");
	int fxSourceId = Shader.PropertyToID("_PostFXSource");
	int fxSource2Id = Shader.PropertyToID("_PostFXSource2");

	int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
	int colorFilterId = Shader.PropertyToID("_ColorFilter");
	int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");

	int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
	int splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");

    int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
    int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
    int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");

    int smhShadowsId = Shader.PropertyToID("_SMHShadows");
    int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
    int smhRangeId = Shader.PropertyToID("_SMHRange");

    int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
    int colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC");

	int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
	int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
	int finalResultId = Shader.PropertyToID("_FinalResultId");
	int copyBicubicId = Shader.PropertyToID("_CopyBicubic");

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};
	ScriptableRenderContext context;
	Camera camera;
	PostFXSettings settings;

	CameraSettings.FinalBlendMode finalBlendMode;
	bool useHDR;
	//最大纹理金字塔级别
	const int maxBloomPyramidLevels = 16;
	//纹理标识符
	int bloomPyramidId;
	//LUT分辨率
    int colorLUTResolution;
	//每个枚举值对应一个后处理着色器Pass
    enum Pass
	{
		BloomHorizontal,
		BloomVertical,
		BloomAdd,
        BloomScatter,
        BloomScatterFinal,
        BloomPrefilter,
		BloomPrefilterFireflies,
		Copy,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        Final,
		FinalRescale

    }
	//判断后效栈是否激活
	public bool IsActive => settings != null;
	//缓冲区大小
    Vector2Int bufferSize;

	CameraBufferSettings.BicubicRescalingMode bicubicRescaling;
    //在构造方法中获取纹理标识符，且只跟踪第一个标识符即可
    public PostFXStack()
	{
		bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
		for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
		{
			Shader.PropertyToID("_BloomPyramid" + i);
		}
	}
    //初始化设置
	public void Setup(ScriptableRenderContext context, Camera camera, Vector2Int bufferSize, PostFXSettings settings, bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode, CameraBufferSettings.BicubicRescalingMode bicubicRescaling)
	{
		this.useHDR = useHDR;
		this.context = context;
		this.camera = camera;
        this.bufferSize = bufferSize;
		this.bicubicRescaling = bicubicRescaling;
		this.colorLUTResolution = colorLUTResolution;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
		this.finalBlendMode = finalBlendMode;
		ApplySceneViewState();
	}
    /// <summary>
    /// 将源数据绘制到指定渲染目标中
    /// </summary>
    /// <param name="from">源标识符</param>
    /// <param name="to">目标标识符</param>
    /// <param name="pass">通道序号</param>
	void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
	{
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //绘制三角形
		buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,MeshTopology.Triangles, 3);
	}
    /// <summary>
    /// 最终绘制
    /// </summary>
    /// <param name="from"></param>
    /// <param name="pass"></param>
	void DrawFinal(RenderTargetIdentifier from, Pass pass)
	{
		buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
		buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);

		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 
			finalBlendMode.destination == BlendMode.Zero ? RenderBufferLoadAction.DontCare: RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
		//最终绘制之前设置视口
		buffer.SetViewport(camera.pixelRect);
		buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3);
	}
    /// <summary>
    /// 渲染后处理特效
    /// </summary>
    /// <param name="sourceId"></param>
	public void Render(int sourceId)
	{
        //渲染Bloom
        if (DoBloom(sourceId))
        {
            //之后同时进行颜色分级和色调映射
			DoColorGradingAndToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
			DoColorGradingAndToneMapping(sourceId);
        }
        context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
    /// <summary>
    /// 渲染Bloom
    /// </summary>
    /// <param name="sourceId"></param>
    /// <returns></returns>
	bool DoBloom(int sourceId)
	{
		PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width, height;
        //若需要渲染缩放，则使用缓冲区大小替代相机屏幕尺寸
        if (bloom.ignoreRenderScale)
        {
            width = camera.pixelWidth / 2;
            height = camera.pixelHeight / 2;
        }
        else
        {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;
        }
        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2)
		{
            
			return false;
		}
        buffer.BeginSample("Bloom");
        //发送阈值和相关数据
        Vector4 threshold;
		threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
		threshold.y = threshold.x * bloom.thresholdKnee;
		threshold.z = 2f * threshold.y;
		threshold.w = 0.25f / (threshold.y + 0.00001f);
		threshold.y -= threshold.x;
		buffer.SetGlobalVector(bloomThresholdId, threshold);

		RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
		buffer.GetTemporaryRT(bloomPrefilterId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, format);
		Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
		width /= 2;
		height /= 2;

		int fromId = bloomPrefilterId;
		int toId = bloomPyramidId + 1;
		int i;
        //逐步下采样
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}
			int midId = toId - 1;
			buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
			buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
			Draw(fromId, midId, Pass.BloomHorizontal);
			Draw(midId, toId, Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= 2;
			height /= 2;
		}
		buffer.ReleaseTemporaryRT(bloomPrefilterId);
		buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }
        //逐步上采样
        if (i > 1)
		{
			buffer.ReleaseTemporaryRT(fromId - 1);
			toId -= 5;
			for (i -= 1; i > 0; i--)
			{
				buffer.SetGlobalTexture(fxSource2Id, toId + 1);
				Draw(fromId, toId, combinePass);
				buffer.ReleaseTemporaryRT(fromId);
				buffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
        }
        else
        {
			buffer.ReleaseTemporaryRT(bloomPyramidId);
		}
		buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
		buffer.SetGlobalTexture(fxSource2Id, sourceId);
        buffer.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0,
            FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);
		buffer.ReleaseTemporaryRT(fromId);
		buffer.EndSample("Bloom");
        return true;
    }
    /// <summary>
    /// 获取颜色调整的配置
    /// </summary>
    void ConfigureColorAdjustments()
	{
		ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
		buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
			Mathf.Pow(2f, colorAdjustments.postExposure),
			colorAdjustments.contrast * 0.01f + 1f,
			colorAdjustments.hueShift * (1f / 360f),
			colorAdjustments.saturation * 0.01f + 1f));
		buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
	}
    /// <summary>
    /// 获取白平衡配置
    /// </summary>
	void ConfigureWhiteBalance()
	{
		WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
		buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
	}
    /// <summary>
    /// 获取色调分离的配置
    /// </summary>
	void ConfigureSplitToning()
	{
		SplitToningSettings splitToning = settings.SplitToning;
		Color splitColor = splitToning.shadows;
		splitColor.a = splitToning.balance * 0.01f;
		buffer.SetGlobalColor(splitToningShadowsId, splitColor);
		buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
	}
    /// <summary>
    /// 获取通道混合器的配置
    /// </summary>
    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }
    /// <summary>
    /// 获取Shadows Midtones Highlights的配置
    /// </summary>
    void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd));
    }
    /// <summary>
    /// 同时进行颜色分级和色调映射
    /// </summary>
    /// <param name="sourceId"></param>
    void DoColorGradingAndToneMapping(int sourceId)
    {
		ConfigureColorAdjustments();
		ConfigureWhiteBalance();
		ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0,FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4( lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));

        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(colorGradingLUTInLogId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
        //将源纹理渲染到LUT纹理中而不是相机目标
        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId,new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
		if (bufferSize.x == camera.pixelWidth)
		{
			DrawFinal(sourceId, Pass.Final);
		}
		else {
			buffer.SetGlobalFloat(finalSrcBlendId, 1f);
			buffer.SetGlobalFloat(finalDstBlendId, 0f);
			buffer.GetTemporaryRT(finalResultId, bufferSize.x, bufferSize.y, 0,FilterMode.Bilinear, RenderTextureFormat.Default);
			Draw(sourceId, finalResultId, Pass.Final);
			bool bicubicSampling = bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
				bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly && bufferSize.x < camera.pixelWidth;
			buffer.SetGlobalFloat(copyBicubicId, bicubicSampling ? 1f : 0f);
			DrawFinal(finalResultId, Pass.FinalRescale);
			buffer.ReleaseTemporaryRT(finalResultId);
		}
		buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }
}