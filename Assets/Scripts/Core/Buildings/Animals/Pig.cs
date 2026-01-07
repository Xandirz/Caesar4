using System.Collections.Generic;
using UnityEngine;

public class Pig : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Pig;

    public Pig()
    {
        cost = new Dictionary<string, int>
        {
            { "Wood", 5 },
        };

        workersRequired = 3;

        // если хочешь, чтобы фермы требовали воду/дорогу — включай
        // needWaterNearby = true;
        // requiresRoadAccess = true;

        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 1 },
            { "Tools", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Meat", 10 },
            { "Fat", 5 },
            { "Hide", 3 },
            { "Bone", 1 },
            { "Manure", 5 },
        };
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Salt", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Meat", 5 },
        };
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Animal2";

        return base.GetResearchIdForLevel(level);
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    private void Awake()
    {
        requiresRoadAccess = false;
    }
}