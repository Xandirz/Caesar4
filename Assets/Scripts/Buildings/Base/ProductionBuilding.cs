using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    public override abstract BuildManager.BuildMode BuildMode { get; }

    [SerializeField] protected bool requiresRoadAccess = true;
    public override bool RequiresRoadAccess => requiresRoadAccess;

    [Header("Economy")]
    // текущее производство (за тик)
    public Dictionary<string, int> production = new();
    // текущее потребление (за тик)
    public Dictionary<string, int> consumptionCost = new();

    // ====== Апгрейд до 2 уровня ======
    [Header("Upgrade to Level 2")]
    public Dictionary<string, int> upgradeProductionBonusLevel2 = new();

    // ✅ добавляемое потребление на lvl2
    public Dictionary<string, int> addConsumptionLevel2 = new();

    // ✅ удаляемое потребление на lvl2 (по имени ресурса)
    public List<string> deleteFromConsumptionLevel2 = new();
    public Sprite level1Sprite;

    public Sprite level2Sprite;

    // ====== Апгрейд до 3 уровня ======
    [Header("Upgrade to Level 3")]
    public Dictionary<string, int> upgradeProductionBonusLevel3 = new();

    // ✅ добавляемое потребление на lvl3
    public Dictionary<string, int> addConsumptionLevel3 = new();
    public Dictionary<string, int> upgradeProductionBonusLevel4 = new();

    // ✅ добавляемое потребление на lvl4
    public Dictionary<string, int> addConsumptionLevel4 = new();

    // ✅ удаляемое потребление на lvl3/lvl4
    public List<string> deleteFromConsumptionLevel3 = new();
    public List<string> deleteFromConsumptionLevel4 = new();

    public Sprite level3Sprite;
    public Sprite level4Sprite;

    // ====== Политика апгрейда ======
    [Header("Upgrade Policy")]
    public bool autoUpgrade = true;
    public int maxLevel = 3;

    protected Dictionary<string, int> cost = new();

    public bool isActive = false;
    public bool needsAreMet;
    public int CurrentStage { get; private set; } = 1;

    private bool isPaused;
    public bool IsPaused => isPaused;
    private GameObject pauseSignInstance;
    protected SpriteRenderer sr;

    [Header("Workforce")]
    protected int workersRequired;
    public int WorkersRequired => workersRequired;
    private bool workersAllocated = false;

    private Dictionary<string, int> storageAdded = new();
    public HashSet<string> lastMissingResources { get; private set; } = new();

    [Header("Noise")]
    public bool isNoisy = false;
    public int noiseRadius = 1;

    // =========================
    // FIX: Grace tick after upgrade
    // =========================
    [Header("Upgrade Grace Tick")]
    public int lastUpgradeTick = -999;
    public Dictionary<string, int> prevConsumptionCost;
    public Dictionary<string, int> prevProduction;

    private Dictionary<string, int> tempProductionOverride;
    public void SetTemporaryProductionOverrideForThisTick(Dictionary<string, int> prod)
    {
        tempProductionOverride = prod;
    }

    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterProducer(this);

        level1Sprite =GetComponent<SpriteRenderer>().sprite;
        CreateStopSign();

        GameObject pauseSignPrefab = Resources.Load<GameObject>("pause");
        if (pauseSignPrefab != null)
        {
            pauseSignInstance = Instantiate(pauseSignPrefab, transform);
            pauseSignInstance.transform.localPosition = Vector3.zero;

            // ✅ СОРТИРОВКА: чуть ниже stop, но выше ghost/highlight
            ApplyFxSorting(pauseSignInstance, gridManager, gridPos, offset: 2050);

            pauseSignInstance.SetActive(false);
        }

        sr = GetComponent<SpriteRenderer>();

        AddStorageBonuses();

        ApplyNeedsResult(false);
        if (stopSignInstance != null)
            stopSignInstance.SetActive(true);
    }

    public override void OnRemoved()
    {
        if (isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;
        }

        if (workersAllocated)
        {
            ResourceManager.Instance.ReleaseWorkers(this);
            workersAllocated = false;
        }

        RemoveStorageBonuses();
        if (isNoisy)
            AllBuildingsManager.Instance.MarkEffectsDirtyAround(gridPos, noiseRadius);
        AllBuildingsManager.Instance?.UnregisterProducer(this);
        ResourceManager.Instance.RefundResources(cost);
        manager?.SetOccupied(gridPos, false);

        if (stopSignInstance != null)
            Destroy(stopSignInstance);

        if (isNoisy) AffectNearbyHousesNoise(false);

        base.OnRemoved();
    }

    public bool CheckNeeds() => CheckEnvironmentOnly();

    public bool CheckEnvironmentOnly()
    {
        if (requiresRoadAccess && !hasRoadAccess)
            return false;

        if (NeedHouseNearby)
        {
            hasHouseNearby = HasAdjacentHouse();
            if (!hasHouseNearby)
                return false;
        }

        return true;
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;

        // ✅ освобождаем рабочих сразу (истина в ResourceManager)
        if (ResourceManager.Instance != null && ResourceManager.Instance.HasWorkersAllocated(this))
        {
            ResourceManager.Instance.ReleaseWorkers(this);
            workersAllocated = false;
        }

        ApplyNeedsResult(false);
    }

    public bool TryDowngradeOneLevel()
    {
        if (CurrentStage <= 1)
            return false;

        int fromLevel = CurrentStage;
        int toLevel = CurrentStage - 1;

        bool wasActive = isActive; // <-- ДОБАВЬ

        // 1) если активно — снимаем текущую экономику
        if (isActive)
        {
            foreach (var kv in production)
                ResourceManager.Instance.UnregisterProducer(kv.Key, kv.Value);

            foreach (var kv in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kv.Key, kv.Value);

            isActive = false; // <-- КРИТИЧНО: мы сняли регистрацию, значит объект больше НЕ активен в терминах RM
        }

        // 2) откат изменений
        if (fromLevel == 4)
        {
            RollbackDict(production, upgradeProductionBonusLevel4);
            RollbackDict(consumptionCost, addConsumptionLevel4);
        }
        else if (fromLevel == 3)
        {
            RollbackDict(production, upgradeProductionBonusLevel3);
            RollbackDict(consumptionCost, addConsumptionLevel3);
        }
        else if (fromLevel == 2)
        {
            RollbackDict(production, upgradeProductionBonusLevel2);
            RollbackDict(consumptionCost, addConsumptionLevel2);
        }

        CleanupZeroEntries(production);
        CleanupZeroEntries(consumptionCost);

        RebuildStorageBonusesNoDip();

        CurrentStage = toLevel;
        UpdateSpriteForCurrentLevel();

        Debug.Log($"{name} понижен с {fromLevel} до {toLevel} из-за нехватки ресурсов");

        // <-- ДОБАВЬ: если здание было активным, попробуем сразу вернуть регистрацию на новых значениях
        if (wasActive)
        {
            // satisfied=true → wantsToBeActive зависит от паузы/рабочих и т.д.
            ApplyNeedsResult(true);
        }

        return true;
    }


    private void RollbackDict(Dictionary<string, int> target, Dictionary<string, int> delta)
    {
        if (delta == null) return;

        foreach (var kv in delta)
        {
            if (target.ContainsKey(kv.Key))
                target[kv.Key] -= kv.Value;
        }
    }

    private void CleanupZeroEntries(Dictionary<string, int> dict)
    {
        var keys = new List<string>(dict.Keys);
        foreach (var k in keys)
        {
            if (dict[k] <= 0)
                dict.Remove(k);
        }
    }

    private void UpdateSpriteForCurrentLevel()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (CurrentStage == 4 && level4Sprite != null)
            sr.sprite = level4Sprite;
        else if (CurrentStage == 3 && level3Sprite != null)
            sr.sprite = level3Sprite;
        else if (CurrentStage == 2 && level2Sprite != null)
            sr.sprite = level2Sprite;
        else if (CurrentStage == 1 && level1Sprite != null)
            sr.sprite = level1Sprite;
        // level 1 — базовый спрайт, уже стоит
    }

    public bool HasEnoughResourcesForConsumption(ResourceManager rm)
    {
        if (consumptionCost == null || consumptionCost.Count == 0)
            return true;

        foreach (var kv in consumptionCost)
        {
            if (rm.GetResource(kv.Key) < kv.Value)
                return false;
        }

        return true;
    }

    public void RunProductionTick()
    {
        if (!isActive)
            return;

        // FIX: если в этом тике нам дали override (grace tick), производим по старому
        var prodToUse = tempProductionOverride ?? production;

        if (prodToUse != null)
        {
            foreach (var kvp in prodToUse)
            {
                ResourceManager.Instance.AddResource(kvp.Key, kvp.Value);

                if (ResearchManager.Instance != null)
                    ResearchManager.Instance.ReportProduced(kvp.Key, kvp.Value);
            }
        }

        // сброс override (только на 1 тик)
        tempProductionOverride = null;

        if (autoUpgrade)
            TryAutoUpgrade();

        if (isNoisy)
            AffectNearbyHousesNoise(true);
    }

    public void ApplyNeedsResult(bool satisfied)
    {
        // ручная пауза принудительно делает wantsToBeActive=false
        bool wantsToBeActive = satisfied && !isPaused;

        // ✅ ФИКС: если здание НЕ должно быть активным — оно НЕ держит рабочих,
        // даже если isActive уже false (иначе рабочие "залипают" в резерве).
        if (!wantsToBeActive && ResourceManager.Instance.HasWorkersAllocated(this))
        {
            ResourceManager.Instance.ReleaseWorkers(this);
            workersAllocated = false;
        }

        // если не на паузе и хотим активироваться — выделяем рабочих
        if (wantsToBeActive && !workersAllocated)
        {
            workersAllocated = ResourceManager.Instance.TryAllocateWorkers(this, workersRequired);
            if (!workersAllocated)
                wantsToBeActive = false;
        }

        // ✅ На всякий случай: если wantsToBeActive стал false после попытки выделения (или из-за других условий),
        // то гарантируем отсутствие резервации.
        if (!wantsToBeActive && ResourceManager.Instance.HasWorkersAllocated(this))
        {
            ResourceManager.Instance.ReleaseWorkers(this);
            workersAllocated = false;
        }

        needsAreMet = wantsToBeActive;

        if (wantsToBeActive && !isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

            isActive = true;

            // индикаторы
            if (stopSignInstance != null) stopSignInstance.SetActive(false);
            if (pauseSignInstance != null) pauseSignInstance.SetActive(false);
        }
        else if (!wantsToBeActive && isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;

            // если это “обычный стоп по нуждам” — показываем stop
            // если это ручная пауза — показываем pause
            if (stopSignInstance != null) stopSignInstance.SetActive(!isPaused);
            if (pauseSignInstance != null) pauseSignInstance.SetActive(isPaused);

            if (workersAllocated)
            {
                ResourceManager.Instance.ReleaseWorkers(this);
                workersAllocated = false;
            }
        }
        else
        {
            // Случай: здание и так неактивно, но нам нужно обновить значок.
            // Например, поставили паузу, когда isActive уже false.
            if (!isActive)
            {
                if (stopSignInstance != null) stopSignInstance.SetActive(!isPaused && !needsAreMet);
                if (pauseSignInstance != null) pauseSignInstance.SetActive(isPaused);
            }
        }
    }

    private void RebuildStorageBonusesNoDip()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        // старые бонусы
        var old = new Dictionary<string, int>(storageAdded);

        // считаем новые бонусы
        storageAdded.Clear();

        if (production != null)
        {
            foreach (var kvp in production)
            {
                if (storageAdded.ContainsKey(kvp.Key)) storageAdded[kvp.Key] += kvp.Value;
                else storageAdded[kvp.Key] = kvp.Value;
            }
        }

        if (consumptionCost != null)
        {
            foreach (var kvp in consumptionCost)
            {
                if (storageAdded.ContainsKey(kvp.Key)) storageAdded[kvp.Key] += kvp.Value;
                else storageAdded[kvp.Key] = kvp.Value;
            }
        }

        // применяем только ДЕЛЬТУ (new - old)
        var keys = new HashSet<string>(old.Keys);
        keys.UnionWith(storageAdded.Keys);

        foreach (var k in keys)
        {
            int oldV = old.TryGetValue(k, out var ov) ? ov : 0;
            int newV = storageAdded.TryGetValue(k, out var nv) ? nv : 0;
            int delta = newV - oldV;

            if (delta != 0)
                rm.ChangeStorageLimit(k, delta);
        }
    }

    public void ForceStopDueToNoWorkers()
    {
        if (isActive || workersAllocated)
            ApplyNeedsResult(false);
    }

    private void AddStorageBonuses()
    {
        storageAdded.Clear();

        foreach (var kvp in production)
        {
            ResourceManager.Instance.ChangeStorageLimit(kvp.Key, kvp.Value);
            storageAdded[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in consumptionCost)
        {
            ResourceManager.Instance.ChangeStorageLimit(kvp.Key, kvp.Value);

            if (storageAdded.ContainsKey(kvp.Key))
                storageAdded[kvp.Key] += kvp.Value;
            else
                storageAdded[kvp.Key] = kvp.Value;
        }
    }

    private void RemoveStorageBonuses()
    {
        foreach (var kvp in storageAdded)
            ResourceManager.Instance.ChangeStorageLimit(kvp.Key, -kvp.Value);

        storageAdded.Clear();
    }

    private void AffectNearbyHousesNoise(bool add)
    {
        if (manager == null) return;

        for (int dx = -noiseRadius; dx <= noiseRadius; dx++)
        for (int dy = -noiseRadius; dy <= noiseRadius; dy++)
        {
            var c = gridPos + new Vector2Int(dx, dy);
            if (manager.TryGetPlacedObject(c, out var obj) && obj is House h)
            {
                if (add) h.SetNoise(true);
                else h.RecheckNoise(manager, gridPos, noiseRadius);
            }
        }
    }

    private void TryAutoUpgrade()
    {
        bool upgraded = true;
        int guard = 0;
        while (autoUpgrade && upgraded && guard++ < 3)
        {
            upgraded = TryUpgradeToNextLevel();
        }
    }

    private bool HasSurplusFor(Dictionary<string, int> extraConsumption)
    {
        if (extraConsumption == null) return true;
        foreach (var kvp in extraConsumption)
        {
            float prod = ResourceManager.Instance.GetProduction(kvp.Key);
            float cons = ResourceManager.Instance.GetConsumption(kvp.Key);
            if (prod - cons < kvp.Value)
                return false;
        }
        return true;
    }

    public override void OnClicked()
    {
        base.OnClicked();

        if (MouseHighlighter.Instance == null) return;

        if (isNoisy && noiseRadius > 0)
        {
            MouseHighlighter.Instance.ShowNoiseRadius(gridPos, noiseRadius);
        }
        else if (buildEffectRadius > 0)
        {
            MouseHighlighter.Instance.ShowEffectRadius(gridPos, buildEffectRadius);
        }
    }
    public void SetStageFromSave(int stage)
    {
        CurrentStage = stage;

        // важно: обновить визуал/логику так же, как при обычном апгрейде
        
    }

    private bool TryUpgradeToNextLevel()
    {
        if (CurrentStage >= maxLevel) return false;
        if (requiresRoadAccess && !hasRoadAccess) return false;

        int target = CurrentStage + 1;

        if (!IsUpgradeAllowedByResearch(target))
            return false;

        Dictionary<string, int> prodAdd = null;
        Dictionary<string, int> consAdd = null;
        List<string> consDelete = null;
        Sprite targetSprite = null;

        if (target == 2)
        {
            prodAdd = upgradeProductionBonusLevel2;
            consAdd = addConsumptionLevel2;
            consDelete = deleteFromConsumptionLevel2;
            targetSprite = level2Sprite;
        }
        else if (target == 3)
        {
            prodAdd = upgradeProductionBonusLevel3;
            consAdd = addConsumptionLevel3;
            consDelete = deleteFromConsumptionLevel3;
            targetSprite = level3Sprite;
        }
        else if (target == 4)
        {
            prodAdd = upgradeProductionBonusLevel4;
            consAdd = addConsumptionLevel4;
            consDelete = deleteFromConsumptionLevel4;
            targetSprite = level4Sprite;
        }
        else
        {
            return false;
        }

        bool hasAnyChange =
            (prodAdd != null && prodAdd.Count > 0) ||
            (consAdd != null && consAdd.Count > 0) ||
            (consDelete != null && consDelete.Count > 0);

        if (!hasAnyChange) return false;

        // Проверяем профицит только для того, что ДОБАВЛЯЕМ
        if (!HasSurplusFor(consAdd))
            return false;

        ApplyUpgradeStep(target, prodAdd, consAdd, consDelete, targetSprite);
        return true;
    }

    private void ApplyUpgradeStep(
        int newLevel,
        Dictionary<string, int> prodAdd,
        Dictionary<string, int> consAdd,
        List<string> consDelete,
        Sprite newSprite)
    {
        // ===== FIX: запоминаем прошлый уровень для grace-тика =====
        prevConsumptionCost = new Dictionary<string, int>(consumptionCost);
        prevProduction      = new Dictionary<string, int>(production);
        lastUpgradeTick     = AllBuildingsManager.Instance != null ? AllBuildingsManager.Instance.EconTickCounter : lastUpgradeTick;

        // --- 0) если активны — сначала СНИМАЕМ то, что будем удалять (из экономики) ---
        if (consDelete != null && consDelete.Count > 0)
        {
            foreach (var resNameRaw in consDelete)
            {
                if (string.IsNullOrEmpty(resNameRaw)) continue;
                string resName = resNameRaw.Trim();

                if (consumptionCost != null && consumptionCost.TryGetValue(resName, out int amount) && amount > 0)
                {
                    if (isActive)
                        ResourceManager.Instance.UnregisterConsumer(resName, amount);

                    consumptionCost.Remove(resName);
                }
            }
        }

        // --- 1) добавляем производство ---
        if (prodAdd != null)
        {
            foreach (var kvp in prodAdd)
            {
                if (production.ContainsKey(kvp.Key)) production[kvp.Key] += kvp.Value;
                else production[kvp.Key] = kvp.Value;

                if (isActive)
                    ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
            }
        }

        // --- 2) добавляем потребление ---
        if (consAdd != null)
        {
            foreach (var kvp in consAdd)
            {
                if (consumptionCost.ContainsKey(kvp.Key)) consumptionCost[kvp.Key] += kvp.Value;
                else consumptionCost[kvp.Key] = kvp.Value;

                if (isActive)
                    ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
            }
        }

        // --- 3) пересчитываем лимиты хранилищ (без просадки/обрезания склада) ---
        RebuildStorageBonusesNoDip();


        // --- 4) меняем спрайт ---
        if (newSprite != null)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = newSprite;
        }

        // --- 5) уровень ---
        CurrentStage = newLevel;

        Debug.Log($"{name} улучшено до уровня {CurrentStage}!");
    }

    protected virtual string GetResearchIdForLevel(int level)
    {
        return null;
    }
 
 
    private bool IsUpgradeAllowedByResearch(int targetLevel)
    {
        string researchId = GetResearchIdForLevel(targetLevel);
        if (string.IsNullOrEmpty(researchId))
            return true;

        if (ResearchManager.Instance == null)
            return false;

        return ResearchManager.Instance.IsResearchCompleted(researchId);
    }

    public bool IsUpgradeUnlocked(int targetLevel)
    {
        string researchId = GetResearchIdForLevel(targetLevel);
        if (string.IsNullOrEmpty(researchId))
            return true;

        return ResearchManager.Instance != null &&
               ResearchManager.Instance.IsResearchCompleted(researchId);
    }
}
