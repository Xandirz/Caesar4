using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    [SerializeField] public Dictionary<string, int> production = new();

    public override abstract BuildManager.BuildMode BuildMode { get; }

    protected Dictionary<string,int> cost = new();
    public override Dictionary<string, int> GetCostDict() => cost;
    public bool isActive = false;
    public bool needsAreMet;
    private GameObject stopSignInstance;
    public Dictionary<string, int> consumptionCost = new(); // что нужно для работы (ресурс → количество)


    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterProducer(this);
        ApplyNeedsResult(CheckNeeds());

        // Загружаем префаб из папки Resources (например, Resources/stop.prefab)
        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f;
            stopSignInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("StopSign prefab not found in Resources!");
        }
    }

    public override void OnRemoved()
    {
        
        ResourceManager.Instance.RefundResources(cost);
        AllBuildingsManager.Instance.UnregisterProducer(this);

        
        
        if (isActive)
        {
            foreach (var kvp in production)
            {
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            }

            if (consumptionCost != null && consumptionCost.Count > 0)
            {
                foreach (var kvp in consumptionCost)
                    ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
            }

            isActive = false;
        }

       

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        if (stopSignInstance != null)
            Destroy(stopSignInstance);

        base.OnRemoved();
    }


    
    public bool CheckNeeds()
    {
        if (!hasRoadAccess)
        {
            needsAreMet = false;
            return false;
        }

        foreach (var cost in consumptionCost)
        {
            if (ResourceManager.Instance.GetResource(cost.Key) < cost.Value)
            {
                needsAreMet = false;
                return false; // не хватает хотя бы одного
            }
        }

        // Если дошли сюда — ресурсов хватает → списываем
        foreach (var cost in consumptionCost)
        {
            ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
            
        }
        

        needsAreMet = true;
        
        foreach (var kvp in production)
        {
            ResourceManager.Instance.AddResource(kvp.Key, kvp.Value);
        }

        return true;
    }
    
    
    public void ApplyNeedsResult(bool satisfied)
    {
        if (satisfied)
        {
            if (!isActive)
            {
                foreach (var kvp in production)
                {
                    ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
                }
                if (consumptionCost != null && consumptionCost.Count > 0)
                {
                    foreach (var kvp in consumptionCost)
                        ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
                }
                isActive = true;
                SetStopSign(false);
            }
        }
        else
        {
            SetStopSign(true);

            
            if (isActive)
            {
                foreach (var kvp in production)
                {
                    ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
                }

                if (consumptionCost != null && consumptionCost.Count > 0)
                {
                    foreach (var kvp in consumptionCost)
                        ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
                }
                
                isActive = false;

            }
            
        }
    }
    

    private void SetStopSign(bool state)
    {
        if (stopSignInstance != null)
            stopSignInstance.SetActive(state);
    }
}
