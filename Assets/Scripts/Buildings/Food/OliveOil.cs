using System.Collections.Generic;
using UnityEngine;

public class OliveOil : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.OliveOil;

    public OliveOil()
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
            { "Olive", 10 },
        };

        production = new Dictionary<string, int>
        {
            { "OliveOil", 15 }
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}