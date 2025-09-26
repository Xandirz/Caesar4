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
            { "People", 1 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Meat", 5 },
            { "Bone", 3 },
            { "Hide", 3 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}