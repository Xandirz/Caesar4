using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance;

    [SerializeField] private List<ResearchNode> allResearches = new();
    private ResearchNode currentResearch;

    public event System.Action<ResearchNode> OnResearchStarted;
    public event System.Action<ResearchNode> OnResearchFinished;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializeResearchTree(); // ✅ создаём дерево при старте
    }

    // === СОЗДАЁМ ДЕРЕВО ===
    private void InitializeResearchTree()
    {
        // 🔥 Создаём ноды
        var fire = new ResearchNode { researchName = "Огонь", researchTime = 5f, isUnlocked = true };
        var cooking = new ResearchNode { researchName = "Готовка пищи", researchTime = 8f };
        var foodPreservation = new ResearchNode { researchName = "Хранение еды", researchTime = 10f };
        var clay = new ResearchNode { researchName = "Обожжённая глина", researchTime = 7f };
        var pottery = new ResearchNode { researchName = "Гончарное дело", researchTime = 9f };

        // 🔗 Устанавливаем связи (ветви дерева)
        fire.nextResearches = new[] { cooking, clay };
        cooking.nextResearches = new[] { foodPreservation };
        clay.nextResearches = new[] { pottery };

        // ✅ Добавляем в список
        allResearches = new List<ResearchNode> { fire, cooking, foodPreservation, clay, pottery };

        Debug.Log($"[ResearchManager] Дерево исследований инициализировано. Узлов: {allResearches.Count}");
    }

    // === Публичный доступ к списку ===
    public List<ResearchNode> GetAllResearches()
    {
        return allResearches;
    }

    // === Запуск исследования ===
    public void StartResearch(ResearchNode node)
    {
        if (node.isUnlocked && !node.isCompleted && currentResearch == null)
        {
            currentResearch = node;
            Debug.Log($"Начато исследование: {node.researchName}");
            OnResearchStarted?.Invoke(node);
            StartCoroutine(ResearchRoutine(node));
        }
    }

    private IEnumerator ResearchRoutine(ResearchNode node)
    {
        yield return new WaitForSeconds(node.researchTime);

        node.isCompleted = true;
        currentResearch = null;
        Debug.Log($"Исследование завершено: {node.researchName}");

        // 🔓 Разблокируем потомков
        if (node.nextResearches != null)
        {
            foreach (var next in node.nextResearches)
            {
                next.isUnlocked = true;
                Debug.Log($"Разблокировано исследование: {next.researchName}");
            }
        }

        OnResearchFinished?.Invoke(node);
    }

    public bool IsResearchInProgress() => currentResearch != null;
}
