using UnityEngine;

public class StaticSceneItem : MonoBehaviour
{
    [Header("静态场景物品设置")]
    [Tooltip("物品唯一ID，必须手动设置！建议格式：场景名_物品名_位置")]
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

        // 检查存档中是否已拾取
        CheckIfAlreadyPicked();
    }

    private void CheckIfAlreadyPicked()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.currentSaveData == null)
            return;

        if (SaveManager.Instance.currentSaveData.IsSceneItemPicked(currentSceneName, itemId))
        {
            // 存档中已拾取，立即销毁物品
            Debug.Log($"物品 {itemId} 已在存档中拾取，自动销毁");
            Destroy(gameObject);
        }
    }

    public void PickUp()
    {
        if (pickableObject == null || pickableObject.itemSO == null)
            return;

        // 添加到背包
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(pickableObject.itemSO);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPickupToast(pickableObject.itemSO);
            }
            // 标记为已拾取
            if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null)
            {
                SaveManager.Instance.currentSaveData.MarkSceneItemAsPicked(currentSceneName, itemId);
                SaveManager.Instance.SaveGame();
            }

            Debug.Log($"拾取静态物品: {itemId}");
            Destroy(gameObject);
        }
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