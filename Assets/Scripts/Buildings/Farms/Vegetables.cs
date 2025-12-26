using System.Collections.Generic;
using UnityEngine;

public class Vegetables : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Vegetables;

    public Vegetables()
    {
        cost = new Dictionary<string, int>
        {
            { "Wood", 5 },
            { "Tools", 5 },
        };

        workersRequired = 3;

        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 },
            { "Manure", 1 },
        };

        production = new Dictionary<string, int>
        {
            { "Vegetables", 5 }
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