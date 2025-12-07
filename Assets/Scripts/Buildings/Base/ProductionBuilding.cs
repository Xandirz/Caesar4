using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    public override abstract BuildManager.BuildMode BuildMode { get; }

    [SerializeField] protected bool requiresRoadAccess = true;

    [Header("Economy")]
    // текущее производство (за тик)
    public Dictionary<string, int> production = new();
    // текущее потребление (за тик)
    public Dictionary<string, int> consumptionCost = new();

    // ====== Апгрейд до 2 уровня ======
    [Header("Upgrade to Level 2")]
    public Dictionary<string, int> upgradeProductionBonusLevel2 = new();
    public Dictionary<string, int> upgradeConsumptionLevel2     = new();
    public Sprite level2Sprite;

    // ====== Апгрейд до 3 уровня ======
    [Header("Upgrade to Level 3")]
    public Dictionary<string, int> upgradeProductionBonusLevel3 = new();
    public Dictionary<string, int> upgradeConsumptionLevel3     = new();
    public Sprite level3Sprite;

    // ====== Политика апгрейда ======
    [Header("Upgrade Policy")]
    public bool autoUpgrade = true;   // автоапгрейд при профиците входных ресурсов
    public int  maxLevel    = 3;      // максимум уровней (1..3). Уровень 1 — базовый.

    protected Dictionary<string, int> cost = new();
    public override Dictionary<string, int> GetCostDict() => cost;

    public bool isActive = false;
    public bool needsAreMet;
    public int CurrentStage { get; private set; } = 1;  // Level 1 — базовый

    private GameObject stopSignInstance;
    protected SpriteRenderer sr;

    // === Новая система рабочих ===
    [Header("Workforce")]
    protected int workersRequired;
    public int WorkersRequired => workersRequired;
    private bool workersAllocated = false;

    // === Хранилище, добавляемое зданием ===
    private Dictionary<string, int> storageAdded = new();

    // ресурсы, которых НЕ хватило именно этому зданию в прошлый тик
    public HashSet<string> lastMissingResources { get; private set; } = new();

    [Header("Noise")]
    public bool isNoisy = false;
    public int noiseRadius = 3;


    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterProducer(this);

        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f;
        }

        sr = GetComponent<SpriteRenderer>();

        AddStorageBonuses();

        // На старте считаем, что здание выключено, пока менеджер не проверит тик
        ApplyNeedsResult(false);
        if (stopSignInstance != null)
            stopSignInstance.SetActive(true);
    }


    public override void OnRemoved()
    {
        // снимаем текущее производство и потребление
        if (isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;
        }

        // освобождаем рабочих при сносе
        if (workersAllocated)
        {
            ResourceManager.Instance.ReleaseWorkers(this);
            workersAllocated = false;
        }

        // --- Убираем лимиты хранения ---
        RemoveStorageBonuses();

        AllBuildingsManager.Instance?.UnregisterProducer(this);
        ResourceManager.Instance.RefundResources(cost);
        manager?.SetOccupied(gridPos, false);

        if (stopSignInstance != null)
            Destroy(stopSignInstance);

        if (isNoisy) AffectNearbyHousesNoise(false);

        base.OnRemoved();
    }

    // ===== Проверка и производство =====
    /// <summary>
    /// Старый CheckNeeds — теперь только обёртка над проверкой окружения.
    /// Никаких ресурсов/рабочих/изменения состояния здесь НЕТ.
    /// </summary>
    public bool CheckNeeds()
    {
        return CheckEnvironmentOnly();
    }

    /// <summary>
    /// Чистая проверка окружения (дорога / дом), без ресурсов и рабочих.
    /// Используется AllBuildingsManager в фазе 1.
    /// </summary>
    public bool CheckEnvironmentOnly()
    {
        if (requiresRoadAccess && !hasRoadAccess)
            return false;

        if (needHouseNearby)
        {
            hasHouseNearby = HasAdjacentHouse();
            if (!hasHouseNearby)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Один тик производства: считаем, что ресурсы уже списал AllBuildingsManager.
    /// Тут только добавляем прод, отмечаем ресёрч, апгрейд и шум.
    /// НИКАКИХ рабочих и переключения isActive здесь нет.
    /// </summary>
    public void RunProductionTick()
    {
        // Производим ресурсы
        if (production != null)
        {
            foreach (var kvp in production)
            {
                ResourceManager.Instance.AddResource(kvp.Key, kvp.Value);

                if (ResearchManager.Instance != null)
                    ResearchManager.Instance.ReportProduced(kvp.Key, kvp.Value);
            }
        }

        // Апгрейд, если нужен
        if (autoUpgrade)
            TryAutoUpgrade();

        // Обновляем шум
        if (isNoisy)
            AffectNearbyHousesNoise(true);
    }


    public void ApplyNeedsResult(bool satisfied)
    {
        // Добавляем проверку рабочих
        bool wantsToBeActive = satisfied;

        if (wantsToBeActive && !workersAllocated)
        {
            workersAllocated = ResourceManager.Instance.TryAllocateWorkers(this, workersRequired);
            if (!workersAllocated)
                wantsToBeActive = false;
        }

        needsAreMet = wantsToBeActive;

        if (wantsToBeActive && !isActive)
        {
            // регистрируем производство/потребление в глобальной экономике
            foreach (var kvp in production)
                ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

            isActive = true;
            if (stopSignInstance != null) stopSignInstance.SetActive(false);
        }
        else if (!wantsToBeActive && isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);
            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            isActive = false;
            if (stopSignInstance != null) stopSignInstance.SetActive(true);

            if (workersAllocated)
            {
                ResourceManager.Instance.ReleaseWorkers(this);
                workersAllocated = false;
            }
        }
    }

    public void ForceStopDueToNoWorkers()
    {
        if (isActive || workersAllocated)
            ApplyNeedsResult(false);
    }

    // ===== Работа с лимитами хранения =====
    private void AddStorageBonuses()
    {
        storageAdded.Clear();

        // Для производимых ресурсов
        foreach (var kvp in production)
        {
            ResourceManager.Instance.ChangeStorageLimit(kvp.Key, kvp.Value);
            storageAdded[kvp.Key] = kvp.Value;
        }

        // Для потребляемых ресурсов
        foreach (var kvp in consumptionCost)
        {
            ResourceManager.Instance.ChangeStorageLimit(kvp.Key, kvp.Value);

            if (storageAdded.ContainsKey(kvp.Key))
                storageAdded[kvp.Key] += kvp.Value;
            else
                storageAdded[kvp.Key] = kvp.Value;
        }

        Debug.Log($"{name}: добавлено хранилище для {storageAdded.Count} ресурсов.");
    }

    private void RemoveStorageBonuses()
    {
        foreach (var kvp in storageAdded)
            ResourceManager.Instance.ChangeStorageLimit(kvp.Key, -kvp.Value);

        storageAdded.Clear();
        Debug.Log($"{name}: убраны лимиты хранилища.");
    }

    // ======= АПГРЕЙДЫ (уровни 1→2→3) =======

    /// <summary>
    /// Ручной апгрейд на следующий уровень (если возможно).
    /// Вернёт true, если апгрейд выполнен.
    /// </summary>
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
                else     h.RecheckNoise(manager, gridPos, noiseRadius);
            }
        }
    }

    /// <summary>
    /// Автоапгрейд: пытаемся поднимать уровень, пока это возможно.
    /// </summary>
    private void TryAutoUpgrade()
    {
        bool upgraded = true;
        int guard = 0;
        while (autoUpgrade && upgraded && guard++ < 3)
        {
            upgraded = TryUpgradeToNextLevel();
        }
    }

    /// <summary>
    /// Проверка профицита для набора дополнительных потреблений.
    /// </summary>
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

    // В ProductionBuilding.cs
    public override void OnClicked()
    {
        base.OnClicked();

        if (MouseHighlighter.Instance == null) return;

        if (isNoisy && noiseRadius > 0)
        {
            // для шумных зданий показываем «красную» зону шума
            MouseHighlighter.Instance.ShowNoiseRadius(gridPos, noiseRadius);
        }
        else if (buildEffectRadius > 0)
        {
            // для обычных — стандартную «голубую» зону эффекта
            MouseHighlighter.Instance.ShowEffectRadius(gridPos, buildEffectRadius);
        }
    }

    /// <summary>
    /// Попытка апгрейда на следующий уровень (2 или 3).
    /// Требует доступа к дороге (если он нужен) и профицита по новым потреблениям.
    /// </summary>
    private bool TryUpgradeToNextLevel()
    {
        if (CurrentStage >= maxLevel) return false;
        if (requiresRoadAccess && !hasRoadAccess) return false;

        int target = CurrentStage + 1;

        // --- Новое: проверяем, разрешён ли апгрейд этим исследованием ---
        if (!IsUpgradeAllowedByResearch(target))
        {
            Debug.Log($"{name}: upgrade to level {target} blocked by research.");
            return false;
        }

        // дальше как у тебя: определяем prodDelta / consDelta / targetSprite
        Dictionary<string, int> prodDelta = null;
        Dictionary<string, int> consDelta = null;
        Sprite targetSprite = null;

        if (target == 2)
        {
            prodDelta    = upgradeProductionBonusLevel2;
            consDelta    = upgradeConsumptionLevel2;
            targetSprite = level2Sprite;
        }
        else if (target == 3)
        {
            prodDelta    = upgradeProductionBonusLevel3;
            consDelta    = upgradeConsumptionLevel3;
            targetSprite = level3Sprite;
        }
        else
        {
            return false;
        }

        // Нечего апгрейдить?
        bool hasAnyChange = (prodDelta != null && prodDelta.Count > 0) || (consDelta != null && consDelta.Count > 0);
        if (!hasAnyChange) return false;

        // Профицит ресурсов для новых потреблений
        if (!HasSurplusFor(consDelta))
            return false;

        // Применяем апгрейд
        ApplyUpgradeStep(target, prodDelta, consDelta, targetSprite);
        return true;
    }

    /// <summary>
    /// Применение дельт (произв./потребл.), пересчёт хранилищ и регистрации, смена спрайта.
    /// </summary>
    private void ApplyUpgradeStep(int newLevel,
                                  Dictionary<string, int> prodDelta,
                                  Dictionary<string, int> consDelta,
                                  Sprite newSprite)
    {
        // 1) обновляем локальные таблицы производства/потребления
        if (prodDelta != null)
        {
            foreach (var kvp in prodDelta)
            {
                if (production.ContainsKey(kvp.Key)) production[kvp.Key] += kvp.Value;
                else production[kvp.Key] = kvp.Value;
            }
        }

        if (consDelta != null)
        {
            foreach (var kvp in consDelta)
            {
                if (consumptionCost.ContainsKey(kvp.Key)) consumptionCost[kvp.Key] += kvp.Value;
                else consumptionCost[kvp.Key] = kvp.Value;
            }
        }

        // 2) пересчитываем лимиты хранилищ
        RemoveStorageBonuses();
        AddStorageBonuses();

        // 3) если здание активно — перерегистрируем дельты (добавляем прирост)
        if (isActive)
        {
            if (prodDelta != null)
                foreach (var kvp in prodDelta)
                    ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);

            if (consDelta != null)
                foreach (var kvp in consDelta)
                    ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
        }

        // 4) применяем спрайт
        if (newSprite != null)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = newSprite;
        }

        // 5) повышаем уровень
        CurrentStage = newLevel;

        Debug.Log($"{name} улучшено до уровня {CurrentStage}! (лимиты хранилища пересчитаны)");
    }


    // === Research gate ===
    protected virtual string GetResearchIdForLevel(int level)
    {
        // По умолчанию улучшения не завязаны на исследования
        return null;
    }

    private bool IsUpgradeAllowedByResearch(int targetLevel)
    {
        string researchId = GetResearchIdForLevel(targetLevel);
        if (string.IsNullOrEmpty(researchId))
            return true; // нет требования – нет ограничения

        if (ResearchManager.Instance == null)
            return false;

        return ResearchManager.Instance.IsResearchCompleted(researchId);
    }


    public bool IsUpgradeUnlocked(int targetLevel)
    {
        string researchId = GetResearchIdForLevel(targetLevel);

        // если исследование не требуется — апгрейд "разрешён"
        if (string.IsNullOrEmpty(researchId))
            return true;

        // если требуется — проверяем, выполнено ли исследование
        return ResearchManager.Instance != null &&
               ResearchManager.Instance.IsResearchCompleted(researchId);
    }
}
