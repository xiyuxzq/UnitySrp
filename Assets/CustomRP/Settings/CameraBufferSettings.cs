using UnityEngine;
//相机缓冲区相关设置
[System.Serializable]
public struct CameraBufferSettings
{
    //是否启用HDR
    public bool allowHDR;
    //是否拷贝深度
    public bool copyDepth;
    public bool copyDepthReflection;
    //是否拷贝颜色
    public bool copyColor;
    public bool copyColorReflection;
    public const float renderScaleMin = 0.1f, renderScaleMax = 2f;
    //渲染缩放比例
    [Range(renderScaleMin, renderScaleMax)]    
    public float renderScale;
    //双三次重新缩放模式:分别是关闭，仅向上采样，向上和向下采样
    public enum BicubicRescalingMode { 
        Off,
        UpOnly,
        UpAndDown 
    }
    public BicubicRescalingMode bicubicRescaling;
}