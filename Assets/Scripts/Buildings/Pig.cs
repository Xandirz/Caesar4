using System.Collections.Generic;
using UnityEngine;

public class Pig : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Pig;

    public Pig()
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
            { "Meat", 2 },
            { "Fat", 2 },
            { "Leather", 2 },
            { "Bone", 2 },

        };
    }

    private void Awake()
    {
        requiresRoadAccess = false;
    }
}