using System.Collections.Generic;
using UnityEngine;

public class Sheep : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Sheep; //!! ЗАПОЛНИТЬ
    
    public Sheep()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Tools", 1 },
           
        };
        
        workersRequired = 2;
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Meat", 5 },
            { "Wool", 1 },
            { "Hide", 2 },
            { "Milk", 2 },
            { "Bone", 1 },
            { "Fat", 1 },
            { "Manure", 5 },

            
        };
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Salt", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Meat", 2 },
        };
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Animal2";

        return base.GetResearchIdForLevel(level);
    }
    private void Awake()
    {
        requiresRoadAccess = false;
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}