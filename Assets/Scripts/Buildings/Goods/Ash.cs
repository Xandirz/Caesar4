using System;
using System.Collections.Generic;
using UnityEngine;

public class Ash : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Ash; //!! ЗАПОЛНИТЬ
    
    public Ash()
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
            { "Wood", 6 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Charcoal", 10 },
            { "Ash", 100 },
        };
        

    }



    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

}