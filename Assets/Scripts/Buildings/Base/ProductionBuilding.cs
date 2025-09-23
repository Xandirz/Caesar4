using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    [SerializeField] protected int rate = 1;   // скорость производства
    public abstract string Resource { get; }
    public override abstract BuildManager.BuildMode BuildMode { get; }

    protected Dictionary<string,int> cost = new();

    public bool isActive = false;
    private GameObject stopSignInstance;

    public override Dictionary<string, int> GetCostDict() => cost;

    public override void OnPlaced()
    {
        ResourceProdManager.Instance.RegisterProducer(this);

        // Загружаем префаб из папки Resources (например, Resources/StopSign.prefab)
        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f; // чуть выше здания
            stopSignInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("StopSign prefab not found in Resources!");
        }
    }

    public override void OnRemoved()
    {
        if (isActive)
        {
            ResourceManager.Instance.UnregisterProducer(Resource, rate);
            isActive = false;
        }

        ResourceProdManager.Instance.UnregisterProducer(this);
        ResourceManager.Instance.RefundResources(cost);

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
            if (!isActive)
            {
                ResourceManager.Instance.RegisterProducer(Resource, rate);
                isActive = true;
                SetStopSign(false);
            }
        }
        else
        {
            if (isActive)
            {
                ResourceManager.Instance.UnregisterProducer(Resource, rate);
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
