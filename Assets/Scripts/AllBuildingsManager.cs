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

        // 🔹 Шаг 1 — проверяем нужды всех домов
        foreach (var house in houses)
        {
            if (house == null) continue;
            house.ApplyNeedsResult(house.CheckNeeds());
        }

        // 🔹 Шаг 2 — считаем экономические излишки
        Dictionary<string, float> surplus = CalculateSurplus();

        // 🔹 Шаг 3 — два списка для разных уровней апгрейда
        List<House> readyLvl1to2 = new();
        List<House> readyLvl2to3 = new();

        foreach (var house in houses)
        {
            if (house == null || !house.CanAutoUpgrade()) continue;

            Dictionary<string, int> nextCons = null;
            if (house.CurrentStage == 1)
                nextCons = house.consumptionLvl2;
            else if (house.CurrentStage == 2)
                nextCons = house.consumptionLvl3;

            if (nextCons == null) continue;

            // Проверяем, можем ли зарезервировать ресурсы
            if (CanReserveResources(nextCons, surplus))
            {
                ReserveResources(nextCons, surplus);
                house.reservedForUpgrade = true;

                if (house.CurrentStage == 1)
                    readyLvl1to2.Add(house);
                else if (house.CurrentStage == 2)
                    readyLvl2to3.Add(house);
            }
        }

        // 🔹 Шаг 4 — улучшаем только один дом каждого типа
        if (readyLvl1to2.Count > 0)
        {
            House chosen = ChooseHouseToUpgrade(readyLvl1to2);
            if (chosen != null)
                chosen.TryAutoUpgrade();
        }

        if (readyLvl2to3.Count > 0)
        {
            House chosen = ChooseHouseToUpgrade(readyLvl2to3);
            if (chosen != null)
                chosen.TryAutoUpgrade();
        }

        reservedResources.Clear();
    }



    // ===== Подсчёт излишков =====
    public Dictionary<string, float> CalculateSurplus()
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
    
    private House ChooseHouseToUpgrade(List<House> list)
    {
        // Можно сделать любую стратегию — сейчас просто выбираем первый
        // (в будущем можно выбирать ближайший к центру, с доступом к воде и т.п.)
        return list.Count > 0 ? list[0] : null;
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
