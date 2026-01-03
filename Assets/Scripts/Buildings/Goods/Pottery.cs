using System.Collections.Generic;
using UnityEngine;

public class Pottery : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Pottery; //!! ЗАПОЛНИТЬ
    
    public Pottery()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
    
        };
        
        
        workersRequired = 12;
        consumptionCost = new Dictionary<string, int>
        {
            { "Clay", 3 },
            { "Wood", 2 },
        };
        production = new Dictionary<string, int>
        {
            { "Pottery", 50 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 } 
        };
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Pottery", 60 }
        };
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Glass", 1 } 
        };
        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Pottery", 60 }
        };
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Pottery2");
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl3/Pottery3");
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Pottery2";
        if (level == 3) return "Pottery3";
        return base.GetResearchIdForLevel(level);
    }
}