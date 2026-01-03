using System;
using System.Collections.Generic;
using UnityEngine;

public class Salt : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Salt; //!! ЗАПОЛНИТЬ
    
    public Salt()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Wood", 1 },
        };
        
        workersRequired = 12;
        
        isNoisy = true;

        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 },
            { "Charcoal", 1 },
            { "Metalware", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Salt", 100 }
        };
        
        
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }




}