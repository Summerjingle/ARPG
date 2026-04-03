using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimatorFunctions : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private ArchiveManager archiveManager;
    [SerializeField] private AudioSource audioSource;


    [Header("场景设置")]
    [SerializeField] private string gameSceneName = "00Scene_Village";
    [SerializeField] private string loadingSceneName = "LoadingScene";

    public bool disableOnce;
   
    

    // 音效
    public void PlaySound(AudioClip clip)
    {
        if (!disableOnce && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            disableOnce = false;
        }
    }

    [SerializeField] MenuButtonController menuButtonController;

 

    public void ExecuteButton()
    {
        Debug.Log("执行按钮");
        Debug.Log("ExecuteButton called on " + gameObject.name + ", index = " + menuButtonController.index);
        int index = menuButtonController.index;

        switch (index)
        {
            case 0:
                StartNewGame();
                Debug.Log("开始新游戏");
                break;

            case 1:
                OpenLoadPanel();
                Debug.Log("打开存档面板");
                break;

            case 2:
                QuitGame();
                Debug.Log("退出游戏");
                break;
        }
    }

    void StartNewGame()
    {
        SaveManager.Instance.CreateNewGame(-1);

        PlayerPrefs.SetString("TargetScene", gameSceneName);
        SceneManager.LoadScene(loadingSceneName);
    }

    void OpenLoadPanel()
    {
        Debug.Log("打开存档面板");
 
        FindObjectOfType<ArchiveManager>().ShowPanel();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}