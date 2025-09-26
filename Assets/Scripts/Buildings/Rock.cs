using System.Collections.Generic;
using UnityEngine;

public class Rock : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Rock;
    
    public Rock()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "People", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Rock", 1 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}