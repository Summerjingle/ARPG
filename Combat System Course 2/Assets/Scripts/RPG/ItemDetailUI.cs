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

    public Button useButton;

    private TextMeshProUGUI buttonText;
   


    private ItemSO itemSO;
    private ItemUI itemUI;
    private void Start()
    {
        propertyTempate.SetActive(false);
        this.gameObject.SetActive(false);
        buttonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
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
                buttonText.text="装备";
                break;
            case ItemType.Consumable:
                type = "消耗品";
                buttonText.text="使用";
                break;
            case ItemType.Armor:
                type = "防具";
                buttonText.text="装备";
                break;
            case ItemType.QuestRelated:
                type = "任务道具";
                buttonText.text="使用";
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
                    propertyName = "精力值+";
                    break;
                case PropertyType.AttackValue:
                    propertyName = "攻击力";
                    break;
                case PropertyType.DefenseValue:
                    propertyName = "护甲值";
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
        // �ȼ���Ƿ�Ϊ�������
        if (itemSO.itemType == ItemType.QuestRelated)
        {
            MessageUI.Instance.Show("任务道具无法被使用");
            this.gameObject.SetActive(false);
            return;
        }

        // ֻ�з�������߲�ִ��ʹ���߼�
        InventoryUI.Instance.OnItemUse(itemSO, itemUI);
        this.gameObject.SetActive(false);
    }
}
