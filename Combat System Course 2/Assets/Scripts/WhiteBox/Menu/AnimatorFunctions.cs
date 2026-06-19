using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimatorFunctions : MonoBehaviour
{
    [Header("����")]
    [SerializeField] private ArchiveManager archiveManager;
    [SerializeField] private AudioSource audioSource;


    [Header("��������")]
    [SerializeField] private string gameSceneName = "WhiteBox_Village";
    [SerializeField] private string loadingSceneName = "LoadingScene";

    public bool disableOnce;
   
    

    // ��Ч
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
        Debug.Log("ִ�а�ť");
        Debug.Log("ExecuteButton called on " + gameObject.name + ", index = " + menuButtonController.index);
        int index = menuButtonController.index;

        switch (index)
        {
            case 0:
                StartNewGame();
                Debug.Log("��ʼ����Ϸ");
                break;

            case 1:
                OpenLoadPanel();
                Debug.Log("�򿪴浵���");
                break;

            case 2:
                QuitGame();
                Debug.Log("�˳���Ϸ");
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
        Debug.Log("�򿪴浵���");
 
        FindObjectOfType<ArchiveManager>().ShowPanel();
    }

    public void Die() => Destroy(gameObject);

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}