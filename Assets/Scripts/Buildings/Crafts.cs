using System.Collections.Generic;
using UnityEngine;

public class Crafts : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Crafts; //!! ЗАПОЛНИТЬ
    
    public Crafts()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
            { "Rock", 1 },
            { "Hide", 1 },

        };
        
        workersRequired = 6;
        
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Bone", 10 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Crafts", 30 },
        };
        
 
        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 },
            { "Copper", 1 },
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Crafts", 30 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Crafts2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}