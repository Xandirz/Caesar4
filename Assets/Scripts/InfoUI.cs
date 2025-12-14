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
            float t0 = Time.realtimeSinceStartup;

            if (!infoPanel.activeSelf) return;

            if (currentHouse != null)
                ShowInfo(currentHouse, false);
            else if (currentProduction != null)
                ShowInfo(currentProduction, false);
            
            float dt = (Time.realtimeSinceStartup - t0) * 1000f;
            if (dt > 5f)
                Debug.Log($"[PERF] refreshVisibe занял {dt:F2} ms");
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
    // локальные хелперы, чтобы метод был самодостаточным
    bool IsFoodLvl1(string name) =>
        name == "Berry" || name == "Fish" || name == "Nuts" || name == "Mushrooms";

    string GetConsumedFoodLvl1Resource(Dictionary<string, int> consumption)
    {
        if (consumption == null) return null;
        foreach (var kvp in consumption)
            if (IsFoodLvl1(kvp.Key))
                return kvp.Key; // текущий выбранный ресурс еды (реально потребляемый)
        return null;
    }

    bool HasAnyFoodLvl1InStorage(ResourceManager rm)
    {
        return rm.GetResource("Berry") > 0 ||
               rm.GetResource("Fish") > 0 ||
               rm.GetResource("Nuts") > 0 ||
               rm.GetResource("Mushrooms") > 0;
    }

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
        sb.Append("\nШум: <color=")
          .Append(inNoise ? "red" : "white")
          .Append(">")
          .Append(inNoise ? "В зоне шума" : "Нет")
          .Append("</color>");

        // 🍖 Потребление дома (FoodLvl1)
        sb.Append("\nПотребляет: ");

        // Реально выбранный домом ресурс (Berry/Fish/Nuts/Mushrooms), если он прописан в consumption
        string consumedFood = GetConsumedFoodLvl1Resource(house.consumption);

        // Факт наличия еды в городе (суммарно по группе)
        bool anyFoodInStorage = HasAnyFoodLvl1InStorage(rm);

        // ✅ Правило из твоего сообщения:
        // - если еды нет → "Food Level 1 (Berry, Fish, Nuts, Mushrooms)"
        // - если еда есть → "<конкретный ресурс> (Food Level 1)"
        if (!anyFoodInStorage)
        {
            sb.Append("<color=red>Food Level 1 (Berry, Fish, Nuts, Mushrooms)</color>");
        }
        else
        {
            // если еда есть, но дом почему-то ещё не выбрал конкретный ресурс — показываем группу (защита)
            if (string.IsNullOrEmpty(consumedFood))
            {
                sb.Append("<color=white>Food Level 1</color> (Berry, Fish, Nuts, Mushrooms)");
            }
            else
            {
                sb.Append("<color=white>")
                  .Append(consumedFood)
                  .Append("</color> (Food Level 1)");
            }
        }

        // === Возможное улучшение ===
        var surplus = AllBuildingsManager.Instance.CalculateSurplus();
        Dictionary<string, int> nextCons = null;
        string nextLevelLabel = "";

        int targetHouseLevel = house.CurrentStage + 1;
        bool upgradeUnlocked = true;

        if (targetHouseLevel <= 3)
            upgradeUnlocked = house.IsUpgradeUnlocked(targetHouseLevel);

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
                if (!house.hasRoadAccess)
                    sb.Append("\n- Дорога: <color=red>Нет</color>");

                sb.Append("\n- Вода: <color=")
                  .Append(house.HasWater ? "white" : "red")
                  .Append(">")
                  .Append(house.HasWater ? "Есть" : "Нет")
                  .Append("</color>");
            }
            else if (house.CurrentStage == 2)
            {
                sb.Append("\n- Рынок: <color=")
                  .Append(house.HasMarket ? "white" : "red")
                  .Append(">")
                  .Append(house.HasMarket ? "Есть" : "Нет")
                  .Append("</color>");
            }

            foreach (var kvp in nextCons)
            {
                surplus.TryGetValue(kvp.Key, out float extra);
                sb.Append("\n- <color=")
                  .Append(extra >= kvp.Value ? "white" : "red")
                  .Append(">")
                  .Append(kvp.Key)
                  .Append(":")
                  .Append(kvp.Value)
                  .Append("</color>");
            }
        }
    }

    // 🏭 Производственное здание
    if (po is ProductionBuilding prod)
    {
        currentProduction = prod;
        currentHouse = null;

        sb.Append("\nАктивно: <color=")
          .Append(prod.isActive ? "white" : "red")
          .Append(">")
          .Append(prod.isActive ? "Да" : "Нет")
          .Append("</color>");

        sb.Append("\nУровень: ").Append(prod.CurrentStage);

        if (prod.isNoisy)
        {
            sb.Append("\n<color=red>Издаёт шум</color> (радиус: ")
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
                sb.Append("\nРабочие: <color=red>Не хватает ")
                  .Append(requiredWorkers - freeWorkers)
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

                bool isMissingForThisBuilding =
                    !prod.isActive &&
                    prod.lastMissingResources != null &&
                    prod.lastMissingResources.Contains(resName);

                string color = isMissingForThisBuilding ? "red" : "white";

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
        
        // === FoodLvl1 helpers for InfoUI ===
        private static readonly string[] FoodLvl1Resources =
        {
            "Berry",
            "Fish",
            "Nuts",
            "Mushrooms"
        };

        private static bool IsFoodLvl1(string name)
        {
            for (int i = 0; i < FoodLvl1Resources.Length; i++)
                if (FoodLvl1Resources[i] == name)
                    return true;
            return false;
        }

        private string GetConsumedFoodLvl1Resource(Dictionary<string, int> consumption)
        {
            if (consumption == null) return null;

            foreach (var kvp in consumption)
            {
                if (IsFoodLvl1(kvp.Key))
                    return kvp.Key; // дом реально потребляет ЭТОТ ресурс
            }

            return null;
        }

        private bool HasAnyFoodLvl1(ResourceManager rm)
        {
            for (int i = 0; i < FoodLvl1Resources.Length; i++)
            {
                if (rm.GetResource(FoodLvl1Resources[i]) > 0)
                    return true;
            }
            return false;
        }

    }
