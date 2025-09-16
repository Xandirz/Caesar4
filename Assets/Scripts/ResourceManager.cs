using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    private Dictionary<string, int> resources = new Dictionary<string, int>();
    private Dictionary<string, int> maxResources = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // стартовые ресурсы
        AddResource("Wood", 115, true, 120);
        AddResource("People", 0);
        AddResource("Rock", 5,true,20);
    }

    public void AddResource(string name, int amount, bool useMax = false, int max = 0)
    {
        if (!resources.ContainsKey(name))
        {
            resources[name] = 0;
            if (useMax) maxResources[name] = max;
        }

        if (maxResources.ContainsKey(name))
            resources[name] = Mathf.Min(resources[name] + amount, maxResources[name]);
        else
            resources[name] += amount;

        // обновляем UI
        ResourceUIManager.Instance?.SetResource(name, resources[name]);
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
        {
            AddResource(kvp.Key, -kvp.Value);
        }
    }

    public void RefundResources(Dictionary<string, int> cost)
    {
        foreach (var kvp in cost)
        {
            AddResource(kvp.Key, kvp.Value);
        }
    }

    public int GetResource(string name)
    {
        if (resources.ContainsKey(name))
            return resources[name];
        return 0;
    }
}