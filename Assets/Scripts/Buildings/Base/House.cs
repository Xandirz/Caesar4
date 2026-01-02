using UnityEngine;
using System.Collections.Generic;

public class House : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    [Header("Settings")]
    public int startPopulation = 3;   // начальная численность
    public int addPopulationLevel2 = 2;
    public int addPopulationLevel3 = 2;
    public int addPopulationLevel4 = 2;
    public int addPopulationLevel5 = 2;

    [Header("Sprites")]
    public Sprite house1Sprite;
    public Sprite house2Sprite;
    public Sprite house3Sprite;
    public Sprite house4Sprite;
    public Sprite house5Sprite;

    private SpriteRenderer sr;
    private new Dictionary<string, int> cost = new() { { "Wood", 1 } };

    public bool needsAreMet;
    public bool reservedForUpgrade = false;
    public override bool RequiresRoadAccess => true;
    public HashSet<string> lastMissingResources = new HashSet<string>();

    private GameObject angryPrefab;
    private GameObject spawnedHuman;

    public bool InNoise { get; private set; }

    public void SetNoise(bool on) => InNoise = on;
    public bool HasWater { get; private set; } = false;
    public bool HasMarket { get; private set; } = false;
    public bool HasTemple { get; private set; } = false;
    public bool HasDoctor { get; private set; } = false;
    public bool HasBathhouse { get; private set; } = false;
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
        { "Fish", 1 },
        { "Meat", 1 },
        { "Hide", 1 },
        { "Pottery", 1 },
    };

    [Header("Level 3 Additional Consumption")]
    public Dictionary<string, int> consumptionLvl3 = new()
    {
        { "Crafts", 1 },
        { "Furniture", 1 },
        { "Bread", 1 },
        { "Beans", 1 },
        { "Beer", 1 },
        { "Yogurt", 1 },
        { "Cheese", 1 },
        { "Clothes", 1 },
        { "Charcoal", 1 },
    };
    
    [Header("Level 4 Additional Consumption")]
    public Dictionary<string, int> consumptionLvl4 = new()
    {
        { "Honey", 1 },
        { "Candle", 1 },
        { "Olive", 1 },
        { "OliveOil", 1 },
        { "Soap", 1 },
        { "Eggs", 1 },
    };
    [Header("Delete From Consumption At Level 4")]
    public List<string> deleteFromConsumptionAtLvl4 = new()
    {
        "Hide"
    };
    [Header("Level 5 Additional Consumption")]
    public Dictionary<string, int> consumptionLvl5 = new()
    {
        { "Wine", 1 },
        { "Herbs", 1 },
        { "Vegetables", 1 },
        { "Metalware", 1 },

    };

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    


