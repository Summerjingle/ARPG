using UnityEngine;

public class StaticSceneItem : MonoBehaviour
{
    [Header("静态场景物品配置")]
    [Tooltip("物品唯一ID / 建议格式：场景名_物品名_位置")]
    public string itemId;

    private PickableObject pickableObject;
    private string currentSceneName;

    void Start()
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"静态物品 {gameObject.name} 没有设置itemId！", this);
            return;
        }

        pickableObject = GetComponent<PickableObject>();
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (pickableObject != null)
            pickableObject.onInteract += OnPickUp;

        CheckIfAlreadyPicked();
    }

    private void CheckIfAlreadyPicked()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.currentSaveData == null)
            return;

        if (SaveManager.Instance.currentSaveData.IsSceneItemPicked(currentSceneName, itemId))
        {
            Debug.Log($"物品 {itemId} 已在存档中被拾取，自动移除");
            Destroy(gameObject);
        }
    }

    private void OnPickUp()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null)
        {
            SaveManager.Instance.currentSaveData.MarkSceneItemAsPicked(currentSceneName, itemId);
            SaveManager.Instance.SaveGame();
        }

        Debug.Log($"拾取静态物品: {itemId}");
    }

    [ContextMenu("生成唯一ID")]
    private void GenerateUniqueId()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 pos = transform.position;
        itemId = $"{sceneName}_{gameObject.name}_{pos.x:F0}_{pos.y:F0}_{pos.z:F0}";
        Debug.Log($"生成的ID: {itemId}");
    }
}
