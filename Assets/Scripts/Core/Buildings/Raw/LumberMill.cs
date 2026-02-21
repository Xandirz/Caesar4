using System.Collections.Generic;
using UnityEngine;

public class LumberMill : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.LumberMill;

    // ⚡ Стоимость задаём прямо здесь
    public LumberMill()
    {
        cost = new Dictionary<string,int>
        {
        };
        
        workersRequired = 8;
        
        production = new Dictionary<string, int>
        {
            { "Wood", 15 }
        };
  
        isNoisy = true;

        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Wood", 40 }
        };
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Wood", 60 }
        };
    }

    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2)
            return "LumberMill2";
        if (level == 3)
            return "LumberMill3";
        return base.GetResearchIdForLevel(level);
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
}