    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    using System.Text;

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

        public void RefreshIfVisible()
        {
            if (!infoPanel.activeSelf) return;

            if (currentHouse != null)
                ShowInfo(currentHouse, false);
            else if (currentProduction != null)
                ShowInfo(currentProduction, false);
        }
        
        /*void Update()
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
        }*/

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

                    // ✅ пропускаем текущее здание
                    if (b == po)
                        continue;

                    if (b.BuildMode == po.BuildMode)
                        sameTypeCells.AddRange(b.GetOccupiedCells());
                }

                // ✅ Добавляем клетки выбранного здания как отдельный параметр
                var selectedCells = po.GetOccupiedCells();

                // отправляем и те, и другие
                MouseHighlighter.Instance.ShowBuildModeHighlights(sameTypeCells, po.BuildMode, selectedCells);
            }

            UpdateText(po);
        }

   private void UpdateText(PlacedObject po)
{
    var sb = new StringBuilder(256);
    var rm = ResourceManager.Instance;

    sb.Append("<b>").Append(po.name).Append("</b>");

    // 🚗 Дорога
    if (!(po is Road))
    {
        string roadColor = po.hasRoadAccess ? "white" : "red";
        sb.Append("\nДорога: <color=")
          .Append(roadColor)
          .Append(">")
          .Append(po.hasRoadAccess ? "Есть" : "Нет")
          .Append("</color>");
    }

    // 🏠 Дом
    if (po is House house)
    {
        currentHouse = house;
        currentProduction = null;

        sb.Append("\nУровень: ").Append(house.CurrentStage);
        sb.Append("\nНаселение: ").Append(house.currentPopulation);

        if (house.CurrentStage >= 2)
        {
            string waterColor = house.HasWater ? "white" : "red";
            sb.Append("\nВода: <color=")
              .Append(waterColor)
              .Append(">")
              .Append(house.HasWater ? "Есть" : "Нет")
              .Append("</color>");
        }

        if (house.CurrentStage >= 3)
        {
            string marketColor = house.HasMarket ? "white" : "red";
            sb.Append("\nРынок: <color=")
              .Append(marketColor)
              .Append(">")
              .Append(house.HasMarket ? "Есть" : "Нет")
              .Append("</color>");
        }

        // 🔊 Шум
        bool inNoise = IsHouseInNoise(house);
        string noiseColor = inNoise ? "red" : "white";
        string noiseText = inNoise ? "В зоне шума" : "Нет";
        sb.Append("\nШум: <color=")
          .Append(noiseColor)
          .Append(">")
          .Append(noiseText)
          .Append("</color>");

        // Потребление дома
        sb.Append("\nПотребляет: ");
        if (house.consumption == null || house.consumption.Count == 0)
        {
            sb.Append("Нет");
        }
        else
        {
            foreach (var kvp in house.consumption)
            {
                int available = rm.GetResource(kvp.Key);
                string color = available >= kvp.Value ? "white" : "red";
                sb.Append("<color=")
                  .Append(color)
                  .Append(">")
                  .Append(kvp.Key)
                  .Append(":")
                  .Append(kvp.Value)
                  .Append("</color> ");
            }
        }

        // === Возможное улучшение ===
        var surplus = AllBuildingsManager.Instance.CalculateSurplus();
        Dictionary<string, int> nextCons = null;
        string nextLevelLabel = "";

        int targetHouseLevel = house.CurrentStage + 1;
        bool upgradeUnlocked = true;

        if (targetHouseLevel <= 3)
        {
            // если IsUpgradeUnlocked есть — просто вызываем
            upgradeUnlocked = house.IsUpgradeUnlocked(targetHouseLevel);
        }

        if (upgradeUnlocked)
        {
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
        }

        if (nextCons != null)
        {
            sb.Append("\n\n<b>Для улучшения до ")
              .Append(nextLevelLabel)
              .Append(":</b>");

            if (house.CurrentStage == 1)
            {
                string needWater = house.HasWater ? "white" : "red";
                if (!house.hasRoadAccess)
                    sb.Append("\n- Дорога: <color=red>Нет</color>");
                sb.Append("\n- Вода: <color=")
                  .Append(needWater)
                  .Append(">")
                  .Append(house.HasWater ? "Есть" : "Нет")
                  .Append("</color>");
            }
            else if (house.CurrentStage == 2)
            {
                string marketColor = house.HasMarket ? "white" : "red";
                sb.Append("\n- Рынок: <color=")
                  .Append(marketColor)
                  .Append(">")
                  .Append(house.HasMarket ? "Есть" : "Нет")
                  .Append("</color>");
            }

            foreach (var kvp in nextCons)
            {
                string resName = kvp.Key;
                int required = kvp.Value;
                surplus.TryGetValue(resName, out float extra);
                string color = (extra >= required) ? "white" : "red";

                sb.Append("\n- <color=")
                  .Append(color)
                  .Append(">")
                  .Append(resName)
                  .Append(":")
                  .Append(required)
                  .Append("</color>");
            }
        }
    }

    // 🏭 Производственное здание
    if (po is ProductionBuilding prod)
    {
        currentProduction = prod;
        currentHouse = null;

        string activeColor = prod.isActive ? "white" : "red";
        sb.Append("\nАктивно: <color=")
          .Append(activeColor)
          .Append(">")
          .Append(prod.isActive ? "Да" : "Нет")
          .Append("</color>");
        sb.Append("\nУровень: ").Append(prod.CurrentStage);

        if (prod.isNoisy)
        {
            sb.Append("\n<color=red>Издаем шум</color> (радиус: ")
              .Append(prod.noiseRadius)
              .Append(")");
        }

        int freeWorkers = rm.FreeWorkers;
        int requiredWorkers = prod.WorkersRequired;

        if (requiredWorkers > 0)
        {
            if (freeWorkers >= requiredWorkers || prod.isActive)
            {
                sb.Append("\nРабочие: <color=white>")
                  .Append(requiredWorkers)
                  .Append("</color> (Доступно: ")
                  .Append(freeWorkers)
                  .Append(")");
            }
            else
            {
                int deficit = requiredWorkers - freeWorkers;
                sb.Append("\nРабочие: <color=red>Не хватает ")
                  .Append(deficit)
                  .Append(" чел.</color> (Требуется: ")
                  .Append(requiredWorkers)
                  .Append(")");
            }
        }

        // Производство
        if (prod.production != null && prod.production.Count > 0)
        {
            foreach (var kvp in prod.production)
            {
                sb.Append("\nПроизводит: <color=white>")
                  .Append(kvp.Key)
                  .Append(" +")
                  .Append(kvp.Value)
                  .Append("/сек</color>");
            }
        }

        // Потребление
        sb.Append("\nПотребляет: ");
        if (prod.consumptionCost == null || prod.consumptionCost.Count == 0)
        {
            sb.Append("Нет");
        }
        else
        {
            foreach (var kvp in prod.consumptionCost)
            {
                string resName = kvp.Key;
                int requiredAmount = kvp.Value;

                int available = rm.GetResource(resName);

                bool isMissingForThisBuilding =
                    !prod.isActive &&
                    prod.lastMissingResources != null &&
                    prod.lastMissingResources.Contains(resName);

                string color;
                if (isMissingForThisBuilding)
                    color = "red";
                else
                    color = available >= requiredAmount ? "white" : "red";

                sb.Append("<color=")
                  .Append(color)
                  .Append(">")
                  .Append(resName)
                  .Append(":")
                  .Append(requiredAmount)
                  .Append("</color> ");
            }
        }

        // === Требования для улучшения ===
        int targetProdLevel = prod.CurrentStage + 1;
        bool prodUpgradeUnlocked = prod.IsUpgradeUnlocked(targetProdLevel);

        if (prodUpgradeUnlocked)
        {
            if (prod.CurrentStage == 1 &&
                (prod.upgradeConsumptionLevel2.Count > 0 || prod.upgradeProductionBonusLevel2.Count > 0))
            {
                sb.Append("\n\n<b>Для улучшения до 2 уровня:</b>");

                foreach (var kvp in prod.upgradeConsumptionLevel2)
                {
                    int available = rm.GetResource(kvp.Key);
                    string color = available >= kvp.Value ? "white" : "red";
                    sb.Append("\n- <color=")
                      .Append(color)
                      .Append(">")
                      .Append(kvp.Key)
                      .Append(":")
                      .Append(kvp.Value)
                      .Append("</color>");
                }
            }
        }
    }

    infoText.text = sb.ToString();
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

        // ======== ВСПОМОГАТЕЛЬНОЕ: проверка шума вокруг дома ========

        private bool IsHouseInNoise(House house)
        {
            if (house == null || AllBuildingsManager.Instance == null) return false;

            Vector2Int hp = house.gridPos;

            foreach (var b in AllBuildingsManager.Instance.GetAllBuildings())
            {
                if (b is ProductionBuilding prod && prod.isNoisy)
                {
                    if (IsInEffectSquare(prod.gridPos, hp, prod.noiseRadius))
                        return true;
                }
            }
            return false;
        }

        // та же логика «квадратного» радиуса, что используется в хайлайтах
        private bool IsInEffectSquare(Vector2Int center, Vector2Int pos, int radius)
        {
            return Mathf.Abs(pos.x - center.x) <= radius &&
                   Mathf.Abs(pos.y - center.y) <= radius;
        }
    }
