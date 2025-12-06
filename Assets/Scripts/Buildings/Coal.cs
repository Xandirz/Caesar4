using System;
using System.Collections.Generic;
using UnityEngine;

public class Coal : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Coal; //!! ЗАПОЛНИТЬ
    
    public Coal()
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
            { "Coal", 40 }
        };
        
    }

 


    public override Dictionary<string, int> GetCostDict() => cost;
}