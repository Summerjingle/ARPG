using UnityEngine;
using UnityEngine.InputSystem;

public class MenuButton : MonoBehaviour
{
    [SerializeField] MenuButtonController menuButtonController;
    [SerializeField] Animator animator;
    [SerializeField] AnimatorFunctions animatorFunctions;
    [SerializeField] int thisIndex;

    private PlayerInputActions input;

    

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.UI.Enable();
        input.UI.Submit.performed += OnSubmit;
        input.UI.Submit.canceled += OnSubmitCanceled;
    }

    void OnDisable()
    {
        input.UI.Submit.performed -= OnSubmit;
        input.UI.Submit.canceled -= OnSubmitCanceled;
        input.UI.Disable();
    }

    void Update()
    {
        animator.SetBool(
            "selected",
            menuButtonController.index == thisIndex
        );
    }

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (menuButtonController.index != thisIndex) return;

        animator.SetBool("pressed", true);
    }

    void OnSubmitCanceled(InputAction.CallbackContext ctx)
    {
        if (!animator.GetBool("pressed")) return;

        animator.SetBool("pressed", false);
        animatorFunctions.disableOnce = true;
    }
}
