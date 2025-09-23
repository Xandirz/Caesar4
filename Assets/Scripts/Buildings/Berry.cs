using System.Collections.Generic;
using UnityEngine;

public class Berry : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Berry;

    public override string Resource => "Berry";

    private new Dictionary<string,int> cost = new()
    {
        { "Wood", 1 },
        { "People", 1 }
    };
    public override Dictionary<string, int> GetCostDict() => cost;
}