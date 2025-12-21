using System.Collections.Generic;
using UnityEngine;

public class Olive : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Olive;

    public Olive()
    {
        cost = new Dictionary<string, int>
        {
            { "Wood", 5 },
            { "Tools", 5 },
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
            { "Olive", 10 }
        };
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