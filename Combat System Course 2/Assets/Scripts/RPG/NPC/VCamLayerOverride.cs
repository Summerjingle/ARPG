using UnityEngine;
using Cinemachine;

public class VCamLayerOverride : MonoBehaviour
{
    [Header("要隐藏的层（如 Player）")]
    public string layerToHide = "Player";

    private Camera mainCamera;
    private int layerMaskBackup;
    private int hideLayerMask;

    private CinemachineVirtualCamera vcam;
    private CinemachineBrain brain;
    private bool applied = false;

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        mainCamera = Camera.main;
        brain = FindObjectOfType<CinemachineBrain>();

        int layer = LayerMask.NameToLayer(layerToHide);
        hideLayerMask = ~(1 << layer);   // 用于隐藏某层
    }

    void OnEnable()
    {
        // 监听 Cinemachine 切换事件
        if (brain != null)
            brain.m_CameraActivatedEvent.AddListener(OnCameraActivated);
    }

    void OnDisable()
    {
        if (brain != null)
            brain.m_CameraActivatedEvent.RemoveListener(OnCameraActivated);

        // 防止 OnDisable 时 vcam 是激活状态 → 恢复
        if (applied)
            RestoreMask();
    }

    private void OnCameraActivated(ICinemachineCamera camA, ICinemachineCamera camB)
    {
        // camA = 当前激活的 vcam
        // camB = 上一个 vcam
        UnityEngine.Object camAObject = camA as UnityEngine.Object;
        UnityEngine.Object vcamObject = vcam as UnityEngine.Object;

        if (camAObject == vcamObject)
        {
            ApplyMask();
        }
        else if (applied)
        {
            RestoreMask();
        }
    }

    void ApplyMask()
    {
        if (mainCamera == null) return;
        if (applied) return;

        layerMaskBackup = mainCamera.cullingMask;
        mainCamera.cullingMask &= hideLayerMask;
        applied = true;
    }

    void RestoreMask()
    {
        if (mainCamera == null) return;

        mainCamera.cullingMask = layerMaskBackup;
        applied = false;
    }
}
