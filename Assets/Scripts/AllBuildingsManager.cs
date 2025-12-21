using UnityEngine;
using System.Collections.Generic;

public class AllBuildingsManager : MonoBehaviour
{
    public static AllBuildingsManager Instance { get; private set; }

    private readonly List<House> houses = new();
    private readonly List<ProductionBuilding> producers = new();
    private readonly List<PlacedObject> otherBuildings = new();

    [SerializeField] private float checkInterval = 5f; // как часто проверять нужды
    private float timer = 0f;

    // для автоапгрейда домов
    private readonly Dictionary<string, float> reservedResources = new();

    // === КЭШИ ДЛЯ ПРОИЗВОДСТВЕННОГО ТИКА ===
    private List<string> cachedResourceNames = new();
    private readonly Dictionary<string, int> pooledResources = new();
    private readonly HashSet<ProductionBuilding> runnableCache = new();
    
    // === КЭШИ ДЛЯ ДОМОВ / АПГРЕЙДА ===
    private readonly List<House> tmpReadyLvl1to2 = new();
    private readonly List<House> tmpReadyLvl2to3 = new();
    private readonly List<House> tmpReadyLvl3to4 = new();

    private readonly Dictionary<string, float> surplusWork = new();

// чтобы не апгрейдить дома каждый тик, а, например, раз в 4 тика экономики
    [SerializeField] private int houseUpgradeEveryNthTick = 1;
    [SerializeField] private int housesToUpgradePerTick = 2;

    private int houseTickCounter = 0;

    public int totalHouses = 0;
    public int satisfiedHousesCount = 0;


    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
           
