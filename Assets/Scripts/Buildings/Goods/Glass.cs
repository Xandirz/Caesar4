using System;
using System.Collections.Generic;
using UnityEngine;

public class Glass : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Glass; //!! ЗАПОЛНИТЬ
    
    public Glass()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Rock", 1 },
            { "Wood", 1 },
        };

        workersRequired = 12;
        isNoisy = true;

        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 },
            { "Sand", 10 },
            { "Charcoal", 5 },
            
        };
        
        production = new Dictionary<string, int>
        {
            { "Glass", 100 }
        };
        

    }



    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}