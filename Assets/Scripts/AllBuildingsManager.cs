using UnityEngine;
using System.Collections.Generic;

public class AllBuildingsManager : MonoBehaviour
{
    public static AllBuildingsManager Instance { get; private set; }

    private readonly List<House> houses = new();
    private readonly List<ProductionBuilding> producers = new();
    [SerializeField] private float checkInterval = 5f; // как часто проверять нужды
    private float timer = 0f;

    // для автоапгрейда домов
    private readonly Dictionary<string, float> reservedResources = new();

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
            CheckNeedsAllProducers();
            CheckNeedsAllHouses();
        }
    }

    // ===== Регистрация зданий =====
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
    public void RegisterProducer(ProductionBuilding pb)
    {
        if (!producers.Contains(pb))
            producers.Add(pb);
    }
    public void UnregisterProducer(ProductionBuilding pb)
    {
        if (producers.Contains(pb))
            producers.Remove(pb);
    }

    // ===== Проверка нужд производств + автоапгрейд =====
    private void CheckNeedsAllProducers()
    {
        if (producers.Count == 0) return;

        foreach (var pb in producers)
        {
            if (pb == null) continue;

            bool satisfied = pb.CheckNeeds();   // Проверяем ресурсы и обновляем stopPrefab
            pb.ApplyNeedsResult(satisfied);
        }
    }


    // ===== Проверка нужд домов + автоапгрейд =====
    private void CheckNeedsAllHouses()
    {
        if (houses.Count == 0) return;

        foreach (var house in houses)
        {
            if (house == null) continue;
            house.ApplyNeedsResult(house.CheckNeeds());
        }

        // 1️⃣ Считаем излишки экономики
        Dictionary<string, float> surplus = CalculateSurplus();

        // 2️⃣ Пытаемся зарезервировать ресурсы для домов
        List<House> readyToUpgrade = new();

        foreach (var house in houses)
        {
            if (house == null || !house.CanAutoUpgrade())
                continue;

            Dictionary<string, int> nextCons = null;
            if (house.CurrentStage == 1)
                nextCons = house.consumptionLvl2;
            else if (house.CurrentStage == 2)
                nextCons = house.consumptionLvl3;

            if (nextCons == null)
                continue;

            if (CanReserveResources(nextCons, surplus))
            {
                ReserveResources(nextCons, surplus);
                house.reservedForUpgrade = true;
                readyToUpgrade.Add(house);
            }
        }

        // 3️⃣ Улучшаем, если хватило излишков
        foreach (var house in readyToUpgrade)
        {
            if (house != null)
                house.TryAutoUpgrade();
        }

        reservedResources.Clear();
    }

    // ===== Подсчёт излишков =====
    private Dictionary<string, float> CalculateSurplus()
    {
        var result = new Dictionary<string, float>();
        var resourceNames = ResourceManager.Instance.GetAllResourceNames();

        foreach (var name in resourceNames)
        {
            float prod = ResourceManager.Instance.GetProduction(name);
            float cons = ResourceManager.Instance.GetConsumption(name);
            float diff = prod - cons;
            if (diff > 0)
                result[name] = diff;
        }

        return result;
    }

    // ===== Проверки и резерв =====
    private bool CanReserveResources(Dictionary<string, int> needs, Dictionary<string, float> surplus)
    {
        foreach (var kvp in needs)
        {
            if (!surplus.ContainsKey(kvp.Key)) return false;
            if (surplus[kvp.Key] < kvp.Value) return false;
        }
        return true;
    }

    private void ReserveResources(Dictionary<string, int> needs, Dictionary<string, float> surplus)
    {
        foreach (var kvp in needs)
        {
            surplus[kvp.Key] -= kvp.Value;
            if (!reservedResources.ContainsKey(kvp.Key))
                reservedResources[kvp.Key] = 0;
            reservedResources[kvp.Key] += kvp.Value;
        }
    }

    // ===== Перепроверка =====
    public void RecheckAllHousesUpgrade()
    {
        foreach (var house in houses)
        {
            if (house != null)
                house.CanAutoUpgrade();
        }
    }

    public int GetHouseCount()
    {
        return houses.Count;
    }
}
