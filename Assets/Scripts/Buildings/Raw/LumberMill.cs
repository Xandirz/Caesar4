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
            { "Wood", 12 }
        };
  
        isNoisy = true;

        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Wood", 30 }
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Lumber2");
    }
    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2)
            return "LumberMill2";

        return base.GetResearchIdForLevel(level);
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
}