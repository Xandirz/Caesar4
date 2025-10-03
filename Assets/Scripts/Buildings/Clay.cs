using System.Collections.Generic;
using UnityEngine;

public class Clay : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clay;
    
    public Clay()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
            { "People", 5 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Clay", 15 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}