using System;
using System.Collections.Generic;
using UnityEngine;

public class Sand : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Sand; //!! ЗАПОЛНИТЬ
    
    public Sand()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Rock", 1 },
            { "Wood", 1 },
        };
        
        workersRequired = 5;
        
        isNoisy = true;

        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 5 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Sand", 20 }
        };
        
        
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }




}