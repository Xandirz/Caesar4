using System.Collections.Generic;
using UnityEngine;

public class Cattle : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Cattle;

    public Cattle()
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
            { "Wheat", 3 },
            { "Tools", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Meat", 10 },
            { "Hide", 20 },
            { "Milk", 4 },
            { "Fat", 2 },
            { "Bone", 2 },
            { "Manure", 7 },
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Salt", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Meat", 10 },
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
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
}