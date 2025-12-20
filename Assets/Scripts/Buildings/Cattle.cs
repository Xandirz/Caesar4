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
            { "Meat", 2 },
            { "Hide", 2 },
            { "Milk", 5 },
            { "Fat", 2 },
            { "Bone", 2 },
            { "Manure", 7 },
        };
    }

    private void Awake()
    {
        requiresRoadAccess = false;
    }
}