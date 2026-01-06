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
    public int CurrentStage { get;  set; } = 1;
    public int currentPopulation = 0;

    public GameObject humanPrefab;
    private bool humanSpawned = false;

    
    private bool effectsDirty = true;   // дом при создании "грязный"
    public void MarkEffectsDirty() => effectsDirty = true;


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
        { "Meat", 1 },
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
        { "Fruit", 1 },
        { "Salt", 1 },
        { "Glass", 1 },
        { "Jewelry", 1 },
        { "Meat", 5 },
        { "Bread", 5 },

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
        base.OnPlaced();
        
        sr = GetComponent<SpriteRenderer>();
        int spawnStage = 1;
        if (ResearchManager.Instance != null && ResearchManager.Instance.IsResearchCompleted("Stage5"))
            spawnStage = 4;

// 1) соберём правильный consumption ДО регистрации
        BuildConsumptionForStage(spawnStage);

// 2) выставим визуал/уровень
        SetStageVisualOnly(spawnStage);

// 3) население сразу итоговое
        currentPopulation = startPopulation
                            + (spawnStage >= 2 ? addPopulationLevel2 : 0)
                            + (spawnStage >= 3 ? addPopulationLevel3 : 0)
                            + (spawnStage >= 4 ? addPopulationLevel4 : 0);

        ResourceManager.Instance.AddResource("People", currentPopulation);

        // 🔹 Регистрируем ВСЁ потребление сразу (для домов, стартующих >1 уровня)
        foreach (var kvp in consumption)
        {
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
        }
        consumersRegistered = true;

        

     

        AllBuildingsManager.Instance.RegisterHouse(this);
        AllBuildingsManager.Instance.MarkHouseEffectsDirty(this); // пересчитать эффекты дому

        angryPrefab = Resources.Load<GameObject>("angry");
        if (angryPrefab != null)
        {
            angryPrefab = Instantiate(angryPrefab, transform);
            angryPrefab.transform.localPosition = Vector3.zero;

            // ✅ СОРТИРОВКА: ставим поверх клетки (angry выше ghost/highlight)
            // cell — это клетка, на которой стоит объект (например, thisPlacedObject.gridPos)
            ApplyFxSorting(angryPrefab, gridManager, gridPos, offset: 992100);

            angryPrefab.SetActive(false);
        }

       
            if (Random.Range(0, 10) > 7)
            {
                humanPrefab = Resources.Load<GameObject>("human");
                float delay = Random.Range(1f, 3f);
                Invoke(nameof(TrySpawnHuman), delay);
            }
        
     
    }


    private void TrySpawnHuman()
    {
        if (humanSpawned) return;

        if (humanPrefab == null)
        {
            Debug.LogError("House: humanPrefab is NULL", this);
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("House: gridManager is NULL (not assigned yet)", this);
            return;
        }

        Vector3 spawnPos = gridManager.GetWorldPositionFromGrid(gridPos);
        spawnedHuman = Instantiate(humanPrefab, spawnPos, Quaternion.identity);

        var human = spawnedHuman.GetComponent<Human>();
        if (human == null)
        {
            Debug.LogError("House: Human component is missing on humanPrefab", humanPrefab);
            return;
        }

        human.Initialize(gridManager);
        humanSpawned = true;
    }


    // === Удаление ===
    public override void OnRemoved()
    {
        // просто вычитаем текущую численность
        ResourceManager.Instance.AddResource("People", -currentPopulation);

        if (consumersRegistered)
        {
            foreach (var kvp in consumption)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
            consumersRegistered = false;
        }


        manager?.SetOccupied(gridPos, false);
        AllBuildingsManager.Instance?.UnregisterHouse(this);

        if (spawnedHuman != null)
        {
            Destroy(spawnedHuman);
            spawnedHuman = null;
        }
        ResourceManager.Instance.RefundResources(cost);

        base.OnRemoved();
        ResourceManager.Instance.UpdateGlobalMood();
    }




    public void ApplyNeedsResult(bool satisfied)
    {
        bool previous = needsAreMet;
        needsAreMet = satisfied;

        if (angryPrefab != null)
        {
            bool wantActive = !satisfied;
            if (angryPrefab.activeSelf != wantActive)
                angryPrefab.SetActive(wantActive);
        }

        if (previous != satisfied)
            AllBuildingsManager.Instance?.OnHouseNeedsChanged(this, satisfied);
    }



    public void SetWaterAccess(bool access)
    {
        if (HasWater == access) return;
        HasWater = access;
        // НЕ вызываем UpdateGlobalMood тут — менеджер делает это один раз в конце тика
    }


    public void SetMarketAccess(bool access)
    {
        if (HasMarket == access) return;
        HasMarket = access;
    }
    
    public void SetTempleAccess(bool access)
    {
        if (HasTemple == access) return;

        HasTemple = access;
    }
    public void SetDoctorAccess(bool access)
    {
        if (HasDoctor == access) return;
        HasDoctor = access;
    }
    public void SetBathhouseAccess(bool access)
    {
        if (HasBathhouse == access) return;
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
        
        if (CurrentStage >= 4 && (!HasBathhouse || !HasDoctor)) 
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

        return CurrentStage == 1 || CurrentStage == 2 || CurrentStage == 3|| CurrentStage == 4|| CurrentStage == 5;
    }


