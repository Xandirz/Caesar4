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
        
        workersRequired = 5;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Bone", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Crafts", 10 },
            { "Needles", 1 },
        };
        
 
        
        upgradeConsumptionLevel1 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel1 = new Dictionary<string, int>
        {
            { "Crafts", 10 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Crafts2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}