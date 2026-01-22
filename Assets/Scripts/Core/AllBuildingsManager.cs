using UnityEngine;
using System.Collections.Generic;

public class AllBuildingsManager : MonoBehaviour
{
    public static AllBuildingsManager Instance { get; private set; }

    private readonly List<House> houses = new();
    private readonly List<ProductionBuilding> producers = new();
    private readonly List<PlacedObject> otherBuildings = new();

    [Header("Economy Tick")]
    [SerializeField] private float checkInterval = 5f; // раз в сколько секунд стартует тик
    private float timer = 0f;
    
    public float CheckInterval => checkInterval;
    public float CheckTimer => timer;

    [Header("Houses batching (no lag)")]
    [Tooltip("Сколько домов обрабатывать за 1 кадр в фазе Needs.")]
    [SerializeField] private int housesNeedsPerFrame = 200;

    [Tooltip("Сколько домов сканировать за 1 кадр в фазе UpgradeScan.")]
    [SerializeField] private int housesUpgradeScanPerFrame = 200;

    [Tooltip("Сколько домов улучшать за тик (на каждый список уровней, как в старой логике).")]
    [SerializeField] private int housesToUpgradePerTick = 2;

    [Header("Perf / Debug")]
    [SerializeField] private bool perfLog = true;
    [SerializeField] private float perfLogThresholdMs = 5f;

    [SerializeField] private bool debugProducers = false;
    [SerializeField] private int debugProducersEveryNthTick = 6;
// === DEBUG: Effects timing ===
    private float perfEffectsMs = 0f;
    private int perfEffectsCount = 0;
    private readonly HashSet<House> dirtyEffects = new();

    // Статистика
    public int totalHouses = 0;
    public int satisfiedHousesCount = 0;

    // ===== РЕЗЕРВЫ / АПГРЕЙДЫ =====
    private readonly Dictionary<string, float> reservedResources = new();
    private readonly Dictionary<string, float> surplusWork = new();

    private readonly List<House> tmpReadyLvl1to2 = new();
    private readonly List<House> tmpReadyLvl2to3 = new();
    private readonly List<House> tmpReadyLvl3to4 = new();
    private readonly List<House> tmpReadyLvl4to5 = new();

    // ===== КЭШ ДЛЯ PRODUCERS =====
    private List<string> cachedResourceNames = new();
    private readonly Dictionary<string, int> pooledResources = new();
    private readonly Dictionary<string, int> totalSpend = new();

    // ===== СОСТОЯНИЕ ТИКА (размазываем по кадрам) =====
    private enum EconPhase { None, HousesNeeds, UpgradesPrepare, UpgradesScan, UpgradesApply, Finish }

    private bool econTickInProgress = false;
    private EconPhase phase = EconPhase.None;

    private int houseCursor = 0;
    private int upgradeCursor = 0;

    private int econTickCounter = 0;
    public int EconTickCounter => econTickCounter;

    // PERF: тик теперь многокадровый, поэтому копим время по фазам
    private float tickStartTime;
    private int tickFrames;
    private float perfProducersMs, perfNeedsMs, perfUpgPrepareMs, perfUpgScanMs, perfUpgApplyMs, perfFinishMs;
    private readonly Dictionary<string, int> housePooledResources = new();
    private readonly Dictionary<string, int> houseTotalSpend = new();
    private float perfHouseSpendMs = 0f;
    public event System.Action OnEconomyTick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void Debug_AddEffectsTime(float ms)
    {
        perfEffectsMs += ms;
        perfEffectsCount++;
    }

    private void Update()
    {

        timer += Time.deltaTime;

        // Старт тика раз в checkInterval, но без наложения тиков друг на друга
        if (!econTickInProgress && timer >= checkInterval)
        {
            
            StartEconomyTick();
            OnEconomyTick?.Invoke();

        }

        // Если тик активен — выполняем 1 шаг (1 фазу/часть фазы) в этот кадр
        if (econTickInProgress)
        {
            tickFrames++;
            StepEconomyTick();
        }
    }

