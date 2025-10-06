using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    private List<Button> buttons = new();

    private void Start()
    {
        panel.SetActive(false);

        // Подписка на события
        ResearchManager.Instance.OnResearchFinished += UpdateUI;
        CreateButtons();
    }

    private void CreateButtons()
    {
        var researches = ResearchManager.Instance.GetAllResearches();
        float xOffset = 220f;   // расстояние между кнопками по горизонтали
        float yOffset = -120f;  // расстояние между кнопками по вертикали
        int columns = 3;
    
        // Очистим контейнер перед созданием (на всякий случай)
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
    
        buttons.Clear();

        for (int i = 0; i < researches.Count; i++)
        {
            ResearchNode node = researches[i];

            // Создаем кнопку
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Text label = btnObj.GetComponentInChildren<Text>();
            label.text = node.researchName;

            // Добавляем обработчик клика
            btn.onClick.AddListener(() => OnResearchButtonClicked(node));

            // Позиционируем кнопку в виде сетки
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            int col = i % columns;
            int row = i / columns;
            rt.anchoredPosition = new Vector2(col * xOffset, row * yOffset);

            buttons.Add(btn);
        }

        // Обновим состояние (блокировки / выполнено и т.п.)
        UpdateUI(null);
    }



    private void OnResearchButtonClicked(ResearchNode node)
    {
        if (node.isUnlocked && !node.isCompleted && !ResearchManager.Instance.IsResearchInProgress())
        {
            ResearchManager.Instance.StartResearch(node);
            UpdateUI(node);
        }
    }

    private void UpdateUI(ResearchNode _)
    {
        foreach (var btn in buttons)
        {
            var node = ResearchManager.Instance
                .GetType()
                .GetField("allResearches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ResearchManager.Instance) as List<ResearchNode>;

            var index = buttons.IndexOf(btn);
            var research = node[index];

            var label = btn.GetComponentInChildren<Text>();

            if (research.isCompleted)
            {
                btn.interactable = false;
                label.text = $"{research.researchName} ✅";
            }
            else if (research.isUnlocked)
            {
                btn.interactable = true;
                label.text = $"{research.researchName}";
            }
            else
            {
                btn.interactable = false;
                label.text = $"{research.researchName} 🔒";
            }
        }
    }

    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
    }
}
