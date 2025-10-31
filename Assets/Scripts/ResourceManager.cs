using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // 🔹 теперь ресурсы хранятся как float (внутреннее накопление)
    private Dictionary<string, float> resourceBuffer = new();
    private Dictionary<string, int> resources = new();          // отображаемые значения (int)
    private Dictionary<string, int> maxResources = new();

    // итоговые скорости (суммарные для всех зданий)
    private Dictionary<string, float> productionRates = new();
    private Dictionary<string, float> consumptionRates = new();

    // 🔹 процент настроения (0–100)
    public int moodPercent { get; private set; } = 0;
    
    private int assignedWorkers = 0;
    private readonly Dictionary<ProductionBuilding, int> workerAllocations = new();

// Свойства
    public int TotalPeople => GetResource("People");
    public int FreeWorkers => Mathf.Max(0, TotalPeople - assignedWorkers);
    public int AssignedWorkers => assignedWorkers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Start()
    {
        // ресурсы по умолчанию
        AddResource("People", 0, false);
        
        AddResource("Wood", 30, true, 30);
        
        AddResource("Berry", 0, true, 10);
        
        AddResource("Rock", 10, true, 10);
        
        AddResource("Clay", 0, true, 10);
        AddResource("Pottery", 0, true, 10);

        AddResource("Meat", 0, true, 10);
        AddResource("Bone", 0, true, 10);
        AddResource("Hide", 0, true, 10);
        
        AddResource("Tools", 0, true, 10);
        
        AddResource("Crafts", 0, true, 10);
        AddResource("Needles", 0, true, 10);
        
        AddResource("Sheep", 0, true, 10);
        AddResource("Wool", 0, true, 10);
        AddResource("Milk", 0, true, 10);
        
        AddResource("Cheese", 0, true, 10);
        AddResource("Yogurt", 0, true, 10);
        
        AddResource("Cloth", 0, true, 10);
        AddResource("Clothes", 0, true, 10);

        AddResource("Beans", 0, true, 10);
        
        AddResource("Beer", 0, true, 10);

        
        AddResource("Wheat", 0, true, 10);
        AddResource("Flour", 0, true, 10);
        AddResource("Bread", 0, true, 10);
        
        AddResource("Furniture", 0, true, 10);
        
        AddResource("Coal", 0, true, 10);
        AddResource("CopperOre", 0, true, 10);
        AddResource("Copper", 0, true, 10);
        
        
    }

    private void Update()
    {
        // 🔹 обновляем ресурсы по дельте времени
        float dt = Time.deltaTime;

        foreach (var kvp in productionRates)
        {
            string res = kvp.Key;
            float prod = kvp.Value;
            float cons = consumptionRates.ContainsKey(res) ? consumptionRates[res] : 0f;
            float delta = (prod - cons) * dt;

            if (!resourceBuffer.ContainsKey(res))
                resourceBuffer[res] = 0f;

            resourceBuffer[res] += delta;

            // ограничиваем максимум
            float max = maxResources.ContainsKey(res) ? maxResources[res] : float.MaxValue;
            resourceBuffer[res] = Mathf.Clamp(resourceBuffer[res], 0, max);

            // обновляем видимое значение (int)
            resources[res] = Mathf.FloorToInt(resourceBuffer[res]);

            UpdateUI(res);
        }
    }

    // === Регистрация производителей и потребителей ===
    public void RegisterProducer(string resource, float rate)
    {
        if (!productionRates.ContainsKey(resource))
            productionRates[resource] = 0;
        productionRates[resource] += rate;

        UpdateUI(resource);
    }

    public void UnregisterProducer(string resource, float rate)
    {
        if (productionRates.ContainsKey(resource))
        {
            productionRates[resource] -= rate;
            if (productionRates[resource] <= 0)
                productionRates.Remove(resource);
        }
        UpdateUI(resource);
    }

    public void RegisterConsumer(string resource, float rate)
    {
        if (!consumptionRates.ContainsKey(resource))
            consumptionRates[resource] = 0;
        consumptionRates[resource] += rate;

        UpdateUI(resource);
    }

    public void UnregisterConsumer(string resource, float rate)
    {
        if (consumptionRates.ContainsKey(resource))
        {
            consumptionRates[resource] -= rate;
            if (consumptionRates[resource] <= 0)
                consumptionRates.Remove(resource);
        }
        UpdateUI(resource);
    }

    // === Управление запасами ===
    public int GetResource(string name)
    {
        return resources.ContainsKey(name) ? resources[name] : 0;
    }

    public int GetMaxResource(string name)
    {
        return maxResources.ContainsKey(name) ? maxResources[name] : int.MaxValue;
    }

    public void IncreaseMaxAll(int amount)
    {
        var keys = new List<string>(maxResources.Keys);
        foreach (var key in keys)
        {
            maxResources[key] += amount;
        }
    }

    public void DecreaseMaxAll(int amount)
    {
        var keys = new List<string>(maxResources.Keys);
        foreach (var key in keys)
        {
            maxResources[key] = Mathf.Max(0, maxResources[key] - amount);
        }
    }
    public void AddResource(string name, int amount, bool useMax = false, int max = 0)
    {
        if (!resources.ContainsKey(name))
            resources[name] = 0;
        if (!resourceBuffer.ContainsKey(name))
            resourceBuffer[name] = resources[name];
        if (!maxResources.ContainsKey(name))
            maxResources[name] = 10;

        if (useMax)
            maxResources[name] = max;

        // просто добавляем, без Clamp
        resourceBuffer[name] += amount;
        resources[name] = Mathf.FloorToInt(resourceBuffer[name]);
        
        
        resources[name] = Mathf.FloorToInt(resourceBuffer[name]);

        UpdateUI(name);

        // 🔸 ВАЖНО: контроль дефицита работников
        if (name == "People")
            OnPeopleChanged();
        

        UpdateUI(name);
        
        
    }