public bool CheckNeedsFromPool(
    BuildManager bm,
    Dictionary<string, int> pooled,
    Dictionary<string, int> totalSpend)
{
    // 0) Эффекты (дорога/сервисы/шум и т.п.) — ТОЛЬКО когда dirty
    if (effectsDirty)
    {
        float tEff = Time.realtimeSinceStartup;
        bm?.CheckEffects(this);
        float effMs = (Time.realtimeSinceStartup - tEff) * 1000f;
        AllBuildingsManager.Instance?.Debug_AddEffectsTime(effMs);

        effectsDirty = false;
    }

    // 1) Подготовка списка недостающих ресурсов
    if (lastMissingResources == null)
        lastMissingResources = new HashSet<string>();
    else
        lastMissingResources.Clear();

    bool canSpend = true;

    // 2) Проверяем потребление по пулу (НЕ трогаем ResourceManager здесь)
    if (consumption != null && consumption.Count > 0)
    {
        // 2.1) проверка по пулу
        foreach (var kvp in consumption)
        {
            string res = kvp.Key;
            int need = kvp.Value;

            int available = pooled.TryGetValue(res, out var v) ? v : 0;
            if (available < need)
            {
                canSpend = false;
                lastMissingResources.Add(res);
            }
        }

        // 2.2) если хватает — списываем из пула и копим итоговое списание
        if (canSpend)
        {
            foreach (var kvp in consumption)
            {
                string res = kvp.Key;
                int need = kvp.Value;

                pooled[res] = (pooled.TryGetValue(res, out var v) ? v : 0) - need;

                if (totalSpend.TryGetValue(res, out var cur))
                    totalSpend[res] = cur + need;
                else
                    totalSpend[res] = need;
            }
        }
    }

    // 3) Проверка сервисных условий (как у тебя было)
    bool servicesOk = true;

    if (!hasRoadAccess) servicesOk = false;
    if (CurrentStage >= 2 && !HasWater) servicesOk = false;
    if (CurrentStage >= 3 && !HasMarket) servicesOk = false;
    if (CurrentStage >= 4 && !HasTemple) servicesOk = false;
    if (CurrentStage >= 5 && (!HasDoctor || !HasBathhouse)) servicesOk = false;
    if (InNoise) servicesOk = false;

    bool satisfied = servicesOk && canSpend;

    // 4) Применяем результат (иконка angry и статистика)
    ApplyNeedsResult(satisfied);
    return satisfied;
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
    
    private void SetStageVisualOnly(int stage)
    {
        CurrentStage = stage;

        if (sr == null) sr = GetComponent<SpriteRenderer>();

        sr.sprite = stage switch
        {
            1 => house1Sprite,
            2 => house2Sprite,
            3 => house3Sprite,
            4 => house4Sprite,
            5 => house5Sprite,
            _ => house1Sprite
        };
    }

    private void BuildConsumptionForStage(int stage)
    {
        // начинаем с базового (berry)
        var newCons = new Dictionary<string, int>();

        // base consumption
        foreach (var kv in consumption)
            newCons[kv.Key] = kv.Value;

        // add lvl2..lvl4 если нужно
        if (stage >= 2) MergeConsumption(newCons, consumptionLvl2);
        if (stage >= 3) MergeConsumption(newCons, consumptionLvl3);

        if (stage >= 4)
        {
            // удалить ресурсы на 4 уровне
            if (deleteFromConsumptionAtLvl4 != null)
            {
                foreach (var r in deleteFromConsumptionAtLvl4)
                    if (!string.IsNullOrWhiteSpace(r))
                        newCons.Remove(r.Trim());
            }

            MergeConsumption(newCons, consumptionLvl4);
        }

        // (stage 5 не нужен для “строятся как 4”, но на будущее)
        if (stage >= 5) MergeConsumption(newCons, consumptionLvl5);

        // заменить текущий словарь consumption
        consumption = newCons;
    }

    private static void MergeConsumption(Dictionary<string, int> target, Dictionary<string, int> add)
    {
        if (add == null) return;
        foreach (var kv in add)
        {
            if (target.ContainsKey(kv.Key))
                target[kv.Key] += kv.Value;
            else
                target[kv.Key] = kv.Value;
        }
    }

}