            Destroy(gameObject);
            return;
        }

        Instance = this;
      

        // ⚠ ВАЖНО: НЕ дергаем ResourceManager здесь, потому что порядок Awake не гарантирован.
        // CacheResourceNames будет вызван лениво в CheckNeedsAllProducers при первой необходимости.
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
            ResourceManager.Instance.UpdateGlobalMood();

            if (ResearchNode.CurrentHoveredNode != null &&
                TooltipUI.Instance != null &&
                TooltipUI.Instance.gameObject.activeSelf)
            {
                TooltipUI.Instance.UpdateText(ResearchNode.CurrentHoveredNode.GetTooltipText());
            }

            // 🔹 Обновляем UI РОВНО один раз после тика экономики
            if (ResourceUIManager.Instance != null)
                ResourceUIManager.Instance.ForceUpdateUI();

            if (InfoUI.Instance != null)
                InfoUI.Instance.RefreshIfVisible();
        }
    }


    private void CacheResourceNames()
    {
        var rm = ResourceManager.Instance;
        if (rm == null)
        {
           
            cachedResourceNames = new List<string>();
            return;
        }

        cachedResourceNames = rm.GetAllResourceNames();  // создаём один раз

        pooledResources.Clear();
        foreach (var r in cachedResourceNames)
            pooledResources[r] = 0;

     
    }
    public void RegisterHouse(House house)
    {
        if (!houses.Contains(house))
        {
            houses.Add(house);
            totalHouses++;                     // NEW
            if (house.needsAreMet)             // если сразу удовлетворён
                satisfiedHousesCount++;        // NEW
        }
    }

    public void UnregisterHouse(House house)
    {
        if (houses.Contains(house))
        {
            if (house.needsAreMet)
                satisfiedHousesCount--;        // NEW

            houses.Remove(house);
            totalHouses--;                     // NEW
        }
    }
    public void OnHouseNeedsChanged(House house, bool nowSatisfied)
    {
        if (nowSatisfied)
            satisfiedHousesCount++;
        else
            satisfiedHousesCount--;

        // ограничение (защита от ошибок)
        if (satisfiedHousesCount < 0)
            satisfiedHousesCount = 0;
        if (satisfiedHousesCount > totalHouses)
            satisfiedHousesCount = totalHouses;
    }


    public void RegisterProducer(ProductionBuilding pb)
    {
        if (!producers.Contains(pb))
        {
            producers.Add(pb);
        }
    }

    public void UnregisterProducer(ProductionBuilding pb)
    {
        if (producers.Contains(pb))
        {
            producers.Remove(pb);
        }
    }

    public void RegisterOther(PlacedObject building)
    {
        if (!otherBuildings.Contains(building))
            otherBuildings.Add(building);
    }

    public void UnregisterOther(PlacedObject building)
    {
        if (otherBuildings.Contains(building))
            otherBuildings.Remove(building);
    }



    // ================= ПРОИЗВОДСТВЕННЫЙ ТИК =================
    private void CheckNeedsAllProducers()
    {
        float t0 = Time.realtimeSinceStartup;

        if (producers.Count == 0) return;

        var rm = ResourceManager.Instance;
        if (rm == null)
        {
            return;
        }

        // --- 0. Имена ресурсов (кеш) ---
        if (cachedResourceNames == null || cachedResourceNames.Count == 0)
        {
            CacheResourceNames();
        }

        // --- 0. БАЗОВЫЙ ПУЛ: склад на начало тика ---
        pooledResources.Clear();
        foreach (var name in cachedResourceNames)
        {
            int val = rm.GetResource(name);
            pooledResources[name] = val;
        }

        // --- 0.1. ДОБАВЛЯЕМ ПРОИЗВОДСТВО ЭТОГО ТИКА В ПУЛ ---
        foreach (var pb0 in producers)
        {
            if (pb0 == null) continue;

            if (pb0.production != null)
            {
                foreach (var kv in pb0.production)
                {
                    if (!pooledResources.ContainsKey(kv.Key))
                    {
                        pooledResources[kv.Key] = 0;
                    }

                    pooledResources[kv.Key] += kv.Value;
                }
            }
        }
        
        int simFreeWorkers = rm.FreeWorkers;
        runnableCache.Clear();

        // --- 1-я фаза: ПРОВЕРКА ОКРУЖЕНИЯ + РЕЗЕРВАЦИЯ В ПУЛЕ ---
        for (int i = 0; i < producers.Count; i++)
        {
            var pb = producers[i];
            if (pb == null) continue;

            pb.lastMissingResources.Clear();

            // 1) окружение
            if (!pb.CheckEnvironmentOnly())
                continue;

// 1.5) проверка рабочих (ВАЖНО: до резерва ресурсов)
            bool alreadyStaffed = rm.HasWorkersAllocated(pb);
            if (!alreadyStaffed)
            {
                int needW = pb.WorkersRequired;
                if (needW > 0)
                {
                    if (simFreeWorkers < needW)
                    {
                        // можно пометить как "не хватает работников"
                        // pb.lastMissingResources.Add("People"); // если хочешь показывать в UI
                        continue;
                    }
                    simFreeWorkers -= needW;
                }
            }

            var needs = pb.consumptionCost;

            // 2) если входов нет — можно запускать без резерва
            if (needs == null || needs.Count == 0)
            {
                runnableCache.Add(pb);
                continue;
            }

            // 3) проверяем ресурсы в пуле
            bool ok = true;
            foreach (var kv in needs)
            {
                int available = pooledResources.TryGetValue(kv.Key, out var v) ? v : 0;
                if (available < kv.Value)
                {
                    ok = false;
                    pb.lastMissingResources.Add(kv.Key);
                }
            }

            if (!ok)
            {
                string missing = string.Join(", ", pb.lastMissingResources);
                continue;
            }

            // 4) резервируем из пула
            foreach (var kv in needs)
            {
                int available = pooledResources.TryGetValue(kv.Key, out var v) ? v : 0;
                pooledResources[kv.Key] = available - kv.Value;
            }

            runnableCache.Add(pb);
        }

        // --- 2-я фаза: ВКЛЮЧАЕМ (АЛЛОЦИРУЕМ РАБОЧИХ) → СПИСЫВАЕМ → ПРОИЗВОДИМ ---
        for (int i = 0; i < producers.Count; i++)
        {
            var pb = producers[i];
            if (pb == null) continue;

            if (!runnableCache.Contains(pb))
            {
                // не прошло окружение/ресурсы в пуле
                pb.ApplyNeedsResult(false);
                continue;
            }

            // 1) СНАЧАЛА пробуем включить здание (тут выделяются рабочие и ставится isActive)
            pb.ApplyNeedsResult(true);

            // 2) Если не хватило рабочих — ничего не списываем и не производим
            if (!pb.isActive)
            {
                // на всякий случай гарантируем выключенное состояние
                pb.ApplyNeedsResult(false);
                continue;
            }

            // 3) Теперь можно списывать входы со склада
            if (pb.consumptionCost != null && pb.consumptionCost.Count > 0)
            {
                rm.SpendResources(pb.consumptionCost);
            }

            // 4) И только после этого — производство
            pb.RunProductionTick();
        }

        
        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dt > 5f)
            Debug.Log($"[PERF] CheckNeedsAllProducers занял {dt:F2} ms");
    }

    // ================= ДОМА + НАСТРОЕНИЕ =================
   private void CheckNeedsAllHouses()
{
    // PERF лог оставляем, чтобы видеть, сколько стало после оптимизации
    float t0 = Time.realtimeSinceStartup;

    if (houses.Count == 0) return;

    // 🔹 Шаг 1 — проверяем нужды всех домов (это нужно каждый тик)
    foreach (var house in houses)
    {
        if (house == null) continue;
        bool satisfied = house.CheckNeeds();
        house.ApplyNeedsResult(satisfied);
        // Debug.Log($"[ABM] House {house.name}: needsAreMet={house.needsAreMet}");
    }

    // 🔹 Шаг 2 — апгрейды делаем НЕ каждый тик, а, скажем, раз в N тиков экономики
    houseTickCounter++;
    if (houseTickCounter % houseUpgradeEveryNthTick != 0)
    {
        float dtFast = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dtFast > 5f)
            Debug.Log($"[PERF] CheckNeedsAllHouses (no upgrades) занял {dtFast:F2} ms");
        return;
    }

    // === дальше — только логика апгрейдов, уже без аллокаций new List/new Dictionary ===

    tmpReadyLvl1to2.Clear();
    tmpReadyLvl2to3.Clear();
    tmpReadyLvl3to4.Clear();
    surplusWork.Clear();

    var rm = ResourceManager.Instance;
    if (rm == null)
    {
        Debug.LogError("[ABM] CheckNeedsAllHouses(): ResourceManager.Instance == null");
        return;
    }

    // 🔹 Шаг 3 — считаем экономические излишки в переиспользуемый словарь
    var resourceNames = rm.GetAllResourceNames();
    foreach (var name in resourceNames)
    {
        float prod = rm.GetProduction(name);
        float cons = rm.GetConsumption(name);
        float diff = prod - cons;
        if (diff > 0)
            surplusWork[name] = diff;
    }

    // 🔹 Шаг 4 — собираем кандидатов на апгрейд
    foreach (var house in houses)
    {
        if (house == null || !house.CanAutoUpgrade()) continue;

        Dictionary<string, int> nextCons = null;
        if (house.CurrentStage == 1) nextCons = house.consumptionLvl2;
        else if (house.CurrentStage == 2) nextCons = house.consumptionLvl3;
        else if (house.CurrentStage == 3) nextCons = house.consumptionLvl4;


        if (nextCons == null || nextCons.Count == 0) continue;

        // Проверяем, можем ли зарезервировать ресурсы
        if (CanReserveResources(nextCons, surplusWork))
        {
            ReserveResources(nextCons, surplusWork);
            house.reservedForUpgrade = true;

            if (house.CurrentStage == 1)
                tmpReadyLvl1to2.Add(house);
            else if (house.CurrentStage == 2)
                tmpReadyLvl2to3.Add(house);
            else if (house.CurrentStage == 3)
                tmpReadyLvl3to4.Add(house);

        }
    }

    // 🔹 Шаг 5 — улучшаем дома
