using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimatorFunctions : MonoBehaviour
{
    [Header("����")]
    [SerializeField] private ArchiveManager archiveManager;
    [SerializeField] private AudioSource audioSource;

    void Start()
    {
        AudioManager.RouteToUI(audioSource);
    }


    [Header("��������")]
    [SerializeField] private string gameSceneName = "WhiteBox_Village";
    [SerializeField] private string loadingSceneName = "LoadingScene";

    public bool disableOnce;
   
    

    // ��Ч
    public void PlaySound(AudioClip clip)
    {
        if (!disableOnce && clip != null)
            AudioManager.Instance.PlayUI(clip);
        else
            disableOnce = false;
    }

    [SerializeField] MenuListController menuListController;

 

    public void ExecuteButton()
    {
        Debug.Log("ִ�а�ť");
        Debug.Log("ExecuteButton called on " + gameObject.name + ", index = " + menuListController.index);
        int index = menuListController.index;

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
        Debug.Log($"<color=red>[TRACE] StartNewGame called, frame={Time.frameCount}</color>");
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

    public void ExecuteBonfireOption()
    {
        int index = menuListController.index;
        Debug.Log("ExecuteBonfireOption, index = " + index);

        if (BonfirePanelCtrl.Instance != null)
            BonfirePanelCtrl.Instance.HandleOption(index);
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