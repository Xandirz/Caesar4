using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ResearchUI : MonoBehaviour
{
    public static ResearchUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject panel;             // Панель окна исследований
    [SerializeField] private ScrollRect scrollRect;        // ScrollRect для прокрутки
    [SerializeField] private RectTransform content;        // Контейнер с LayoutGroup
    [SerializeField] private ResearchRow researchRowPrefab;// Префаб строки исследования

    private readonly Dictionary<string, ResearchRow> rows = new();
    private ResearchManager manager;
    private bool isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        // Связываем ScrollRect и Content (важно!)
        if (scrollRect != null && content != null)
            scrollRect.content = content;
    }

    // Привязка менеджера (для колбэков)
    public void Initialize(ResearchManager mgr)
    {
        manager = mgr;
    }

    // Добавление новой строки исследования
    public void AddRow(string id, string requirementText)
    {
        if (rows.ContainsKey(id)) return;

        var row = Instantiate(researchRowPrefab, content);
        row.name = $"ResearchRow_{id}";
        row.Setup(
            id,
            requirementText,
            onOk: () => manager.CompleteResearch(id)
        );

        rows[id] = row;

        // Обновляем макет и прокручиваем вниз
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
    public void UpdateRowText(string id, string text)
    {
        if (rows.TryGetValue(id, out var row))
            row.SetText(text);
    }

    // Включить/выключить кнопку ОК
    public void SetAvailable(string id, bool available)
    {
        if (rows.TryGetValue(id, out var row))
            row.SetAvailable(available);
    }

    // Пометить как завершённое — меняем текст и убираем кнопку
    public void SetCompleted(string id, string discoveryText)
    {
        if (rows.TryGetValue(id, out var row))
            row.ShowCompleted(discoveryText);
    }

    // Переключить панель
    public void TogglePanel()
    {
        if (panel == null) return;
        isVisible = !isVisible;
        panel.SetActive(isVisible);
    }
}
