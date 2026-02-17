using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions Actions { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Actions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        Actions.Enable();
    }

    private void OnDisable()
    {
        Actions.Disable();
    }

    public void SwitchToUI()
    {
        Actions.Player.Disable();
        Actions.UI.Enable();
        UIStateManager.SetUIActive(true); // œ‘ æ Û±Í
    }

    public void SwitchToPlayer()
    {
        Actions.UI.Disable();
        Actions.Player.Enable();
        UIStateManager.SetUIActive(false); // “˛≤ÿ Û±Í
    }
}
