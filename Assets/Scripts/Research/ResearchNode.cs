using System;
using UnityEngine;

[Serializable]
public class ResearchNode
{
    public string researchName;
    public float researchTime = 10f;
    public bool isUnlocked = false;
    public bool isCompleted = false;

    public ResearchNode[] nextResearches;

    // события (для расширений, если надо)
    public Action<ResearchNode> OnResearchCompleted;
}