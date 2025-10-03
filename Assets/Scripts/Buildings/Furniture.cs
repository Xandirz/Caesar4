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
            { "People", 4 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wood", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Furniture", 10 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}