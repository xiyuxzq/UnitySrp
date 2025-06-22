using System;
using UnityEngine;
/// <summary>
/// 后处理特效栈的配置
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject 
{
    [SerializeField]
    Shader shader = default;

	[System.NonSerialized]
	Material material;
	[System.Serializable]
	public struct BloomSettings
	{
        //模糊迭代次数
		[Range(0f, 16f)]
		public int maxIterations;
        //下采样纹理尺寸下限
		[Min(1f)]
		public int downscaleLimit;
        //双三次滤波上采样
		public bool bicubicUpsampling;
        //阈值
		[Min(0f)]
		public float threshold;
        //阈值拐点
		[Range(0f, 1f)]
		public float thresholdKnee;
        //Bloom强度
		[Min(0f)]
		public float intensity;
		//淡化闪烁
		public bool fadeFireflies;
        //Bloom模式：叠加或散射
        public enum Mode { Additive, Scattering }

        public Mode mode;
        //控制光线散射的程度
        [Range(0.05f, 0.95f)]
        public float scatter;
        //是否忽略渲染缩放
        public bool ignoreRenderScale;
    }
	//颜色调整的配置
	[Serializable]
	public struct ColorAdjustmentsSettings {
        //后曝光，调整场景的整体曝光度
        public float postExposure;
        //对比度，扩大或缩小色调值的总体范围
        [Range(-100f, 100f)]
		public float contrast;
        //颜色滤镜，通过乘以颜色来给渲染器着色
        [ColorUsage(false, true)]
		public Color colorFilter;
        //色调偏移，改变所有颜色的色调
        [Range(-180f, 180f)]
		public float hueShift;
        //饱和度，推动所有颜色的强度
        [Range(-100f, 100f)]
		public float saturation;
	}

	[SerializeField]
	ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings
	{
		colorFilter = Color.white
	};

	public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;
    //白平衡的配置
	[Serializable]
	public struct WhiteBalanceSettings
	{
        //色温，调整白平衡的冷暖偏向
        [Range(-100f, 100f)]
        public float temperature;
        //色调，调整温度变化后的颜色
        [Range(-100f, 100f)]
        public float tint;
    }

	[SerializeField]
	WhiteBalanceSettings whiteBalance = default;

	public WhiteBalanceSettings WhiteBalance => whiteBalance;
    //色调分离的配置
	[Serializable]
	public struct SplitToningSettings
	{
        //用于对阴影和高光着色
        [ColorUsage(false)]
        public Color shadows, highlights;
        //设置阴影和高光之间的平衡的滑块
        [Range(-100f, 100f)]
        public float balance;
    }

	[SerializeField]
	SplitToningSettings splitToning = new SplitToningSettings
	{
		shadows = Color.gray,
		highlights = Color.gray
	};

	public SplitToningSettings SplitToning => splitToning;
    //通道混合器的配置
    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }

    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward
    };

    public ChannelMixerSettings ChannelMixer => channelMixer;
    //Shadows Midtones Highlights的配置
    [Serializable]
    public struct ShadowsMidtonesHighlightsSettings
    {
        //阴影、中间色调和高光
        [ColorUsage(false, true)]
        public Color shadows, midtones, highlights;
        //滑块分别用于设置阴影和渲染中间色调之间过渡的起始点和结束点,渲染中间色调和高光之间过渡的起始点和结束点
        [Range(0f, 2f)]
        public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
    }

    [SerializeField]
    ShadowsMidtonesHighlightsSettings
        shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
        {
            shadows = Color.white,
            midtones = Color.white,
            highlights = Color.white,
            shadowsEnd = 0.3f,
            highlightsStart = 0.55f,
            highLightsEnd = 1f
        };

    public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights => shadowsMidtonesHighlights;
    //色调映射的配置
    [System.Serializable]
    public struct ToneMappingSettings
    {
        //色调映射常用的几种模式
        public enum Mode {
            None,
            ACES,
            Neutral,
            Reinhard
        }

        public Mode mode;
    }

    [SerializeField]
    ToneMappingSettings toneMapping = default;

    public ToneMappingSettings ToneMapping => toneMapping;

    [SerializeField]
	BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f
    };

    public BloomSettings Bloom => bloom;
	public Material Material
	{
		get
		{
			if (material == null && shader != null)
			{
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}
}
