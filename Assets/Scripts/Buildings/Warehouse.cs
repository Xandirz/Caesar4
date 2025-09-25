using UnityEngine;
using System.Collections.Generic;

public class Warehouse : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Warehouse;
    public override int SizeX => 2;
    public override int SizeY => 2;

    
    private int capacityBonus = 20;


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
        // Увеличиваем максимальное хранилище всех ресурсов
        ResourceManager.Instance.IncreaseMaxAll(capacityBonus);
    }
    
    public override void OnRemoved()
    {
        // Убираем бонус
        ResourceManager.Instance.DecreaseMaxAll(capacityBonus);

        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        base.OnRemoved();
    }
    
}