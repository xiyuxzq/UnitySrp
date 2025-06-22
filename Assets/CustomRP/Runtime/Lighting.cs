using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 灯光管理类
/// </summary>
public class Lighting
{

	const string bufferName = "Lighting";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};
    //设置最大可见定向光数量
    const int maxDirLightCount = 4;
    //设置最大非定性光数量
    const int maxOtherLightCount = 64;

    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsAndMasksId = Shader.PropertyToID("_DirectionalLightDirectionsAndMasks");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    static int otherLightDirectionsAndMasksId = Shader.PropertyToID("_OtherLightDirectionsAndMasks");
    static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");


    //存储定向光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirectionsAndMasks = new Vector4[maxDirLightCount];
    //存储定向光的阴影数据
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    //存储非定向光源的颜色和位置数据
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightDirectionsAndMasks = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];
    //存储相机剔除后的结果
    CullingResults cullingResults;

    Shadows shadows = new Shadows();

    static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";
    //初始化设置
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,ShadowSettings shadowSettings, bool useLightsPerObject, int renderingLayerMask)
	{
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //阴影的初始化设置
        shadows.Setup(context, cullingResults, shadowSettings);
        //存储并发送所有光源数据
        SetupLights(useLightsPerObject, renderingLayerMask);
        //渲染阴影
        shadows.Render();
        buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
    /// <summary>
    /// 存储定向光的数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleIndex"></param>
    /// <param name="visibleLight"></param>
    /// <param name="light"></param>
	void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light) {
        dirLightColors[index] = visibleLight.finalColor;
        //通过VisibleLight.localToWorldMatrix属性找到前向矢量,它在矩阵第三列，还要进行取反
        Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        dirLightDirectionsAndMasks[index] = dirAndMask;
        //存储阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(light,visibleIndex);
    }
    /// <summary>
    /// 存储点光源的数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleIndex"></param>
    /// <param name="visibleLight"></param>
    /// <param name="light"></param>
    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        otherLightColors[index] = visibleLight.finalColor;
        //位置信息在本地到世界的转换矩阵的最后一列
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        //将光照范围的平方的倒数存储在位置信息的w分量中
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);

        Vector4 dirAndmask = Vector4.zero;
        dirAndmask.w = light.renderingLayerMask.ReinterpretAsFloat();
        otherLightDirectionsAndMasks[index] = dirAndmask;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    /// <summary>
    /// 存储聚光灯的数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleIndex"></param>
    /// <param name="visibleLight"></param>
    /// <param name="light"></param>
    void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        //本地到世界的转换矩阵的第三列在求反得到光照方向
        otherLightDirectionsAndMasks[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        otherLightDirectionsAndMasks[index] = dirAndMask;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    /// <summary>
    /// 存储并发送所有光源数据
    /// </summary>
    /// <param name="useLightsPerObject"></param>
    /// <param name="renderingLayerMask"></param>
    void SetupLights(bool useLightsPerObject, int renderingLayerMask) {
        //得到光源索引列表
        NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        //得到所有影响相机渲染物体的可见光数据
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        
        int dirLightCount = 0;
        //其他类型光源数量
        int otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            Light light = visibleLight.light;
            if ((light.renderingLayerMask & renderingLayerMask) != 0)
            {
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                        {
                            SetupDirectionalLight(dirLightCount++, i, ref visibleLight, light);
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, i, ref visibleLight, light);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, i, ref visibleLight, light);
                        }
                        break;
                }
            }
            //匹配光源索引
            if (useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }
        }
        //消除所有不可见光的索引
        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
            //最后重新设置调整后的灯光索引列表，发送给unity，然后indexMap就不需要了，进行释放
            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(lightsPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(lightsPerObjectKeyword);
        }
        //发送所有光源数据
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsAndMasksId, dirLightDirectionsAndMasks);
           
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        
        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsAndMasksId, otherLightDirectionsAndMasks);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
    }
    //释放申请的RT内存
    public void Cleanup()
    {
        shadows.Cleanup();
    }
}
