using System.Collections.Generic;
using UnityEngine;

public class Flour : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Flour; //!! ЗАПОЛНИТЬ
    
    public Flour()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            { "Tools", 1 },
            { "People", 2 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Flour", 5 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}