// 🔹 Шаг 5 — улучшаем до N домов каждого типа за тик
    int n = Mathf.Max(1, housesToUpgradePerTick);

    UpgradeUpToNFromList(tmpReadyLvl1to2, n);
    UpgradeUpToNFromList(tmpReadyLvl2to3, n);
    UpgradeUpToNFromList(tmpReadyLvl3to4, n);




    float dt = (Time.realtimeSinceStartup - t0) * 1000f;
    if (dt > 5f)
        Debug.Log($"[PERF] CheckNeedsAllHouses (with upgrades) занял {dt:F2} ms");
}

    // ===== Подсчёт излишков =====
    public Dictionary<string, float> CalculateSurplus()
    {
        float t0 = Time.realtimeSinceStartup;

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

        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dt > 5f)
            Debug.Log($"[PERF] calculateSurplus занял {dt:F2} ms");
        return result;
        
    }

    private House ChooseHouseToUpgrade(List<House> list)
    {
        return list.Count > 0 ? list[0] : null;
    }
    
    private void UpgradeUpToNFromList(List<House> list, int n)
    {
        if (list == null || list.Count == 0 || n <= 0) return;

        int upgraded = 0;

        for (int i = 0; i < list.Count && upgraded < n; i++)
        {
            var h = list[i];
            if (h == null) continue;

            // Должен быть зарезервирован (это твой маркер кандидата)
            if (!h.reservedForUpgrade)
                continue;

            // ❗ НЕ сбрасываем флаг ДО апгрейда — иначе TryAutoUpgrade может отказать
            h.TryAutoUpgrade();

            // Теперь можно сбросить (текущий тик закончился)
            h.reservedForUpgrade = false;

            upgraded++;
        }
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

    // ===== Остальное как было =====
    public int GetBuildingCount(BuildManager.BuildMode mode)
    {
        int count = 0;
        foreach (var h in houses)
        {
            if (h == null) continue;
            if (h.BuildMode == mode) count++;
        }
        foreach (var p in producers)
        {
            if (p == null) continue;
            if (p.BuildMode == mode) count++;
        }
        foreach (var p in otherBuildings)
        {
            if (p == null) continue;
            if (p.BuildMode == mode) count++;
        }
        return count;
    }

    public int GetBuildingCount<T>() where T : PlacedObject
    {
        int count = 0;

        foreach (var h in houses)
            if (h != null && h is T) count++;

        foreach (var p in producers)
            if (p != null && p is T) count++;

        return count;
    }

    public int GetTotalBuildingsCount()
    {
        int count = 0;
        foreach (var h in houses) if (h != null) count++;
        foreach (var p in producers) if (p != null) count++;
        return count;
    }
    
    public IReadOnlyList<ProductionBuilding> GetProducers()
    {
        return producers;
    }


    public IEnumerable<PlacedObject> GetAllBuildings()
    {
        foreach (var h in houses) yield return h;
        foreach (var p in producers) yield return p;
    }
}
