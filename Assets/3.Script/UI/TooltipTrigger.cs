using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData item;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null && item != null)
        {
            TooltipUI.Instance.Show(item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
        {
            TooltipUI.Instance.Hide();
        }
    }
}
