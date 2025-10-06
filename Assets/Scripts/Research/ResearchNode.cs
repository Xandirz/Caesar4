using System;
using UnityEngine;

[Serializable]
public class ResearchNode
{
    public string researchName;
    public float researchTime = 10f;
    public bool isUnlocked = false;
    public bool isCompleted = false;

    public ResearchNode[] nextResearches; // ссылки на потомков

    // Событие завершения
    public Action<ResearchNode> OnResearchCompleted;
}