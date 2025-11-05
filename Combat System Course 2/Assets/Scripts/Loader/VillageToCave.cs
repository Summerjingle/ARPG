using UnityEngine;
using UnityEngine.SceneManagement;

public class VillageToCave : MonoBehaviour
{
    public string gameSceneName = "01Scene_Cave";
    public string loadingSceneName = "LoadingScene";
    private bool hasTriggered=false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
            if (SaveManager.Instance != null)
            {
                if (hasTriggered) return;
                hasTriggered = true;

                SaveManager.Instance.SaveGame();
                SaveManager.Instance.currentSaveData.currentScene = gameSceneName;
                SaveManager.isNewGame = false;
            }

            // 设置目标场景并加载
            PlayerPrefs.SetString("TargetScene", gameSceneName);
            SaveManager.shouldLoadFromSave = true;
            SaveManager.shouldLoadPosition = false; 
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}