using UnityEditor;
using UnityEngine;
/// <summary>
/// 后处理效果管理类：只用于编辑器
/// </summary>
partial class PostFXStack
{
	partial void ApplySceneViewState();

#if UNITY_EDITOR
	partial void ApplySceneViewState()
	{
		//如果当前绘制的Scene视图的状态为禁用图像效果，我们则将后处理特效资产配置设为空，来停止对后处理特效的渲染
		if (camera.cameraType == CameraType.SceneView &&!SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
		{
			settings = null;
		}
	}
#endif
}