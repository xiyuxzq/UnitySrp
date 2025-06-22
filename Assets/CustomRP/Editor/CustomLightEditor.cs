using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
///扩展灯光属性面板
[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor 
{
    static GUIContent renderingLayerMaskLabel = new GUIContent("Rendering Layer Mask", "Functional version of above property.");


    //重写灯光属性面板
    public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
 
        RenderingLayerMaskDrawer.Draw(settings.renderingLayerMask, renderingLayerMaskLabel);
        //如果是聚光灯面板
        if (!settings.lightType.hasMultipleDifferentValues &&(LightType)settings.lightType.enumValueIndex == LightType.Spot)
		{
            //绘制一个调节内外聚光角度滑块
            settings.DrawInnerAndOuterSpotAngle();
		}
        settings.ApplyModifiedProperties();
        //如果光源的cullingMask不是Everything层，显示警告:cullingMask只影响阴影
        //如果不是定向光源，则提示除非开启逐对象光照，除了影响阴影还可以影响物体受光
        var light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(light.type == LightType.Directional ?
                     "Culling Mask only affects shadows." :
                     "Culling Mask only affects shadow unless Use Lights Per Objects is on.",
                 MessageType.Warning);
        }
    }
}
