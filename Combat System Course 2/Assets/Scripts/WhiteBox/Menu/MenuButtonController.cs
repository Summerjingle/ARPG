using UnityEngine;
using UnityEngine.InputSystem;

public class MenuButtonController : MonoBehaviour
{
    public int index;
    public int maxIndex;

    public AudioSource audioSource;
    private PlayerInputActions input => InputManager.Instance?.Actions;
    public bool inputLocked;

    [SerializeField] float deadZone = 0.5f;

   

    void OnEnable()
    {
        input.UI_MainMenu.Enable();
        input.UI_MainMenu.Navigate.performed += OnNavigate;
        input.UI_MainMenu.Navigate.canceled += OnNavigateCanceled;
    }

    void OnDisable()
    {
        input.UI_MainMenu.Navigate.performed -= OnNavigate;
        input.UI_MainMenu.Navigate.canceled -= OnNavigateCanceled;
        input.UI_MainMenu.Disable();
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (inputLocked) return;

        Vector2 dir = ctx.ReadValue<Vector2>();

        if (Mathf.Abs(dir.y) < deadZone) return;

        if (dir.y < 0)
        {
            index = (index < maxIndex) ? index + 1 : 0;
        }
        else if (dir.y > 0)
        {
            index = (index > 0) ? index - 1 : maxIndex;
        }

        audioSource?.Play();
        inputLocked = true;
    }

    void OnNavigateCanceled(InputAction.CallbackContext ctx)
    {
        inputLocked = false;
    }
}
