using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 阴影管理类
/// </summary>
public class Shadows
{

	const string bufferName = "Shadows";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	ScriptableRenderContext context;

	CullingResults cullingResults;

	ShadowSettings settings;
	//可投射阴影的定向光源最大数量
	const int maxShadowedDirectionalLightCount = 4;
    //可投射阴影的非定向光源最大数量
    const int maxShadowedOtherLightCount = 16;
	//最大级联数量
	const int maxCascades = 4;
	//定向光的阴影数据
	struct ShadowedDirectionalLight
	{
        //可见光索引
		public int visibleLightIndex;
		//斜度比例偏差值
		public float slopeScaleBias;
		//近平面偏移
		public float nearPlaneOffset;
	}
    //非定向光的阴影数据
    struct ShadowedOtherLight
    {
        //可见光索引
        public int visibleLightIndex;
        //斜度比例偏差值
        public float slopeScaleBias;
        //法线偏移
        public float normalBias;
        //是否为点光源
        public bool isPoint;
    }
    //存储可投射阴影的非定向光源的数据
    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

    //存储可投射阴影的定向光源的数据
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
	//已存储的可投射阴影的定向光数量
	int ShadowedDirectionalLightCount;
    //已存储的可投射阴影的非定向光数量
    int shadowedOtherLightCount;

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
    static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
    static int otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");

	
	static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
	static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	
	static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    //存储级联数据
    static Vector4[] cascadeData = new Vector4[maxCascades];

    static Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
    //存储级联包围球数据
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

	static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");
    
