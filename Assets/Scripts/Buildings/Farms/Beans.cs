using System;
using System.Collections.Generic;
using UnityEngine;

public class Beans : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Beans; //!! ЗАПОЛНИТЬ
    
    public Beans()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 5 },
        };
        
        workersRequired = 2;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Beans", 5 }
        };
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Manure", 1 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Beans", 5 } 
        };
    }

    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl1/Beans");

        requiresRoadAccess = false;
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Farm2";
        return base.GetResearchIdForLevel(level);
    }
}