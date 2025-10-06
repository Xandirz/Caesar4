using System;
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
        Instance = this;
    }

    private void Start()
    {
        // === создаём ноды ===
        var fire = new ResearchNode { researchName = "Огонь", researchTime = 5f, isUnlocked = true };
        var cooking = new ResearchNode { researchName = "Готовка пищи", researchTime = 10f };
        var foodPreservation = new ResearchNode { researchName = "Хранение еды", researchTime = 12f };

        // === задаём связи (цепочка) ===
        fire.nextResearches = new[] { cooking };           // 🔥 Огонь -> 🍖 Готовка пищи
        cooking.nextResearches = new[] { foodPreservation }; // 🍖 Готовка пищи -> ❄️ Хранение еды

        // === добавляем все в менеджер ===
        ResearchManager.Instance.AddResearches(new List<ResearchNode> { fire, cooking, foodPreservation });
    }


    // Запуск исследования
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
    
// ✅ Возвращает список всех исследований
    public List<ResearchNode> GetAllResearches()
    {
        return allResearches;
    }

// ✅ Позволяет добавить новые исследования
    public void AddResearches(List<ResearchNode> researches)
    {
        if (researches == null) return;

        foreach (var r in researches)
        {
            if (!allResearches.Contains(r))
                allResearches.Add(r);
        }
    }

    

    private IEnumerator ResearchRoutine(ResearchNode node)
    {
        yield return new WaitForSeconds(node.researchTime);

        node.isCompleted = true;
        currentResearch = null;
        Debug.Log($"Исследование завершено: {node.researchName}");

        // Разблокируем потомков
        foreach (var next in node.nextResearches)
        {
            next.isUnlocked = true;
        }

        OnResearchFinished?.Invoke(node);
    }

    public bool IsResearchInProgress() => currentResearch != null;
}