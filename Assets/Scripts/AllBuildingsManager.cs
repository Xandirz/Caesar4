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
    private readonly List<House> tmpReadyLvl4to5 = new();

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

            if (ResourceUIManager.Instance != null)
                ResourceUIManager.Instance.ForceUpdateUI();

            if (InfoUI.Instance != null)
                InfoUI.Instance.RefreshIfVisible();

            // 🔴 ДЕБАГ: ПОЧЕМУ ЗДАНИЯ НЕ АКТИВНЫ
            DebugProducersState();
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

    private void DebugProducersState()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        foreach (var pb in AllBuildingsManager.Instance.GetProducers())
        {
            if (pb == null) continue;

            // интересуют только неактивные
            if (pb.isActive) continue;

            bool hasAlloc = rm.HasWorkersAllocated(pb);

            string reason =
                pb.IsPaused ? "PAUSED" :
                    (!pb.needsAreMet ? "NEEDS_NOT_MET" :
                        (pb.WorkersRequired > 0 && rm.FreeWorkers < pb.WorkersRequired ? "NO_WORKERS" :
                            (hasAlloc ? "HAS_WORKERS_BUT_INACTIVE" : "NO_ALLOCATION")));

            Debug.Log(
                $"[PROD CHECK] {pb.name} | paused={pb.IsPaused} active={pb.isActive} " +
                $"needsAreMet={pb.needsAreMet} req={pb.WorkersRequired} " +
                $"alloc={hasAlloc} free={rm.FreeWorkers} assigned={rm.AssignedWorkers} " +
                $"=> {reason}"
            );
        }
    }

    private void CheckNeedsAllProducers()
    {
        float t0 = Time.realtimeSinceStartup;

        if (producers.Count == 0)
            return;

        var rm = ResourceManager.Instance;
        if (rm == null)
            return;

        // --- кеш имён ресурсов ---
        if (cachedResourceNames == null || cachedResourceNames.Count == 0)
            CacheResourceNames();

        // --- пул ресурсов на начало тика ---
        pooledResources.Clear();
        foreach (var name in cachedResourceNames)
            pooledResources[name] = rm.GetResource(name);

        // ✅ FIX #1: добавляем производство АКТИВНЫХ зданий в пул (цепочки в один тик)
        for (int i = 0; i < producers.Count; i++)
        {
            var pb0 = producers[i];
            if (pb0 == null) continue;
            if (!pb0.isActive) continue;
            if (pb0.production == null) continue;

            foreach (var kv in pb0.production)
            {
                if (!pooledResources.ContainsKey(kv.Key))
                    pooledResources[kv.Key] = 0;

                pooledResources[kv.Key] += kv.Value;
            }
        }

        runnableCache.Clear();

        // --- основной проход ---
        for (int i = 0; i < producers.Count; i++)
        {
            var pb = producers[i];
            if (pb == null)
                continue;

            pb.lastMissingResources.Clear();

            bool shouldRun = true;

            // 1) окружение
            if (!pb.CheckEnvironmentOnly())
                shouldRun = false;

            // ✅ FIX #3: авто-понижение уровня при нехватке ресурсов (ПО ПУЛУ, не по складу)
            if (shouldRun && pb.CurrentStage > 1 && pb.consumptionCost != null && pb.consumptionCost.Count > 0)
            {
                bool enoughForLevel = true;
                foreach (var kv in pb.consumptionCost)
                {
                    int available = pooledResources.TryGetValue(kv.Key, out var v) ? v : 0;
                    if (available < kv.Value)
                    {
                        enoughForLevel = false;
                        break;
                    }
                }

                if (!enoughForLevel)
                {
                    pb.TryDowngradeOneLevel();
                    shouldRun = false; // в этом тике не работаем
                }
            }

            // 2) проверка входных ресурсов (без списания) — по пулу
            var needs = pb.consumptionCost;
            if (shouldRun && needs != null && needs.Count > 0)
            {
                foreach (var kv in needs)
                {
                    int available = pooledResources.TryGetValue(kv.Key, out var v) ? v : 0;
                    if (available < kv.Value)
                    {
                        shouldRun = false;
                        pb.lastMissingResources.Add(kv.Key);
                    }
                }
            }

            // ✅ FIX #2: один-единственный вызов ApplyNeedsResult на тик для здания
            pb.ApplyNeedsResult(shouldRun);
            
            
// ✅ FIX: если не хватает рабочих — считаем нужды НЕ удовлетворены (чтобы появился stopPrefab)
            if (shouldRun && !pb.IsPaused && pb.WorkersRequired > 0)
            {
                // если уже есть назначенные рабочие — ок
                // если нет — проверяем, хватает ли свободных (по реальному RM)
                // (этого достаточно, чтобы корректно включать stopPrefab при нехватке рабочих)
                if (!rm.HasWorkersAllocated(pb) && rm.FreeWorkers < pb.WorkersRequired)
                {
                    shouldRun = false;

                    // опционально: чтобы в UI/дебаге было видно, что проблема в рабочих
                    // pb.lastMissingResources.Add("People");
                }
            }

            // если не должно работать или не активировалось (нет рабочих/пауза/прочее) — выходим
            if (!shouldRun || !pb.isActive)
                continue;

            // 4) теперь можно списывать входы
            if (needs != null && needs.Count > 0)
            {
                foreach (var kv in needs)
                    pooledResources[kv.Key] -= kv.Value;

                rm.SpendResources(needs);
            }

            // 5) производство
            pb.RunProductionTick();
        }

        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dt > 5f)
            Debug.Log($"[PERF] CheckNeedsAllProducers занял {dt:F2} ms");
    }

    // ================= ДОМА + НАСТРОЕНИЕ =================
    private void CheckNeedsAllHouses()
    {
        float t0 = Time.realtimeSinceStartup;

        if (houses.Count == 0) return;

        // 🔹 Шаг 1 — проверяем нужды всех домов (это нужно каждый тик)
        foreach (var house in houses)
        {
            if (house == null) continue;
            bool satisfied = house.CheckNeeds();
            house.ApplyNeedsResult(satisfied);
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

        // 🔹 Шаг 3 — считаем экономические излишки
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
            else if (house.CurrentStage == 4) nextCons = house.consumptionLvl5;

            if (nextCons == null || nextCons.Count == 0) continue;

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
                else if (house.CurrentStage == 4)
                    tmpReadyLvl4to5.Add(house);
            }
        }

        // 🔹 Шаг 5 — улучшаем дома
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

            if (!h.reservedForUpgrade)
                continue;

            h.TryAutoUpgrade();
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
