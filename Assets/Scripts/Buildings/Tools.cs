using System.Collections.Generic;
using UnityEngine;

public class Tools : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Tools; //!! ЗАПОЛНИТЬ
    
    public Tools()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 5 },
            { "Rock", 2 },
        };
        
        workersRequired = 16;
                
        consumptionCost = new Dictionary<string, int>
        {
            { "Wood", 2 },
            { "Rock", 2 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Tools", 40 }
        };
        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Copper", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Tools", 50 }  
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}