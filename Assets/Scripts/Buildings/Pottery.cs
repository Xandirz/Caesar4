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
        
        workersRequired = 5;
        consumptionCost = new Dictionary<string, int>
        {
            { "Clay", 10 },
            { "Wood", 1 },
        };
        production = new Dictionary<string, int>
        {
            { "Pottery", 60 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}