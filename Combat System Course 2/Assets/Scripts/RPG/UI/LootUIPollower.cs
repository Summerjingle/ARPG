using UnityEngine;
using TMPro;
public class LootUIPollower : MonoBehaviour
{
     public RectTransform uiElement; // UI
    public Transform target;        // SoulLoot
    public Vector3 offset = new Vector3(0, 2f, 0);
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI actionText;

    private Camera cam;
    void Awake()
    {
        SetTarget(null, null); // 初始状态传 null
    }
    void Start()
    {
        cam = Camera.main;
    }
    public void SetTarget(Transform newTarget,ItemSO item)
    {
        target = newTarget;
        if (uiElement != null) 
            uiElement.gameObject.SetActive(newTarget != null);
        if (newTarget == null) return;
        if (itemNameText != null)
        {
            if (item != null)
            {
                itemNameText.text = item.nameOfItem; // 改为 SO 里的名字
                itemNameText.gameObject.SetActive(true); // 启用组件
            }
            else
            {
                itemNameText.gameObject.SetActive(false); // 没拿到就禁用组件
            }
        }
        if (actionText != null)
        {
            
            actionText.text = (item != null) ? "拾取" : "交互";
        }
    }
    void LateUpdate()
    {
        if (cam == null || uiElement == null || target == null || !uiElement.gameObject.activeSelf) return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position + offset);

        if (screenPos.z < 0)
        {
            uiElement.localScale = Vector3.zero; // 避免闪烁，隐藏时缩放归零
            return;
        }

        uiElement.localScale = Vector3.one;
        uiElement.position = screenPos;
    }
}
