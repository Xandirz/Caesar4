﻿using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    public override abstract BuildManager.BuildMode BuildMode { get; }

    [SerializeField] protected bool requiresRoadAccess = true;

    [Header("Economy")]
    [SerializeField] public Dictionary<string, int> production = new();       // текущее производство
    [SerializeField] public Dictionary<string, int> consumptionCost = new();  // текущее потребление

    [Header("Upgrade Level 1")]
    public Dictionary<string, int> upgradeProductionBonusLevel1 = new();
    public Dictionary<string, int> upgradeConsumptionLevel1 = new();
    public Sprite level2Sprite;

    [SerializeField] protected Dictionary<string, int> cost = new();
    public override Dictionary<string, int> GetCostDict() => cost;

    public bool isActive = false;
    public bool needsAreMet;
    public int CurrentStage { get; private set; } = 1;

    private GameObject stopSignInstance;
    private SpriteRenderer sr;

    // === Новая система рабочих ===
    [Header("Workforce")]
    [SerializeField] public int workersRequired = 0;
    private bool workersAllocated = false;

    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterProducer(this);

        // значок «стоп»
        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f;
        }

        sr = GetComponent<SpriteRenderer>();
        ApplyNeedsResult(CheckNeeds());
        if (stopSignInstance != null)
            stopSignInstance.SetActive(!needsAreMet);
    }

    public override void OnRemoved()
    {
        // снимаем текущее производство и потребление
        if (isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;
        }

        // === освобождаем рабочих при сносе ===
        if (workersAllocated)
        {
            ResourceManager.Instance.ReleaseWorkers(this);
            workersAllocated = false;
        }

        AllBuildingsManager.Instance?.UnregisterProducer(this);
        ResourceManager.Instance.RefundResources(cost);
        manager?.SetOccupied(gridPos, false);

        if (stopSignInstance != null)
            Destroy(stopSignInstance);

        base.OnRemoved();
    }

    // ===== Проверка и производство =====
    public bool CheckNeeds()
    {
        if (requiresRoadAccess && !hasRoadAccess)
        {
            ApplyNeedsResult(false);
            return false;
        }

        foreach (var cost in consumptionCost)
        {
            if (ResourceManager.Instance.GetResource(cost.Key) < cost.Value)
            {
                ApplyNeedsResult(false);
                return false;
            }
        }

        // списываем ресурсы
        foreach (var cost in consumptionCost)
            ResourceManager.Instance.SpendResource(cost.Key, cost.Value);

        // производим
        foreach (var kvp in production)
            ResourceManager.Instance.AddResource(kvp.Key, kvp.Value);

        TryAutoUpgrade();

        ApplyNeedsResult(true);
        return true;
    }

    public void ApplyNeedsResult(bool satisfied)
    {
        // === Добавляем проверку рабочих ===
        bool wantsToBeActive = satisfied;

        if (wantsToBeActive && !workersAllocated)
        {
            workersAllocated = ResourceManager.Instance.TryAllocateWorkers(this, workersRequired);
            if (!workersAllocated)
            {
                wantsToBeActive = false;
            }
        }

        needsAreMet = wantsToBeActive;

        if (wantsToBeActive && !isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

            isActive = true;
            if (stopSignInstance != null) stopSignInstance.SetActive(false);
        }
        else if (!wantsToBeActive && isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;
            if (stopSignInstance != null) stopSignInstance.SetActive(true);

            // освобождаем рабочих при остановке
            if (workersAllocated)
            {
                ResourceManager.Instance.ReleaseWorkers(this);
                workersAllocated = false;
            }
        }
    }

    // метод, вызываемый менеджером при нехватке населения
    public void ForceStopDueToNoWorkers()
    {
        if (isActive || workersAllocated)
        {
            ApplyNeedsResult(false);
        }
    }

    // ===== Апгрейд =====
    private void TryAutoUpgrade()
    {
        if (CurrentStage != 1) return;
        if (upgradeProductionBonusLevel1.Count == 0 && upgradeConsumptionLevel1.Count == 0) return;
        if (requiresRoadAccess && !hasRoadAccess) return;

        // проверяем профицит ресурсов
        foreach (var kvp in upgradeConsumptionLevel1)
        {
            float prod = ResourceManager.Instance.GetProduction(kvp.Key);
            float cons = ResourceManager.Instance.GetConsumption(kvp.Key);
            if (prod - cons < kvp.Value)
                return;
        }

        // === Выполняем улучшение ===
        CurrentStage = 2;

        // добавляем бонусы прямо в словари
        foreach (var kvp in upgradeProductionBonusLevel1)
        {
            if (production.ContainsKey(kvp.Key))
                production[kvp.Key] += kvp.Value;
            else
                production[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in upgradeConsumptionLevel1)
        {
            if (consumptionCost.ContainsKey(kvp.Key))
                consumptionCost[kvp.Key] += kvp.Value;
            else
                consumptionCost[kvp.Key] = kvp.Value;
        }

        // если здание активно — перерегистрируем новое значение
        if (isActive)
        {
            foreach (var kvp in upgradeProductionBonusLevel1)
                ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in upgradeConsumptionLevel1)
                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
        }

        // визуал
        if (level2Sprite != null)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = level2Sprite;
        }

        Debug.Log($"{name} улучшено до уровня 2!");
    }
}
