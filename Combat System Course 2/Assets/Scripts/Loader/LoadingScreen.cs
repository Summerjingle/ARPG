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
        // ȷ��Ŀ�곡��������ǴӴ浵�������г������ݣ���ʹ�ô浵����
        string targetScene;
        if (SaveManager.shouldLoadFromSave && SaveManager.Instance != null &&
            SaveManager.Instance.currentSaveData != null &&
            !string.IsNullOrEmpty(SaveManager.Instance.currentSaveData.currentScene))
        {
            targetScene = SaveManager.Instance.currentSaveData.currentScene;
            Debug.Log($"�Ӵ浵���س���: {targetScene}");
        }
        else
        {
            targetScene = PlayerPrefs.GetString("TargetScene", "WhiteBox_Village");
            Debug.Log($"��PlayerPrefs���س���: {targetScene}");
        }

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

        if (SaveManager.shouldLoadFromSave)
        {
            SaveManager.shouldLoadFromSave = false;
        }
    }
}