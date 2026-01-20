using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    [SerializeField] private Transform linesContainer;           // куда инстансить строки (внутри Content)
    [SerializeField] private TutorialLineUI tutorialLinePrefab;  // префаб строки

    [Header("Navigation Buttons")]
    [SerializeField] private Button prevLineButton;
    [SerializeField] private Button nextLineButton;

    private RectTransform parentRect;
    private Camera eventCamera;
    private Vector2 pointerOffset;
    private bool dragging;

    public GameObject closeButton;

    // Прогресс
    private bool step1, step2, step3, step4, step5, step6;
    private int housesCount;

    // Ссылки на созданные строки
    private TutorialLineUI line0, line1, line2, line3, line4, line5, line6, line7;

    private const int TOTAL_STEPS = 7;

    // Навигация по линиям (ручной режим)
    private bool manualBrowse;
    private int displayedStep = 1; // 1..7
    public static TutorialWindow Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
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
        TutorialEvents.InfoUIOpened += OnInfoUIOpened;
        TutorialEvents.ResearchCompleted += OnResearchCompleted;

        if (prevLineButton != null)
        {
            prevLineButton.onClick.RemoveListener(PrevLine);
            prevLineButton.onClick.AddListener(PrevLine);
        }

        if (nextLineButton != null)
        {
            nextLineButton.onClick.RemoveListener(NextLine);
            nextLineButton.onClick.AddListener(NextLine);
        }
    }

    private void OnDisable()
    {
        TutorialEvents.RoadConnectedToObelisk -= OnRoadConnected;
        TutorialEvents.HousePlaced -= OnHousePlaced;
        TutorialEvents.LumberMillPlaced -= OnLumberMillPlaced;
        TutorialEvents.BerryPlaced -= OnBerryPlaced;
        TutorialEvents.InfoUIOpened -= OnInfoUIOpened;
        TutorialEvents.ResearchCompleted -= OnResearchCompleted;

        if (prevLineButton != null) prevLineButton.onClick.RemoveListener(PrevLine);
        if (nextLineButton != null) nextLineButton.onClick.RemoveListener(NextLine);
    }

    private void Start()
    {
        BuildLines();

        // Изначально кнопка закрытия скрыта до конца обучения
        if (closeButton != null)
            closeButton.SetActive(false);

        // В начале показываем первый шаг (авто-режим)
        manualBrowse = false;
        displayedStep = 1;

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
        {
            var child = linesContainer.GetChild(i);
            if (child.GetComponent<TutorialLineUI>() != null)
                Destroy(child.gameObject);
        }


        // Создаём строки
      //  line0 = Instantiate(tutorialLinePrefab, linesContainer);
        line1 = Instantiate(tutorialLinePrefab, linesContainer);
        line2 = Instantiate(tutorialLinePrefab, linesContainer);
        line3 = Instantiate(tutorialLinePrefab, linesContainer);
        line4 = Instantiate(tutorialLinePrefab, linesContainer);
        line5 = Instantiate(tutorialLinePrefab, linesContainer);
        line6 = Instantiate(tutorialLinePrefab, linesContainer);
        line7 = Instantiate(tutorialLinePrefab, linesContainer);

        // Тексты (без прогресса домов — он в RefreshLines)
//        line0.SetText("Двигать камеру wasd/arrow keys or middle mouse button, zoom - mouse wheel.");
        line1.SetText("1) Roads must be built from the Obelisk. Build a road connected to the Obelisk. Go to the Main tab and select Road.");
        line3.SetText("3) Build 1 Lumber Mill next to a road. Go to the Raw tab and select Lumber Mill.");
        line4.SetText("4) Build 1 Berry building next to a road. Go to the Food tab and select Berry.");
        line5.SetText("5) Click a building to open the Info window. It shows the building's needs.");
        line6.SetText("6) Open Research (top-left) and study Clay. You will unlock a new building.");
        line7.SetText("7) Develop your settlement and lead it to power and knowledge. You can close this window.");



        // Все строки шагов изначально выключаем; включим нужную в RefreshLines
        if (line1 != null) line1.gameObject.SetActive(false);
        if (line2 != null) line2.gameObject.SetActive(false);
        if (line3 != null) line3.gameObject.SetActive(false);
        if (line4 != null) line4.gameObject.SetActive(false);
        if (line5 != null) line5.gameObject.SetActive(false);
        if (line6 != null) line6.gameObject.SetActive(false);
        if (line7 != null) line7.gameObject.SetActive(false);
    }

    private void RefreshLines()
    {
        if (line1 == null) return;

        // 2) обновляем текст с прогрессом
        if (line2 != null)
            line2.SetText($"2) Build 10 houses next to a road ({housesCount}/10).  Main — House");

        // чекбоксы
        line1.SetChecked(step1);
        line2.SetChecked(step2);
        line3.SetChecked(step3);
        line4.SetChecked(step4);
        line5.SetChecked(step5);
        line6.SetChecked(step6);

        // зачёркивание
        line1.SetStrikethrough(step1);
        line2.SetStrikethrough(step2);
        line3.SetStrikethrough(step3);
        line4.SetStrikethrough(step4);
        line5.SetStrikethrough(step5);
        line6.SetStrikethrough(step6);

        // Какой шаг показываем
        int autoStep = GetCurrentStepIndex();                 // максимально "открытый" шаг по прогрессу
        int showStep = manualBrowse ? displayedStep : autoStep;

        showStep = Mathf.Clamp(showStep, 1, TOTAL_STEPS);
        displayedStep = showStep;

        // Одновременно активна только текущая строка (1..7)
        SetStepLineActive(1, showStep == 1);
        SetStepLineActive(2, showStep == 2);
        SetStepLineActive(3, showStep == 3);
        SetStepLineActive(4, showStep == 4);
        SetStepLineActive(5, showStep == 5);
        SetStepLineActive(6, showStep == 6);
        SetStepLineActive(7, showStep == 7);

        // Кнопка закрытия появляется только когда обучение завершено (step6) и показываем финальную строку
        if (closeButton != null)
            closeButton.SetActive(step6 && showStep == 7);

        // Кнопки навигации
        if (prevLineButton != null)
            prevLineButton.interactable = showStep > 1;

        if (nextLineButton != null)
        {
            // Next активен только если следующий шаг уже открыт прогрессом
            // (то есть showStep < autoStep)
            nextLineButton.interactable = showStep < autoStep;
        }
    }

    private int GetCurrentStepIndex()
    {
        if (!step1) return 1;
        if (!step2) return 2;
        if (!step3) return 3;
        if (!step4) return 4;
        if (!step5) return 5;
        if (!step6) return 6;
        return 7;
    }

    private void SetStepLineActive(int stepIndex, bool active)
    {
        TutorialLineUI line = stepIndex switch
        {
            1 => line1,
            2 => line2,
            3 => line3,
            4 => line4,
            5 => line5,
            6 => line6,
            7 => line7,
            _ => null
        };

        if (line == null) return;

        if (line.gameObject.activeSelf != active)
            line.gameObject.SetActive(active);
    }

    // ===== Навигация по линиям (кнопки) =====

    public void PrevLine()
    {
        manualBrowse = true;
        displayedStep = Mathf.Clamp(displayedStep - 1, 1, TOTAL_STEPS);
        RefreshLines();
    }

    public void NextLine()
    {
        manualBrowse = true;
        displayedStep = Mathf.Clamp(displayedStep + 1, 1, TOTAL_STEPS);
        RefreshLines();
    }

    // Опционально: можно вызвать, чтобы вернуться в авто-режим
    public void ResumeAuto()
    {
        manualBrowse = false;
        displayedStep = GetCurrentStepIndex();
        RefreshLines();
    }

    // ===== Handlers =====

    private void OnRoadConnected()
    {
        if (step1) return;
        step1 = true;

        if (!manualBrowse)
            displayedStep = GetCurrentStepIndex();

        RefreshLines();
    }

    private void OnHousePlaced(int total)
    {
        if (step2) return;

        housesCount = total;
        if (housesCount >= 10)
            step2 = true;

        if (!manualBrowse)
            displayedStep = GetCurrentStepIndex();

        RefreshLines();
    }

    private void OnLumberMillPlaced()
    {
        if (step3) return;
        step3 = true;

        if (!manualBrowse)
            displayedStep = GetCurrentStepIndex();

        RefreshLines();
    }

    private void OnBerryPlaced()
    {
        if (step4) return;
        step4 = true;

        if (!manualBrowse)
            displayedStep = GetCurrentStepIndex();

        RefreshLines();
    }

    private void OnInfoUIOpened()
    {
        if (step5) return;
        step5 = true;

        if (!manualBrowse)
            displayedStep = GetCurrentStepIndex();

        RefreshLines();
    }

    private void OnResearchCompleted()
    {
        if (step6) return;
        step6 = true;

        if (!manualBrowse)
            displayedStep = GetCurrentStepIndex();

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
