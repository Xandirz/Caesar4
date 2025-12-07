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

    // ===== Регистрация зданий =====
    public void RegisterHouse(House house)
    {
        if (!houses.Contains(house))
        {
            houses.Add(house);
          
        }
    }

    public void UnregisterHouse(House house)
    {
        if (houses.Contains(house))
        {
            houses.Remove(house);
          
        }
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

        runnableCache.Clear();

        // --- 1-я фаза: ПРОВЕРКА ОКРУЖЕНИЯ + РЕЗЕРВАЦИЯ В ПУЛЕ ---
        for (int i = 0; i < producers.Count; i++)
        {
            var pb = producers[i];
            if (pb == null) continue;

            pb.lastMissingResources.Clear();

            // 1) окружение
            if (!pb.CheckEnvironmentOnly())
            {
                continue;
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

        // --- 2-я фаза: СПИСЫВАЕМ РЕСУРСЫ СО СКЛАДА И ЗАПУСКАЕМ ПРОИЗВОДСТВО ---
        for (int i = 0; i < producers.Count; i++)
        {
            var pb = producers[i];
            if (pb == null) continue;

            bool wasActiveBefore = pb.isActive;

            if (runnableCache.Contains(pb))
            {
                // списываем входы со склада
                if (pb.consumptionCost != null && pb.consumptionCost.Count > 0)
                {
                    rm.SpendResources(pb.consumptionCost);
                }
                else
                {
                }

                // производим
                pb.RunProductionTick();

                // пробуем включить здание
                pb.ApplyNeedsResult(true);

                if (!pb.isActive)
                {
                    int freeWorkers = rm.FreeWorkers;
                    int reqWorkers  = pb.WorkersRequired;
                }
              
            }
            else
            {
                
                pb.ApplyNeedsResult(false);
            }
        }
    }

    // ================= ДОМА + НАСТРОЕНИЕ =================
    private void CheckNeedsAllHouses()
    {

        if (houses.Count == 0) return;

        // 🔹 Шаг 1 — проверяем нужды всех домов
        foreach (var house in houses)
        {
            if (house == null) continue;
            bool satisfied = house.CheckNeeds();
            house.ApplyNeedsResult(satisfied);
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

    public IEnumerable<PlacedObject> GetAllBuildings()
    {
        foreach (var h in houses) yield return h;
        foreach (var p in producers) yield return p;
    }
}
