using System;
using System.Collections.Generic;
using UnityEngine;

public class CopperOre : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.CopperOre; //!! ЗАПОЛНИТЬ
    
    public CopperOre()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Rock", 1 },
            { "Wood", 1 },
            { "People", 2 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 5 },
        };
        
        production = new Dictionary<string, int>
        {
            { "CopperOre", 20 }
        };
        
    }




    public override Dictionary<string, int> GetCostDict() => cost;
}