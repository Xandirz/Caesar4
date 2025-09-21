using System.Collections.Generic;
using UnityEngine;

public class Berry : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Berry;

    public string resource = "Berry";
    public int rate = 5;

    private new Dictionary<string,int> cost = new()
    {
        { "Wood", 1 },
        { "People", 1 }
    };

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

    public override void OnPlaced()
    {
        // Регистрируем как производитель дерева
        ResourceManager.Instance.RegisterProducer(resource,rate);
    }

    public override void OnRemoved()
    {
        // Убираем вклад в производство
        ResourceManager.Instance.UnregisterProducer(resource,rate);

        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        base.OnRemoved();
    }
}