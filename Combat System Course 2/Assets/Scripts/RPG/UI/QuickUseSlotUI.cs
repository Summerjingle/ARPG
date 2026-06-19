using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuickUseSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public Image iconImage;
    public GameObject highlight;
    public int slotIndex;

    public System.Action<QuickUseSlotUI> onClick;

    public void OnSelect(BaseEventData eventData)
    {
        if (highlight != null)
            highlight.SetActive(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (highlight != null)
            highlight.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(this);
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }
}
