using System.Collections;
using UnityEngine;

public class BonfirePanelCtrl : MonoBehaviour
{
    public static BonfirePanelCtrl Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject bonfirePanel;
    [SerializeField] private Collider bonfireCollider;

    [Header("Menu")]
    [SerializeField] private MenuListController menuListController;
    [SerializeField] private int optionCount = 3;

    private Bonfire currentBonfire;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnBonfireExit += OnExitPressed;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnBonfireExit -= OnExitPressed;
    }

    private void Start()
    {
        if (bonfirePanel != null)
            bonfirePanel.SetActive(false);
    }

    public void Show(Bonfire bonfire)
    {
        currentBonfire = bonfire;
        bonfireCollider.enabled = false;

        if (menuListController != null)
        {
            menuListController.maxIndex = optionCount - 1;
            menuListController.index = 0;
        }

        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        InputManager.Instance?.SwitchToEmpty();
        yield return null;

        if (bonfirePanel != null)
            bonfirePanel.SetActive(true);

        InputManager.Instance?.SwitchToBonfireMenu();
    }

    /// <summary>
    /// 由 AnimatorFunctions.ExecuteBonfireOption() 通过 Animation Event 调用。
    /// </summary>
    public void HandleOption(int index)
    {
        switch (index)
        {
            case 0:
                Rest();
                break;
            case 1:
                Debug.Log("Bonfire option 1: reserved for future feature");
                break;
            case 2:
                StartCoroutine(CloseSequence());
                break;
        }
    }

    private void Rest()
    {
        Debug.Log("Bonfire: Rest");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<HealthSystem>();
            if (health != null)
                health.RestoreHealth(health.MaxHealth);

            var prop = player.GetComponent<PlayerProperty>();
            if (prop != null)
                prop.SetEnergy(prop.MaxEnergy);
        }
    }

    private void OnExitPressed()
    {
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        if (bonfirePanel != null)
        {
            // 离开前关闭所有按钮的 Selected，防止 Animator 残留状态
            foreach (var btn in bonfirePanel.GetComponentsInChildren<BonfireOptionButton>(includeInactive: true))
            {
                if (btn.Animator != null)
                    btn.Animator.SetBool("Selected", false);
            }
            bonfirePanel.SetActive(false);
        }

        InputManager.Instance?.SwitchToEmpty();
        yield return null;

        InputManager.Instance?.SwitchToPlayer();
        bonfireCollider.enabled = true;

        if (currentBonfire != null)
        {
            currentBonfire.OnPanelClosed();
            currentBonfire = null;
        }
    }
}
