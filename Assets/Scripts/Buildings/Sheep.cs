using System.Collections.Generic;
using UnityEngine;

public class Sheep : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Sheep; //!! ЗАПОЛНИТЬ
    
    public Sheep()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Tools", 1 },
            { "People", 2 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Sheep", 1 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}