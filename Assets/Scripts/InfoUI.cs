using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    public static InfoUI Instance;

    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;

    private House currentHouse;
    private ProductionBuilding currentProduction;

    void Awake()
    {
        Instance = this;
        infoPanel.SetActive(false);
    }

    public void ShowInfo(PlacedObject po)
    {
        infoPanel.SetActive(true);
        currentHouse = null;
        currentProduction = null;

        string text = $"<b>{po.name}</b>";

        // === 🚗 Дорога ===
        if (!(po is Road))
        {
            string roadColor = po.hasRoadAccess ? "white" : "red";
            text += $"\nДорога: <color={roadColor}>{(po.hasRoadAccess ? "Есть" : "Нет")}</color>";
        }

        // === 🏠 ДОМ ===
        if (po is House house)
        {
            currentHouse = house;

            text += $"\nУровень: {house.CurrentStage}";
            text += $"\nНаселение: {house.currentPopulation}";

            // 💧 Вода (для уровней 2+)
            if (house.CurrentStage >= 2)
            {
                string waterColor = house.HasWater ? "white" : "red";
                text += $"\nВода: <color={waterColor}>{(house.HasWater ? "Есть" : "Нет")}</color>";
            }

            // 🏪 Рынок (для уровней 3+)
            if (house.CurrentStage >= 3)
            {
                string marketColor = house.HasMarket ? "white" : "red";
                text += $"\nРынок: <color={marketColor}>{(house.HasMarket ? "Есть" : "Нет")}</color>";
            }

            // 🔹 Текущее потребление
            string consumptionText = "";
            foreach (var kvp in house.consumption)
            {
                int available = ResourceManager.Instance.GetResource(kvp.Key);
                string color = available >= kvp.Value ? "white" : "red";
                consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
            }

            text += "\nПотребляет: " + (string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText);

            // === Возможное улучшение ===
            var surplus = AllBuildingsManager.Instance.CalculateSurplus();
            string reqText = "";

            Dictionary<string, int> nextCons = null;
            string nextLevelLabel = "";

            if (house.CurrentStage == 1 && house.consumptionLvl2.Count > 0)
            {
                nextCons = house.consumptionLvl2;
                nextLevelLabel = "2 уровня";
            }
            else if (house.CurrentStage == 2 && house.consumptionLvl3.Count > 0)
            {
                nextCons = house.consumptionLvl3;
                nextLevelLabel = "3 уровня";
            }

            if (nextCons != null)
            {
                reqText += $"\n\n<b>Для улучшения до {nextLevelLabel}:</b>";

                if (house.CurrentStage == 1)
                {
                    string needWater = house.HasWater ? "white" : "red";
                    if (!house.hasRoadAccess)
                        reqText += $"\n- Дорога: <color=red>Нет</color>";
                    reqText += $"\n- Вода: <color={needWater}>{(house.HasWater ? "Есть" : "Нет")}</color>";
                }
                else if (house.CurrentStage == 2)
                {
                    string marketColor = house.HasMarket ? "white" : "red";
                    reqText += $"\n- Рынок: <color={marketColor}>{(house.HasMarket ? "Есть" : "Нет")}</color>";
                }

                foreach (var kvp in nextCons)
                {
                    string resName = kvp.Key;
                    int required = kvp.Value;

                    surplus.TryGetValue(resName, out float extra);
                    string color = (extra >= required) ? "white" : "red";
                    reqText += $"\n- <color={color}>{resName}:{required}</color>";
                }

                text += reqText;
            }
        }

        // === 🏭 ПРОИЗВОДСТВЕННОЕ ЗДАНИЕ ===
        if (po is ProductionBuilding prod)
        {
            currentProduction = prod;

            string activeColor = prod.isActive ? "white" : "red";
            text += $"\nАктивно: <color={activeColor}>{(prod.isActive ? "Да" : "Нет")}</color>";
            text += $"\nУровень: {prod.CurrentStage}";

            // 🔹 Производство
            string productionText = "";
            if (prod.production != null && prod.production.Count > 0)
            {
                foreach (var kvp in prod.production)
                    productionText += $"\nПроизводит: <color=white>{kvp.Key} +{kvp.Value}/сек</color>";
            }

            // 🔹 Потребление
            string consumptionText = "";
            if (prod.consumptionCost != null && prod.consumptionCost.Count > 0)
            {
                foreach (var kvp in prod.consumptionCost)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
                }
            }

            text += productionText;
            text += "\nПотребляет: " + (string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText);

            if (!prod.isActive && prod.consumptionCost.Count > 0)
                text += "\n<color=red>⚠ Не работает: не хватает ресурсов!</color>";

            // 🔹 Требования для апгрейда
            if (prod.CurrentStage == 1 &&
                (prod.upgradeConsumptionLevel1.Count > 0 || prod.upgradeProductionBonusLevel1.Count > 0))
            {
                string reqText = "\n\n<b>Для улучшения до 2 уровня:</b>";

                foreach (var kvp in prod.upgradeConsumptionLevel1)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    reqText += $"\n- <color={color}>{kvp.Key}:{kvp.Value}</color>";
                }

                if (prod.upgradeProductionBonusLevel1.Count > 0)
                {
                    reqText += "\n\n<b>После улучшения производит дополнительно:</b>";
                    foreach (var kvp in prod.upgradeProductionBonusLevel1)
                        reqText += $"\n+ <color=green>{kvp.Key} +{kvp.Value}/сек</color>";
                }

                text += reqText;
            }
        }

        infoText.text = text;
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
        currentHouse = null;
        currentProduction = null;
        infoText.text = "";
    }
}
