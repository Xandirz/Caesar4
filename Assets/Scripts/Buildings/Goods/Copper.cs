using System;
using System.Collections.Generic;
using UnityEngine;

public class Copper : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Copper; //!! ЗАПОЛНИТЬ
    
    public Copper()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "Clay", 5 },
            { "Rock", 5 },
            { "Wood", 1 },
        };
        
        workersRequired = 5;
        
        isNoisy = true;

        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 5 },
            { "Charcoal", 5 },
            { "CopperOre", 20 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Copper", 40 }
        };
        
        
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }




}