    static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    //定向光源的PCF滤波模式
    static string[] directionalFilterKeywords = {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };
    //级联混合模式
    static string[] cascadeBlendKeywords = {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };
	//阴影蒙版模式
	static string[] shadowMaskKeywords = {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
	};
    //非定向光源的PCF滤波模式
    static string[] otherFilterKeywords = {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };
    bool useShadowMask;
	//存储光源的阴影转换矩阵
	static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];

    Vector4 atlasSizes;
    //初始化设置
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,ShadowSettings settings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
		
		ShadowedDirectionalLightCount = 0;
        shadowedOtherLightCount = 0;
        useShadowMask = false;
	}
    /// <summary>
    /// 执行缓冲区命令并清除缓冲区
    /// </summary>
    void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
    /// <summary>
    /// 存储定向光源的阴影数据
    /// </summary>
    /// <param name="light"></param>
    /// <param name="visibleLightIndex"></param>
    /// <returns></returns>
    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex) {

		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f)
		{
            float maskChannel = -1;
            //如果使用了ShadowMask
            LightBakingOutput lightBaking = light.bakingOutput;
			if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
			{
				useShadowMask = true;
                //得到光源的阴影蒙版通道索引
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b ))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight{ visibleLightIndex = visibleLightIndex,slopeScaleBias = light.shadowBias, 
				nearPlaneOffset = light.shadowNearPlane };
            //返回阴影强度、阴影图块的偏移索引、法线偏差、阴影蒙版通道索引
            return new Vector4(light.shadowStrength, settings.directional.cascadeCount * ShadowedDirectionalLightCount++, light.shadowNormalBias, maskChannel);
        }
		return new Vector4(0f, 0f, 0f, -1f);
    }
    /// <summary>
    /// 存储非定向光源的阴影数据
    /// </summary>
    /// <param name="light"></param>
    /// <param name="visibleLightIndex"></param>
    /// <returns></returns>
    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }
        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            ////得到光源的阴影蒙版通道索引
            maskChannel = lightBaking.occlusionMaskChannel;
           
        }
        bool isPoint = light.type == LightType.Point;
        int newLightCount = shadowedOtherLightCount + (isPoint ? 6 : 1);
        //非定向光源数量是否超过了设置的最大值或者是否没有阴影需要渲染
        if (newLightCount >= maxShadowedOtherLightCount ||!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b) )
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
            visibleLightIndex = visibleLightIndex,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias,
            isPoint = isPoint
        };
        Vector4 data = new Vector4(light.shadowStrength, shadowedOtherLightCount,isPoint ? 1f : 0f, maskChannel);
        shadowedOtherLightCount = newLightCount;
        return data;
    }

    /// <summary>
    /// 渲染阴影
    /// </summary>
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            //渲染定向光阴影
			RenderDirectionalShadows();
		}
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
        if (shadowedOtherLightCount > 0)
        {
            //渲染非定向光阴影
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
        }
        
        buffer.BeginSample(bufferName);

        SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

        //发送级联数量
        buffer.SetGlobalInt(cascadeCountId, ShadowedDirectionalLightCount > 0 ? settings.directional.cascadeCount : 0);
        //发送最大阴影距离和过渡距离
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        //发送图集大小和纹素大小
        buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);

        buffer.EndSample(bufferName);
		ExecuteBuffer();
	}
    /// <summary>
    /// 渲染定向光阴影
    /// </summary>
    void RenderDirectionalShadows() {
        int atlasSize = (int)settings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;

        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //指定渲染的阴影数据存储到阴影图集中
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //清除深度缓冲区
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.SetGlobalFloat(shadowPancakingId, 1f);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();
		//要分割的图块数量和大小
		int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        //遍历所有定向光源渲染阴影贴图
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
		{
			RenderDirectionalShadows(i,split, tileSize);
		}
		buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //发送级联数据
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
		//发送阴影转换矩阵
		buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);

        //设置关键字
        SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);

        buffer.EndSample(bufferName);
		ExecuteBuffer();
	}
    /// <summary>
    /// 渲染非定向光阴影
    /// </summary>
    void RenderOtherShadows()
    {
        int atlasSize = (int)settings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1f / atlasSize;

        buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //指定渲染的阴影数据存储到阴影图集中
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //清除深度缓冲区
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingId, 0f);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        //要分割的图块数量和大小
        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        //遍历所有非定向光源渲染阴影贴图
        for (int i = 0; i < shadowedOtherLightCount;)
        {
            if (shadowedOtherLights[i].isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i, split, tileSize);
                i += 1;
            }
        }

        //发送阴影转换矩阵
        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
        SetKeywords(otherFilterKeywords, (int)settings.other.filter - 1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    /// <summary>
    /// 渲染单个定向光源阴影
    /// </summary>
    /// <param name="index"></param>
    /// <param name="split"></param>
    /// <param name="tileSize"></param>
    void RenderDirectionalShadows(int index, int split, int tileSize)
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };
        int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
        float tileScale = 1f / split;
        for (int i=0;i<cascadeCount;i++)
        {
			//计算视图和投影矩阵和裁剪空间的立方体
			cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount,ratios, tileSize, light.nearPlaneOffset,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
			//得到第一个光源的包围球数据
            if (index == 0)
            {
				//设置级联数据
				SetCascadeData(i, splitData.cullingSphere, tileSize);
			}
            
            //剔除偏差
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            //剔除投影对象的数据
            shadowSettings.splitData = splitData;
			//设置视口图块
			int tileIndex = tileOffset + i;
            //得到从世界空间到阴影纹理图块空间的转换矩阵
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix,SetTileViewport(tileIndex, split, tileSize), tileScale);
			//设置视图投影矩阵
			buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //设置斜度比例偏差值
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias);        
            ExecuteBuffer();
            //绘制阴影
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
		
	}
    /// <summary>
    /// 渲染单个聚光灯光源阴影
    /// </summary>
    /// <param name="index"></param>
    /// <param name="split"></param>
    /// <param name="tileSize"></param>
    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix,out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        //剔除投影对象的数据
        shadowSettings.splitData = splitData;
        //计算法线偏差
        float texelSize = 2f / (tileSize * projectionMatrix.m00);
        float filterSize = texelSize * ((float)settings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        Vector2 offset = SetTileViewport(index, split, tileSize);
        float tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);
        //得到从世界空间到阴影纹理图块空间的转换矩阵
        otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
        //设置视图投影矩阵
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        //设置斜度比例偏差值
        buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
        
        ExecuteBuffer();
        //绘制阴影
        context.DrawShadows(ref shadowSettings);
        buffer.SetGlobalDepthBias(0f, 0f);
    }

    /// <summary>
    /// 渲染单个点光源的阴影
    /// </summary>
    /// <param name="index"></param>
    /// <param name="split"></param>
    /// <param name="tileSize"></param>
    void RenderPointShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };
        //计算法线偏差
        float texelSize = 2f / tileSize;
        float filterSize = texelSize * ((float)settings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        float tileScale = 1f / split;
        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        for (int i = 0; i < 6; i++)
        {
            cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex, (CubemapFace)i, fovBias, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,out ShadowSplitData splitData);
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;
            //剔除投影对象的数据
            shadowSettings.splitData = splitData;
            int tileIndex = index + i;         
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            SetOtherTileData(tileIndex, offset, tileScale, bias);
            //得到从世界空间到阴影纹理图块空间的转换矩阵
            otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
            //设置视图投影矩阵
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //设置斜度比例偏差值
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            
            ExecuteBuffer();
            //绘制阴影
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
        
    }
    /// <summary>
    /// 设置级联数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="cullingSphere"></param>
    /// <param name="tileSize"></param>
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
	{
		//包围球半径除以阴影图块大小=近似纹素大小
		float texelSize = 2f * cullingSphere.w / tileSize;

        float filterSize = texelSize * ((float)settings.directional.filter + 1f);
        //得到半径的平方值
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;
		cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}
    /// <summary>
    /// 存储非定向光阴影图块数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="offset"></param>
    /// <param name="scale"></param>
    /// <param name="bias"></param>
    void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
    {
        float border = atlasSizes.w * 0.5f;
        Vector4 data;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z = scale - border - border;
        data.w = bias;
        otherShadowTiles[index] = data;
    }
    /// <summary>
    /// 释放申请的RT内存
    /// </summary>
    public void Cleanup()
	{       
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        if (shadowedOtherLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasId);
        }
        ExecuteBuffer();
	}
    /// <summary>
    /// 设置视口的图块
    /// </summary>
    /// <param name="index"></param>
    /// <param name="split"></param>
    /// <param name="tileSize"></param>
    /// <returns></returns>
    Vector2 SetTileViewport(int index, int split,float tileSize)
    {
        //计算索引图块的偏移位置
        Vector2 offset = new Vector2(index % split, index / split);
        //设置渲染视口，拆分成多个图块
        buffer.SetViewport(new Rect( offset.x * tileSize, offset.y * tileSize, tileSize, tileSize ));
        return offset;
    }
    /// <summary>
    /// 得到从世界空间到阴影纹理图块空间的转换矩阵
    /// </summary>
    /// <param name="m"></param>
    /// <param name="offset"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
    {
        //如果使用了反向Zbuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵坐标
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
    /// <summary>
    /// 设置关键字
    /// </summary>
    /// <param name="keywords"></param>
    /// <param name="enabledIndex"></param>
    void SetKeywords(string[] keywords, int enabledIndex)
    {
       // int enabledIndex = (int)settings.directional.filter - 1;
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
}