using UnityEngine;
using System.Collections.Generic;

public class Well : PlacedObject
{
    public int waterRadius = 5; // радиус в клетках (полуразмер стороны зоны 10x10)
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Well;

    private new Dictionary<string,int> cost = new()
    {
        { "Rock", 1 },
    };

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    

    public override void OnPlaced()
    {
        base.OnPlaced();
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
    }
    
}
