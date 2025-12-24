using System;
using System.Collections.Generic;
using UnityEngine;

public class Charcoal : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Charcoal; //!! ЗАПОЛНИТЬ
    
    public Charcoal()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Rock", 1 },
            { "Wood", 1 },
          
        };

        workersRequired = 24;
        isNoisy = true;

        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 },
            { "Wood", 6 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Charcoal", 40 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Charcoal", 100 }  
        };
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Charcoal2";
        return base.GetResearchIdForLevel(level);
    }

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}