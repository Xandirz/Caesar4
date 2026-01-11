using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialWindow : MonoBehaviour,
    IPointerDownHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Window Links")]
    [SerializeField] private RectTransform window;
    [SerializeField] private RectTransform[] dragAreas; // Header + Content
    [SerializeField] private GameObject content;
    [SerializeField] private bool keepInsideParent = true;

    [Header("Tutorial Lines UI")]
    [SerializeField] private Transform linesContainer;         // куда инстансить строки (внутри Content)
    [SerializeField] private TutorialLineUI tutorialLinePrefab; // префаб строки

    private RectTransform parentRect;
    private Camera eventCamera;
    private Vector2 pointerOffset;
    private bool dragging;

    // Прогресс
    private bool step1, step2, step3, step4, step5;
    private int housesCount;

    // Ссылки на созданные строки
    private TutorialLineUI line1, line2, line3, line4, line5;

    private void Awake()
    {
        if (window == null)
            window = transform as RectTransform;

        parentRect = window.parent as RectTransform;
        if (parentRect == null)
            Debug.LogError("[TutorialWindow] Window must have a RectTransform parent.");
    }

    private void OnEnable()
    {
        TutorialEvents.RoadConnectedToObelisk += OnRoadConnected;
        TutorialEvents.HousePlaced += OnHousePlaced;
        TutorialEvents.LumberMillPlaced += OnLumberMillPlaced;
        TutorialEvents.BerryPlaced += OnBerryPlaced;
        TutorialEvents.ResearchCompleted += OnResearchCompleted;
    }

    private void OnDisable()
    {
        TutorialEvents.RoadConnectedToObelisk -= OnRoadConnected;
        TutorialEvents.HousePlaced -= OnHousePlaced;
        TutorialEvents.LumberMillPlaced -= OnLumberMillPlaced;
        TutorialEvents.BerryPlaced -= OnBerryPlaced;
        TutorialEvents.ResearchCompleted -= OnResearchCompleted;
    }

    private void Start()
    {
        BuildLines();
        RefreshLines();
    }

    private void BuildLines()
    {
        if (linesContainer == null)
        {
            Debug.LogError("[TutorialWindow] linesContainer is not set.");
            return;
        }

        if (tutorialLinePrefab == null)
        {
            Debug.LogError("[TutorialWindow] tutorialLinePrefab is not set.");
            return;
        }

        // На всякий случай очищаем контейнер (если в редакторе уже лежали элементы)
        for (int i = linesContainer.childCount - 1; i >= 0; i--)
            Destroy(linesContainer.GetChild(i).gameObject);

        line1 = Instantiate(tutorialLinePrefab, linesContainer);
        line2 = Instantiate(tutorialLinePrefab, linesContainer);
        line3 = Instantiate(tutorialLinePrefab, linesContainer);
        line4 = Instantiate(tutorialLinePrefab, linesContainer);
        line5 = Instantiate(tutorialLinePrefab, linesContainer);

        // Тексты (без прогресса домов — он в RefreshLines)
        line1.SetText("1) Дороги нужно проводить от обелиска. Постройте дорогу, соединенную с обелиском.  Main — Road");
        line3.SetText("3) Постройте 1 лесопилку у дороги.  Raw — Lumber Mill");
        line4.SetText("4) Постройте 1 berry у дороги.  Food — Berry");
        line5.SetText("5) Откройте Research слева сверху и изучите Clay.");
    }

    private void RefreshLines()
    {
        if (line1 == null) return;

        // 2) обновляем текст с прогрессом
        if (line2 != null)
            line2.SetText($"2) Постройте 10 домов у дороги ({housesCount}/10).  Main — House");

        // чекбоксы
        line1.SetChecked(step1);
        line2.SetChecked(step2);
        line3.SetChecked(step3);
        line4.SetChecked(step4);
        line5.SetChecked(step5);

        // опционально: зачёркивание
        line1.SetStrikethrough(step1);
        line2.SetStrikethrough(step2);
        line3.SetStrikethrough(step3);
        line4.SetStrikethrough(step4);
        line5.SetStrikethrough(step5);
    }

    // ===== Handlers =====

    private void OnRoadConnected()
    {
        if (step1) return;
        step1 = true;
        RefreshLines();
    }

    private void OnHousePlaced(int total)
    {
        if (step2) return;

        housesCount = total;
        if (housesCount >= 10) step2 = true;

        RefreshLines();
    }

    private void OnLumberMillPlaced()
    {
        if (step3) return;
        step3 = true;
        RefreshLines();
    }

    private void OnBerryPlaced()
    {
        if (step4) return;
        step4 = true;
        RefreshLines();
    }

    private void OnResearchCompleted()
    {
        if (step5) return;
        step5 = true;
        RefreshLines();
    }

    // ===== Drag =====

    public void OnPointerDown(PointerEventData eventData)
    {
        window.SetAsLastSibling();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsValidDrag(eventData) || parentRect == null) return;

        dragging = true;
        eventCamera = eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventCamera, out Vector2 pointerLocal))
        {
            pointerOffset = pointerLocal - window.anchoredPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventCamera, out Vector2 pointerLocal))
        {
            Vector2 targetPos = pointerLocal - pointerOffset;

            if (keepInsideParent)
                targetPos = ClampToParent(targetPos);

            window.anchoredPosition = targetPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    private bool IsValidDrag(PointerEventData eventData)
    {
        if (dragAreas == null || dragAreas.Length == 0) return true;

        for (int i = 0; i < dragAreas.Length; i++)
        {
            var area = dragAreas[i];
            if (area == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    area, eventData.position, eventData.pressEventCamera))
                return true;
        }

        return false;
    }

    private Vector2 ClampToParent(Vector2 pos)
    {
        Rect pr = parentRect.rect;
        Rect wr = window.rect;

        Vector2 pp = parentRect.pivot;
        Vector2 wp = window.pivot;

        float minX = -pr.width * pp.x + wr.width * wp.x;
        float maxX = pr.width * (1f - pp.x) - wr.width * (1f - wp.x);

        float minY = -pr.height * pp.y + wr.height * wp.y;
        float maxY = pr.height * (1f - pp.y) - wr.height * (1f - wp.y);

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    // ===== Buttons =====

    public void ToggleContent()
    {
        if (content == null) return;
        content.SetActive(!content.activeSelf);
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
