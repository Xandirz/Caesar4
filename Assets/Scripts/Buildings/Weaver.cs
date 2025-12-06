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
        
        workersRequired = 6;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Wool", 3 },
            { "Tools", 1 },
            { "Crafts", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Cloth", 6 }
        };
        
    }


    
    public override Dictionary<string, int> GetCostDict() => cost;
}