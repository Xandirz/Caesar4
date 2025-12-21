using System.Collections.Generic;
using UnityEngine;

public class Soap : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Soap;

    public Soap()
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
            { "Fat", 5 },
            { "Tools", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Soap", 30 },
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}