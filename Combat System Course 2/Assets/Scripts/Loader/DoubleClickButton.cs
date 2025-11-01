using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DoubleClickButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private float doubleClickTime = 0.3f;
    public ArmorType armorType; // 需要指定这是哪个部位的装备按钮
    public bool isWeaponButton = false; // 新增：标记是否是武器按钮

    private int clickCount = 0;
    private float lastClickTime = 0f;
    private ArmorEquipmentManager armorEquipmentManager;

    private void Awake()
    {
        button.onClick.AddListener(OnClick);
        if (!isWeaponButton) // 只有防具按钮需要ArmorEquipmentManager
        {
            armorEquipmentManager = FindObjectOfType<ArmorEquipmentManager>();
        }
    }

    private void OnClick()
    {
        float currentTime = Time.time;

        if (currentTime - lastClickTime < doubleClickTime)
        {
            clickCount++;
            if (clickCount == 2)
            {
                Debug.Log($"双击触发卸载 {(isWeaponButton ? "武器" : armorType.ToString())}");
                if (isWeaponButton)
                {
                    UnequipWeapon();
                }
                else
                {
                    UnequipArmor();
                }
                clickCount = 0;
            }
        }
        else
        {
            clickCount = 1;
        }

        lastClickTime = currentTime;
    }

    private void UnequipArmor()
    {
        if (ArmorEquipmentManager.Instance != null)
        {
            var socket = ArmorEquipmentManager.Instance.GetSocketByType(armorType);
            if (socket != null)
            {
                ArmorEquipmentManager.Instance.UnequipArmor(socket);
            }
            else
            {
                Debug.LogError($"找不到 {armorType} 对应的装备槽");
            }
        }
        else
        {
            Debug.LogError("ArmorEquipmentManager 未找到");
        }
    }

    private void UnequipWeapon()
    {
        ItemUsageHandler.Instance.UnequipWeapon();
    }
}