using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MenuSceneController : MonoBehaviour
{
    public Animator playerAnimator;
    public Transform portalTransform;
    public Button playButton;
    public Button loadButton;
    public TextMeshProUGUI Heading;
    public Button SettingButton;
    public Button QuitButton;
    public string gameSceneName = "00Scene_Village";
    public string loadingSceneName = "LoadingScene";

    private bool isStartingGame = false;
    private bool hasReachedPortal = false;

    void Start()
    {
        playButton.onClick.AddListener(StartNewGame);
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadGame);
        }
        playerAnimator.SetBool("IsSitting", true);

        // 确保60FPS运行
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Update()
    {
        if (isStartingGame && !hasReachedPortal)
        {
            RotateTowardsPortal();
        }
    }

    void RotateTowardsPortal()
    {
        Vector3 direction = (portalTransform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 3f
            );
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Portal") && isStartingGame)
        {
            EnterPortal();
        }
    }

    public void StartNewGame()
    {
        if (!isStartingGame)
        {
            // 设置新游戏标志
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.StartNewGame();
            }

            isStartingGame = true;
            HideAllUI();
            StandUp();
        }
    }

    public void LoadGame()
    {
        if (!isStartingGame)
        {
            // 设置加载游戏标志
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
                SaveManager.shouldLoadFromSave = true;
            }

            isStartingGame = true;
            HideAllUI();
            StandUp();
        }
    }

    void StandUp()
    {
        playerAnimator.SetBool("IsSitting", false);
        playerAnimator.SetTrigger("IsStandingUp");
        StartCoroutine(StartWalkingAfterStandUp());
    }

    IEnumerator StartWalkingAfterStandUp()
    {
        yield return new WaitForSeconds(0.01f);
        playerAnimator.SetBool("IsWalking", true);
    }

    void EnterPortal()
    {
        playerAnimator.SetBool("IsWalking", false);

        // 保存游戏状态
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        // 设置目标场景并加载
        PlayerPrefs.SetString("TargetScene", gameSceneName);
        SceneManager.LoadScene(loadingSceneName);
    }

    private void HideAllUI()
    {
        playButton.gameObject.SetActive(false);
        if (loadButton != null) loadButton.gameObject.SetActive(false);
        SettingButton.gameObject.SetActive(false);
        QuitButton.gameObject.SetActive(false);
        Heading.gameObject.SetActive(false);
    }
}