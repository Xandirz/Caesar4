using System.Collections.Generic;
using UnityEngine;

public class Brewery : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Brewery; //!! ЗАПОЛНИТЬ
    
    public Brewery()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
          
        };
        
        workersRequired = 12;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 2 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Beer", 30 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 2 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Beer", 60 }
        };
    }


    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Brewery2";
        return base.GetResearchIdForLevel(level);
    }
}