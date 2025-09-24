using System.Collections.Generic;
using UnityEngine;

public class Clay : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clay;
    public override string Resource => "Clay";
    
    public Clay()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "People", 1 }
        };
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}