using UnityEngine;

public class BonfireOptionButton : MonoBehaviour
{
    [SerializeField] private MenuListController menuListController;
    [SerializeField] private Animator animator;
    [SerializeField] private int thisIndex;

    public Animator Animator => animator;

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
        if (menuListController == null)
        {
            Debug.LogWarning($"[BonfireOption {thisIndex}] menuListController is NULL");
            return;
        }

        if (animator != null)
        {
            bool shouldSelect = menuListController.index == thisIndex;
            animator.SetBool("Selected", shouldSelect);

            // 只在异常时打印：animator 状态跟代码预期不一致
            if (animator.GetBool("Selected") != shouldSelect)
                Debug.LogWarning($"[BonfireOption {thisIndex}] MISMATCH! ctrl.index={menuListController.index} shouldSelect={shouldSelect} anim.Selected={animator.GetBool("Selected")}");
        }
        else
        {
            Debug.LogWarning($"[BonfireOption {thisIndex}] animator is NULL");
        }
    }

    private void OnSubmit(int index)
    {
        if (index != thisIndex) return;

        if (animator != null)
            animator.SetBool("Pressed", true);
    }

    private void OnSubmitCanceledHandler()
    {
        if (animator == null) return;
        if (!animator.GetBool("Pressed")) return;

        animator.SetBool("Pressed", false);
    }
}
