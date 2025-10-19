using UnityEngine;
using System.Collections.Generic;

public class House : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    [Header("Settings")]
    public int basePopulation = 3;
    public int upgradePopulation = 3;

    [Header("Sprites")]
    public Sprite house1Sprite;
    public Sprite house2Sprite;
    private GameObject spawnedHuman;

    private SpriteRenderer sr;
    private new Dictionary<string, int> cost = new() { { "Wood", 1 } };

    public bool needsAreMet;

    private GameObject angryPrefab;
    private GameObject upgradePrefab;

    public bool HasWater { get; private set; } = false;
    public int CurrentStage { get; private set; } = 1;
    
    public GameObject humanPrefab;
    private GridManager gridManager;
    private bool humanSpawned = false;

    public int ResourceСapacityBonus = 1;

    // ⚡ список потребляемых ресурсов
    public Dictionary<string, int> consumptionCost = new() { { "Berry", 1 } };

    public Dictionary<string, int> upgradeCostLevel2 = new()
    {
        { "Clay", 25 },
        { "Wood", 15 },
        { "Rock", 10 },
        { "Pottery", 5 },
        { "Hide", 5 },
    };
    
    [Header("Upgrade to Level 3")]
    public Sprite house3Sprite;
    public Dictionary<string, int> upgradeCostLevel3 = new()
    {
        { "Wood", 20 },
        { "Rock", 15 },
        { "Crafts", 3 },
        { "Furniture", 3 },
        { "Bread", 10 },
        { "Beans", 10 },
        { "Beer", 10 },
        { "Cheese", 5 },
        { "Clothes", 5 },
        { "Coal", 5 },
    };
    public int upgradePopulationLevel3 = 4;

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

        upgradePrefab = Resources.Load<GameObject>("upgrade");
        if (upgradePrefab != null)
        {
            upgradePrefab = Instantiate(upgradePrefab, transform);
            upgradePrefab.transform.localPosition = Vector3.up * 0f;
            upgradePrefab.SetActive(false);
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

        // Спавним человека прямо на текущей дороге (по центру)
        Vector3 spawnPos = gridManager.GetWorldPositionFromGrid(gridPos);

        spawnedHuman = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
        Human humanScript = spawnedHuman.GetComponent<Human>();
        humanScript.Initialize(gridManager);
        humanSpawned = true;
    }


    public override void OnRemoved()
    {
        // Убираем население
        ResourceManager.Instance.AddResource("People", -basePopulation);
        if (CurrentStage == 2)
            ResourceManager.Instance.AddResource("People", -upgradePopulation);

        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        // Убираем из менеджера
        if (AllBuildingsManager.Instance != null)
            AllBuildingsManager.Instance.UnregisterHouse(this);

        // ⚡ убираем потребление
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

        // 🔹 обновляем глобальное настроение
        ResourceManager.Instance.UpdateGlobalMood();
    }

    public void SetWaterAccess(bool access)
    {
        HasWater = access;
        ResourceManager.Instance.UpdateGlobalMood();
    }

    /// <summary>
    /// Проверяем, получает ли дом все необходимые товары.
    /// Теперь перед проверкой hasRoadAccess вызываем обновление эффектов (дороги, вода и т.д.).
    /// Даже если нет дороги или воды, ресурсы всё равно потребляются.
    /// </summary>
    public bool CheckNeeds()
    {
        bool allSatisfied = true;

        // 🔹 Обновляем эффекты (дороги, инфраструктуру) перед проверкой
        if (BuildManager.Instance != null)
            BuildManager.Instance.CheckEffects(this);

        // ⚡ Всегда пробуем потреблять ресурсы
        foreach (var cost in consumptionCost)
        {
            int available = ResourceManager.Instance.GetResource(cost.Key);

            if (available >= cost.Value)
            {
                ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
            }
            else
            {
                allSatisfied = false;
            }
        }

        // Если нет дороги или воды — просто снижаем удовлетворённость, но не блокируем потребление
        if (!hasRoadAccess || !HasWater)
        {
            ApplyNeedsResult(false);
            CanUpgrade(); // всё равно проверяем апгрейд
            return false;
        }

        // Проверяем возможность апгрейда
        CanUpgrade();

        // Если все потребности закрыты → довольны, иначе — нет
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
    /// Попытка улучшения дома вручную
    /// </summary>
    public bool TryUpgrade()
    {
        if (CurrentStage == 1)
        {
            if (!needsAreMet)
                return false;

            if (CanUpgrade())
            {
                // списываем апгрейдные ресурсы
                ResourceManager.Instance.SpendResources(upgradeCostLevel2);

                // применяем апгрейд
                CurrentStage = 2;
                sr.sprite = house2Sprite;
                ResourceManager.Instance.AddResource("People", upgradePopulation);

                consumptionCost.Add("Wood", 1);
                ResourceManager.Instance.RegisterConsumer("Wood", 1);
                consumptionCost.Add("Meat", 1);
                ResourceManager.Instance.RegisterConsumer("Meat", 1);
                consumptionCost.Add("Hide", 1);
                ResourceManager.Instance.RegisterConsumer("Hide", 1);
                consumptionCost.Add("Pottery", 2);
                ResourceManager.Instance.RegisterConsumer("Pottery", 1);
               

                AllBuildingsManager.Instance.RecheckAllHousesUpgrade();

                return true;
            }
        }
        
        
        if (CurrentStage == 2)
        {
            if (!needsAreMet)
                return false;

            // Проверяем, есть ли ресурсы для 3 уровня
            if (ResourceManager.Instance.CanSpend(upgradeCostLevel3))
            {
                ResourceManager.Instance.SpendResources(upgradeCostLevel3);

                CurrentStage = 3;
                sr.sprite = house3Sprite;
                ResourceManager.Instance.AddResource("People", upgradePopulationLevel3);

              
                    consumptionCost.Add("Crafts", 1);
                    ResourceManager.Instance.RegisterConsumer("Crafts", 1);
                    
                    consumptionCost.Add("Furniture", 1);
                    ResourceManager.Instance.RegisterConsumer("Furniture", 1);
                
                    consumptionCost.Add("Beans", 1);
                    ResourceManager.Instance.RegisterConsumer("Beans", 1);
                    
                    consumptionCost.Add("Beer", 1);
                    ResourceManager.Instance.RegisterConsumer("Beer", 1);
             
                    consumptionCost.Add("Clothes", 1);
                    ResourceManager.Instance.RegisterConsumer("Clothes", 1);
                    
                    consumptionCost.Add("Coal", 1);
                    ResourceManager.Instance.RegisterConsumer("Coal", 1);
                    
                    consumptionCost.Add("Cheese", 1);
                    ResourceManager.Instance.RegisterConsumer("Cheese", 1);
                    
                    consumptionCost.Add("Yogurt", 1);
                    ResourceManager.Instance.RegisterConsumer("Yogurt", 1);
                    
                    consumptionCost.Add("Bread", 1);
                    ResourceManager.Instance.RegisterConsumer("Bread", 1);
                    
                    consumptionCost.Add("Milk", 1);
                    ResourceManager.Instance.RegisterConsumer("Milk", 1);

                    // Можно в будущем добавить эффект для красоты
                AllBuildingsManager.Instance.RecheckAllHousesUpgrade();
                return true;
            }
        }
        return false;
    }

    public bool CanUpgrade()
    {
        bool can = false;

        // Проверка для 1→2
        if (CurrentStage == 1)
        {
            if (HasAllResources(upgradeCostLevel2) && hasRoadAccess && HasWater)
                can = true;
        }
        // Проверка для 2→3
        else if (CurrentStage == 2)
        {
            if (HasAllResources(upgradeCostLevel3) && hasRoadAccess && HasWater)
                can = true;
        }

        if (upgradePrefab != null)
            upgradePrefab.SetActive(can);

        return can;
    }


    private bool HasAllResources(Dictionary<string, int> cost)
    {
        foreach (var kvp in cost)
        {
            int available = ResourceManager.Instance.GetResource(kvp.Key);
            if (available < kvp.Value)
                return false;
        }
        return true;
    }



}
