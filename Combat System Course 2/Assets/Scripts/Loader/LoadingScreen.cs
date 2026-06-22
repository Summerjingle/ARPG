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

    private void Start()
    {
        Debug.Log($"<color=yellow>[TRACE] LoadingScreen.Start, frame={Time.frameCount}</color>");
        progressFill.fillAmount = 0f;
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        string targetScene;
        if (SaveManager.shouldLoadFromSave && SaveManager.Instance != null &&
            SaveManager.Instance.currentSaveData != null &&
            !string.IsNullOrEmpty(SaveManager.Instance.currentSaveData.currentScene))
        {
            targetScene = SaveManager.Instance.currentSaveData.currentScene;
        }
        else
        {
            targetScene = PlayerPrefs.GetString("TargetScene", "WhiteBox_Village");
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            progressFill.fillAmount = progress;
            progressText.text = $"{progress * 100:F0}";

            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        progressFill.fillAmount = 1f;
        progressText.text = "100";

        if (SaveManager.shouldLoadFromSave)
        {
            SaveManager.shouldLoadFromSave = false;
        }
    }
}