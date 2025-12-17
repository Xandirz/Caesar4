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
            { "Meat", 1 },
            { "Fat", 1 },
            { "Wool", 1 },
            { "Leather", 1 },
            { "Bone", 2 },

        };
    }


}