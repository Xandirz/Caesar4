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

    public override Dictionary<string, int> GetCostDict() => cost;

    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        ResourceManager.Instance.AddResource("People", basePopulation);

        // Регистрируем дом в менеджере
        HouseManager.Instance.RegisterHouse(this);

        // ⚡ Регистрируем потребление ягод
        ResourceManager.Instance.RegisterConsumer("Berry", 1);
    }

    public override void OnRemoved()
    {
        // Убираем население
        ResourceManager.Instance.AddResource("People", -basePopulation);
        if (CurrentStage == 2)
            ResourceManager.Instance.AddResource("People", -upgradePopulation);

        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
            manager.SetOccupied(gridPos, false);

        // Убираем из менеджера
        if (HouseManager.Instance != null)
            HouseManager.Instance.UnregisterHouse(this);

        // ⚡ Снимаем потребление ягод
        ResourceManager.Instance.UnregisterConsumer("Berry", 1);

        base.OnRemoved();
    }

    public void SetWaterAccess(bool access) => HasWater = access;

    /// <summary>
    /// Проверка условий для апгрейда/даунгрейда
    /// </summary>
    public void CheckUpgradeConditions()
    {
        bool hasRoad = hasRoadAccess;
        bool hasFood = ResourceManager.Instance.GetResource("Berry") > 1;

        if (HasWater && hasRoad && hasFood)
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
            ResourceManager.Instance.AddResource("People", upgradePopulation);
        }
    }

    private void DowngradeToStage1()
    {
        if (CurrentStage != 1)
        {
            CurrentStage = 1;
            sr.sprite = house1Sprite;
            ResourceManager.Instance.AddResource("People", -upgradePopulation);
        }
    }
}
