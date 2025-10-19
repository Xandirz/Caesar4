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
            { "People", 4 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Tools", 25 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}