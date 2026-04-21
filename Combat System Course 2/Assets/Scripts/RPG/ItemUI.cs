using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI amountText; 
    public ItemSO itemSO;
    public GameObject highlightObject;

    public void InitItem(ItemSO itemSO)
    {
        iconImage.sprite = itemSO.icon;
        this.itemSO = itemSO;

        // ��ʾ����������Ƕѵ���Ʒ��
        if (itemSO.IsStackable() && itemSO.amount > 1)
        {
            amountText.text = itemSO.amount.ToString();
            amountText.gameObject.SetActive(true);
        }
        else
        {
            amountText.gameObject.SetActive(false);
        }
        SetHighlight(false);
    }

    

    // ����������ʾ
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
      public void SetHighlight(bool active)
    {
        if (highlightObject != null)
            highlightObject.SetActive(active);
    }
}