    private void StartEconomyTick()
    {
        timer = 0f;
        econTickInProgress = true;
        phase = EconPhase.HousesNeeds;

        econTickCounter++;
        tickFrames = 0;

        // === PERF reset ===
        tickStartTime = Time.realtimeSinceStartup;

        perfProducersMs = 0f;
        perfNeedsMs = 0f;
        perfUpgPrepareMs = 0f;
        perfUpgScanMs = 0f;
        perfUpgApplyMs = 0f;
        perfFinishMs = 0f;
        perfHouseSpendMs = 0f;

        // 🔍 DEBUG: CheckEffects profiling
        perfEffectsMs = 0f;
        perfEffectsCount = 0;

        // === 1) PRODUCERS — одним куском (они быстрые) ===
        float tProd = Time.realtimeSinceStartup;
        CheckNeedsAllProducers();
        perfProducersMs += (Time.realtimeSinceStartup - tProd) * 1000f;

        // === 2) Подготовка фаз домов ===
        houseCursor = 0;
        upgradeCursor = 0;

        // === 3) Чистим апгрейд-кэши ===
        reservedResources.Clear();
        surplusWork.Clear();

        tmpReadyLvl1to2.Clear();
        tmpReadyLvl2to3.Clear();
        tmpReadyLvl3to4.Clear();
        tmpReadyLvl4to5.Clear();

        // === 4) Инициализация пулов ресурсов для домов ===
        InitHousePool();
    }



    private void StepEconomyTick()
    {
        switch (phase)
        {
            case EconPhase.HousesNeeds:
            {
                float t0 = Time.realtimeSinceStartup;
                ProcessHousesNeedsBatch(Mathf.Max(1, housesNeedsPerFrame));
                perfNeedsMs += (Time.realtimeSinceStartup - t0) * 1000f;

                if (houseCursor >= houses.Count)
                {
                    float tSpend = Time.realtimeSinceStartup;
                    if (houseTotalSpend.Count > 0)
                        ResourceManager.Instance.SpendResources(houseTotalSpend);
                    perfHouseSpendMs += (Time.realtimeSinceStartup - tSpend) * 1000f;

                    phase = EconPhase.UpgradesPrepare;
                }


                break;
            }

            case EconPhase.UpgradesPrepare:
            {
                float t0 = Time.realtimeSinceStartup;
                PrepareSurplusForUpgrades(); // считаем prod-cons
                perfUpgPrepareMs += (Time.realtimeSinceStartup - t0) * 1000f;

                phase = EconPhase.UpgradesScan;
                break;
            }

            case EconPhase.UpgradesScan:
            {
                float t0 = Time.realtimeSinceStartup;
                ProcessUpgradeScanBatch(Mathf.Max(1, housesUpgradeScanPerFrame));
                perfUpgScanMs += (Time.realtimeSinceStartup - t0) * 1000f;

                if (upgradeCursor >= houses.Count)
                    phase = EconPhase.UpgradesApply;

                break;
            }

            case EconPhase.UpgradesApply:
            {
                float t0 = Time.realtimeSinceStartup;
                ApplyUpgrades(); // апгрейды каждый тик
                perfUpgApplyMs += (Time.realtimeSinceStartup - t0) * 1000f;

                phase = EconPhase.Finish;
                break;
            }

            case EconPhase.Finish:
            {
                float t0 = Time.realtimeSinceStartup;
                FinishEconomyTick();
                perfFinishMs += (Time.realtimeSinceStartup - t0) * 1000f;

                EndEconomyTick();
                break;
            }
        }
    }

    private void EndEconomyTick()
    {
        econTickInProgress = false;
        phase = EconPhase.None;

        float totalMs = (Time.realtimeSinceStartup - tickStartTime) * 1000f;

        if (perfLog && totalMs >= perfLogThresholdMs)
        {
            Debug.Log(
                $"[PERF] ECON TICK total={totalMs:F2}ms frames={tickFrames} " +
                $"Producers={perfProducersMs:F2} " +
                $"Needs={perfNeedsMs:F2} " +
                $"Effects={perfEffectsMs:F2}ms({perfEffectsCount}) " + // <-- ВАЖНО
                $"HouseSpend={perfHouseSpendMs:F2} " +
                $"UpgPrep={perfUpgPrepareMs:F2} " +
                $"UpgScan={perfUpgScanMs:F2} " +
                $"UpgApply={perfUpgApplyMs:F2} " +
                $"Finish={perfFinishMs:F2} " +
                $"houses={houses.Count} producers={producers.Count}"
            );

        }

        if (debugProducers && (econTickCounter % Mathf.Max(1, debugProducersEveryNthTick) == 0))
        {
            DebugProducersState();
        }
    }

    // ========================= PRODUCERS =========================

