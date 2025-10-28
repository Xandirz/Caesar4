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
            ResourceManager.Instance.ApplyStorageLimits();
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

    var rm = ResourceManager.Instance;

    // --- 1) Снимок складов на начало тика ---
    // Берём все ресурсы и формируем "пул" для резервации (не трогаем реальные склады).
    var names = rm.GetAllResourceNames();
    var pool = new Dictionary<string, int>(names.Count);
    foreach (var name in names)
        pool[name] = rm.GetResource(name);

    // Кого сможем запустить в этом тике (по приоритету старых)
    var runnable = new HashSet<ProductionBuilding>();

    // --- 1-я фаза: РЕЗЕРВАЦИЯ ВХОДОВ (старые -> новые) ---
    // Резервируем только по имеющемуся на начало тика; выпуск текущего тика не учитываем.
    for (int i = 0; i < producers.Count; i++)
    {
        var pb = producers[i];
        if (pb == null) continue;

        // если входов нет — запуск возможен без резерва
        var needs = pb.consumptionCost;
        if (needs == null || needs.Count == 0)
        {
            runnable.Add(pb);
            continue;
        }

        bool ok = true;
        foreach (var kv in needs)
        {
            int available = pool.TryGetValue(kv.Key, out var v) ? v : 0;
            if (available < kv.Value) { ok = false; break; }
        }

        if (!ok) continue;

        // резервируем (снимаем из пула, но не с реальных складов)
        foreach (var kv in needs)
        {
            int available = pool.TryGetValue(kv.Key, out var v) ? v : 0;
            pool[kv.Key] = available - kv.Value;
        }

        runnable.Add(pb);
    }

    // --- 2-я фаза: ФАКТИЧЕСКОЕ ВЫПОЛНЕНИЕ (старые -> новые) ---
    // Теперь реально списываем входы и производим выходы только тем, кому хватило в резервации.
    for (int i = 0; i < producers.Count; i++)
    {
        var pb = producers[i];
        if (pb == null) continue;

        if (runnable.Contains(pb))
        {
            bool satisfied = pb.CheckNeeds();   // здесь произойдёт реальное списание/выпуск
            pb.ApplyNeedsResult(satisfied);
        }
        else
        {
            // этим в этом тике не хватило входов по приоритету — явно выключаем
            pb.ApplyNeedsResult(false);
        }
    }
}


public IEnumerable<PlacedObject> GetAllBuildings()
{
    foreach (var h in houses) yield return h;
    foreach (var p in producers) yield return p;
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
