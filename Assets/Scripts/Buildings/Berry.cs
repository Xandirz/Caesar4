using System.Collections.Generic;
using UnityEngine;

public class Berry : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Berry;


    public Berry()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "People", 3 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Berry", 12 }
        };
    }
    public override Dictionary<string, int> GetCostDict() => cost;
}