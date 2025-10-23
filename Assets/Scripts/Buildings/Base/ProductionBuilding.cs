using System.Collections.Generic;
using UnityEngine;

public abstract class ProductionBuilding : PlacedObject
{
    public override abstract BuildManager.BuildMode BuildMode { get; }

    [SerializeField] protected bool requiresRoadAccess = true;

    [SerializeField] protected Dictionary<string, int> cost = new();
    [SerializeField] public Dictionary<string, int> production = new();
    [SerializeField] public Dictionary<string, int> consumptionCost = new();

    public override Dictionary<string, int> GetCostDict() => cost;

    public bool isActive = false;
    public bool needsAreMet;

    private GameObject stopSignInstance;
    private SpriteRenderer sr;

    // === Автоапгрейд ===
    public int CurrentStage { get; private set; } = 1;
    public Dictionary<string, int> upgradeProductionBonusLevel1 = new();
    public Dictionary<string, int> upgradeConsumptionLevel1 = new();
    public Sprite level2Sprite;

    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterProducer(this);
        ApplyNeedsResult(CheckNeeds());

        // Загружаем иконку "остановки"
        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f;
            stopSignInstance.SetActive(!needsAreMet);
        }

        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnRemoved()
    {
        // ⚙️ Снимаем базовое производство и потребление, если здание активно
        if (isActive)
        {
            foreach (var kvp in production)
                ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);

            foreach (var kvp in consumptionCost)
                ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

            // ⚙️ Если здание было улучшено — снимаем бонусы
            if (CurrentStage > 1)
            {
                foreach (var kvp in upgradeProductionBonusLevel1)
                    ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);

                foreach (var kvp in upgradeConsumptionLevel1)
                    ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
            }

            isActive = false;
        }

        // ⚙️ Убираем из глобального менеджера
        if (AllBuildingsManager.Instance != null)
            AllBuildingsManager.Instance.UnregisterProducer(this);

        // ⚙️ Возвращаем ресурсы игроку
        ResourceManager.Instance.RefundResources(cost);

        // ⚙️ Освобождаем клетку в сетке
        if (manager != null)
            manager.SetOccupied(gridPos, false);

        // ⚙️ Удаляем визуальный индикатор (например, стоп-знак)
        if (stopSignInstance != null)
            Destroy(stopSignInstance);

        // ⚙️ Базовое удаление
        base.OnRemoved();
    }


    // === Проверка условий, расход, производство, апгрейд ===
    public bool CheckNeeds()
    {
        // 1️⃣ Проверяем дорогу
        if (requiresRoadAccess && !hasRoadAccess)
        {
            needsAreMet = false;
            ApplyNeedsResult(false);
            return false;
        }

        // 2️⃣ Проверяем наличие входных ресурсов
        foreach (var cost in consumptionCost)
        {
            if (ResourceManager.Instance.GetResource(cost.Key) < cost.Value)
            {
                needsAreMet = false;
                ApplyNeedsResult(false);
                return false;
            }
        }

        // 3️⃣ Потребляем ресурсы
        foreach (var cost in consumptionCost)
            ResourceManager.Instance.SpendResource(cost.Key, cost.Value);

        // 4️⃣ Производим ресурсы
        foreach (var kvp in production)
            ResourceManager.Instance.AddResource(kvp.Key, kvp.Value);

        // 5️⃣ Попробуем автоапгрейд
        TryAutoUpgrade();

        // 6️⃣ Всё прошло успешно
        needsAreMet = true;
        ApplyNeedsResult(true);
        return true;
    }

    public void ApplyNeedsResult(bool satisfied)
    {
        if (satisfied)
        {
            if (!isActive)
            {
                foreach (var kvp in production)
                    ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);

                foreach (var kvp in consumptionCost)
                    ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

                isActive = true;
            }

            if (stopSignInstance != null)
                stopSignInstance.SetActive(false);
        }
        else
        {
            if (isActive)
            {
                foreach (var kvp in production)
                    ResourceManager.Instance.UnregisterProducer(kvp.Key, kvp.Value);

                foreach (var kvp in consumptionCost)
                    ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

                isActive = false;
            }

            if (stopSignInstance != null)
                stopSignInstance.SetActive(true);
        }
    }

    // === Проверка и выполнение автоапгрейда ===
    private void TryAutoUpgrade()
    {
        if (CurrentStage != 1) return;
        if (upgradeConsumptionLevel1 == null || upgradeConsumptionLevel1.Count == 0) return;

        // проверяем дорогу
        if (requiresRoadAccess && !hasRoadAccess) return;

        // проверяем профицит экономики
        foreach (var kvp in upgradeConsumptionLevel1)
        {
            float prod = ResourceManager.Instance.GetProduction(kvp.Key);
            float cons = ResourceManager.Instance.GetConsumption(kvp.Key);
            if (prod - cons < kvp.Value)
                return;
        }

        // === Выполняем улучшение ===
        CurrentStage = 2;

        // Спрайт
        if (level2Sprite != null)
        {
            if (sr == null)
                sr = GetComponent<SpriteRenderer>();
            sr.sprite = level2Sprite;
        }

        // Добавляем новое потребление
        foreach (var kvp in upgradeConsumptionLevel1)
        {
            if (consumptionCost.ContainsKey(kvp.Key))
                consumptionCost[kvp.Key] += kvp.Value;
            else
                consumptionCost.Add(kvp.Key, kvp.Value);

            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
        }

        // Добавляем бонусное производство
        foreach (var kvp in upgradeProductionBonusLevel1)
        {
            if (production.ContainsKey(kvp.Key))
                production[kvp.Key] += kvp.Value;
            else
                production.Add(kvp.Key, kvp.Value);

            ResourceManager.Instance.RegisterProducer(kvp.Key, kvp.Value);
        }

        Debug.Log($"{name} автоматически улучшено до 2 уровня!");
    }
}
