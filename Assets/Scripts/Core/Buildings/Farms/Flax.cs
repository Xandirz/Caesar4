using System.Collections.Generic;
using UnityEngine;

public class Flax : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Flax;

    public Flax()
    {
        cost = new Dictionary<string, int>
        {
            { "Tools", 5 },
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
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Manure", 1 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Flax", 1 } // итого 2
        };
        
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };

        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Flax", 1 } // итого 3
        };
        addConsumptionLevel4 = new Dictionary<string, int>
        {
            { "Ash", 1 }
        };

        upgradeProductionBonusLevel4 = new Dictionary<string, int>
        {
            { "Flax", 1 } 
        };
    }

    private void Awake()
    {
        requiresRoadAccess = false;
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl1/Flax");

    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Farm2";
        if (level == 3) return "Farm3";
        if (level == 4) return "Farm4";

        return base.GetResearchIdForLevel(level);
    }
}