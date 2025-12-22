using System.Collections.Generic;
using UnityEngine;

public class Fish : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Fish; //!! ЗАПОЛНИТЬ
    

    public Fish()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
        };

        needWaterNearby = true;
        
        
        workersRequired = 3;
        
        consumptionCost = new Dictionary<string, int>
        {
            
        };
        
        production = new Dictionary<string, int>
        {
            { "Fish", 10 }
        };
        
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Fish", 30 }  
        };


    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Fish2");
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2)
            return "Fish2";

        return base.GetResearchIdForLevel(level);
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}