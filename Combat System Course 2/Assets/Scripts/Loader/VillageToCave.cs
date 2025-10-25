using UnityEngine;
using UnityEngine.SceneManagement;

public class VillageToCave : MonoBehaviour
{
    public string gameSceneName = "01Scene_Cave";
    public string loadingSceneName = "LoadingScene";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 保存游戏状态
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                SaveManager.isNewGame = false;
            }

            // 设置目标场景并加载
            PlayerPrefs.SetString("TargetScene", gameSceneName);
            SaveManager.shouldLoadFromSave = true;
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}