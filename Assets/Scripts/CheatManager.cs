using System.Collections.Generic;
using UnityEngine;

public class CheatManager : MonoBehaviour
{
    [Header("Cheat")]
    public KeyCode cheatKey = KeyCode.P;
    public int addAmount = 100;
    public int cheatStorageMax = 9999;

    private void Update()
    {
        if (Input.GetKeyDown(cheatKey))
            ApplyCheat();
    }

    private void ApplyCheat()
    {
        var rm = ResourceManager.Instance;
        if (rm == null)
        {
            Debug.LogWarning("[CHEAT] ResourceManager.Instance == null");
            return;
        }

        // 1) Собираем все ресурсы, которые вообще встречаются в игре
        var resources = CollectAllResourceNames(rm);

        // 2) Для каждого: поднимаем лимит и добавляем +100
        foreach (var res in resources)
        {
            if (string.IsNullOrEmpty(res)) continue;

            // People можно тоже бустануть, если хочешь — оставляю как обычный ресурс
            // Сначала гарантируем высокий лимит (иначе ApplyStorageLimits обрежет до 10) :contentReference[oaicite:5]{index=5} :contentReference[oaicite:6]{index=6}
            rm.AddResource(res, 0, true, cheatStorageMax);

            // Потом добавляем
            rm.AddResource(res, addAmount);
        }

        // чтобы сразу не обрезало, лимит уже высокий, но можно применить для консистентности
        rm.ApplyStorageLimits();


  

        Debug.Log($"[CHEAT] +{addAmount} to {resources.Count} resources and unlocked all research");
    }

    private HashSet<string> CollectAllResourceNames(ResourceManager rm)
    {
        var set = new HashSet<string>();

        // то, что уже известно ResourceManager'у
        foreach (var k in rm.resourceBuffer.Keys)
            set.Add(k);

        // "еда 1 уровня", которая у тебя фигурирует в логике дома/InfoUI
        set.Add("Nuts");
        set.Add("Mushrooms");

        // вытащим ресурсы из всех построек на карте
        if (AllBuildingsManager.Instance != null)
        {
            foreach (var b in AllBuildingsManager.Instance.GetAllBuildings())
            {
                if (b == null) continue;

                if (b is ProductionBuilding pb)
                {
                    AddKeys(set, pb.production);
                    AddKeys(set, pb.consumptionCost);

                    AddKeys(set, pb.upgradeConsumptionLevel2);
                    AddKeys(set, pb.upgradeConsumptionLevel3);
                    AddKeys(set, pb.upgradeProductionBonusLevel2);
                    AddKeys(set, pb.upgradeProductionBonusLevel3);
                }
                else if (b is House h)
                {
                    AddKeys(set, h.consumption);      // текущее (например выбранная еда)
                    AddKeys(set, h.consumptionLvl2);
                    AddKeys(set, h.consumptionLvl3);
                    AddKeys(set, h.consumptionLvl4);
                }
            }
        }

        return set;
    }

    private void AddKeys(HashSet<string> set, Dictionary<string, int> dict)
    {
        if (dict == null) return;
        foreach (var kvp in dict)
            set.Add(kvp.Key);
    }
}
