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

    [Header("Ограничение зоны панорамирования")]
    [Tooltip("Базовый предел смещения от стартовой позиции (даже на минимальном зуме)")]
    [SerializeField] private float basePanLimit = 500f;

    [Tooltip("Дополнительный предел смещения на максимальном зуме")]
    [SerializeField] private float extraPanLimitAtMaxZoom = 1500f;

    // текущий зум (для информации / отладки)
    public float CurrentZoom { get; private set; } = 1f;

    private bool isPanning = false;
    private Vector2 lastMousePos;

    // стартовая позиция дерева (anchoredPosition)
    private Vector2 startAnchoredPos;

    private void Awake()
    {
        if (content != null)
        {
            // если в инспекторе уже задан scale — подхватываем
            CurrentZoom = content.localScale.x;

            // запоминаем стартовую позицию содержимого
            startAnchoredPos = content.anchoredPosition;
        }
    }

    private void Update()
    {
        if (content == null || researchTreeRect == null)
            return;

        // работаем ТОЛЬКО когда панель дерева исследований открыта
        if (!researchTreeRect.gameObject.activeInHierarchy)
        {
            isPanning = false;
            return;
        }

        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        float current = content.localScale.x;
        float target = current + scroll * zoomSpeed;
        target = Mathf.Clamp(target, minZoom, maxZoom);

        content.localScale = new Vector3(target, target, 1f);
        CurrentZoom = target;

        // ⚡ после изменения зума обновляем толщину линий
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.RefreshLineThickness(CurrentZoom);
        }

        // после смены зума поджимем под новую зону
        ClampContentToZone();
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(mouseButton))
        {
            isPanning = true;
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(mouseButton))
        {
            isPanning = false;
        }

        if (!isPanning)
            return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 delta = mousePos - lastMousePos;
        lastMousePos = mousePos;

        // ощущение «тяну карту»
        content.anchoredPosition += delta;
        // если хочешь наоборот — меняешь на "-="

        ClampContentToZone();
    }

    /// <summary>
    /// Ограничиваем позицию content в прямоугольной зоне вокруг стартовой позиции.
    /// Зона расширяется при увеличении зума и сужается при уменьшении.
    /// </summary>
    private void ClampContentToZone()
    {
        // нормализованный зум 0..1
        float t = 0f;
        if (Mathf.Abs(maxZoom - minZoom) > 0.0001f)
        {
            t = (CurrentZoom - minZoom) / (maxZoom - minZoom);
            t = Mathf.Clamp01(t);
        }

        // текущий лимит смещения от стартовой точки
        float limit = basePanLimit + extraPanLimitAtMaxZoom * t;

        Vector2 pos = content.anchoredPosition;

        pos.x = Mathf.Clamp(pos.x, startAnchoredPos.x - limit, startAnchoredPos.x + limit);
        pos.y = Mathf.Clamp(pos.y, startAnchoredPos.y - limit, startAnchoredPos.y + limit);

        content.anchoredPosition = pos;
    }
}
