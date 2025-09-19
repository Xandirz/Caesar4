using System.Collections.Generic;
using UnityEngine;

public class LumberMill : PlacedObject
{
    public float interval = 1f;
    private float timer = 0f;
    public int maxWood = 20;
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.LumberMill;
    private new Dictionary<string,int> cost = new()
    {
        { "Wood", 1 },
        { "People", 1 }
    };



    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            int currentWood = ResourceManager.Instance.GetResource("Wood");
            if (currentWood >= maxWood)
                return;

            ResourceManager.Instance.AddResource("Wood", 1);
        }
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