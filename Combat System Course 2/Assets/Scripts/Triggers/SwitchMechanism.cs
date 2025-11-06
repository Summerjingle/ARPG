using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchMechanism : MonoBehaviour
{
    [Header("机关标识")]
    [Tooltip("机关的唯一ID，必须手动设置！建议格式：场景名_机关名_位置")]
    public string mechanismId;

    [Header("存档设置")]
    [Tooltip("如果为true，机关激活状态会在当前存档中永久保存；如果为false，每次场景加载都会重置")]
    public bool persistAcrossSaves = true; 

    private string currentSceneName;
    private bool isActivated = false;

    private void Awake()
    {
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(mechanismId))
        {
            GenerateMechanismId();
        }
    }

    private void Start()
    {
        // 启动时检查存档状态
        CheckActivationState();
    }

    /// <summary>
    /// 检查机关是否已在存档中激活
    /// </summary>
    private void CheckActivationState()
    {
        if (!persistAcrossSaves) return;

        if (SaveManager.Instance == null || SaveManager.Instance.currentSaveData == null)
            return;

        if (SaveManager.Instance.currentSaveData.IsMechanismActivated(currentSceneName, mechanismId))
        {
            isActivated = true;
            Debug.Log($"机关 {mechanismId} 已从存档加载激活状态");
        }
    }

    /// <summary>
    /// 激活机关（由具体机关脚本调用）
    /// </summary>
    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;

        // 保存状态到存档
        if (persistAcrossSaves)
        {
            SaveActivationState();
        }

        Debug.Log($"机关 {mechanismId} 已激活并保存");
    }

    /// <summary>
    /// 查询机关是否已激活
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }

    /// <summary>
    /// 重置机关状态（用于调试）
    /// </summary>
    public void ResetMechanism()
    {
        isActivated = false;
        Debug.Log($"机关 {mechanismId} 已重置");
    }

    private void SaveActivationState()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null)
        {
            SaveManager.Instance.currentSaveData.MarkMechanismAsActivated(currentSceneName, mechanismId);
            SaveManager.Instance.SaveGame();
        }
    }

    [ContextMenu("生成唯一ID")]
    private void GenerateMechanismId()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 pos = transform.position;
        mechanismId = $"{sceneName}_Mech_{gameObject.name}_{pos.x:F0}_{pos.y:F0}_{pos.z:F0}";
        Debug.Log($"生成的机关ID: {mechanismId}");
    }
}