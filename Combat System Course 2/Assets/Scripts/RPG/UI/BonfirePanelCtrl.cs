using System.Collections;
using UnityEngine;

public class BonfirePanelCtrl : MonoBehaviour
{
    public static BonfirePanelCtrl Instance { get; private set; }

    [SerializeField] private GameObject bonfirePanel;
    [SerializeField] private Collider bonfireCollider;

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

    private void OnExitPressed()
    {
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        if (bonfirePanel != null)
            bonfirePanel.SetActive(false);

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
