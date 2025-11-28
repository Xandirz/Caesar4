using UnityEngine;

public class ResearchTreePanner : MonoBehaviour
{
    [Header("Что двигаем (содержимое дерева)")]
    [SerializeField] private RectTransform content;       // NodesRoot или сам ResearchTree

    [Header("Окно панели (объект ResearchTree)")]
    [SerializeField] private RectTransform researchTreeRect;

    [Header("Кнопка мыши")]
    [SerializeField] private int mouseButton = 2;         // 2 = средняя кнопка

    private bool isPanning = false;
    private Vector2 lastMousePos;

    private void Update()
    {
        if (content == null || researchTreeRect == null)
            return;

        // Нажали среднюю кнопку — начинаем панорамирование только если мышь над ResearchTree
        if (Input.GetMouseButtonDown(mouseButton) &&
            RectTransformUtility.RectangleContainsScreenPoint(researchTreeRect, Input.mousePosition, null))
        {
            isPanning = true;
            lastMousePos = Input.mousePosition;
        }

        // Отпустили — заканчиваем
        if (Input.GetMouseButtonUp(mouseButton))
        {
            isPanning = false;
        }

        if (!isPanning)
            return;

        // Сдвиг мыши в Screen Space
        Vector2 mousePos = Input.mousePosition;
        Vector2 delta = mousePos - lastMousePos;
        lastMousePos = mousePos;

        // Можно инвертировать оси, если нужно ощущение «двигаю карту»
        content.anchoredPosition += delta;
        // или так, если хочется наоборот:
        // content.anchoredPosition -= delta;
    }
}