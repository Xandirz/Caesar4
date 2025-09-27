using System.Collections.Generic;
using UnityEngine;

public class Clothes : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clothes; //!! ЗАПОЛНИТЬ
    
    public Clothes()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            { "People", 1 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Hide", 1 },
            { "Bone", 1 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Clothes", 5 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}