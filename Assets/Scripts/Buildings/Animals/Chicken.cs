using System.Collections.Generic;
using UnityEngine;

public class Chicken : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Chicken;

    public Chicken()
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
            { "Meat", 1 },
            { "Eggs", 10 },
        };
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