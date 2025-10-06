using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UI.Extensions;

public class ResearchUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private GameObject linePrefab;

    private readonly List<Button> buttons = new();
    private readonly Dictionary<ResearchNode, Button> nodeToButton = new();

    private void Start()
    {
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitUntil(() => ResearchManager.Instance != null);
        panel.SetActive(false);
        ResearchManager.Instance.OnResearchFinished += UpdateUI;
        CreateButtons();
        DrawConnections();
    }

    private void CreateButtons()
    {
        var researches = ResearchManager.Instance.GetAllResearches();
        Debug.Log($"Создаю кнопки, найдено {researches.Count} исследований");

        // === параметры позиционирования ===
        float xOffset = 140f;   // 🔹 минимальное расстояние между кнопками
        float yOffset = 110f;   // 🔹 плотное расстояние по вертикали
        int columns = 3;

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        buttons.Clear();
        nodeToButton.Clear();

        // Начинаем строить снизу панели
        float startY = -buttonContainer.rect.height / 2f + 80f;

        for (int i = 0; i < researches.Count; i++)
        {
            var node = researches[i];
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
            label.text = node.researchName;

            btn.onClick.AddListener(() => OnResearchButtonClicked(node));

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            int col = i % columns;
            int row = i / columns;

            float xPos = (col - (columns - 1) / 2f) * xOffset; // центрируем по X
            float yPos = startY + row * yOffset; // растёт вверх

            rt.anchoredPosition = new Vector2(xPos, yPos);

            buttons.Add(btn);
            nodeToButton[node] = btn;
        }

        UpdateUI(null);
    }


private void DrawConnections()
{
    var researches = ResearchManager.Instance.GetAllResearches();
    if (linePrefab == null || lineContainer == null) return;

    // очистить старые линии
    foreach (Transform child in lineContainer)
        Destroy(child.gameObject);

    const float pad = 6f; // небольшой отступ от границ кнопок

    foreach (var node in researches)
    {
        if (node.nextResearches == null) continue;
        if (!nodeToButton.TryGetValue(node, out var startBtn)) continue;

        RectTransform srt = startBtn.GetComponent<RectTransform>();
        // верхняя середина стартовой кнопки
        Vector2 startTop = srt.anchoredPosition + new Vector2(0, srt.rect.height * 0.5f + pad);

        foreach (var next in node.nextResearches)
        {
            if (!nodeToButton.TryGetValue(next, out var endBtn)) continue;

            RectTransform ert = endBtn.GetComponent<RectTransform>();
            // нижняя середина целевой кнопки
            Vector2 endBottom = ert.anchoredPosition - new Vector2(0, ert.rect.height * 0.5f + pad);

            // создать объект линии
            GameObject lineObj = Instantiate(linePrefab, lineContainer);
            var line = lineObj.GetComponent<UILineRenderer>();
            line.raycastTarget = false;     // не блокировать клики
            line.LineThickness = 2f;

            // если по одной вертикали — одна вертикальная линия
            if (Mathf.Abs(startTop.x - endBottom.x) < 0.01f)
            {
                line.Points = new Vector2[] { startTop, endBottom };
                continue;
            }

            // если по одному уровню — одна горизонтальная линия
            if (Mathf.Abs(startTop.y - endBottom.y) < 0.01f)
            {
                line.Points = new Vector2[] { startTop, endBottom };
                continue;
            }

            // иначе делаем "колено" (вертикаль → горизонталь → вертикаль)
            float elbowY = (startTop.y + endBottom.y) * 0.5f; // общий уровень колена

            Vector2 p1 = new Vector2(startTop.x, elbowY);   // вертикально вверх/вниз
            Vector2 p2 = new Vector2(endBottom.x, elbowY);  // горизонтально к цели

            line.Points = new Vector2[] { startTop, p1, p2, endBottom };
        }
    }
}


    private void OnResearchButtonClicked(ResearchNode node)
    {
        if (node.isUnlocked && !node.isCompleted && !ResearchManager.Instance.IsResearchInProgress())
        {
            ResearchManager.Instance.StartResearch(node);
            var btn = nodeToButton[node];
            StartCoroutine(ShowProgressBar(btn, node.researchTime));
            UpdateUI(node);
        }
    }

    private IEnumerator ShowProgressBar(Button btn, float time)
    {
        Slider slider = btn.GetComponentInChildren<Slider>(true);
        if (slider == null) yield break;

        slider.gameObject.SetActive(true);
        slider.value = 0;

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            slider.value = Mathf.Clamp01(elapsed / time);
            yield return null;
        }

        slider.value = 1f;
        yield return new WaitForSeconds(0.3f);
        slider.gameObject.SetActive(false);
    }

    private void UpdateUI(ResearchNode _)
    {
        var researches = ResearchManager.Instance.GetAllResearches();

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            var node = researches[i];
            var label = btn.GetComponentInChildren<TMP_Text>();

            if (node.isCompleted)
            {
                btn.interactable = false;
                label.text = $"{node.researchName} ✅";
            }
            else if (node.isUnlocked)
            {
                btn.interactable = true;
                label.text = node.researchName;
            }
            else
            {
                btn.interactable = false;
                label.text = $"{node.researchName} 🔒";
            }
        }
    }

    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf)
        {
            CreateButtons();
            DrawConnections();
        }
    }
}
