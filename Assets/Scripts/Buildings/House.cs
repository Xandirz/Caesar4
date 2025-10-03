using UnityEngine;
using System.Collections.Generic;

public class House : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.House;

    [Header("Settings")]
    public int basePopulation = 3;
    public int upgradePopulation = 3;

    [Header("Sprites")]
    public Sprite house1Sprite;
    public Sprite house2Sprite;

    private SpriteRenderer sr;
    private new Dictionary<string,int> cost = new() { { "Wood", 1 } };

    public bool needsAreMet;

    private GameObject angryPrefab;
    private GameObject upgradePrefab;

    public bool HasWater { get; private set; } = false;
    public int CurrentStage { get; private set; } = 1;

    // ⚡ список потребляемых ресурсов
    public Dictionary<string, int> consumptionCost  = new() { { "Berry", 1 } };

    public Dictionary<string, int> upgradeCost  = new()
    {
        { "Clay", 25 },
        { "Wood", 15 },
        { "Rock", 10 },
        { "Pottery", 5 },
        { "Clothes", 5 },
    };

    public override Dictionary<string, int> GetCostDict() => cost;

    public override void OnPlaced()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = house1Sprite;

        ResourceManager.Instance.AddResource("People", basePopulation);

        // Регистрируем дом
        AllBuildingsManager.Instance.RegisterHouse(this);

        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.RegisterConsumer(kvp.Key, kvp.Value);

        angryPrefab = Resources.Load<GameObject>("angry");
        if (angryPrefab != null)
        {
            angryPrefab = Instantiate(angryPrefab, transform);
            angryPrefab.transform.localPosition = Vector3.up * 0f;
            angryPrefab.SetActive(false);
        }
        upgradePrefab = Resources.Load<GameObject>("upgrade");

        if (upgradePrefab != null)
        {
            upgradePrefab = Instantiate(upgradePrefab, transform);
            upgradePrefab.transform.localPosition = Vector3.up * 0f;
            upgradePrefab.SetActive(false);
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
        if (AllBuildingsManager.Instance != null)
            AllBuildingsManager.Instance.UnregisterHouse(this);

        // ⚡ убираем потребление
        foreach (var kvp in consumptionCost)
            ResourceManager.Instance.UnregisterConsumer(kvp.Key, kvp.Value);

        base.OnRemoved();

        ResourceManager.Instance.UpdateGlobalMood();
    }

    public void ApplyNeedsResult(bool satisfied)
    {
        needsAreMet = satisfied;

        if (angryPrefab != null)
            angryPrefab.SetActive(!satisfied);

        // 🔹 обновляем глобальное настроение
        ResourceManager.Instance.UpdateGlobalMood();
    }

    public void SetWaterAccess(bool access)
    {
        HasWater = access;
        ResourceManager.Instance.UpdateGlobalMood();
    }

    /// <summary>
    /// Проверяем, получает ли дом все необходимые товары.
    /// Если нет — уменьшаем настроение.
    /// </summary>
    public bool CheckNeeds()
    {
        if (!hasRoadAccess || !HasWater)
        {
            ApplyNeedsResult(false);
            return false;
        }

        bool allSatisfied = true;

        // Проверяем и потребляем ресурсы
        foreach (var cost in consumptionCost)
        {
            int available = ResourceManager.Instance.GetResource(cost.Key);

            if (available >= cost.Value)
            {
                // ✅ хватает → списываем
                ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
            }
            else
            {
                // ❌ не хватает → дом недоволен
                allSatisfied = false;
            }
        }

        // Проверяем возможность апгрейда
        CanUpgrade();

        if (allSatisfied)
        {
            // Все потребности закрыты → настроение растёт
            ApplyNeedsResult(true);
            return true;
        }
        else
        {
            // Чего-то не хватило → настроение падает
            ApplyNeedsResult(false);
            return false;
        }
    }



    /// <summary>
    /// Попытка улучшения дома вручную
    /// </summary>
    public bool TryUpgrade()
    {
        if (CurrentStage == 1)
        {
            if (!needsAreMet)
                return false;

            if (CanUpgrade())
            {
                // списываем апгрейдные ресурсы
                ResourceManager.Instance.SpendResources(upgradeCost);

                // применяем апгрейд
                CurrentStage = 2;
                sr.sprite = house2Sprite;
                ResourceManager.Instance.AddResource("People", upgradePopulation);

                consumptionCost.Add("Wood", 1);
                ResourceManager.Instance.RegisterConsumer("Wood", 1);
                consumptionCost.Add("Meat", 1);
                ResourceManager.Instance.RegisterConsumer("Meat", 1);
                consumptionCost.Add("Hide", 1);
                ResourceManager.Instance.RegisterConsumer("Hide", 1);
                consumptionCost.Add("Pottery", 2);
                ResourceManager.Instance.RegisterConsumer("Pottery", 2);
                consumptionCost.Add("Clothes", 1);
                ResourceManager.Instance.RegisterConsumer("Clothes", 1);

                AllBuildingsManager.Instance.RecheckAllHousesUpgrade();

                
                return true;
            }
        }
        return false;
    }

    public bool CanUpgrade()
    {
        if (CurrentStage == 1)
        {
            if (ResourceManager.Instance.CanSpend(upgradeCost))
            {
                if (upgradePrefab != null)
                    upgradePrefab.SetActive(true);
                return true;
            }
        }

        if (upgradePrefab != null)
            upgradePrefab.SetActive(false);

        return false;
    }
}
