using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ResearchUI : MonoBehaviour
{
    public static ResearchUI Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button okButton;

    private bool isVisible = false;
    private Action onConfirm;

    // список исследований
    private readonly List<ResearchEntry> researches = new();

    void Awake()
    {
        Instance = this;
        if (panel != null)
            panel.SetActive(false);
    }

    void Start()
    {
        InitializeResearches();
        UpdateResearchList();
    }

    void Update()
    {
        if (isVisible)
            UpdateResearchList();
    }

    /// <summary>
    /// Создаём список исследований
    /// </summary>
    void InitializeResearches()
    {
        researches.Add(new ResearchEntry
        {
            description = "Постройте 10 домов.",
            requirement = () => AllBuildingsManager.Instance.GetBuildingCount(BuildManager.BuildMode.House) >= 10,
            onUnlock = () =>
            {
                ShowResearch("Открытие!", "Вы нашли глину!", () =>
                {
                    BuildManager.Instance.UnlockBuilding(BuildManager.BuildMode.Clay);
                    Debug.Log("Здание Clay разблокировано!");
                });
            }
        });
    }

    /// <summary>
    /// Переключает видимость панели
    /// </summary>
    public void TogglePanel()
    {
        if (panel == null) return;

        isVisible = !isVisible;
        panel.SetActive(isVisible);

        if (isVisible)
        {
            titleText.text = "Исследования";
            messageText.text = "Выберите доступное исследование:";
            okButton.gameObject.SetActive(false);
            UpdateResearchList();
        }
    }

    /// <summary>
    /// Обновляет список исследований (просто текст)
    /// </summary>
    void UpdateResearchList()
    {
        if (!isVisible || panel == null) return;

        // очищаем старые строки (кроме служебных)
        foreach (Transform child in panel.transform)
        {
            if (child == titleText.transform.parent || child == messageText.transform || child == okButton.transform)
                continue;
            Destroy(child.gameObject);
        }

        // добавляем строки исследований
        foreach (var r in researches)
        {
            GameObject textObj = new GameObject(r.title, typeof(TextMeshProUGUI));
            textObj.transform.SetParent(panel.transform, false);

            TMP_Text txt = textObj.GetComponent<TMP_Text>();
            txt.fontSize = 20;
            txt.text = $"{r.title}\n{r.description}";
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Left;

            if (r.requirement != null && r.requirement.Invoke())
            {
                // если условие выполнено — можно нажать мышкой
                Button btn = textObj.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => r.onUnlock?.Invoke());
            }
        }
    }

    /// <summary>
    /// Показывает окно "Открытие!"
    /// </summary>
    public void ShowResearch(string title, string message, Action onOk)
    {
        if (panel == null) return;

        panel.SetActive(true);
        isVisible = true;

        titleText.text = title;
        messageText.text = message;
        okButton.gameObject.SetActive(true);

        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() =>
        {
            onOk?.Invoke();
            TogglePanel();
        });
    }
}

/// <summary>
/// Структура данных для одного исследования
/// </summary>
[System.Serializable]
public class ResearchEntry
{
    public string title;
    public string description;
    public Func<bool> requirement; // условие
    public Action onUnlock;        // действие при открытии
}
