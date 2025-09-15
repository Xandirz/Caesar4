using System.Collections.Generic;
using UnityEngine;

public class Road : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Road;
    private new Dictionary<string,int> cost = new()
    {
        { "Wood", 1 },
       
    };

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }


    public override void OnPlaced() { }

    public override void OnRemoved()
    {
        ResourceManager.Instance.RefundResources(cost);
        if (manager != null)
            manager.SetOccupied(gridPos, false);

        Destroy(gameObject);
    }
}