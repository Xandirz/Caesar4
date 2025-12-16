using System.Collections.Generic;
using UnityEngine;

public class Clothes : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clothes; //!! ЗАПОЛНИТЬ
    
    public Clothes()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            
        };
        
        workersRequired = 18;
        
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Cloth", 6 },
            { "Tools", 1 },
            { "Crafts", 1 },
         
            
        };
        
        production = new Dictionary<string, int>
        {
            { "Clothes", 30 }
        };
        
 
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Cloth", 5 },
            { "Linen", 5 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Clothes", 10 } // прирост выпуска одежды на lvl2
        };

    }
    
    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Clothes2";
        return base.GetResearchIdForLevel(level);
    }

    
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Clothes2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}