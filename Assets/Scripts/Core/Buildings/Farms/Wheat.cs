using System;
using System.Collections.Generic;
using UnityEngine;

public class Wheat : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Wheat; //!! ЗАПОЛНИТЬ

    public Wheat()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 5 }
        };
        
        workersRequired = 3;
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Wheat", 1 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Manure", 1 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Wheat", 1 } // итого 2
        };
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };

        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Wheat", 1 } 
        };
        
        addConsumptionLevel4 = new Dictionary<string, int>
        {
            { "Ash", 1 }
        };

        upgradeProductionBonusLevel4 = new Dictionary<string, int>
        {
            { "Wheat", 1 } 
        };
    }

    private void Awake()
    {
        requiresRoadAccess = false;

    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Farm2";
        if (level == 3) return "Farm3";
        if (level == 4) return "Farm4";

        return base.GetResearchIdForLevel(level);
    }
    
    
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
}