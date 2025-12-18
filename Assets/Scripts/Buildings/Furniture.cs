using System.Collections.Generic;
using UnityEngine;

public class Furniture : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Furniture; //!! ЗАПОЛНИТЬ
    
    public Furniture()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 5 },
            { "Rock", 1 },
            { "Tools", 1 },
        };
        
        workersRequired = 34;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Wood", 6 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Furniture", 50 }
        };

        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 2 },
            { "Copper", 2 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Furniture", 80 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Furniture2");
    }

    

}