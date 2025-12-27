using System.Collections.Generic;
using UnityEngine;

public class Smithy : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Smithy;

    public Smithy()
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
            { "Bronze", 5 },
            { "Tools", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Metalware", 30 },
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Gold", 5 }, 
            { "Bronze", 5 },
        };
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Metalware", 30 },
            { "Coin", 60 }
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Smithy2";
        return base.GetResearchIdForLevel(level);
    }
}