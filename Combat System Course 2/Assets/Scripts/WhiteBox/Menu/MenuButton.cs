using UnityEngine;
using UnityEngine.InputSystem;

public class MenuButton : MonoBehaviour
{
    [SerializeField] MenuButtonController menuButtonController;
    [SerializeField] Animator animator;
    [SerializeField] AnimatorFunctions animatorFunctions;
    [SerializeField] int thisIndex;



    private PlayerInputActions input => InputManager.Instance?.Actions;

    
    void OnEnable()
    {
        input.UI_MainMenu.Enable();
        input.UI_MainMenu.Submit.performed += OnSubmit;
        input.UI_MainMenu.Submit.canceled += OnSubmitCanceled;
    }

    void OnDisable()
    {
        input.UI_MainMenu.Submit.performed -= OnSubmit;
        input.UI_MainMenu.Submit.canceled -= OnSubmitCanceled;
        input.UI_MainMenu.Disable();
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
