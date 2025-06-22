using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 自定义渲染管线实例
/// </summary>
public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer;
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    //阴影的配置
    ShadowSettings shadowSettings;
    //后处理的配置
    PostFXSettings postFXSettings;
    //LUT分辨率
    int colorLUTResolution;

    CameraBufferSettings cameraBufferSettings;

    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject,
        ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution,Shader cameraRendererShader)
    {
        this.cameraBufferSettings = cameraBufferSettings;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.colorLUTResolution = colorLUTResolution;
    
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        //灯光使用线性强度
        GraphicsSettings.lightsUseLinearIntensity = true;

        InitializeForEditor();

        renderer = new CameraRenderer(cameraRendererShader);
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //遍历所有相机单独渲染
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, cameraBufferSettings, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSettings, colorLUTResolution);
        }
    }

  
}
