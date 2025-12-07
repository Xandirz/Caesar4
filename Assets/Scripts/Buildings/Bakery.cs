using System.Collections.Generic;
using UnityEngine;

public class Bakery : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Bakery; //!! ЗАПОЛНИТЬ
    
    public Bakery()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
        };
        
        workersRequired = 12;
        
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Flour", 3 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Bread", 30 }
        };
        
    }


    
    public override Dictionary<string, int> GetCostDict() => cost;
}