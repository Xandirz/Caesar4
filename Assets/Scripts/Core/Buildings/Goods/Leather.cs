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
            { "Rock", 5 }
        };

        workersRequired = 8;

        consumptionCost = new Dictionary<string, int>
        {
            { "Hide", 40 },
            { "Tools", 1 }
        };

        production = new Dictionary<string, int>
        {
            { "Leather", 30 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Salt", 2 },   
            { "Hide", 40 },   
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Leather", 10 },
        };
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 1) return "Leather";
        if (level == 2) return "Leather2";
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