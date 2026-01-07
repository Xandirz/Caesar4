using System.Collections.Generic;
using UnityEngine;

public class Wine : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Wine;

    public Wine()
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
            { "Tools", 1 },
            { "Grape", 15 },
        };

        production = new Dictionary<string, int>
        {
            { "Wine", 25 }
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}