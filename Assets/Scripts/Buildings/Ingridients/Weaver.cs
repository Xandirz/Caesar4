using System.Collections.Generic;
using UnityEngine;

public class Weaver : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Weaver; //!! ЗАПОЛНИТЬ
    
    public Weaver()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
        };
        
        workersRequired = 18;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Wool", 3 },
            { "Tools", 1 },
            { "Crafts", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Cloth", 12 }
        };
        
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Flax", 3 },   
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Cloth", 5 },  
            { "Linen", 5 }  
        };
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Salt", 1 },   
        };

        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Cloth", 5 },  
            { "Linen", 5 }  
        };
        
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Weaver2";
        if (level == 3) return "Weaver3";
        return base.GetResearchIdForLevel(level);
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Weaver2");
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl3/Weaver3");
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}