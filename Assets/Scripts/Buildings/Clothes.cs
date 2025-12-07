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
            { "Tools", 1 },
            { "Copper", 1 },
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Clothes", 30 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Clothes2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}