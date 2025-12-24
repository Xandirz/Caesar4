using System.Collections.Generic;
using UnityEngine;

public class Dairy : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Dairy; //!! ЗАПОЛНИТЬ
    
    public Dairy()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },

        };
        
        workersRequired = 12;
        
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Milk", 10 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Cheese", 30 },
            { "Yogurt", 30 },
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 2 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Cheese", 30 },
            { "Yogurt", 30 },  
        };
    }


    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Dairy2";
        return base.GetResearchIdForLevel(level);
    }
}