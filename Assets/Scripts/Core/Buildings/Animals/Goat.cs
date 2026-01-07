using System.Collections.Generic;
using UnityEngine;

public class Goat : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Goat;

    public Goat()
    {
        cost = new Dictionary<string, int>
        {
            { "Wood", 5 },
        };

        workersRequired = 2;

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
            { "Meat", 2 },
            { "Fat", 1 },
            { "Wool", 1 },
            { "Milk", 2 },
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
            { "Meat", 2 },
        };
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Animal2";

        return base.GetResearchIdForLevel(level);
    }
    private void Awake()
    {
        requiresRoadAccess = false;
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
}