using UnityEngine;
using System.Collections.Generic;

public class House : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    [Header("Settings")]
    public int basePopulation = 3;
    public int upgradePopulation = 3;
    public int upgradePopulationLevel3 = 4;

    [Header("Sprites")]
    public Sprite house1Sprite;
    public Sprite house2Sprite;
    public Sprite house3Sprite;

    private SpriteRenderer sr;
    private new Dictionary<string, int> cost = new() { { "Wood", 1 } };

    public bool needsAreMet;
    public bool reservedForUpgrade = false; // резерв для автоапгрейда

    private GameObject angryPrefab;
    private GameObject spawnedHuman;

    public bool HasWater { get; private set; } = false;
    public int CurrentStage { get; private set; } = 1;
    public bool HasMarket { get; private set; } = false;

    public GameObject humanPrefab;
    private GridManager gridManager;
    private bool humanSpawned = false;

    public int ResourceСapacityBonus = 1;

    // === ПОТРЕБЛЕНИЕ ===
    [Header("Consumption")]
    public Dictionary<string, int> consumptionCost = new() { { "Berry", 1 } };

    [Header("Level 2 Consumption")]
    public Dictionary<string, int> consumptionLvl2 = new()
    {
        { "Wood", 1 },
        { "Meat", 1 },
        { "Hide", 1 },
    };

    [Header("Level 3 Consumption")]
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

    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        ResourceManager.Instance.AddResource("People", basePopulation);
        ResourceManager.Instance.IncreaseMaxAll(ResourceСapacityBonus);

        // Регистрируем дом
        AllBuildingsManager.Instance.RegisterHouse(this);

        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

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
        humanPrefab = Resources.Load<GameObject>("human");
        TrySpawnHuman();
    }

    void TrySpawnHuman()
    {
        if (humanSpawned || humanPrefab == null || gridManager == null) return;

        Vector3 spawnPos = gridManager.GetWorldPositionFromGrid(gridPos);
        spawnedHuman = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
        Human humanScript = spawnedHuman.GetComponent<Human>();
        humanScript.Initialize(gridManager);
        humanSpawned = true;
    }

    public override void OnRemoved()
    {
        ResourceManager.Instance.AddResource("People", -basePopulation);
        if (CurrentStage == 2)
            ResourceManager.Instance.AddResource("People", -upgradePopulation);
        else if (CurrentStage == 3)
            ResourceManager.Instance.AddResource("People", -upgradePopulationLevel3);

        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        if (AllBuildingsManager.Instance != null)
            AllBuildingsManager.Instance.UnregisterHouse(this);

        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

        if (spawnedHuman != null)
        {
            Destroy(spawnedHuman);
            spawnedHuman = null;
        }

        base.OnRemoved();
        ResourceManager.Instance.UpdateGlobalMood();
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

    /// <summary>
    /// Проверяем текущие нужды (как раньше — не трогаем mood/ресурсы)
    /// </summary>
    public bool CheckNeeds()
    {
        bool allSatisfied = true;
        if (BuildManager.Instance != null)
            BuildManager.Instance.CheckEffects(this);

        foreach (var cost in consumptionCost)
        {
            int available = ResourceManager.Instance.GetResource(cost.Key);
            if (available >= cost.Value)
                ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
            else
                allSatisfied = false;
        }

        if (!hasRoadAccess || !HasWater)
        {
            ApplyNeedsResult(false);
            return false;
        }
        
        if (CurrentStage >= 3)
        {
            if (!HasMarket)
            {
                ApplyNeedsResult(false);
                return false; // ❌ без рынка не апгрейдится
            }
        }

        if (allSatisfied)
        {
            ApplyNeedsResult(true);
            return true;
        }
        else
        {
            ApplyNeedsResult(false);
            return false;
        }
        
    }

    /// <summary>
    /// Автоматическое улучшение (вызов из AllBuildingsManager)
    /// </summary>
    public bool TryAutoUpgrade()
    {
        // Дом не готов — не улучшать
        if (!needsAreMet || !hasRoadAccess || !HasWater)
            return false;

        // === 1 → 2 ===
        if (CurrentStage == 1 && reservedForUpgrade)
        {
            CurrentStage = 2;
            sr.sprite = house2Sprite;
            ResourceManager.Instance.AddResource("People", upgradePopulation);

            foreach (var kvp in consumptionLvl2)
            {
                consumptionCost[kvp.Key] = kvp.Value;
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
            ResourceManager.Instance.AddResource("People", upgradePopulationLevel3);

            foreach (var kvp in consumptionLvl3)
            {
                consumptionCost[kvp.Key] = kvp.Value;
                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
            }

            reservedForUpgrade = false;
            AllBuildingsManager.Instance.RecheckAllHousesUpgrade();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяем — готов ли дом потенциально к апгрейду
    /// (без ресурсов, только по условиям инфраструктуры и довольства)
    /// </summary>
    public bool CanAutoUpgrade()
    {
        if (!needsAreMet || !hasRoadAccess || !HasWater)
            return false;
        if (CurrentStage >= 2)
        {
            if (!HasMarket)
            {
                return false;
            }
        }
       

        if (CurrentStage == 1 || CurrentStage == 2)
            return true;

        return false;
    }
}
