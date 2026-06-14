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

        float timer = 0f;

        while (!asyncLoad.isDone)
        {
            float rawProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (timer < minLoadTime)
            {
                rawProgress = Mathf.Clamp01(timer / minLoadTime);
            }

            float displayProgress = asyncLoad.allowSceneActivation ? rawProgress : Mathf.Min(rawProgress, 0.9f);

            progressFill.fillAmount = displayProgress;
            progressText.text = $"{displayProgress * 100:F0}";

            if (rawProgress >= 0.9f && timer >= minLoadTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        float fillSpeed = 1f / 2f;
        float f = progressFill.fillAmount;
        while (f < 1f)
        {
            f = Mathf.MoveTowards(f, 1f, fillSpeed * Time.deltaTime);
            progressFill.fillAmount = f;
            progressText.text = $"{f * 100:F0}";
            yield return null;
        }

        if (SaveManager.shouldLoadFromSave)
        {
            SaveManager.shouldLoadFromSave = false;
        }
    }
}