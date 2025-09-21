using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<string, int> resources = new();
    private Dictionary<string, int> maxResources = new();

    // итоговые скорости (суммарные для всех зданий)
    private Dictionary<string, float> productionRates = new();
    private Dictionary<string, float> consumptionRates = new();

    // буферы для накопления дробных частей
    private Dictionary<string, float> productionBuffer = new();
    private Dictionary<string, float> consumptionBuffer = new();

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
        AddResource("Wood",30,true,20);
        AddResource("Rock",10,true,20);
        AddResource("Berry",0,true,20);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // === производство ===
        foreach (var kvp in productionRates)
        {
            string res = kvp.Key;
            float rate = kvp.Value;

            if (!productionBuffer.ContainsKey(res))
                productionBuffer[res] = 0;

            productionBuffer[res] += rate * dt;

            if (productionBuffer[res] >= 1f)
            {
                int whole = Mathf.FloorToInt(productionBuffer[res]);
                productionBuffer[res] -= whole;
                AddResource(res, whole);
            }
        }

        // === потребление ===
        foreach (var kvp in consumptionRates)
        {
            string res = kvp.Key;
            float rate = kvp.Value;

            if (!consumptionBuffer.ContainsKey(res))
                consumptionBuffer[res] = 0;

            consumptionBuffer[res] += rate * dt;

            if (consumptionBuffer[res] >= 1f)
            {
                int whole = Mathf.FloorToInt(consumptionBuffer[res]);
                consumptionBuffer[res] -= whole;
                SpendResource(res, whole);
            }
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

    public void AddResource(string name, int amount, bool useMax = false, int max = 0)
    {
        if (!resources.ContainsKey(name))
            resources[name] = 0;

        resources[name] += amount;

        if (useMax)
            maxResources[name] = max;

        if (maxResources.ContainsKey(name))
            resources[name] = Mathf.Min(resources[name], maxResources[name]);

        UpdateUI(name);
    }

    public void SpendResource(string name, int amount)
    {
        if (resources.ContainsKey(name))
        {
            resources[name] = Mathf.Max(0, resources[name] - amount);
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
        foreach (var kvp in refund)
            AddResource(kvp.Key, kvp.Value);
    }

    // === UI ===
    private void UpdateUI(string name)
    {
        float prod = productionRates.ContainsKey(name) ? productionRates[name] : 0;
        float cons = consumptionRates.ContainsKey(name) ? consumptionRates[name] : 0;

        ResourceUIManager.Instance?.SetResource(
            name,
            resources.ContainsKey(name) ? resources[name] : 0,
            prod,
            cons
        );
    }
}
