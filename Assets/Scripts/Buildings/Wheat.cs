using System.Collections.Generic;
using UnityEngine;

public class Wheat : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Wheat; //!! ЗАПОЛНИТЬ
    
    public Wheat()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
            { "People", 2 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Wheat", 20 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}