// Чтобы понимать, зарегистрированы ли уже consumer-rate'ы этого дома
    private bool consumersRegistered = false;


    // === Постройка ===
    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        currentPopulation = startPopulation;
        ResourceManager.Instance.AddResource("People", currentPopulation);

        

        
        foreach (var kvp in consumption)
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
        
        
        consumersRegistered = true;


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
        BuildManager.Instance.CheckEffects(this);

        // 1) Сначала проверяем и (если можем) СПИСЫВАЕМ потребление.
        // Это должно происходить даже если нет дороги.
        if (lastMissingResources == null)
            lastMissingResources = new HashSet<string>();
        else
            lastMissingResources.Clear();

        bool canSpend = ResourceManager.Instance.CanSpend(consumption);

        if (canSpend)
        {
            ResourceManager.Instance.SpendResources(consumption);
        }
        else
        {
            // Заполняем, чего именно не хватило (для InfoUI подсветки)
            if (consumption != null)
            {
                foreach (var kvp in consumption)
                {
                    string res = kvp.Key;
                    int need = kvp.Value;

                    // Проверяем поштучно, чтобы точно понять, что именно missing
                    if (ResourceManager.Instance.GetResource(res) < need)
                        lastMissingResources.Add(res);
                }
            }
        }

        // 2) Теперь отдельно считаем "удовлетворены ли нужды" (дорога/сервисы/шум),
        // но это НЕ влияет на факт списания ресурсов.
        bool servicesOk = true;

        if (!hasRoadAccess) servicesOk = false;
        if (CurrentStage >= 2 && !HasWater) servicesOk = false;
        if (CurrentStage >= 3 && !HasMarket) servicesOk = false;
        if (CurrentStage >= 4 && !HasTemple) servicesOk = false;
        if (CurrentStage >= 5 && !HasDoctor && !HasBathhouse) servicesOk = false;
        if (InNoise) servicesOk = false;

        bool satisfied = servicesOk && canSpend;

        ApplyNeedsResult(satisfied);
        return satisfied;
    }



    public  void ApplyNeedsResult(bool satisfied)
    {
        bool previous = needsAreMet;
        needsAreMet = satisfied;

        if (angryPrefab != null)
            angryPrefab.SetActive(!satisfied);

        if (previous != satisfied)
        {
            AllBuildingsManager.Instance?.OnHouseNeedsChanged(this, satisfied);
        }


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
    
    public void SetTempleAccess(bool access)
    {
        HasTemple = access;
    }
    public void SetDoctorAccess(bool access)
    {
        HasDoctor = access;
    }
    public void SetBathhouseAccess(bool access)
    {
        HasBathhouse = access;
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
        
        if (CurrentStage == 3 && reservedForUpgrade)
        {
            CurrentStage = 4;
            sr.sprite = house4Sprite;

            currentPopulation += addPopulationLevel4;
            ResourceManager.Instance.AddResource("People", addPopulationLevel4);

            // ✅ Удаляем потребление некоторых ресурсов на 4 уровне (например Hide)
            if (deleteFromConsumptionAtLvl4 != null && deleteFromConsumptionAtLvl4.Count > 0)
            {
                foreach (var resNameRaw in deleteFromConsumptionAtLvl4)
                {
                    if (string.IsNullOrEmpty(resNameRaw)) continue;
                    string resName = resNameRaw.Trim();

                    if (consumption != null && consumption.TryGetValue(resName, out int amount) && amount > 0)
                    {
                        // снимаем глобального потребителя
                        ResourceManager.Instance.UnregisterConsumer(resName, amount);

                        // удаляем из словаря потребления дома
                        consumption.Remove(resName);
                    }
                }
            }

            // ✅ Добавляем потребление 4 уровня
            foreach (var kvp in consumptionLvl4)
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
       
        if (CurrentStage == 4 && reservedForUpgrade)
        {
            CurrentStage = 5;
            sr.sprite = house5Sprite;

            currentPopulation += addPopulationLevel5;
            ResourceManager.Instance.AddResource("People", addPopulationLevel5);
            

            // ✅ Добавляем потребление 5 уровня
            foreach (var kvp in consumptionLvl5)
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

        // новое требование для апгрейда 3 -> 4
        if (CurrentStage >= 3 && !HasTemple)
            return false;
        
        if (CurrentStage == 4 && (!HasBathhouse || !HasDoctor)) 
            return false;



        if (ResearchManager.Instance == null)
            return false;

        if (CurrentStage == 1 && !ResearchManager.Instance.IsResearchCompleted("Stage2"))
            return false;

        if (CurrentStage == 2 && !ResearchManager.Instance.IsResearchCompleted("Stage3"))
            return false;

        if (CurrentStage == 3 && !ResearchManager.Instance.IsResearchCompleted("Stage4"))
            return false;
        if (CurrentStage == 4 && !ResearchManager.Instance.IsResearchCompleted("Stage5"))
            return false;

        return CurrentStage == 1 || CurrentStage == 2 || CurrentStage == 3|| CurrentStage == 4;
    }



    public void RecheckNoise(GridManager mgr, Vector2Int center, int radius)
    {
        InNoise = HasAnyNoisyBuildingAround(mgr, radius);
    }

    private bool HasAnyNoisyBuildingAround(GridManager mgr, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            var c = gridPos + new Vector2Int(dx, dy);
            if (mgr.TryGetPlacedObject(c, out var obj) && obj is ProductionBuilding pb && pb.isNoisy)
                return true;
        }
        return false;
    }
    
    public bool IsUpgradeUnlocked(int targetLevel)
    {
        if (ResearchManager.Instance == null) return false;

        if (targetLevel == 2)
            return ResearchManager.Instance.IsResearchCompleted("Stage2");

        if (targetLevel == 3)
            return ResearchManager.Instance.IsResearchCompleted("Stage3");

        if (targetLevel == 4)
            return ResearchManager.Instance.IsResearchCompleted("Stage4");
        
        if (targetLevel == 5)
            return ResearchManager.Instance.IsResearchCompleted("Stage5");


        return true;
    }
}
