using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 通用多选项菜单导航控制器。不依赖具体 InputActionMap，
/// 通过 Inspector 的 InputActionReference 拖入对应 Action 即可复用。
/// </summary>
public class MenuListController : MonoBehaviour
{
    public int index;
    [field: SerializeField] public int maxIndex { get; set; }

    [Header("Input")]
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private float deadZone = 0.5f;

    [Header("Feedback")]
    [SerializeField] private AudioSource audioSource;

    [Header("Events")]
    public UnityEvent<int> OnSelectionChanged;
    public UnityEvent<int> OnSubmit;
    public UnityEvent OnSubmitCanceled;

    public bool inputLocked { get; private set; }

    private InputAction Navigate => navigateAction?.action;
    private InputAction Submit => submitAction?.action;

    private void OnEnable()
    {
        if (Navigate != null)
        {
            Navigate.performed += OnNavigate;
            Navigate.canceled += OnNavigateCanceled;
        }

        if (Submit != null)
        {
            Submit.performed += OnSubmitPerformed;
            Submit.canceled += OnSubmitCanceledPerformed;
        }
    }

    private void OnDisable()
    {
        if (Navigate != null)
        {
            Navigate.performed -= OnNavigate;
            Navigate.canceled -= OnNavigateCanceled;
        }

        if (Submit != null)
        {
            Submit.performed -= OnSubmitPerformed;
            Submit.canceled -= OnSubmitCanceledPerformed;
        }
    }

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        AudioManager.RouteToUI(audioSource);
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (inputLocked) return;
 
        Vector2 dir = ctx.ReadValue<Vector2>();
        if (Mathf.Abs(dir.y) < deadZone) return;

        if (dir.y < 0)
            index = (index < maxIndex) ? index + 1 : 0;
        else if (dir.y > 0)
            index = (index > 0) ? index - 1 : maxIndex;

        if (audioSource != null)
            AudioManager.Instance.PlayUI(audioSource.clip);

        inputLocked = true;
        OnSelectionChanged?.Invoke(index);
    }

    private void OnNavigateCanceled(InputAction.CallbackContext ctx)
    {
        inputLocked = false;
    }

    private void OnSubmitPerformed(InputAction.CallbackContext ctx)
    {
        OnSubmit?.Invoke(index);
    }

    private void OnSubmitCanceledPerformed(InputAction.CallbackContext ctx)
    {
        OnSubmitCanceled?.Invoke();
    }
}
