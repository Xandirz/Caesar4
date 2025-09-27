using System.Collections.Generic;
using UnityEngine;

public class Crafts : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Crafts; //!! ЗАПОЛНИТЬ
    
    public Crafts()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            { "Hide", 1 },
            { "People", 1 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Bone", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Crafts", 5 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}