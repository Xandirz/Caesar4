using System;
using System.Collections.Generic;
using UnityEngine;

public class CopperOre : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.CopperOre; //!! ЗАПОЛНИТЬ
    
    public CopperOre()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Rock", 1 },
            { "Wood", 1 },
        };
        
        workersRequired = 5;
        
        isNoisy = true;

        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 5 },
        };
        
        production = new Dictionary<string, int>
        {
            { "CopperOre", 20 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "CopperOre", 20 }  
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }


    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2)
            return "Mining2";

        return base.GetResearchIdForLevel(level);
    }


}