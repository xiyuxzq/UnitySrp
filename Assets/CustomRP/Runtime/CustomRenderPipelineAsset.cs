using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 自定义渲染管线资产
/// </summary>
//该标签会在你在Project下右键->Asset/Create菜单中添加一个新的子菜单
[CreateAssetMenu(menuName ="Rendering/CreateCustomRenderPipeline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    //设置批处理启用状态
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    [SerializeField]
    CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        allowHDR = true,
        renderScale = 1f
    };
    //是否使用逐对象光照
    [SerializeField]
    bool useLightsPerObject = true;
    //阴影配置
    [SerializeField]
    ShadowSettings shadows = default;
    //后处理配置
    [SerializeField]
    PostFXSettings postFXSettings = default;
    //LUT纹理分辨率
    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }
    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    [SerializeField]
    Shader cameraRendererShader = default; 

    //重写抽象方法，需要返回一个RenderPipeline实例对象
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution, cameraRendererShader);
    }
}
