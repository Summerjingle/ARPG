using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public ItemSO itemSO;
    public GameObject highlightObject;
    public GameObject quickLightObject;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnClick);
        }
    }

    public void InitItem(ItemSO itemSO)
    {
        iconImage.sprite = itemSO.icon;
        this.itemSO = itemSO;

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
        UpdateQuickLight();
    }

    public void UpdateQuickLight()
    {
        if (quickLightObject != null && itemSO != null)
            quickLightObject.SetActive(QuickItemBar.Instance?.HasItem(itemSO) ?? false);
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

    private void OnClick()
    {
        Debug.Log($"[DEBUG] OnClick触发, itemSO={itemSO?.nameOfItem ?? "NULL"}");
        InventoryUI.Instance.OnItemClick(itemSO, this);
    }
}