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
            { "Bone", 20 },
            { "Hide", 20 }
        };
 
        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Meat", 20 },
            { "Bone", 20 },
            { "Hide", 20 } 
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Hunter2");
    }
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2)
            return "Hunter2";

        return base.GetResearchIdForLevel(level);
    }
    
    public override Dictionary<string, int> GetCostDict() => cost;
}