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
            { "People", 4 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Meat", 10 },
            { "Bone", 10 },
            { "Hide", 10 }
        };
 
        
        upgradeConsumption = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonus = new Dictionary<string, int>
        {
            { "Meat", 10 },
            { "Bone", 10 },
            { "Hide", 10 } 
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Hunter2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}