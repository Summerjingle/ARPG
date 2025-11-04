using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI amountText; 
    public ItemSO itemSO;

    public void InitItem(ItemSO itemSO)
    {
        iconImage.sprite = itemSO.icon;
        this.itemSO = itemSO;

        // 显示数量（如果是堆叠物品）
        if (itemSO.IsStackable() && itemSO.amount > 1)
        {
            amountText.text = itemSO.amount.ToString();
            amountText.gameObject.SetActive(true);
        }
        else
        {
            amountText.gameObject.SetActive(false);
        }
    }

    public void OnClick()
    {
        InventoryUI.Instance.OnItemClick(itemSO, this);
    }

    // 更新数量显示
    public void UpdateAmountDisplay()
    {
        if (itemSO.IsStackable() && itemSO.amount > 1)
        {
            amountText.text = itemSO.amount.ToString();
            amountText.gameObject.SetActive(true);
        }
        else if (itemSO.amount <= 1)
        {
            amountText.gameObject.SetActive(false);
        }
    }
}