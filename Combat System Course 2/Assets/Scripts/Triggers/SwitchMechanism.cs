using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchMechanism : MonoBehaviour
{
    [Header("���ر�ʶ")]
    [Tooltip("���ص�ΨһID�������ֶ����ã������ʽ��������_������_λ��")]
    public string mechanismId;

    [Header("�浵����")]
    [Tooltip("���Ϊtrue�����ؼ���״̬���ڵ�ǰ�浵�����ñ��棻���Ϊfalse��ÿ�γ������ض�������")]
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
        // ����ʱ���浵״̬
        CheckActivationState();
    }

    /// <summary>
    /// �������Ƿ����ڴ浵�м���
    /// </summary>
    private void CheckActivationState()
    {
        if (!persistAcrossSaves) return;

        if (SaveManager.Instance == null || SaveManager.Instance.currentSaveData == null)
            return;

        if (SaveManager.Instance.currentSaveData.IsMechanismActivated(currentSceneName, mechanismId))
        {
            isActivated = true;
            Debug.Log($"���� {mechanismId} �ѴӴ浵���ؼ���״̬");
        }
    }

    /// <summary>
    /// ������أ��ɾ�����ؽű����ã�
    /// </summary>
    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;

        // ����״̬���浵
        if (persistAcrossSaves)
        {
            SaveActivationState();
        }

        Debug.Log($"���� {mechanismId} �Ѽ������");
    }

    /// <summary>
    /// ��ѯ�����Ƿ��Ѽ���
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }

    /// <summary>
    /// ���û���״̬�����ڵ��ԣ�
    /// </summary>
    public void ResetMechanism()
    {
        isActivated = false;
        Debug.Log($"���� {mechanismId} ������");
    }

    private void SaveActivationState()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null)
        {
            SaveManager.Instance.currentSaveData.MarkMechanismAsActivated(currentSceneName, mechanismId);
            SaveManager.Instance.SaveGame(updatePosition: false);
        }
    }

    [ContextMenu("����ΨһID")]
    private void GenerateMechanismId()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 pos = transform.position;
        mechanismId = $"{sceneName}_Mech_{gameObject.name}_{pos.x:F0}_{pos.y:F0}_{pos.z:F0}";
        Debug.Log($"���ɵĻ���ID: {mechanismId}");
    }
}