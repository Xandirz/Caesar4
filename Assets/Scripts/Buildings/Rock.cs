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
        };
        
        workersRequired = 12;

        isNoisy = true;
        
        production = new Dictionary<string, int>
        {
            { "Rock", 6 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}