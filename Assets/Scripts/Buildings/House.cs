using System.Collections.Generic;
using UnityEngine;

public class House : PlacedObject
{
    public int populationBonus = 5;
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    private new Dictionary<string,int> cost = new() { { "Wood", 1 } };

    public bool HasWater { get; private set; } = false;

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

    public override void OnPlaced()
    {
        ResourceManager.Instance.AddResource("People", populationBonus);

        // Дом потребляет еду (например, ягоды)
        ResourceManager.Instance.RegisterConsumer("Berry",1);
    }

    public override void OnRemoved()
    {
        ResourceManager.Instance.AddResource("People", -populationBonus);

        // Убираем потребление
        ResourceManager.Instance.UnregisterConsumer("Berry",1);

        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        base.OnRemoved();
    }

    public void SetWaterAccess(bool access)
    {
        HasWater = access;
        Debug.Log($"{name} water access: {access}");
    }
}