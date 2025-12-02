using UnityEngine;

public class ResearchTreePanner : MonoBehaviour
{
    [Header("Что двигаем (содержимое дерева)")]
    [SerializeField] private RectTransform content;       // NodesRoot или сам ResearchTree

    [Header("Окно панели (объект ResearchTree)")]
    [SerializeField] private RectTransform researchTreeRect;

    [Header("Панорамирование")]
    [SerializeField] private int mouseButton = 2;         // 2 = средняя кнопка

    [Header("Зум дерева")]
    [SerializeField] private float zoomSpeed = 0.2f;      // чувствительность зума
    [SerializeField] private float minZoom = 0.5f;        // минимальный масштаб
    [SerializeField] private float maxZoom = 2.0f;        // максимальный масштаб

    private bool isPanning = false;
    private Vector2 lastMousePos;

    private void Update()
    {
        if (content == null || researchTreeRect == null)
            return;

        bool mouseOverTree = RectTransformUtility.RectangleContainsScreenPoint(
            researchTreeRect,
            Input.mousePosition,
            null
        );

        // ===== ЗУМ ДЕРЕВА КОЛЁСИКОМ, если мышка над ResearchTree =====
        if (mouseOverTree)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // текущий масштаб (X и Y должны быть одинаковые)
                float current = content.localScale.x;
                float target = current + scroll * zoomSpeed;
                target = Mathf.Clamp(target, minZoom, maxZoom);

                content.localScale = new Vector3(target, target, 1f);
            }
        }

        // ===== ПАНОРАМИРОВАНИЕ ПРИ ЗАЖАТОЙ СРЕДНЕЙ КНОПКЕ, если мышка над ResearchTree =====

        // Нажали среднюю кнопку — начинаем панорамирование только если мышь над ResearchTree
        if (Input.GetMouseButtonDown(mouseButton) && mouseOverTree)
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
