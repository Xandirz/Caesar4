using System;
using System.Collections.Generic;
using UnityEngine;

public class Jewelry : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Jewelry; //!! ЗАПОЛНИТЬ
    
    public Jewelry()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Brick", 5 },
            { "Wood", 1 },
        };
        
        workersRequired = 5;
        
        isNoisy = true;

        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 5 },
            { "Charcoal", 5 },
            { "Gold", 20 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Jewelry", 40 }
        };
        
        
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }




}