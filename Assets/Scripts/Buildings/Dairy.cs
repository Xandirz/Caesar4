using System.Collections.Generic;
using UnityEngine;

public class Dairy : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Dairy; //!! ЗАПОЛНИТЬ
    
    public Dairy()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            { "People", 5 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Milk", 5 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Cheese", 10 },
            { "Yogurt", 10 },
            
        };
        
    }


    
    public override Dictionary<string, int> GetCostDict() => cost;
}