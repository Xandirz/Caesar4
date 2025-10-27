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
            { "Rock", 1 },
            { "Wood", 1 },
        };
        
        workersRequired = 2;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 5 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Copper", 20 }
        };
        
    }



    public override Dictionary<string, int> GetCostDict() => cost;
}