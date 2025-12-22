using System.Collections.Generic;
using UnityEngine;

public class Leather : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Leather;

    public Leather()
    {
        cost = new Dictionary<string, int>
        {
            { "Wood", 5 },
            { "Brick", 5 }
        };

        workersRequired = 8;

        consumptionCost = new Dictionary<string, int>
        {
            { "Hide", 4 },
            { "Tools", 1 }
        };

        production = new Dictionary<string, int>
        {
            { "Leather", 10 }
        };
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 1) return "Leather";
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