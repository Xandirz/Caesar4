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

    if (!(po is Road))
        text += "\nДорога: " + (po.hasRoadAccess ? "Есть" : "Нет");

    if (po is House house)
    {
        currentHouse = house;

        text += "\nВода: " + (house.HasWater ? "Есть" : "Нет");
        text += "\nУровень: " + house.CurrentStage;

        // потребление
        string consumptionText = "";
        foreach (var kvp in house.consumptionCost)
            consumptionText += $"{kvp.Key}:{kvp.Value} ";
        text += "\nПотребляет: " + (consumptionText == "" ? "Нет" : consumptionText);

        // кнопка улучшения (только для Stage 1)
        if (house.CurrentStage == 1)
        {
            upgradeButton.gameObject.SetActive(true);
            upgradeButton.GetComponentInChildren<TMP_Text>().text = "Upgrade";
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => TryUpgradeHouse(house));

            // 🔹 добавляем цену в текст
            string costStr = "";
            foreach (var kvp in house.upgradeCost)
                costStr += $"{kvp.Key}:{kvp.Value} ";
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

    if (po is ProductionBuilding prodBuilding)
    {
        text += "\nАктивно: " + (prodBuilding.isActive ? "Да" : "Нет");

        string consumptionText = "";
        if (prodBuilding.consumptionCost != null && prodBuilding.consumptionCost.Count > 0)
        {
            foreach (var kvp in prodBuilding.consumptionCost)
                consumptionText += $"{kvp.Key}:{kvp.Value}  ";
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
