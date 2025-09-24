using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    [SerializeField] public int rate = 1;   // скорость производства
    public abstract string Resource { get; }
    public override abstract BuildManager.BuildMode BuildMode { get; }

    protected Dictionary<string,int> cost = new();
    public override Dictionary<string, int> GetCostDict() => cost;
    public bool isActive = false;
    private GameObject stopSignInstance;
    public Dictionary<string, int> consumptionCost = new(); // что нужно для работы (ресурс → количество)


    public override void OnPlaced()
    {
        ResourceProdManager.Instance.RegisterProducer(this);

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

        
        
        if (isActive)
        {
            ResourceManager.Instance.UnregisterProducer(Resource, rate);

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


   public void CheckProductionReq()
{
    if (hasRoadAccess)
    {
        // Проверяем хватает ли ресурсов
        bool canWork = true;
        if (consumptionCost != null && consumptionCost.Count > 0)
        {
            canWork = ResourceManager.Instance.CanSpend(consumptionCost);
        }

        if (canWork)
        {
            if (!isActive)
            {
                // Активируем
                ResourceManager.Instance.RegisterProducer(Resource, rate);

                if (consumptionCost != null && consumptionCost.Count > 0)
                {
                    foreach (var kvp in consumptionCost)
                        ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
                }

                isActive = true;
                SetStopSign(false);
            }

            // 🔥 Списываем ресурсы
            if (consumptionCost != null && consumptionCost.Count > 0)
            {
                ResourceManager.Instance.SpendResources(consumptionCost);
            }
        }
        else
        {
            // ❌ Не хватает ресурсов → выключаем производство
            if (isActive)
            {
                ResourceManager.Instance.UnregisterProducer(Resource, rate);

                if (consumptionCost != null && consumptionCost.Count > 0)
                {
                    foreach (var kvp in consumptionCost)
                        ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
                }

                isActive = false;
            }
            SetStopSign(true);
        }
    }
    else
    {
        // ❌ Нет дороги → выключаем
        if (isActive)
        {
            ResourceManager.Instance.UnregisterProducer(Resource, rate);

            if (consumptionCost != null && consumptionCost.Count > 0)
            {
                foreach (var kvp in consumptionCost)
                    ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
            }

            isActive = false;
        }
        SetStopSign(true);
    }
}

    private void SetStopSign(bool state)
    {
        if (stopSignInstance != null)
            stopSignInstance.SetActive(state);
    }
}
