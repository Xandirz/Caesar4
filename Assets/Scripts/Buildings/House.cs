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
    public Dictionary<string, int> GetNeeds()
    {
        return consumptionCost;
    }

    
    bool isProducerOfMood = false;
    bool isConsumerOfMood= false;

    public bool HasWater { get; private set; } = false;
    public int CurrentStage { get; private set; } = 1;

    // ⚡ список потребляемых ресурсов
    public Dictionary<string, int> consumptionCost = new();

    public override Dictionary<string, int> GetCostDict() => cost;

    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        ResourceManager.Instance.AddResource("People", basePopulation);

        // Регистрируем дом
        HouseManager.Instance.RegisterHouse(this);

        // ⚡ потребление для 1 уровня
        consumptionCost.Clear();
        consumptionCost["Berry"] = 1;
        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);
    }

    public void OnConsumeResult(bool success)
    {
        if (success)
        {
            ApplyNeedsResult(true);
        }
        else
        {
            ApplyNeedsResult(false);
        }
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

        // ⚡ убираем потребление
        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);
        
        if (isProducerOfMood)
            ResourceManager.Instance.UnregisterProducer("Mood", 1);

        if (isConsumerOfMood)
            ResourceManager.Instance.UnregisterConsumer("Mood", 1);

        isProducerOfMood = false;
        isConsumerOfMood = false;

        base.OnRemoved();
    }
    public void ApplyNeedsResult(bool satisfied)
    {
        if (satisfied)
        {
            if (!isProducerOfMood)
            {
                ResourceManager.Instance.RegisterProducer("Mood", 1);
                if (isConsumerOfMood)
                    ResourceManager.Instance.UnregisterConsumer("Mood", 1);
                isProducerOfMood = true;
                isConsumerOfMood = false;
            }
        }
        else
        {
            if (!isConsumerOfMood)
            {
                ResourceManager.Instance.RegisterConsumer("Mood", 1);
                if (isProducerOfMood)
                    ResourceManager.Instance.UnregisterProducer("Mood", 1);
                isConsumerOfMood = true;
                isProducerOfMood = false;
            }
        }
    }

    public void SetWaterAccess(bool access) => HasWater = access;

    /// <summary>
    /// Проверяем, получает ли дом все необходимые товары.
    /// Если нет — уменьшаем Mood.
    /// </summary>
    public void CheckNeeds()
    {
        bool hasAll = true;

        // проверяем ресурсы
        foreach (var kvp in consumptionCost)
        {
            int current = ResourceManager.Instance.GetResource(kvp.Key);
            if (current < kvp.Value)
            {
                hasAll = false;
                break;
            }
        }

        // учитываем дорогу и воду
        if (!hasRoadAccess || !HasWater)
            hasAll = false;

        if (hasAll)
        {
            // ⚡ если дом должен производить, а ещё не производит
            if (!isProducerOfMood)
            {
                ResourceManager.Instance.RegisterProducer("Mood", 1);

                if (isConsumerOfMood)
                {
                    ResourceManager.Instance.UnregisterConsumer("Mood", 1);
                    isConsumerOfMood = false;
                }

                isProducerOfMood = true;
            }
        }
        else
        {
            // ⚡ если дом должен потреблять, а ещё не потребляет
            if (!isConsumerOfMood)
            {
                ResourceManager.Instance.RegisterConsumer("Mood", 1);

                if (isProducerOfMood)
                {
                    ResourceManager.Instance.UnregisterProducer("Mood", 1);
                    isProducerOfMood = false;
                }

                isConsumerOfMood = true;
            }
        }
    }


    /// <summary>
    /// Попытка улучшения дома вручную
    /// </summary>
    public bool TryUpgrade()
    {
        if (CurrentStage == 1)
        {
            // проверяем, что дом получает все ресурсы
            foreach (var kvp in consumptionCost)
            {
                int current = ResourceManager.Instance.GetResource(kvp.Key);
                if (current < kvp.Value)
                    return false;
            }

            // проверяем, хватает ли глины
            var upgradeCost = new Dictionary<string, int> { { "Clay", 1 } };
            if (!ResourceManager.Instance.CanSpend(upgradeCost))
                return false;

            // списываем глину
            ResourceManager.Instance.SpendResources(upgradeCost);

            // применяем апгрейд
            CurrentStage = 2;
            sr.sprite = house2Sprite;
            ResourceManager.Instance.AddResource("People", upgradePopulation);

            // добавляем в потребление дерево
            if (!consumptionCost.ContainsKey("Wood"))
            {
                consumptionCost["Wood"] = 1;
                ResourceManager.Instance.RegisterConsumer("Wood", 1);
            }

            return true;
        }

        return false;
    }
}
