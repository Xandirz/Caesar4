using System.Collections.Generic;
using UnityEngine;

public class Brick : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Brick;

    public Brick()
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
            { "Clay", 5 },
            { "Charcoal", 5 },
            { "Tools", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Brick", 50 },
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}