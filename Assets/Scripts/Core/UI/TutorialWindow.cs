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
    [Header("Links")]
    [SerializeField] private RectTransform window;
    [SerializeField] private RectTransform[] dragAreas; // Header + Content
    [SerializeField] private GameObject content;

    [Header("Tutorial Text")]
    [SerializeField] private TMP_Text tutorialText;

    [Header("Options")]
    [SerializeField] private bool keepInsideParent = true;

    private RectTransform parentRect;
    private Camera eventCamera;
    private Vector2 pointerOffset;
    private bool dragging;

    // прогресс шагов
    private bool step1, step2, step3, step4, step5;
    private int housesCount;

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
        RefreshText();
    }

    // ===== Handlers =====

    private void OnRoadConnected()
    {
        if (step1) return;
        step1 = true;
        RefreshText();
        CheckFinish();
    }

    private void OnHousePlaced(int total)
    {
        if (step2) return;

        housesCount = total; // всегда синхронизируемся с реальным счётчиком

        if (housesCount >= 10)
        {
            step2 = true;
            RefreshText();
            CheckFinish();
        }
        else
        {
            RefreshText();
        }
    }

    private void OnLumberMillPlaced()
    {
        if (step3) return;
        step3 = true;
        RefreshText();
        CheckFinish();
    }

    private void OnBerryPlaced()
    {
        if (step4) return;
        step4 = true;
        RefreshText();
        CheckFinish();
    }

    private void OnResearchCompleted()
    {
        if (step5) return;
        step5 = true;
        RefreshText();
        CheckFinish();
    }

    private void CheckFinish()
    {
        // Если хочешь — можно автоматически закрывать окно по завершению:
        // if (step1 && step2 && step3 && step4 && step5) Destroy(gameObject);
    }

    private void RefreshText()
    {
        if (tutorialText == null) return;

        string L(bool done, string text) => (done ? "☑ " : "☐ ") + (done ? $"<s>{text}</s>" : text);

        string line1 = "Дороги нужно проводить от обелиска. Постройте дорогу, соединенную с обелиском.  <b>Main</b> — <b>Road</b>";
        string line2 = $"Постройте 10 домов у дороги ({housesCount}/10).  <b>Main</b> — <b>House</b>";
        string line3 = "Постройте 1 лесопилку у дороги.  <b>Raw</b> — <b>Lumber Mill</b>";
        string line4 = "Постройте 1 berry у дороги.  <b>Food</b> — <b>Berry</b>";
        string line5 = "Откройте Research слева сверху и изучите <b>Clay</b>.";

        tutorialText.text =
            "Обучение\n" +
            L(step1, line1) + "\n" +
            L(step2, line2) + "\n" +
            L(step3, line3) + "\n" +
            L(step4, line4) + "\n" +
            L(step5, line5);
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
