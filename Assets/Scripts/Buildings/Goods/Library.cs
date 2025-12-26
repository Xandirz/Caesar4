using System.Collections.Generic;
using UnityEngine;

public class Library : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Library;

    public Library()
    {
        cost = new Dictionary<string, int>
        {
            { "Wood", 5 },
            { "Brick", 5 }
        };

        workersRequired = 8;

        consumptionCost = new Dictionary<string, int>
        {
            { "Clay", 4 },
            { "Tools", 1 }
        };

        production = new Dictionary<string, int>
        {
            { "Books", 50 }
        };
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 1) return "Library";
        return base.GetResearchIdForLevel(level);
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    private void Awake()
    {
        requiresRoadAccess = true;
    }
}