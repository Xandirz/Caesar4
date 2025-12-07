using UnityEngine;
using UnityEngine.EventSystems;

public class BuildButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string tooltipText;   // текст, который мы передаём из BuildUIManager

    private bool isHovered = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (TooltipUI.Instance != null)
        {
            // позиция кнопки на экране
            Vector3 pos = transform.position;

            // показываем сразу
            TooltipUI.Instance.Show(tooltipText, pos);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (TooltipUI.Instance != null)
        {
            TooltipUI.Instance.Hide();
        }
    }
}