    private void CacheResourceNames()
    {
        var rm = ResourceManager.Instance;
        if (rm == null)
        {
            cachedResourceNames = new List<string>();
            return;
        }

        cachedResourceNames = rm.GetAllResourceNames();

        // pooledResources используется producers
        pooledResources.Clear();

        // housePooledResources используется houses
        housePooledResources.Clear();

        for (int i = 0; i < cachedResourceNames.Count; i++)
        {
            string r = cachedResourceNames[i];
            pooledResources[r] = 0;
            housePooledResources[r] = 0;
        }
    }

    private void InitHousePool()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        if (cachedResourceNames == null || cachedResourceNames.Count == 0)
            CacheResourceNames();

        for (int i = 0; i < cachedResourceNames.Count; i++)
        {
            string name = cachedResourceNames[i];
            housePooledResources[name] = rm.GetResource(name);
        }

        houseTotalSpend.Clear();
    }



  private void CheckNeedsAllProducers()
{
    float t0 = Time.realtimeSinceStartup;

    totalSpend.Clear();

    if (producers.Count == 0) return;

    ResourceManager rm = ResourceManager.Instance;
    if (rm == null) return;



    if (cachedResourceNames == null || cachedResourceNames.Count == 0)
        CacheResourceNames();

    // 1) Пул ресурсов на начало тика = реальные складские остатки
    for (int i = 0; i < cachedResourceNames.Count; i++)
    {
        string name = cachedResourceNames[i];
        pooledResources[name] = rm.GetResource(name);
    }

    // 2) Добавляем производство активных зданий в пул (цепочки в один тик)
    for (int i = 0; i < producers.Count; i++)
    {
        ProductionBuilding pb0 = producers[i];
        if (pb0 == null) continue;
        if (!pb0.isActive) continue;
        if (pb0.production == null) continue;

        foreach (KeyValuePair<string, int> kv in pb0.production)
        {
            int cur;
            if (!pooledResources.TryGetValue(kv.Key, out cur))
                cur = 0;

            pooledResources[kv.Key] = cur + kv.Value;
        }
    }

    // 3) Основной проход
    for (int i = 0; i < producers.Count; i++)
    {
        ProductionBuilding pb = producers[i];
        if (pb == null) continue;

        pb.lastMissingResources.Clear();

        // Пауза = не работаем
        if (pb.IsPaused)
        {
            pb.ApplyNeedsResult(false);
            continue;
        }

        bool shouldRun = true;

        // 1) окружение
        if (!pb.CheckEnvironmentOnly())
            shouldRun = false;

        Dictionary<string, int> needs = pb.consumptionCost;

// 2) downgrade при нехватке ресурсов — проверяем по ПУЛУ (цепочки в один тик)
        if (shouldRun && pb.CurrentStage > 1 && needs != null && needs.Count > 0)
        {
            string missingKey = null;
            int missingNeed = 0;
            int missingAvail = 0;

            foreach (var kv in needs)
            {
                int availableNow = pooledResources.TryGetValue(kv.Key, out var v) ? v : 0;
                if (availableNow < kv.Value)
                {
                    missingKey = kv.Key;
                    missingNeed = kv.Value;
                    missingAvail = availableNow;
                    break;
                }
            }

            if (missingKey != null)
            {
                int storageNow = rm.GetResource(missingKey);

                Debug.Log(
                    $"[PROD DOWNGRADE] '{pb.name}' stage {pb.CurrentStage}->{pb.CurrentStage - 1} " +
                    $"reason: not enough '{missingKey}' need={missingNeed} avail(pooled)={missingAvail} storage={storageNow} " +
                    $"tick={econTickCounter}"
                );

                // ВАЖНО: делаем downgrade, но НЕ выключаем тик навсегда.
                // Пусть дальше код попробует пройти check inputs уже на пониженном уровне.
                pb.TryDowngradeOneLevel();

                // обновим needs после downgrade (они могли измениться)
                needs = pb.consumptionCost;

                // НЕ ставим shouldRun=false здесь
                // shouldRun будет решаться на шаге 3 (проверка входов) уже с новым needs
            }
        }



        

        // 3) проверка входов (по пулу, чтобы работали цепочки в один тик)
        // 3) проверка входов (по пулу)
        bool failedNewLevel = false;

        if (shouldRun && needs != null && needs.Count > 0)
        {
            foreach (var kv in needs)
            {
                int v = pooledResources.TryGetValue(kv.Key, out var val) ? val : 0;
                if (v < kv.Value)
                {
                    shouldRun = false;
                    failedNewLevel = true;
                    pb.lastMissingResources.Add(kv.Key);
                    break;
                }
            }
        }

// FIX: если это тик сразу после апгрейда — один тик работаем по старому уровню
        if (!shouldRun && failedNewLevel && pb.lastUpgradeTick == econTickCounter - 1)
        {
            var fallbackNeeds = pb.prevConsumptionCost;
            var fallbackProd  = pb.prevProduction;

            if (fallbackNeeds != null && fallbackProd != null)
            {
                bool canRunFallback = true;
                foreach (var kv in fallbackNeeds)
                {
                    int v = pooledResources.TryGetValue(kv.Key, out var val) ? val : 0;
                    if (v < kv.Value) { canRunFallback = false; break; }
                }

                if (canRunFallback)
                {
                    shouldRun = true;
                    needs = fallbackNeeds;
                    pb.SetTemporaryProductionOverrideForThisTick(fallbackProd);
                }
            }
        }



        // 4) рабочие (до ApplyNeedsResult)
        if (shouldRun && pb.WorkersRequired > 0)
        {
            if (!rm.HasWorkersAllocated(pb) && rm.FreeWorkers < pb.WorkersRequired)
                shouldRun = false;
        }

        pb.ApplyNeedsResult(shouldRun);

        if (!shouldRun || !pb.isActive)
            continue;

        // 5) копим списания (и обновляем reservedSpend, чтобы следующие здания видели "уже потрачено")
        if (needs != null && needs.Count > 0)
        {
            foreach (KeyValuePair<string, int> kv in needs)
            {
                // pooledResources для цепочек
                pooledResources[kv.Key] = pooledResources[kv.Key] - kv.Value;
                if (pooledResources[kv.Key] < 0) pooledResources[kv.Key] = 0;

      

                // totalSpend как было
                int curSpend;
                if (totalSpend.TryGetValue(kv.Key, out curSpend))
                    totalSpend[kv.Key] = curSpend + kv.Value;
                else
                    totalSpend[kv.Key] = kv.Value;
            }
        }

        // 6) производство
        pb.RunProductionTick();
    }

    if (totalSpend.Count > 0)
        rm.SpendResources(totalSpend);

    float dt = (Time.realtimeSinceStartup - t0) * 1000f;
    if (perfLog && dt > 5f)
        Debug.Log("[PERF] CheckNeedsAllProducers занял " + dt.ToString("F2") + " ms");
}


    // ========================= HOUSES =========================

    private void ProcessHousesNeedsBatch(int batchSize)
    {
        int processed = 0;
        var bm = BuildManager.Instance;

        while (houseCursor < houses.Count && processed < batchSize)
        {
            var house = houses[houseCursor++];
            processed++;

            if (house == null) continue;

            house.CheckNeedsFromPool(bm, housePooledResources, houseTotalSpend);
        }
    }



    private void PrepareSurplusForUpgrades()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        // 0) Проверяем: появились ли новые ресурсы с момента последнего кеша
        // (самый простой и надёжный индикатор — изменился размер списка)
        var freshNames = rm.GetAllResourceNames();
        if (cachedResourceNames == null || cachedResourceNames.Count == 0 || cachedResourceNames.Count != freshNames.Count)
        {
            CacheResourceNames(); // пересоберёт cachedResourceNames + pooledResources + housePooledResources
        }

        // На всякий случай: если CacheResourceNames() не вызвался, но freshNames обновился,
        // можно синхронизировать cachedResourceNames (не обязательно, но безопасно)
        // cachedResourceNames = freshNames; // <-- обычно НЕ нужно, если CacheResourceNames всё делает правильно

        surplusWork.Clear();

        for (int i = 0; i < cachedResourceNames.Count; i++)
        {
            var name = cachedResourceNames[i];
            float prod = rm.GetProduction(name);
            float cons = rm.GetConsumption(name);
            float diff = prod - cons;
            if (diff > 0)
                surplusWork[name] = diff;
        }
    }

    public void MarkHouseEffectsDirty(House h)
    {
        if (h == null) return;
        h.MarkEffectsDirty();
        dirtyEffects.Add(h);
    }

