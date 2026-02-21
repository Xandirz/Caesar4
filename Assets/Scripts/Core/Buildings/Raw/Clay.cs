using System.Collections.Generic;
using UnityEngine;

public class Clay : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clay;
    
    public Clay()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
          
        };
        
        workersRequired = 12;
        isNoisy = true;
        needWaterNearby = true;
        
        production = new Dictionary<string, int>
        {
            { "Clay", 4 },
         
        };
        

        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Clay", 4 }  
        };
    }

    
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Clay2";
        return base.GetResearchIdForLevel(level);
    }
}