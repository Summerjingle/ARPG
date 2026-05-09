using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasCameraFixer : MonoBehaviour
{
    private Canvas canvas;

    void Start()
    {
        Canvas[] canvases = GetComponents<Canvas>();

        for (int i = 0; i < canvases.Length; i++)
        {
            Debug.Log($"Canvas[{i}]: {canvases[i].name}, RenderMode: {canvases[i].renderMode}, WorldCamera: {canvases[i].worldCamera != null}");
        }

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

        string sceneName = SceneManager.GetActiveScene().name;
        bool isMenu = sceneName == "000Scene_Menu" || sceneName == "WhiteBox_Menu";

        if (isMenu)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
                canvas.renderMode = RenderMode.WorldSpace;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            if (mainCamera != null)
                canvas.worldCamera = mainCamera;
            else
                Debug.LogError("没有找到任何相机");
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
