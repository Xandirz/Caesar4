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
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Salt", 1 },   
            { "Metalware", 1 },   
            { "Ash", 1 },   
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Soap", 30 },
        };
    }
    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 1) return "Soap";
        if (level == 2) return "Soap2";
        return base.GetResearchIdForLevel(level);
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Soap2");
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}