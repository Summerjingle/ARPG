using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;
    public GameObject propertyGrid; 
    public GameObject propertyTempate;

    private ItemSO itemSO;
    private ItemUI itemUI;
    private void Start()
    {
        propertyTempate.SetActive(false);
        this.gameObject.SetActive(false);
    }
    public void UpdateDetailUI(ItemSO itemSO,ItemUI itemUI)
    {
        this.itemSO = itemSO;
        this.itemUI = itemUI;
        this.gameObject.SetActive(true);

        string type = "";
        switch (itemSO.itemType)
        {
            case ItemType.Weapon:
                type = "武器";
                break;
            case ItemType.Consumable:
                type = "可消耗品";
                break;
            case ItemType.Armor:
                type = "防具";
                break;
            
        }
        iconImage.sprite=itemSO.icon;
        nameText.text=itemSO.nameOfItem;
        typeText.text= type;
        descriptionText.text=itemSO.description;

        foreach (Transform child  in propertyGrid.transform)
        {
            if (child.gameObject.activeSelf)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Property property in itemSO.propertyList)
        {
            string propertyStr = "";
            string propertyName = "";
            switch (property.propertyType)
            {
                case PropertyType.HPValue:
                    propertyName = "生命值+";
                    break;
                case PropertyType.EnergyValue:
                    propertyName = "能量值+";
                    break;
                case PropertyType.AttackValue:
                    propertyName = "攻击力：";
                    break;
                case PropertyType.DefenseValue:
                    propertyName = "护甲值：";
                    break;
                default:
                    break;
            }
            propertyStr += propertyName;
            propertyStr += property.value;
            GameObject go= GameObject.Instantiate(propertyTempate);
            go.SetActive(true);
            go.transform.SetParent(propertyGrid.transform);
            go.transform.Find("Property").GetComponent<TextMeshProUGUI>().text = propertyStr;
        }
        
    }

    public void OnUseButtonClick()
    {
        InventoryUI.Instance.OnItemUse(itemSO, itemUI);
        this.gameObject.SetActive(false);
    }
}
