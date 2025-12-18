using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // 🔹 теперь ресурсы хранятся как float (внутреннее накопление)
    public Dictionary<string, float> resourceBuffer = new();
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
        AddResource("Research", 0, false);
        
        AddResource("Wood", 30, true, 30);
        
        AddResource("Berry", 0, true, 10);
        
        AddResource("Rock", 10, true, 10);
        
        AddResource("Fish", 0, true, 10);
        
        AddResource("Clay", 0, true, 10);
        AddResource("Pottery", 0, true, 10);

        AddResource("Tools", 0, true, 10);
        
        AddResource("Meat", 0, true, 10);
        AddResource("Bone", 0, true, 10);
        AddResource("Hide", 0, true, 10);

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

        AddResource("Wheat", 0, true, 10);
        AddResource("Flour", 0, true, 10);
        AddResource("Bread", 0, true, 10);
        
        AddResource("Beer", 0, true, 10);
        
        AddResource("Furniture", 0, true, 10);
        
        AddResource("Coal", 0, true, 10);
        AddResource("CopperOre", 0, true, 10);
        AddResource("Copper", 0, true, 10);
        
        SyncResourceBufferFromResources();

    }
    private void SyncResourceBufferFromResources()
    {
        if (resourceBuffer == null)
            resourceBuffer = new Dictionary<string, float>();

        foreach (var kvp in resources)
            resourceBuffer[kvp.Key] = kvp.Value;
    }

    /*
    private void Update()
    {
        float dt = Time.deltaTime;

        // перебираем только те ресурсы, по которым есть производство
        foreach (var kvp in productionRates)
        {
            string res = kvp.Key;
            float prod = kvp.Value;
            float cons = consumptionRates.ContainsKey(res) ? consumptionRates[res] : 0f;
            float delta = (prod - cons) * dt;

            if (!resourceBuffer.ContainsKey(res))
                resourceBuffer[res] = 0f;

            resourceBuffer[res] += delta;

            float max = maxResources.ContainsKey(res) ? maxResources[res] : float.MaxValue;
            resourceBuffer[res] = Mathf.Clamp(resourceBuffer[res], 0, max);

            int oldAmount = resources.ContainsKey(res) ? resources[res] : 0;
            int newAmount = Mathf.FloorToInt(resourceBuffer[res]);

            resources[res] = newAmount;

            // ⚡ дергаем UI только когда реально что-то изменилось
            if (newAmount != oldAmount)
                UpdateUI(res);
        }
    }
    */
    public int GetResourceSnapshot(string name)
    {
        if (string.IsNullOrEmpty(name))
            return 0;

        name = name.Trim();

        // Если буфер отсутствует, но int-значение есть — подтягиваем в буфер (важно для сейвов/инициализации)
        if (!resourceBuffer.ContainsKey(name) && resources.ContainsKey(name))
            resourceBuffer[name] = resources[name];

        // Берём из буфера, если есть
        if (resourceBuffer.TryGetValue(name, out float v))
            return Mathf.FloorToInt(v);

        // Фоллбек: берём отображаемое значение (то, что видит UI)
        return resources.TryGetValue(name, out int i) ? i : 0;
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
        // берём данные из AllBuildingsManager (счётчики домов)
        if (AllBuildingsManager.Instance == null)
            return;

        int total = AllBuildingsManager.Instance.GetHouseCount();      // или totalHouses, если сделал публичным
        int satisfied = AllBuildingsManager.Instance.satisfiedHousesCount;  // сделай для него public getter

        if (total == 0)
        {
            moodPercent = 0;
        }
        else
        {
            moodPercent = Mathf.RoundToInt((satisfied / (float)total) * 100f);
        }

        // обновляем UI
        UpdateUI("Mood");

        // ⚡ ВАЖНО: сообщаем ресерчу текущее настроение
        if (ResearchManager.Instance != null)
            ResearchManager.Instance.OnDayPassed(moodPercent);
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
        int deficit = assignedWorkers - TotalPeople;
        if (deficit <= 0)
            return;

        var allProducers = AllBuildingsManager.Instance.GetProducers();

        int safety = 1000;
        while (deficit > 0 && safety-- > 0)
        {
            ProductionBuilding newestWithWorkers = null;

            // Идём с конца списка (самое новое — последние элементы)
            for (int i = allProducers.Count - 1; i >= 0; i--)
            {
                var pb = allProducers[i];
                if (pb == null)
                    continue;

                // проверяем, есть ли у этого здания назначенные рабочие
                if (workerAllocations.ContainsKey(pb))
                {
                    newestWithWorkers = pb;
                    break;
                }
            }

            if (newestWithWorkers == null)
            {
                // нет зданий, у которых можно забрать рабочих
                break;
            }

            // отключаем производство
            newestWithWorkers.ForceStopDueToNoWorkers();

            // пересчитываем дефицит
            deficit = assignedWorkers - TotalPeople;
        }
    }


}
