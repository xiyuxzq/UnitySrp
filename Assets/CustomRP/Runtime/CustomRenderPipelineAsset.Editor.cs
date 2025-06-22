/// <summary>
/// 自定义渲染管线资产:只用于编辑器
/// </summary>
partial class CustomRenderPipelineAsset
{
#if UNITY_EDITOR
    static string[] renderingLayerNames;
    //在静态构造函数中为Rendering Layer Mask的层级重命名
    static CustomRenderPipelineAsset()
    {
        renderingLayerNames = new string[31];
        for (int i = 0; i < renderingLayerNames.Length; i++)
        {
            renderingLayerNames[i] = "Layer " + (i + 1);
        }
    }
    public override string[] renderingLayerMaskNames => renderingLayerNames;

#endif
}