// массово: пометить все дома в радиусе
    public void MarkEffectsDirtyAround(Vector2Int center, int radius)
    {
        // простой вариант O(N): быстро внедрить и уже даст огромный выигрыш
        for (int i = 0; i < houses.Count; i++)
        {
            var h = houses[i];
            if (h == null) continue;

            // если есть gridPos в PlacedObject
            var p = h.gridPos;
            if (Mathf.Abs(p.x - center.x) <= radius && Mathf.Abs(p.y - center.y) <= radius)
            {
                h.MarkEffectsDirty();
                dirtyEffects.Add(h);
            }
        }
    }
    
  private void ProcessUpgradeScanBatch(int batchSize)
{
    int processed = 0;

    // включай/выключай в инспекторе, чтобы не спамить логами
    bool dbg = perfLog; // или сделай отдельный флаг: [SerializeField] private bool debugUpgrades = true;

    while (upgradeCursor < houses.Count && processed < batchSize)
    {
        var house = houses[upgradeCursor++];
        processed++;

        if (house == null)
            continue;

        // ===== DEBUG: почему дом НЕ проходит CanAutoUpgrade =====
        // (логируем только для stage 3, чтобы не утонуть в спаме)
        if (dbg && house.CurrentStage == 3)
        {
            bool researchStage4 =
                ResearchManager.Instance != null &&
                ResearchManager.Instance.IsResearchCompleted("Stage4");

            Debug.Log(
                $"[UPG3->4 CHECK] {house.name} " +
                $"needsAreMet={house.needsAreMet} road={house.hasRoadAccess} " +
                $"water={house.HasWater} market={house.HasMarket} temple={house.HasTemple} " +
                $"noise={house.InNoise} " +
                $"researchStage4={researchStage4}"
            );
        }

        // Если дом не готов по сервисам/исследованиям — пропускаем
        if (!house.CanAutoUpgrade())
            continue;

        // ===== Выбираем нужды следующего уровня =====
        Dictionary<string, int> nextCons = null;
        if (house.CurrentStage == 1) nextCons = house.consumptionLvl2;
        else if (house.CurrentStage == 2) nextCons = house.consumptionLvl3;
        else if (house.CurrentStage == 3) nextCons = house.consumptionLvl4;
        else if (house.CurrentStage == 4) nextCons = house.consumptionLvl5;

        if (nextCons == null || nextCons.Count == 0)
            continue;

        // ===== DEBUG: почему НЕ проходит surplusWork (самая частая причина) =====
        if (dbg && house.CurrentStage == 3)
        {
            bool ok = true;

            foreach (var kv in nextCons)
            {
                float s = surplusWork.TryGetValue(kv.Key, out var v) ? v : 0f;
                if (s < kv.Value)
                {
                    ok = false;
                    Debug.Log(
                        $"[UPG3->4 BLOCK SURPLUS] {house.name} " +
                        $"need {kv.Key}:{kv.Value} but surplus={s:F2}"
                    );
                }
            }

            if (ok)
            {
                Debug.Log($"[UPG3->4 SURPLUS OK] {house.name} passes surplus check");
            }
        }

        // ===== Основная логика резервирования =====
        if (CanReserveResources(nextCons, surplusWork))
        {
            ReserveResources(nextCons, surplusWork);
            house.reservedForUpgrade = true;

            if (dbg && house.CurrentStage == 3)
                Debug.Log($"[UPG3->4 RESERVED] {house.name} reservedForUpgrade=true");

            if (house.CurrentStage == 1) tmpReadyLvl1to2.Add(house);
            else if (house.CurrentStage == 2) tmpReadyLvl2to3.Add(house);
            else if (house.CurrentStage == 3) tmpReadyLvl3to4.Add(house);
            else if (house.CurrentStage == 4) tmpReadyLvl4to5.Add(house);
        }
        else
        {
            // На случай если CanReserveResources вернул false,
            // но мы не залогировали детали (например не stage 3)
            if (dbg && house.CurrentStage == 3)
                Debug.Log($"[UPG3->4 NOT RESERVED] {house.name} CanReserveResources=false");
        }
    }
}


    private void ApplyUpgrades()
    {
        int n = Mathf.Max(1, housesToUpgradePerTick);

        UpgradeUpToNFromList(tmpReadyLvl1to2, n);
        UpgradeUpToNFromList(tmpReadyLvl2to3, n);
        UpgradeUpToNFromList(tmpReadyLvl3to4, n);
        UpgradeUpToNFromList(tmpReadyLvl4to5, n);
    }

    private void FinishEconomyTick()
    {
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
    }

    // ========================= Registration / Stats =========================

    public void RegisterHouse(House house)
    {
        if (house == null) return;
        if (houses.Contains(house)) return;

        houses.Add(house);
        totalHouses++;
        TutorialEvents.RaiseHousePlaced(totalHouses);

        if (house.needsAreMet)
            satisfiedHousesCount++;
    }

    public void UnregisterHouse(House house)
    {
        if (house == null) return;
        if (!houses.Contains(house)) return;

        if (house.needsAreMet)
            satisfiedHousesCount--;

        houses.Remove(house);
        totalHouses--;
    }

    public void OnHouseNeedsChanged(House house, bool nowSatisfied)
    {
        if (nowSatisfied) satisfiedHousesCount++;
        else satisfiedHousesCount--;

        if (satisfiedHousesCount < 0) satisfiedHousesCount = 0;
        if (satisfiedHousesCount > totalHouses) satisfiedHousesCount = totalHouses;
    }

    public void RegisterProducer(ProductionBuilding pb)
    {
        if (pb == null) return;
        if (!producers.Contains(pb))
            producers.Add(pb);
        
        if (pb.BuildMode == BuildManager.BuildMode.LumberMill)
            TutorialEvents.RaiseLumberMillPlaced();
        if (pb.BuildMode == BuildManager.BuildMode.Berry)
            TutorialEvents.RaiseBerryPlaced();
    }

    public void UnregisterProducer(ProductionBuilding pb)
    {
        if (pb == null) return;
        producers.Remove(pb);
    }

    public void RegisterOther(PlacedObject building)
    {
        if (building == null) return;
        if (!otherBuildings.Contains(building))
            otherBuildings.Add(building);
    }

    public void UnregisterOther(PlacedObject building)
    {
        if (building == null) return;
        otherBuildings.Remove(building);
    }

    // ========================= Upgrades helpers =========================

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

    private bool CanReserveResources(Dictionary<string, int> needs, Dictionary<string, float> surplus)
    {
        foreach (var kvp in needs)
        {
            if (!surplus.TryGetValue(kvp.Key, out var available)) return false;
            if (available < kvp.Value) return false;
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

    public void RecheckAllHousesUpgrade()
    {
        for (int i = 0; i < houses.Count; i++)
        {
            if (houses[i] != null)
                houses[i].CanAutoUpgrade();
        }
    }

    // ========================= Debug / Queries =========================

    private void DebugProducersState()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        for (int i = 0; i < producers.Count; i++)
        {
            var pb = producers[i];
            if (pb == null) continue;

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

    public int GetHouseCount() => houses.Count;

    public int GetBuildingCount(BuildManager.BuildMode mode)
    {
        int count = 0;

        for (int i = 0; i < houses.Count; i++)
        {
            var h = houses[i];
            if (h != null && h.BuildMode == mode) count++;
        }

        for (int i = 0; i < producers.Count; i++)
        {
            var p = producers[i];
            if (p != null && p.BuildMode == mode) count++;
        }

        for (int i = 0; i < otherBuildings.Count; i++)
        {
            var b = otherBuildings[i];
            if (b != null && b.BuildMode == mode) count++;
        }

        return count;
    }

    public int GetBuildingCount<T>() where T : PlacedObject
    {
        int count = 0;

        for (int i = 0; i < houses.Count; i++)
            if (houses[i] != null && houses[i] is T) count++;

        for (int i = 0; i < producers.Count; i++)
            if (producers[i] != null && producers[i] is T) count++;

        return count;
    }

    public int GetTotalBuildingsCount()
    {
        int count = 0;

        for (int i = 0; i < houses.Count; i++) if (houses[i] != null) count++;
        for (int i = 0; i < producers.Count; i++) if (producers[i] != null) count++;

        return count;
    }

    public IReadOnlyList<ProductionBuilding> GetProducers() => producers;

    public IEnumerable<PlacedObject> GetAllBuildings()
    {
        for (int i = 0; i < houses.Count; i++) yield return houses[i];
        for (int i = 0; i < producers.Count; i++) yield return producers[i];
    }

    // Оставил для совместимости, если где-то используется
    public Dictionary<string, float> CalculateSurplus()
    {
        float t0 = Time.realtimeSinceStartup;

        var rm = ResourceManager.Instance;
        var result = new Dictionary<string, float>();

        if (rm == null) return result;

        if (cachedResourceNames == null || cachedResourceNames.Count == 0)
            CacheResourceNames();

        for (int i = 0; i < cachedResourceNames.Count; i++)
        {
            var name = cachedResourceNames[i];
            float prod = rm.GetProduction(name);
            float cons = rm.GetConsumption(name);
            float diff = prod - cons;
            if (diff > 0)
                result[name] = diff;
        }

        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (perfLog && dt > 5f)
            Debug.Log($"[PERF] CalculateSurplus занял {dt:F2} ms");

        return result;
    }
}
