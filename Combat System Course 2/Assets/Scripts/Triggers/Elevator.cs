using UnityEngine;

/// <summary>
/// [已废弃] OnElevatorEnd 动画事件转发已不再需要。
/// ElevatorController 现在通过协程 + Rigidbody.MovePosition 驱动平台位移，
/// 移动完成后直接调用 ElevatorFinished()，不再依赖此脚本的动画事件转发。
/// 请在场景中从 Elevator GameObject 上移除此组件。
/// </summary>
public class Elevator : MonoBehaviour
{
    // 保留脚本文件避免场景引用丢失，但功能已废弃。
    // 移除 Elevator GameObject 上的 Animator 和本组件后即可安全删除此文件。
}
