using System.Collections.Generic;
using UnityEngine;

public class Weaver : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Weaver; //!! ЗАПОЛНИТЬ
    
    public Weaver()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            { "People", 5 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wool", 5 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Cloth", 10 }
        };
        
    }


    
    public override Dictionary<string, int> GetCostDict() => cost;
}