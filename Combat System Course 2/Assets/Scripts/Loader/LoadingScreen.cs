using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float minLoadTime = 2f;

    private void Start()
    {
        progressFill.fillAmount = 0f;
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        string targetScene = PlayerPrefs.GetString("TargetScene", "00Scene_Village");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        float timer = 0f;
        float progress = 0f;

        while (!asyncLoad.isDone)
        {
            progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (timer < minLoadTime)
            {
                progress = Mathf.Clamp01(timer / minLoadTime);
            }

            progressFill.fillAmount = progress;
            progressText.text = $"{progress * 100:F0}";

            if (progress >= 0.9f && timer >= minLoadTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 场景加载完成后，如果需要加载存档，则应用存档数据
        if (SaveManager.shouldLoadFromSave && SaveManager.Instance != null)
        {
            // 等待一帧确保所有对象都已初始化
            yield return null;
            SaveManager.Instance.ApplySaveData();
            SaveManager.shouldLoadFromSave = false;
        }
    }
}