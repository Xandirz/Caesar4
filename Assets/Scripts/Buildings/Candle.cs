using System.Collections.Generic;
using UnityEngine;

public class Candle : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Candle;

    public Candle()
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
            { "Wax", 5 },
            { "Fat", 5 },
            { "Tools", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Candle", 50 },
        };
    }


}