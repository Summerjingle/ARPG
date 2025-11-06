using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasCameraFixer : MonoBehaviour
{
    private Canvas canvas;

    void Start()
    {
        // 获取所有Canvas组件
        Canvas[] canvases = GetComponents<Canvas>();

        for (int i = 0; i < canvases.Length; i++)
        {
            Debug.Log($"Canvas[{i}]: {canvases[i].name}, RenderMode: {canvases[i].renderMode}, WorldCamera: {canvases[i].worldCamera != null}");
        }

        // 使用第一个Canvas
        if (canvases.Length > 0)
        {
            canvas = canvases[0];
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        FixCamera();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景切换: {scene.name}");
        FixCamera();
    }

    private void FixCamera()
    {
        if (canvas == null) return;

        // 只在主菜单场景中绑定摄像机
        if (SceneManager.GetActiveScene().name != "000Scene_Menu")
        {
            Debug.Log($"当前场景不是主菜单，跳过摄像机绑定: {SceneManager.GetActiveScene().name}");
            return;
        }

        Debug.Log($"在主菜单场景中绑定摄像机");

        // 强制设置为ScreenSpaceCamera模式
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            Debug.Log($"通过FindObjectOfType找到摄像机: {mainCamera != null}");
        }

        if (mainCamera != null)
        {
            canvas.worldCamera = mainCamera;
            Debug.Log($"已为Canvas绑定摄像机: {mainCamera.name}");
        }
        else
        {
            Debug.LogError("没有找到任何摄像机！");
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}