using UnityEngine;
using TMPro; // если используешь TextMeshPro

public class InfoUI : MonoBehaviour
{
    public static InfoUI Instance;

    [SerializeField] private GameObject infoPanel; // окно Info
    [SerializeField] private TMP_Text infoText;    // текст внутри окна

    void Awake()
    {
        Instance = this;
        infoPanel.SetActive(false); // прячем по умолчанию
    }

    public void ShowInfo(PlacedObject po)
    {
        infoPanel.SetActive(true);

        string text = po.name;

        // Общая проверка на дорогу
        if (!(po is Road))
        {
            text += "\nДорога: " + (po.hasRoadAccess ? "Есть" : "Нет");
        }

        // Если это дом — показываем воду и стадию
        if (po is House house)
        {
            text += "\nВода: " + (house.HasWater ? "Есть" : "Нет");
            text += "\nСтадия: " + house.CurrentStage;
        }

        if (po is ProductionBuilding prodBuilding)
        {
            text += "\n Активно: " + prodBuilding.isActive;
            
            string consumptionText = "";
            if (prodBuilding.consumptionCost != null && prodBuilding.consumptionCost.Count > 0)
            {
                foreach (var kvp in prodBuilding.consumptionCost)
                    consumptionText += $"{kvp.Key}: {kvp.Value}  ";
            }

            text += $"Ресурс: {prodBuilding.Resource} (+{prodBuilding.rate}/сек)\n" +
                    $"Требуется: {consumptionText}\n";
        }

        infoText.text = text;
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
    }
}