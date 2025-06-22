using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 相机渲染管理类：单独控制每个相机的渲染
/// </summary>
public partial class CameraRenderer
{

    ScriptableRenderContext context;

    Camera camera;
    
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    //存储相机剔除后的结果
    CullingResults cullingResults;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    //光照实例
    Lighting lighting = new Lighting();

    PostFXStack postFXStack = new PostFXStack();

    static int bufferSizeId = Shader.PropertyToID("_CameraBufferSize");
    static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
    static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
    static int sourceTextureId = Shader.PropertyToID("_SourceTexture");

    static CameraSettings defaultCameraSettings = new CameraSettings();

    bool useHDR;
    bool useScaledRendering;
    //最终使用的缓冲区大小
    Vector2Int bufferSize;
    public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

    //是否使用深度纹理
    bool useDepthTexture;
    //是否使用颜色纹理
    bool useColorTexture;
    //是否使用中间帧缓冲
    bool useIntermediateBuffer;
    //平台是否支持拷贝纹理
    static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

    Material material;
    Texture2D missingTexture;

    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        //默认创建一个1X1大小的缺失纹理，对深度纹理采样时，确保无效的采样也能得到正确结果
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }

    public void Dispose()
    {
        //销毁创建的材质和缺失纹理
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }
    /// <summary>
    /// 相机渲染
    /// </summary>
    public void Render(ScriptableRenderContext context, Camera camera, CameraBufferSettings bufferSettings,
        bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject,ShadowSettings shadowSettings, PostFXSettings postFXSettings,
        int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;

        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = bufferSettings.copyDepthReflection;
            useColorTexture = bufferSettings.copyColorReflection;
        }
        else
        {
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
            useColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
        }
        //如果需要覆盖后处理配置，将渲染管线的后处理配置替换成该相机的后处理配置
        if (cameraSettings.overridePostFX)
        {
            postFXSettings = cameraSettings.postFXSettings;
        }
        //根据管线中渲染比例的值和相机的设置来判断是否需要使用渲染缩放
        float renderScale = cameraSettings.GetRenderScale(bufferSettings.renderScale);
        useScaledRendering = renderScale != 1f;
        //设置buffer缓冲区的名字
        PrepareBuffer();
        // 在Game视图绘制的几何体也绘制到Scene视图中
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        //按比例缩放相机屏幕像素尺寸
        if (useScaledRendering)
        {
            renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
            bufferSize.x = (int)(camera.pixelWidth * renderScale);
            bufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        {
            bufferSize.x = camera.pixelWidth;
            bufferSize.y = camera.pixelHeight;
        }

        buffer.BeginSample(SampleName);
        buffer.SetGlobalVector(bufferSizeId, new Vector4(1f / bufferSize.x, 1f / bufferSize.y,bufferSize.x, bufferSize.y));
        ExecuteBuffer();

        lighting.Setup(context, cullingResults, shadowSettings, useLightsPerObject, cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
        postFXStack.Setup(context, camera, bufferSize,postFXSettings, useHDR, colorLUTResolution,cameraSettings.finalBlendMode,bufferSettings.bicubicRescaling);
        buffer.EndSample(SampleName);
        Setup();

        //绘制几何体
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing,useLightsPerObject, cameraSettings.renderingLayerMask);
        //绘制SRP不支持的内置shader类型
        DrawUnsupportedShaders();

        //绘制Gizmos
        DrawGizmosBeforeFX();
        if (postFXStack.IsActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if (useIntermediateBuffer)
        {
            Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();
        // // 释放申请的RT内存空间
        Cleanup();

        //提交命令缓冲区
        Submit();
    }
    /// <summary>
    /// 释放申请的RT内存空间
    /// </summary>
    void Cleanup()
    {
        
        lighting.Cleanup();
        if (useIntermediateBuffer)
        {
            //释放颜色和深度纹理
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);

            if (useColorTexture)
            {
                buffer.ReleaseTemporaryRT(colorTextureId);
            }
            //释放临时深度纹理
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }
    }

    /// <summary>
    /// 绘制几何体
    /// </summary>
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //设置绘制顺序和指定渲染相机
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的shader pass和渲染排序
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //设置渲染时批处理的使用状态
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes | lightsPerObjectFlags
        };
        //渲染CustomLit表示的pass块
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        ////只绘制RenderQueue为opaque不透明的物体
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);
        //1.绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        //2.绘制天空盒
        context.DrawSkybox(camera);

        //深度拷贝
        if (useColorTexture || useDepthTexture)
        {
            CopyAttachments();
        }

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        //只绘制RenderQueue为transparent透明的物体
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }
    /// <summary>
    /// 提交命令缓冲区
    /// </summary>
    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }
    /// <summary>
    /// 设置相机的属性和矩阵
    /// </summary>
    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机的clear flags
        CameraClearFlags flags = camera.clearFlags;

        useIntermediateBuffer = useScaledRendering || useColorTexture || useDepthTexture || postFXStack.IsActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(colorAttachmentId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

            buffer.GetTemporaryRT(depthAttachmentId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);

            buffer.SetRenderTarget(colorAttachmentId,RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthAttachmentId,RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }
        //设置相机清除状态
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        ExecuteBuffer();
        
    }
    /// <summary>
    /// 执行缓冲区命令
    /// </summary>
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    /// <summary>
    /// 剔除
    /// </summary>
    /// <returns></returns>
    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;

        if (camera.TryGetCullingParameters(out p))
        {
            //得到最大阴影距离,和相机远裁剪面距离作比较，取最小的那个作为阴影距离
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
    //拷贝深度数据
    void CopyAttachments()
    {
        if (useColorTexture)
        {
            buffer.GetTemporaryRT(colorTextureId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            if (copyTextureSupported)
            {
                buffer.CopyTexture(colorAttachmentId, colorTextureId);
            }
            else
            {
                //将颜色附件数据拷贝到颜色纹理中
                Draw(colorAttachmentId, colorTextureId);
            }
        }

        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
            if (copyTextureSupported)
            {       
                buffer.CopyTexture(depthAttachmentId, depthTextureId);
            }
            else
            {
                //将深度附件数据拷贝到深度纹理中
                Draw(depthAttachmentId, depthTextureId, true);
     
            }
        }

        if (!copyTextureSupported)
        {
            buffer.SetRenderTarget(colorAttachmentId,RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachmentId,RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }
        ExecuteBuffer();
    }
    /// <summary>
    /// 将源数据绘制到指定渲染目标中
    /// </summary>
    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }
}
