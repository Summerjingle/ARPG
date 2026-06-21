using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchMechanism : MonoBehaviour
{
    [Header("开关标识")]
    [Tooltip("唯一ID，格式：场景名_开关名_位置")]
    public string mechanismId;

    [Header("存档设置")]
    [Tooltip("true=永久保存，false=每次重置")]
    public bool persistAcrossSaves = true;

    [Header("读档恢复")]
    [Tooltip("读档时直接跳到该 State 的最后一帧，空字符串=不处理")]
    public string restoreStateName = "Open";

    private string currentSceneName;
    private bool isActivated = false;

    private void Awake()
    {
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(mechanismId))
        {
            GenerateMechanismId();
        }

        // 尽早在 Awake 恢复，确保其他脚本 Start() 时 IsActivated() 已正确
        CheckActivationState();
    }

    // 检查存档中是否已激活
    private void CheckActivationState()
    {
        if (!persistAcrossSaves) return;

        if (SaveManager.Instance == null || SaveManager.Instance.currentSaveData == null)
            return;

        if (SaveManager.Instance.currentSaveData.IsMechanismActivated(currentSceneName, mechanismId))
        {
            isActivated = true;
            Debug.Log($"开关 {mechanismId} 已从存档恢复");

            // 自动恢复 Animator 状态（机关门等），直接跳到最后一帧
            if (!string.IsNullOrEmpty(restoreStateName))
            {
                var anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.Play(restoreStateName, 0, 1f);
                    anim.Update(0);
                }
            }
        }
    }

    // 激活开关
    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;

        if (persistAcrossSaves)
        {
            SaveActivationState();
        }

        Debug.Log($"开关 {mechanismId} 已激活");
    }

    // 查询是否已激活
    public bool IsActivated()
    {
        return isActivated;
    }

    // 重置开关（调试用）
    public void ResetMechanism()
    {
        isActivated = false;
        Debug.Log($"开关 {mechanismId} 已重置");
    }

    private void SaveActivationState()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null)
        {
            SaveManager.Instance.currentSaveData.MarkMechanismAsActivated(currentSceneName, mechanismId);
            SaveManager.Instance.SaveGame(updatePosition: false);
        }
    }

    [ContextMenu("生成唯一ID")]
    private void GenerateMechanismId()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 pos = transform.position;
        mechanismId = $"{sceneName}_Mech_{gameObject.name}_{pos.x:F0}_{pos.y:F0}_{pos.z:F0}";
        Debug.Log($"生成的开关ID: {mechanismId}");
    }
}