using System.Collections.Generic;
using UnityEngine;

public class Brewery : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Brewery; //!! ЗАПОЛНИТЬ
    
    public Brewery()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            { "People", 5 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 5 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Beer", 10 }
        };
        
    }


    
    public override Dictionary<string, int> GetCostDict() => cost;
}