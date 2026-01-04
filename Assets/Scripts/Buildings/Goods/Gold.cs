using System;
using System.Collections.Generic;
using UnityEngine;

public class Gold : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Gold; //!! ЗАПОЛНИТЬ
    
    public Gold()
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
            { "GoldOre", 20 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Gold", 50 }
        };
        
        
    }
    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }




}