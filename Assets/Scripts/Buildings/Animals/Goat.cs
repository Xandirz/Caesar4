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
            { "Meat", 5 },
            { "Fat", 1 },
            { "Wool", 1 },
            { "Hide", 5 },
            { "Bone", 2 },
            { "Manure", 5 },
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