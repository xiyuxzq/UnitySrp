using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;
/// <summary>
/// 自定义渲染管线实例:只用于编辑器
/// </summary>
public partial class CustomRenderPipeline
{

    partial void InitializeForEditor();
    partial void DisposeForEditor();

#if UNITY_EDITOR
    //重写lightmapper设置光照数据
    static Lightmapping.RequestLightsDelegate lightsDelegate =
        (Light[] lights, NativeArray<LightDataGI> output) => {
            var lightData = new LightDataGI();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                switch (light.type)
                {
                    case LightType.Directional:
                        var directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight);
                        lightData.Init(ref directionalLight);
                        break;
                    case LightType.Point:
                        var pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight);
                        break;
                    case LightType.Spot:
                        var spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.innerConeAngle =
                            light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff =
                            AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight);
                        break;
                    case LightType.Area:
                        var rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.mode = LightMode.Baked;
                        lightData.Init(ref rectangleLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }
                lightData.falloff = FalloffType.InverseSquared;
                output[i] = lightData;
            }
        };
    //通过unity编辑器中执行光照烘焙之前提供一个委托方法，来告诉Unity使用不同的衰减
    partial void InitializeForEditor()
    {
        Lightmapping.SetDelegate(lightsDelegate);
    }
    //重置委托
    partial void DisposeForEditor()
    {
        Lightmapping.ResetDelegate();
    }


#endif

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        renderer.Dispose();
    }
}