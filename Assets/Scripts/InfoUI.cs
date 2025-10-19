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

        // === Общая проверка дороги ===
        if (!(po is Road))
        {
            string roadColor = po.hasRoadAccess ? "white" : "red";
            text += $"\nДорога: <color={roadColor}>{(po.hasRoadAccess ? "Есть" : "Нет")}</color>";
        }

        // === 🏠 ДОМ ===
        if (po is House house)
        {
            currentHouse = house;

            string waterColor = house.HasWater ? "white" : "red";
            text += $"\nВода: <color={waterColor}>{(house.HasWater ? "Есть" : "Нет")}</color>";
            text += $"\nУровень: {house.CurrentStage}";

            // 🔹 Потребление
            string consumptionText = "";
            foreach (var kvp in house.consumptionCost)
            {
                int available = ResourceManager.Instance.GetResource(kvp.Key);
                string color = available >= kvp.Value ? "white" : "red";
                consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
            }
            text += "\nПотребляет: " + (string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText);

            // 🔹 Требования для следующего уровня
            if (house.CurrentStage == 1 && house.consumptionLvl2 != null)
            {
                string reqText = "\n\n<b>Для улучшения до 2 уровня:</b>";
                reqText += "\n- Доступ к дороге";
                reqText += "\n- Доступ к воде";

                foreach (var kvp in house.consumptionLvl2)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    reqText += $"\n- <color={color}>{kvp.Key}:{kvp.Value}</color>";
                }

                text += reqText;
            }
            else if (house.CurrentStage == 2 && house.consumptionLvl3 != null)
            {
                string reqText = "\n\n<b>Для улучшения до 3 уровня:</b>";
                reqText += "\n- Доступ к дороге";
                reqText += "\n- Доступ к воде";

                foreach (var kvp in house.consumptionLvl3)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    reqText += $"\n- <color={color}>{kvp.Key}:{kvp.Value}</color>";
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

            // 🔹 Текущее потребление
            string consumptionText = "";
            bool anyMissing = false;

            if (prod.consumptionCost != null && prod.consumptionCost.Count > 0)
            {
                foreach (var kvp in prod.consumptionCost)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    if (!prod.isActive && available < kvp.Value)
                    {
                        color = "red";
                        anyMissing = true;
                    }
                    consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
                }
            }

            // 🔹 Производство
            string productionText = "";
            if (prod.production != null && prod.production.Count > 0)
            {
                foreach (var kvp in prod.production)
                    productionText += $"\nПроизводит: <color=white>{kvp.Key} +{kvp.Value}/сек</color>";
            }

            text += productionText;
            text += "\nПотребляет: " + (string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText);

            if (!prod.isActive && prod.consumptionCost.Count > 0 && anyMissing)
                text += "\n<color=red>⚠ Не работает: не хватает ресурсов!</color>";

            // 🔹 Требования для улучшения
            if (prod.CurrentStage == 1 &&
                ((prod.upgradeConsumption != null && prod.upgradeConsumption.Count > 0) ||
                 (prod.upgradeProductionBonus != null && prod.upgradeProductionBonus.Count > 0)))
            {
                string reqText = "\n\n<b>Для улучшения до 2 уровня:</b>";

  

                // Потребности экономики
                if (prod.upgradeConsumption != null)
                {
                    foreach (var kvp in prod.upgradeConsumption)
                    {
                        int available = ResourceManager.Instance.GetResource(kvp.Key);
                        string color = available >= kvp.Value ? "white" : "red";
                        reqText += $"\n- <color={color}>{kvp.Key}:{kvp.Value}</color>";
                    }
                }

                // Покажем бонусы от апгрейда
                if (prod.upgradeProductionBonus != null && prod.upgradeProductionBonus.Count > 0)
                {
                    reqText += "\n\n<b>После улучшения производит дополнительно:</b>";
                    foreach (var kvp in prod.upgradeProductionBonus)
                    {
                        reqText += $"\n+ <color=green>{kvp.Key} +{kvp.Value}/сек</color>";
                    }
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