// ⚙️ вызывать после применения производства и потребления:
    public void ApplyStorageLimits()
    {
        var keys = new List<string>(resourceBuffer.Keys);

        foreach (var name in keys)
        {
            // Пропускаем ресурсы, для которых лимит не применяется
            if (name == "People" || !maxResources.ContainsKey(name))
                continue;

            float limit = maxResources[name];
            if (resourceBuffer[name] > limit)
            {
                resourceBuffer[name] = limit;
                resources[name] = Mathf.FloorToInt(limit);
            }
        }
    }


    public void ChangeStorageLimit(string name, int amount)
    {
        if (!maxResources.ContainsKey(name))
            maxResources[name] = 0;

        maxResources[name] += amount;

        if (maxResources[name] < 0)
            maxResources[name] = 0;

        ApplyStorageLimits(); // применяем лимиты сразу
    }


    public void SpendResource(string name, int amount)
    {
        if (resourceBuffer.ContainsKey(name))
        {
            resourceBuffer[name] = Mathf.Max(0, resourceBuffer[name] - amount);
            resources[name] = Mathf.FloorToInt(resourceBuffer[name]);
            UpdateUI(name);
        }
    }

    public bool CanSpend(Dictionary<string, int> cost)
    {
        foreach (var kvp in cost)
        {
            if (!resources.ContainsKey(kvp.Key) || resources[kvp.Key] < kvp.Value)
                return false;
        }
        return true;
    }

    public void SpendResources(Dictionary<string, int> cost)
    {
        foreach (var kvp in cost)
            SpendResource(kvp.Key, kvp.Value);
    }

    public void RefundResources(Dictionary<string, int> refund)
    {
        if (refund == null || refund.Count == 0)
            return;

        foreach (var kvp in refund)
        {
            if (kvp.Value > 0)
                AddResource(kvp.Key, kvp.Value);
        }
    }

    // === Настроение ===
    public void UpdateGlobalMood()
    {
        House[] houses = FindObjectsOfType<House>();
        if (houses.Length == 0)
        {
            moodPercent = 0;
            return;
        }

        int satisfied = 0;
        foreach (var h in houses)
        {
            if (h.needsAreMet)
                satisfied++;
        }

        moodPercent = Mathf.RoundToInt((satisfied / (float)houses.Length) * 100f);
        UpdateUI("Mood");
    }

    // === UI ===
    private void UpdateUI(string name)
    {
        if (name == "Mood")
        {
            ResourceUIManager.Instance?.SetResource(
                "Mood",
                moodPercent,
                0,
                0
            );
            return;
        }

        float prod = productionRates.ContainsKey(name) ? productionRates[name] : 0;
        float cons = consumptionRates.ContainsKey(name) ? consumptionRates[name] : 0;

        ResourceUIManager.Instance?.SetResource(
            name,
            resources.ContainsKey(name) ? resources[name] : 0,
            prod,
            cons
        );
    }
    
    // === ДОБАВИТЬ ВНИЗ В КЛАСС ResourceManager ===

    public List<string> GetAllResourceNames()
    {
        return new List<string>(resources.Keys);
    }

    /// <summary>
    /// Получить текущее производство ресурса в секунду.
    /// Основано на зарегистрированных производителях.
    /// </summary>
    public float GetProduction(string resource)
    {
        if (!productionRates.ContainsKey(resource))
            return 0;
        return productionRates[resource];
    }

    /// <summary>
    /// Получить текущее потребление ресурса в секунду.
    /// Основано на зарегистрированных потребителях.
    /// </summary>
    public float GetConsumption(string resource)
    {
        if (!consumptionRates.ContainsKey(resource))
            return 0;
        return consumptionRates[resource];
    }
    
    public bool TryAllocateWorkers(ProductionBuilding b, int count)
    {
        if (count <= 0) return true;
        if (FreeWorkers < count) return false;

        assignedWorkers += count;
        workerAllocations[b] = count;
        // при желании — обновить UI: "Workers: assigned/total"
        return true;
    }
    
    public void ReleaseWorkers(ProductionBuilding b)
    {
        if (workerAllocations.TryGetValue(b, out int cnt))
        {
            assignedWorkers = Mathf.Max(0, assignedWorkers - cnt);
            workerAllocations.Remove(b);
        }
    }
    // Хук на изменение населения
    private void OnPeopleChanged()
    {
        // если людей стало меньше, чем уже занято — отключаем часть производств
        int deficit = assignedWorkers - TotalPeople;
        if (deficit <= 0) return;

        // Простая стратегия: снимаем занятость у "последних" в словаре
        // (можно улучшить приоритезацией)
        foreach (var kv in new List<ProductionBuilding>(workerAllocations.Keys))
        {
            if (deficit <= 0) break;
            // Попросим здание остановиться (оно само освободит людей через ReleaseWorkers)
            if (kv != null) kv.ForceStopDueToNoWorkers();
            deficit = assignedWorkers - TotalPeople;
        }
    }

}
