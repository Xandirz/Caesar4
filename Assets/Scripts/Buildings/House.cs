using System.Collections.Generic;
using UnityEngine;

public class House : PlacedObject
{
    public int populationBonus = 5;
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;
    private new Dictionary<string,int> cost =new() { { "Wood", 1 } };
    public bool HasWater { get; private set; } = false;
    // статическая стоимость для проверки перед созданием


    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    public override void OnPlaced()
    {
        ResourceManager.Instance.AddResource("People", populationBonus);
    }

    public override void OnRemoved()
    {
        ResourceManager.Instance.AddResource("People", -populationBonus);
        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        Destroy(gameObject);
    }
    
    
    public void SetWaterAccess(bool access)
    {
        HasWater = access;
        // Можно добавить визуальный эффект или информацию
        Debug.Log($"{name} water access: {access}");
    }
}