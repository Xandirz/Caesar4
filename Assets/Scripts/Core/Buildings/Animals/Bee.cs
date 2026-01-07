using System.Collections.Generic;
using UnityEngine;

public class Bee : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Bee;

    public Bee()
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
        };

        production = new Dictionary<string, int>
        {
            { "Wax", 10 },
            { "Honey", 30 },
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}