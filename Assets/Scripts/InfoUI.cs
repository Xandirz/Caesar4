using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    public static InfoUI Instance;

    [SerializeField] private GameObject infoPanel; 
    [SerializeField] private TMP_Text infoText;    
    [SerializeField] private Button upgradeButton; // кнопка улучшения дома

    private House currentHouse;

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

    string text = po.name;

    // 🔹 Дорога
    if (!(po is Road))
    {
        string roadColor = po.hasRoadAccess ? "white" : "red";
        text += $"\nДорога: <color={roadColor}>{(po.hasRoadAccess ? "Есть" : "Нет")}</color>";
    }

    // 🔹 Дом
    if (po is House house)
    {
        currentHouse = house;

        // Вода
        string waterColor = house.HasWater ? "white" : "red";
        text += $"\nВода: <color={waterColor}>{(house.HasWater ? "Есть" : "Нет")}</color>";

        // Уровень
        text += $"\nУровень: {house.CurrentStage}";

        // Потребление
        string consumptionText = "";
        foreach (var kvp in house.consumptionCost)
        {
            int available = ResourceManager.Instance.GetResource(kvp.Key);
            string color = available >= kvp.Value ? "white" : "red";
            consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
        }
        text += "\nПотребляет: " + (string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText);

        // Кнопка улучшения (только для Stage 1)
        if (house.CurrentStage == 1)
        {
            upgradeButton.gameObject.SetActive(true);
            upgradeButton.GetComponentInChildren<TMP_Text>().text = "Upgrade";
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => TryUpgradeHouse(house));

            // Цена апгрейда с подсветкой
            string costStr = "";
            foreach (var kvp in house.upgradeCost)
            {
                int available = ResourceManager.Instance.GetResource(kvp.Key);
                string color = available >= kvp.Value ? "white" : "red";
                costStr += $"<color={color}>{kvp.Key}:{kvp.Value}</color> ";
            }
            text += $"\nТребуется для улучшения: {costStr.Trim()}";
        }
        else
        {
            upgradeButton.gameObject.SetActive(false);
        }
    }
    else
    {
        upgradeButton.gameObject.SetActive(false);
    }

    // 🔹 Производственные здания
    if (po is ProductionBuilding prodBuilding)
    {
        text += "\nАктивно: " + (prodBuilding.isActive ? "Да" : "Нет");

        string consumptionText = "";
        if (prodBuilding.consumptionCost != null && prodBuilding.consumptionCost.Count > 0)
        {
            foreach (var kvp in prodBuilding.consumptionCost)
            {
                int available = ResourceManager.Instance.GetResource(kvp.Key);
                string color = available >= kvp.Value ? "white" : "red";
                consumptionText += $"<color={color}>{kvp.Key}:{kvp.Value}</color>  ";
            }
        }

        string productionText = "";
        foreach (var kvp in prodBuilding.production)
        {
            productionText += $"\nРесурс: {kvp.Key} (+{kvp.Value}/сек)";
        }

        text += productionText +
                $"\nТребуется: {(string.IsNullOrEmpty(consumptionText) ? "Нет" : consumptionText)}";
    }

    infoText.text = text;
}



    private void TryUpgradeHouse(House house)
    {
        if (house.TryUpgrade())
            ShowInfo(house);
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
        currentHouse = null;
        infoText.text = ""; // 🔹 очищаем текст, чтобы не висели старые данные
    }
}
