using UnityEngine;

public class HeadCollisionChecker : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer; // 阻挡站立的层
    public BoxCollider headCollider;

    // 每帧检测头顶障碍
    private bool headBlocked = false;

    private void Update()
    {
        if (headCollider == null) return;

        // 使用 Physics.CheckBox 检测障碍
        headBlocked = Physics.CheckBox(
            headCollider.bounds.center,
            headCollider.bounds.extents,
            Quaternion.identity,
            obstacleLayer
        );
    }

    // 外部调用判断能否站立
    public bool CanStandUpFromCrouch()
    {
        return !headBlocked;
    }

  
    public void EnableHeadCheck()
    {
        if (headCollider != null)
            headCollider.enabled = true;
    }
    public void DisableHeadCheck()
    {
        if (headCollider != null)
            headCollider.enabled = false;
    }
}
