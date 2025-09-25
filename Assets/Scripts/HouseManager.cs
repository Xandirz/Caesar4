using UnityEngine;
using System.Collections.Generic;

public class HouseManager : MonoBehaviour
{
    public static HouseManager Instance { get; private set; }

    private readonly List<House> houses = new();
    [SerializeField] private float checkInterval = 5f; // как часто проверять нужды
    private float timer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckNeedsAllHouses();
        }
    }

    public void RegisterHouse(House house)
    {
        if (!houses.Contains(house))
            houses.Add(house);
    }

    public void UnregisterHouse(House house)
    {
        if (houses.Contains(house))
            houses.Remove(house);
    }

    private void CheckNeedsAllHouses()
    {
        if (houses.Count == 0) return;

        foreach (var house in houses)
        {
            if (house == null) continue;

            // дорога и вода обязательны
            if (!house.hasRoadAccess || !house.HasWater)
            {
                house.ApplyNeedsResult(false);
                continue;
            }

            bool hasAll = true;

            // проверяем только наличие ресурсов (не списываем!)
            foreach (var kvp in house.consumptionCost)
            {
                if (ResourceManager.Instance.GetResource(kvp.Key) < kvp.Value)
                {
                    hasAll = false;
                    break;
                }
            }

            house.ApplyNeedsResult(hasAll);
        }
    }




    public int GetHouseCount()
    {
        return houses.Count;
    }
}
