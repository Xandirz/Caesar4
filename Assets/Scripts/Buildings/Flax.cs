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
        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Manure", 1 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Flax", 1 } // итого 2
        };
    }

    private void Awake()
    {
        requiresRoadAccess = false;
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl1/Flax");

    }
    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Farm2";
        return base.GetResearchIdForLevel(level);
    }
}