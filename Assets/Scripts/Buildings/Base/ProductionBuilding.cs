using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    public override abstract BuildManager.BuildMode BuildMode { get; }
    
    [SerializeField] protected bool requiresRoadAccess = true; // ✅ новое поле


    [SerializeField] protected Dictionary<string, int> cost = new();
    [SerializeField] public Dictionary<string, int> production = new();
    [SerializeField] public Dictionary<string, int> consumptionCost = new();

    public override Dictionary<string, int> GetCostDict() => cost;

    public bool isActive = false;
    public bool needsAreMet;

    private GameObject stopSignInstance;
    private GameObject upgradePrefab;
    private SpriteRenderer sr;

    // === Улучшение ===
    public int CurrentStage { get; private set; } = 1;
    public Dictionary<string, int> upgradeCost = new();
    public Dictionary<string, int> upgradeProductionBonus = new();
    public Dictionary<string, int> upgradeConsumption = new();
    public Sprite level2Sprite;

    private float checkUpgradeTimer = 0f;
    private const float upgradeCheckInterval = 1f; // проверяем раз в секунду

    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterProducer(this);
        ApplyNeedsResult(CheckNeeds());

        // Загружаем иконки
        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f;
            stopSignInstance.SetActive(false);
        }

        upgradePrefab = Resources.Load<GameObject>("upgrade");
        if (upgradePrefab != null)
        {
            upgradePrefab = Instantiate(upgradePrefab, transform);
            upgradePrefab.transform.localPosition = Vector3.up * 0f;
            upgradePrefab.SetActive(false);
        }

        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // 🔹 Каждую секунду проверяем, можно ли улучшить
        checkUpgradeTimer += Time.deltaTime;
        if (checkUpgradeTimer >= upgradeCheckInterval)
        {
            checkUpgradeTimer = 0f;
            if (CurrentStage == 1)
                CanUpgrade(); // обновит состояние стрелки
        }
    }

    public override void OnRemoved()
    {
        ResourceManager.Instance.RefundResources(cost);
        AllBuildingsManager.Instance.UnregisterProducer(this);

        if (isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);

            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;
        }

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        if (stopSignInstance != null)
            Destroy(stopSignInstance);
        if (upgradePrefab != null)
            Destroy(upgradePrefab);

        base.OnRemoved();
    }

    // === Проверка доступности ресурсов и дорог ===
    public bool CheckNeeds()
    {
        if (requiresRoadAccess && !hasRoadAccess)
        {
            needsAreMet = false;
            return false;
        }

        foreach (var cost in consumptionCost)
        {
            if (ResourceManager.Instance.GetResource(cost.Key) < cost.Value)
            {
                needsAreMet = false;
                return false;
            }
        }

        foreach (var cost in consumptionCost)
            ResourceManager.Instance.SpendResource(cost.Key, cost.Value);

        needsAreMet = true;

        foreach (var kvp in production)
            ResourceManager.Instance.AddResource(kvp.Key, kvp.Value);

        return true;
    }

    public void ApplyNeedsResult(bool satisfied)
    {
        if (satisfied)
        {
            if (!isActive)
            {
                foreach (var kvp in production)
                    ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);

                foreach (var kvp in consumptionCost)
                    ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

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
                    ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);

                foreach (var kvp in consumptionCost)
                    ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

                isActive = false;
            }
        }
    }

    private void SetStopSign(bool state)
    {
        if (stopSignInstance != null)
            stopSignInstance.SetActive(state);
    }

    // === Проверка возможности апгрейда ===
    public bool CanUpgrade()
    {
        if (CurrentStage == 1 && upgradeCost != null && upgradeCost.Count > 0)
        {
            bool can = ResourceManager.Instance.CanSpend(upgradeCost);
            if (upgradePrefab != null)
                upgradePrefab.SetActive(can); // 🔹 теперь визуально обновляется
            return can;
        }

        if (upgradePrefab != null)
            upgradePrefab.SetActive(false);

        return false;
    }

    // === Улучшение ===
    public bool TryUpgrade()
    {
        if (CurrentStage != 1)
            return false;

        if (!CanUpgrade())
            return false;

        // Списываем ресурсы
        ResourceManager.Instance.SpendResources(upgradeCost);

        CurrentStage = 2;

        // ✅ Получаем SpriteRenderer
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        // ⚡ Сохраняем слой и порядок
        string oldLayer = sr.sortingLayerName;
        int oldOrder = sr.sortingOrder;

        // Меняем спрайт
        if (level2Sprite != null)
            sr.sprite = level2Sprite;

        // ✅ Возвращаем старый слой и порядок
        sr.sortingLayerName = oldLayer;
        sr.sortingOrder = oldOrder;

        // Производство
        foreach (var kvp in upgradeProductionBonus)
        {
            if (production.ContainsKey(kvp.Key))
                production[kvp.Key] += kvp.Value;
            else
                production[kvp.Key] = kvp.Value;

            ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
        }

        // Потребление
        foreach (var kvp in upgradeConsumption)
        {
            if (consumptionCost.ContainsKey(kvp.Key))
                consumptionCost[kvp.Key] += kvp.Value;
            else
                consumptionCost[kvp.Key] = kvp.Value;

            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
        }

        // Скрываем стрелку улучшения
        if (upgradePrefab != null)
            upgradePrefab.SetActive(false);

        Debug.Log($"{name} улучшен до уровня 2!");
        return true;
    }

}
