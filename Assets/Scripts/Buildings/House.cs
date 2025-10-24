﻿using UnityEngine;
using System.Collections.Generic;

public class House : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    [Header("Settings")]
    public int startPopulation = 3;   // начальная численность
    public int addPopulationLevel2 = 3;
    public int addPopulationLevel3 = 4;

    [Header("Sprites")]
    public Sprite house1Sprite;
    public Sprite house2Sprite;
    public Sprite house3Sprite;

    private SpriteRenderer sr;
    private new Dictionary<string, int> cost = new() { { "Wood", 1 } };

    public bool needsAreMet;
    public bool reservedForUpgrade = false;

    private GameObject angryPrefab;
    private GameObject spawnedHuman;

    public bool HasWater { get; private set; } = false;
    public bool HasMarket { get; private set; } = false;
    public int CurrentStage { get; private set; } = 1;
    public int currentPopulation = 0;

    public GameObject humanPrefab;
    private GridManager gridManager;
    private bool humanSpawned = false;

    // === ПОТРЕБЛЕНИЕ ===
    [Header("Consumption")]
    public Dictionary<string, int> consumption = new() { { "Berry", 1 } };

    [Header("Level 2 Additional Consumption")]
    public Dictionary<string, int> consumptionLvl2 = new()
    {
        { "Wood", 1 },
        { "Meat", 1 },
        { "Hide", 1 },
    };

    [Header("Level 3 Additional Consumption")]
    public Dictionary<string, int> consumptionLvl3 = new()
    {
        { "Crafts", 1 },
        { "Furniture", 1 },
        { "Bread", 1 },
        { "Beans", 1 },
        { "Beer", 1 },
        { "Cheese", 1 },
        { "Clothes", 1 },
        { "Coal", 1 },
    };

    public override Dictionary<string, int> GetCostDict() => cost;

    // === Постройка ===
    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        currentPopulation = startPopulation;
        ResourceManager.Instance.AddResource("People", currentPopulation);

        foreach (var kvp in consumption)
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

        AllBuildingsManager.Instance.RegisterHouse(this);

        angryPrefab = Resources.Load<GameObject>("angry");
        if (angryPrefab != null)
        {
            angryPrefab = Instantiate(angryPrefab, transform);
            angryPrefab.transform.localPosition = Vector3.up * 0f;
            angryPrefab.SetActive(false);
        }
    }

    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        if (Random.Range(0, 10) > 7)
        {
            humanPrefab = Resources.Load<GameObject>("human");
            float delay = Random.Range(1f, 3f);
            Invoke(nameof(TrySpawnHuman), delay);
        }

    }

    private void TrySpawnHuman()
    {
        if (humanSpawned || humanPrefab == null || gridManager == null) return;
        Vector3 spawnPos = gridManager.GetWorldPositionFromGrid(gridPos);
        spawnedHuman = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
        spawnedHuman.GetComponent<Human>().Initialize(gridManager);
        humanSpawned = true;
    }

    // === Удаление ===
    public override void OnRemoved()
    {
        // просто вычитаем текущую численность
        ResourceManager.Instance.AddResource("People", -currentPopulation);

        // снимаем всё текущее потребление
        foreach (var kvp in consumption)
            ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

        manager?.SetOccupied(gridPos, false);
        AllBuildingsManager.Instance?.UnregisterHouse(this);

        if (spawnedHuman != null)
        {
            Destroy(spawnedHuman);
            spawnedHuman = null;
        }

        base.OnRemoved();
        ResourceManager.Instance.UpdateGlobalMood();
    }

    // === Проверка нужд ===
    public bool CheckNeeds()
    {
        bool allSatisfied = true;
        BuildManager.Instance?.CheckEffects(this);

        foreach (var cost in consumption)
        {
            int available = ResourceManager.Instance.GetResource(cost.Key);
            if (available >= cost.Value)
                ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
            else
                allSatisfied = false;
        }

        if (!hasRoadAccess)
        {
            ApplyNeedsResult(false);
            return false;
        }

        if (CurrentStage >= 2 && !HasWater)
        {
            ApplyNeedsResult(false);
            return false;
        }

        if (CurrentStage >= 3 && !HasMarket)
        {
            ApplyNeedsResult(false);
            return false;
        }

        ApplyNeedsResult(allSatisfied);
        return allSatisfied;
    }

    public void ApplyNeedsResult(bool satisfied)
    {
        needsAreMet = satisfied;
        if (angryPrefab != null)
            angryPrefab.SetActive(!satisfied);
        ResourceManager.Instance.UpdateGlobalMood();
    }

    public void SetWaterAccess(bool access)
    {
        HasWater = access;
        ResourceManager.Instance.UpdateGlobalMood();
    }

    public void SetMarketAccess(bool access)
    {
        HasMarket = access;
    }

    // === Автоматическое улучшение ===
    public bool TryAutoUpgrade()
    {
        if (!needsAreMet || !hasRoadAccess)
            return false;

        // === 1 → 2 ===
        if (CurrentStage == 1 && reservedForUpgrade)
        {
            CurrentStage = 2;
            sr.sprite = house2Sprite;

            currentPopulation += addPopulationLevel2;
            ResourceManager.Instance.AddResource("People", addPopulationLevel2);

            foreach (var kvp in consumptionLvl2)
            {
                if (consumption.ContainsKey(kvp.Key))
                    consumption[kvp.Key] += kvp.Value;
                else
                    consumption[kvp.Key] = kvp.Value;

                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
            }

            reservedForUpgrade = false;
            AllBuildingsManager.Instance.RecheckAllHousesUpgrade();
            return true;
        }

        // === 2 → 3 ===
        if (CurrentStage == 2 && reservedForUpgrade)
        {
            CurrentStage = 3;
            sr.sprite = house3Sprite;

            currentPopulation += addPopulationLevel3;
            ResourceManager.Instance.AddResource("People", addPopulationLevel3);

            foreach (var kvp in consumptionLvl3)
            {
                if (consumption.ContainsKey(kvp.Key))
                    consumption[kvp.Key] += kvp.Value;
                else
                    consumption[kvp.Key] = kvp.Value;

                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
            }

            reservedForUpgrade = false;
            AllBuildingsManager.Instance.RecheckAllHousesUpgrade();
            return true;
        }

        return false;
    }

    public bool CanAutoUpgrade()
    {
        if (!needsAreMet || !hasRoadAccess || !HasWater)
            return false;
        if (CurrentStage >= 2 && !HasMarket)
            return false;
        return CurrentStage == 1 || CurrentStage == 2;
    }
}
