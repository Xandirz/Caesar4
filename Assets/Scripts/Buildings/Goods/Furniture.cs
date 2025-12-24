using System.Collections.Generic;
using UnityEngine;

public class Furniture : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Furniture; //!! ЗАПОЛНИТЬ
    
    public Furniture()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 5 },
            { "Rock", 1 },
            { "Tools", 1 },
        };
        
        workersRequired = 34;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Wood", 6 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Furniture", 50 }
        };

        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 4 },
            { "Wool", 2 },
            { "Leather", 2 },
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Furniture", 100 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Furniture2");
    }

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Furniture2";
        return base.GetResearchIdForLevel(level);
    }
}