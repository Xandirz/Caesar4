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
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Salt", 2 },
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Cheese", 30 },
            { "Yogurt", 30 },  
        };
    }


    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Dairy2");
        level3Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl3/Dairy3");
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Dairy2";
        if (level == 3) return "Dairy3";
        return base.GetResearchIdForLevel(level);
    }
}