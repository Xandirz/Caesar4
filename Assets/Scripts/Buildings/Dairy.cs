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
            { "Milk", 10 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Cheese", 20 },
            { "Yogurt", 20 },
            
        };
        
    }


    
    public override Dictionary<string, int> GetCostDict() => cost;
}