using System.Collections.Generic;
using UnityEngine;

public class Flax : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Flax;

    public Flax()
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
            // пока пусто
        };

        production = new Dictionary<string, int>
        {
            { "Flax", 1 }
        };
    }

    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl1/FlaxFarm");
    }
}