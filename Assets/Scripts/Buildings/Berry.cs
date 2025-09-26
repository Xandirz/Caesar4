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
            { "People", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Berry", 5 }
        };
    }
    public override Dictionary<string, int> GetCostDict() => cost;
}