using System.Collections.Generic;
using UnityEngine;

public class Pottery : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Pottery; //!! ЗАПОЛНИТЬ
    
    public Pottery()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
    
        };
        
        
        workersRequired = 12;
        consumptionCost = new Dictionary<string, int>
        {
            { "Clay", 3 },
            { "Wood", 2 },
        };
        production = new Dictionary<string, int>
        {
            { "Pottery", 60 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}