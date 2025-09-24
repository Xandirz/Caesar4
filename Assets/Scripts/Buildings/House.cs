using UnityEngine;
using System.Collections.Generic;

public class House : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    [Header("Settings")]
    public int basePopulation = 5;
    public int upgradePopulation = 3;

    [Header("Sprites")]
    public Sprite house1Sprite;
    public Sprite house2Sprite;

    private SpriteRenderer sr;
    private new Dictionary<string,int> cost = new() { { "Wood", 1 } };

    public bool HasWater { get; private set; } = false;
    public int CurrentStage { get; private set; } = 1;

    // ⚡ список потребляемых ресурсов
    private Dictionary<string, int> consumptionCost = new();

    public override Dictionary<string, int> GetCostDict() => cost;

    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        ResourceManager.Instance.AddResource("People", basePopulation);

        // Регистрируем дом в менеджере
        HouseManager.Instance.RegisterHouse(this);

        // ⚡ потребление для 1 уровня
        consumptionCost.Clear();
        consumptionCost["Berry"] = 1;
        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
    }

    public override void OnRemoved()
    {
        // Убираем население
        ResourceManager.Instance.AddResource("People", -basePopulation);
        if (CurrentStage == 2)
            ResourceManager.Instance.AddResource("People", -upgradePopulation);

        // Вернём стоимость постройки
        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        // Убираем из менеджера
        if (HouseManager.Instance != null)
            HouseManager.Instance.UnregisterHouse(this);

        // ⚡ убираем потребление
        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

        base.OnRemoved();
    }

    public void SetWaterAccess(bool access) => HasWater = access;

    /// <summary>
    /// Проверка условий для апгрейда/даунгрейда
    /// </summary>
    public void CheckUpgradeConditions()
    {
        bool hasRoad = hasRoadAccess;
        bool hasWater = HasWater;

        // ⚡ проверяем, хватает ли всех ресурсов
        bool hasEnoughResources = true;
        if (consumptionCost != null && consumptionCost.Count > 0)
        {
            foreach (var kvp in consumptionCost)
            {
                int current = ResourceManager.Instance.GetResource(kvp.Key);
                if (current < kvp.Value)
                {
                    hasEnoughResources = false;
                    break;
                }
            }
        }

        if (hasRoad && hasWater && hasEnoughResources)
            UpgradeToStage2();
        else
            DowngradeToStage1();
    }


    private void UpgradeToStage2()
    {
        if (CurrentStage != 2)
        {
            CurrentStage = 2;
            sr.sprite = house2Sprite;

            // Добавляем население
            ResourceManager.Instance.AddResource("People", upgradePopulation);

            // ⚡ добавляем требование: Wood
            if (!consumptionCost.ContainsKey("Wood"))
            {
                consumptionCost["Wood"] = 1;
                ResourceManager.Instance.RegisterConsumer("Wood", 1);
            }
        }
    }

    private void DowngradeToStage1()
    {
        if (CurrentStage != 1)
        {
            CurrentStage = 1;
            sr.sprite = house1Sprite;

            // Убираем население
            ResourceManager.Instance.AddResource("People", -upgradePopulation);

            // ⚡ убираем требование дерева
            if (consumptionCost.ContainsKey("Wood"))
            {
                ResourceManager.Instance.UnregisterConsumer("Wood", 1);
                consumptionCost.Remove("Wood");
            }
        }
    }
}
