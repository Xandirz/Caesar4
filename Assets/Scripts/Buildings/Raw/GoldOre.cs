using System;
using System.Collections.Generic;
using UnityEngine;

public class GoldOre : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.GoldOre; //!! ЗАПОЛНИТЬ
    
    public GoldOre()
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
            { "GoldOre", 20 }
        };
        
        
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }




}