using UnityEngine;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private MenuListController menuListController;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimatorFunctions animatorFunctions;
    [SerializeField] private int thisIndex;

    private void OnEnable()
    {
        if (menuListController != null)
        {
            menuListController.OnSubmit.AddListener(OnSubmit);
            menuListController.OnSubmitCanceled.AddListener(OnSubmitCanceledHandler);
        }
    }

    private void OnDisable()
    {
        if (menuListController != null)
        {
            menuListController.OnSubmit.RemoveListener(OnSubmit);
            menuListController.OnSubmitCanceled.RemoveListener(OnSubmitCanceledHandler);
        }
    }

    private void Update()
    {
        if (menuListController != null && animator != null)
        {
            animator.SetBool("selected", menuListController.index == thisIndex);
        }
    }

    private void OnSubmit(int index)
    {
        if (index != thisIndex) return;

        if (animator != null)
            animator.SetBool("pressed", true);
    }

    private void OnSubmitCanceledHandler()
    {
        if (animator == null) return;
        if (!animator.GetBool("pressed")) return;

        animator.SetBool("pressed", false);

        if (animatorFunctions != null)
            animatorFunctions.disableOnce = true;
    }
}
