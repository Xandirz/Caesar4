using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    public static InfoUI Instance;

    [SerializeField] private GameObject infoPanel; 
    [SerializeField] private TMP_Text infoText;    
    [SerializeField] private Button upgradeButton;

    private House currentHouse;
    private ProductionBuilding currentProduction;

    void Awake()
    {
        Instance = this;
        infoPanel.SetActive(false);
        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(false);
    }

    public void ShowInfo(PlacedObject po)
    {
        infoPanel.SetActive(true);
        currentHouse = null;
        currentProduction = null;

        string text = po.name;

        // 🔹 Проверка дороги
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

            // === 🔹 Кнопка улучшения ===
            upgradeButton.onClick.RemoveAllListeners();
            string costStr = "";

            if (house.CurrentStage == 1 && house.upgradeCostLevel2 != null && house.upgradeCostLevel2.Count > 0)
            {
                // === Требования для 1→2 ===
                upgradeButton.gameObject.SetActive(true);
                upgradeButton.GetComponentInChildren<TMP_Text>().text = "Улучшить (до 2)";
                upgradeButton.onClick.AddListener(() => TryUpgradeHouse(house));

                foreach (var kvp in house.upgradeCostLevel2)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    costStr += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
                }

                text += $"\nТребуется для улучшения (до 2): {costStr.Trim()}";
            }
            else if (house.CurrentStage == 2 && house.upgradeCostLevel3 != null && house.upgradeCostLevel3.Count > 0)
            {
                // === Требования для 2→3 ===
                upgradeButton.gameObject.SetActive(true);
                upgradeButton.GetComponentInChildren<TMP_Text>().text = "Улучшить (до 3)";
                upgradeButton.onClick.AddListener(() => TryUpgradeHouse(house));

                string reqText = "\n<b>Требования для улучшения до 3 уровня:</b>";
                reqText += "\n- Доступ к дороге";
                reqText += "\n- Доступ к воде";

                foreach (var kvp in house.upgradeCostLevel3)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    reqText += $"\n- <color={color}>{kvp.Key} × {kvp.Value}</color>";
                }

                text += reqText;
            }
            else
            {
                upgradeButton.gameObject.SetActive(false);
            }
        }

        // === 🏭 ПРОИЗВОДСТВО ===
        if (po is ProductionBuilding prod)
        {
            currentProduction = prod;

            string activeColor = prod.isActive ? "white" : "red";
            text += $"\nАктивно: <color={activeColor}>{(prod.isActive ? "Да" : "Нет")}</color>";

            // 🔹 Потребление
            string consumptionText = "";
            bool anyMissing = false;

            if (prod.consumptionCost != null && prod.consumptionCost.Count > 0)
            {
                foreach (var kvp in prod.consumptionCost)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color;
                    if (!prod.isActive && available < kvp.Value)
                    {
                        color = "red";
                        anyMissing = true;
                    }
                    else
                    {
                        color = available >= kvp.Value ? "white" : "red";
                    }
                    consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
                }
            }

            // 🔹 Производство
            string productionText = "";
            foreach (var kvp in prod.production)
                productionText += $"\nПроизводит: <color=white>{kvp.Key} +{kvp.Value}/сек</color>";

            text += productionText;
            text += "\nПотребляет: " + (string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText);

            if (!prod.isActive && prod.consumptionCost.Count > 0 && anyMissing)
                text += "\n<color=red>⚠ Не работает: не хватает ресурсов!</color>";

            // === Кнопка апгрейда ===
            if (prod.CurrentStage == 1 && prod.upgradeCost != null && prod.upgradeCost.Count > 0)
            {
                upgradeButton.gameObject.SetActive(true);
                upgradeButton.GetComponentInChildren<TMP_Text>().text = "Upgrade";
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(() => TryUpgradeProduction(prod));

                string costStr = "";
                foreach (var kvp in prod.upgradeCost)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    costStr += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
                }

                text += $"\nТребуется для улучшения: {costStr.Trim()}";
            }
            else if (currentHouse == null)
            {
                upgradeButton.gameObject.SetActive(false);
            }
        }

        infoText.text = text;
    }

    private void TryUpgradeHouse(House house)
    {
        if (house.TryUpgrade())
            ShowInfo(house);
    }

    private void TryUpgradeProduction(ProductionBuilding prod)
    {
        if (prod.TryUpgrade())
            ShowInfo(prod);
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
        currentHouse = null;
        currentProduction = null;
        infoText.text = "";
    }
}
