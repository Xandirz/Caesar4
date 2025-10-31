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

    // Флаг, чтобы не вызывать повторно подсветку
    private bool infoAlreadyVisible = false;
    private PlacedObject lastSelected;

    // таймер автообновления
    private float refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 1f;

    void Awake()
    {
        Instance = this;
        infoPanel.SetActive(false);
    }

    void Update()
    {
        if (!infoPanel.activeSelf) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= REFRESH_INTERVAL)
        {
            refreshTimer = 0f;

            if (currentHouse != null)
                ShowInfo(currentHouse, false);
            else if (currentProduction != null)
                ShowInfo(currentProduction, false);
        }
    }

    public void ShowInfo(PlacedObject po, bool triggerHighlight = true)
    {
        infoPanel.SetActive(true);

        // ✅ Проверяем — если уже открыто для того же объекта, не повторяем подсветку
        if (infoAlreadyVisible && lastSelected == po)
        {
            UpdateText(po);
            return;
        }

        // запоминаем объект
        lastSelected = po;
        infoAlreadyVisible = true;

        // подсвечиваем здания того же типа (один раз)
        if (triggerHighlight && AllBuildingsManager.Instance != null && MouseHighlighter.Instance != null)
        {
            var sameTypeCells = new List<Vector2Int>();

            foreach (var b in AllBuildingsManager.Instance.GetAllBuildings())
            {
                if (b == null) continue;
                if (b.BuildMode == po.BuildMode)
                    sameTypeCells.AddRange(b.GetOccupiedCells());
            }

            if (sameTypeCells.Count > 0)
                MouseHighlighter.Instance.ShowBuildModeHighlights(sameTypeCells,po.BuildMode);
        }

        UpdateText(po);
    }

    private void UpdateText(PlacedObject po)
    {
        string text = $"<b>{po.name}</b>";

        // 🚗 Дорога
        if (!(po is Road))
        {
            string roadColor = po.hasRoadAccess ? "white" : "red";
            text += $"\nДорога: <color={roadColor}>{(po.hasRoadAccess ? "Есть" : "Нет")}</color>";
        }

        // 🏠 Дом
        if (po is House house)
        {
            currentHouse = house;
            currentProduction = null;

            text += $"\nУровень: {house.CurrentStage}";
            text += $"\nНаселение: {house.currentPopulation}";

            if (house.CurrentStage >= 2)
            {
                string waterColor = house.HasWater ? "white" : "red";
                text += $"\nВода: <color={waterColor}>{(house.HasWater ? "Есть" : "Нет")}</color>";
            }

            if (house.CurrentStage >= 3)
            {
                string marketColor = house.HasMarket ? "white" : "red";
                text += $"\nРынок: <color={marketColor}>{(house.HasMarket ? "Есть" : "Нет")}</color>";
            }

            // текущее потребление
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
                text += $"\n\n<b>Для улучшения до {nextLevelLabel}:</b>";

                if (house.CurrentStage == 1)
                {
                    string needWater = house.HasWater ? "white" : "red";
                    if (!house.hasRoadAccess)
                        text += $"\n- Дорога: <color=red>Нет</color>";
                    text += $"\n- Вода: <color={needWater}>{(house.HasWater ? "Есть" : "Нет")}</color>";
                }
                else if (house.CurrentStage == 2)
                {
                    string marketColor = house.HasMarket ? "white" : "red";
                    text += $"\n- Рынок: <color={marketColor}>{(house.HasMarket ? "Есть" : "Нет")}</color>";
                }

                foreach (var kvp in nextCons)
                {
                    string resName = kvp.Key;
                    int required = kvp.Value;
                    surplus.TryGetValue(resName, out float extra);
                    string color = (extra >= required) ? "white" : "red";
                    text += $"\n- <color={color}>{resName}:{required}</color>";
                }
            }
        }

        // 🏭 Производственное здание
        if (po is ProductionBuilding prod)
        {
            currentProduction = prod;
            currentHouse = null;

            string activeColor = prod.isActive ? "white" : "red";
            text += $"\nАктивно: <color={activeColor}>{(prod.isActive ? "Да" : "Нет")}</color>";
            text += $"\nУровень: {prod.CurrentStage}";

            // 👷 Рабочие
            int totalPeople = ResourceManager.Instance.GetResource("People");
            int freeWorkers = ResourceManager.Instance.FreeWorkers;
            int required = prod.workersRequired;

            if (required > 0)
            {
                if (freeWorkers >= required || prod.isActive)
                    text += $"\nРабочие: <color=white>{required}</color> (Доступно: {freeWorkers})";
                else
                {
                    int deficit = required - freeWorkers;
                    text += $"\nРабочие: <color=red>Не хватает {deficit} чел.</color> (Требуется: {required})";
                }
            }

            // Производство
            string productionText = "";
            if (prod.production != null && prod.production.Count > 0)
            {
                foreach (var kvp in prod.production)
                    productionText += $"\nПроизводит: <color=white>{kvp.Key} +{kvp.Value}/сек</color>";
            }

            // Потребление
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

            // === Требования для улучшения ===
            if (prod.CurrentStage == 1 &&
                (prod.upgradeConsumptionLevel1.Count > 0 || prod.upgradeProductionBonusLevel1.Count > 0))
            {
                text += "\n\n<b>Для улучшения до 2 уровня:</b>";

                foreach (var kvp in prod.upgradeConsumptionLevel1)
                {
                    int available = ResourceManager.Instance.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    text += $"\n- <color={color}>{kvp.Key}:{kvp.Value}</color>";
                }
            }
        }

        infoText.text = text;
    }

    public void HideInfo()
    {
        if (MouseHighlighter.Instance && MouseHighlighter.Instance.gameObject != null)
            MouseHighlighter.Instance.ClearHighlights();

        infoPanel.SetActive(false);
        currentHouse = null;
        currentProduction = null;
        infoText.text = "";
        refreshTimer = 0f;
        infoAlreadyVisible = false;
        lastSelected = null;
    }
}
