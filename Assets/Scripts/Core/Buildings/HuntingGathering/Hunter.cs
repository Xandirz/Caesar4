using System.Collections.Generic;
using UnityEngine;

public class Hunter : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Hunter; //!! ЗАПОЛНИТЬ
    
    public Hunter()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Tools", 1 },
        };
        
        workersRequired = 20;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Meat", 20 },
            { "Bone", 10 },
            { "Hide", 20 }
        };
 
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Meat", 5 },
            { "Hide", 5 } 
        };
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Salt", 1 }
        };
        
        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Meat", 5 },
            { "Hide", 5 } 
        };
    }
    private void Awake()
    {
        level3Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl3/Hunter3");
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Hunter2";
        if (level == 3) return "Hunter3";

        return base.GetResearchIdForLevel(level);
    }
    
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
}