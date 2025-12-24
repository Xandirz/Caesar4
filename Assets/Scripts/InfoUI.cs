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

    [Header("Production Button (Prefab)")] [SerializeField]
    private RectTransform root; // место, где должна быть кнопка (ровно в точке root)

    [SerializeField] private GameObject pauseButtonPrefab; // prefab кнопки (должен иметь Button + TMP_Text внутри)

    private Button pauseButton;
    private TMP_Text pauseButtonLabel;

    private House currentHouse;
    private ProductionBuilding currentProduction;

    private bool infoAlreadyVisible = false;
    private PlacedObject lastSelected;

    private float refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 1f;

    void Awake()
    {
        Instance = this;
        infoPanel.SetActive(false);

        EnsurePauseButtonCreated();
        SetPauseButtonVisible(false);
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

    public void ShowInfo(PlacedObject po, bool triggerHighlight = true)
    {
        infoPanel.SetActive(true);

        // ✅ если уже открыто для того же объекта, не повторяем подсветку
        if (infoAlreadyVisible && lastSelected == po)
        {
            UpdateText(po);
            return;
        }

        lastSelected = po;
        infoAlreadyVisible = true;

        // подсветка (как у тебя)
        if (triggerHighlight && AllBuildingsManager.Instance != null && MouseHighlighter.Instance != null)
        {
            var sameTypeCells = new List<Vector2Int>();

            foreach (var b in AllBuildingsManager.Instance.GetAllBuildings())
            {
                if (b == null) continue;
                if (b == po) continue;

                if (b.BuildMode == po.BuildMode)
                    sameTypeCells.AddRange(b.GetOccupiedCells());
            }

            var selectedCells = po.GetOccupiedCells();
            MouseHighlighter.Instance.ShowBuildModeHighlights(sameTypeCells, po.BuildMode, selectedCells);
        }

        UpdateText(po);
    }

    private void UpdateText(PlacedObject po)
    {
        var sb = new StringBuilder(256);

        currentHouse = null;
        currentProduction = null;

        // по умолчанию прячем кнопку, покажем только для ProductionBuilding
        EnsurePauseButtonCreated();
        SetPauseButtonVisible(false);

        var rm = ResourceManager.Instance;
        if (rm == null)
        {
            infoText.text = $"<b>{po.name}</b>";
            return;
        }

        sb.Append("<b>").Append(po.name).Append("</b>");

        // 🚗 Дорога (показываем только если объект не Road)
        if (!(po is Road))
        {
            bool needsRoad = po.RequiresRoadAccess;

            if (!needsRoad)
            {
                sb.Append("\nДорога: <color=white>Не нужна</color>");
            }
            else
            {
                string roadColor = po.hasRoadAccess ? "white" : "red";
                sb.Append("\nДорога: <color=")
                    .Append(roadColor)
                    .Append(">")
                    .Append(po.hasRoadAccess ? "Есть" : "Нет")
                    .Append("</color>");
            }
        }

        // 🏠 Дом
        if (po is House house)
        {
            currentHouse = house;

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

            if (house.CurrentStage >= 4)
            {
                string templeColor = house.HasTemple ? "white" : "red";
                sb.Append("\nХрам: <color=")
                    .Append(templeColor)
                    .Append(">")
                    .Append(house.HasTemple ? "Есть" : "Нет")
                    .Append("</color>");
            }

            bool inNoise = IsHouseInNoise(house);
            sb.Append("\nШум: <color=")
                .Append(inNoise ? "red" : "white")
                .Append(">")
                .Append(inNoise ? "В зоне шума" : "Нет")
                .Append("</color>");

            if (house.consumption != null && house.consumption.Count > 0)
            {
                sb.Append("\nПотребление: ");

                foreach (var kvp in house.consumption)
                {
                    string resName = kvp.Key;
                    int amount = kvp.Value;

                    bool missingForThisHouse =
                        house.lastMissingResources != null &&
                        house.lastMissingResources.Contains(resName);

                    string color = missingForThisHouse ? "red" : "white";

                    sb.Append("<color=")
                        .Append(color)
                        .Append(">")
                        .Append(resName)
                        .Append(":")
                        .Append(amount)
                        .Append("</color> ");
                }
            }
            else
            {
                sb.Append("\nПотребление: Нет");
            }

            var surplus = AllBuildingsManager.Instance != null
                ? AllBuildingsManager.Instance.CalculateSurplus()
                : new Dictionary<string, float>();

            Dictionary<string, int> nextCons = null;
            string nextLevelLabel = "";

            int targetHouseLevel = house.CurrentStage + 1;
            bool upgradeUnlocked = (targetHouseLevel <= 4) && house.IsUpgradeUnlocked(targetHouseLevel);

            if (upgradeUnlocked)
            {
                if (house.CurrentStage == 1 && house.consumptionLvl2 != null && house.consumptionLvl2.Count > 0)
                {
                    nextCons = house.consumptionLvl2;
                    nextLevelLabel = "2 уровня";
                }
                else if (house.CurrentStage == 2 && house.consumptionLvl3 != null && house.consumptionLvl3.Count > 0)
                {
                    nextCons = house.consumptionLvl3;
                    nextLevelLabel = "3 уровня";
                }
                else if (house.CurrentStage == 3 && house.consumptionLvl4 != null && house.consumptionLvl4.Count > 0)
                {
                    nextCons = house.consumptionLvl4;
                    nextLevelLabel = "4 уровня";
                }
            }

            if (nextCons != null)
            {
                sb.Append("\n\n<b>Для улучшения до ")
                    .Append(nextLevelLabel)
                    .Append(":</b>");

                if (house.CurrentStage == 1)
                {
                    if (house.RequiresRoadAccess && !house.hasRoadAccess)
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
                else if (house.CurrentStage == 3)
                {
                    sb.Append("\n- Храм: <color=")
                        .Append(house.HasTemple ? "white" : "red")
                        .Append(">")
                        .Append(house.HasTemple ? "Есть" : "Нет")
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

        // 🏭 ProductionBuilding
        if (po is ProductionBuilding prod)
        {
            currentProduction = prod;

            EnsurePauseButtonCreated();
            SetPauseButtonVisible(true);
            RefreshPauseButtonVisuals();

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

            // ================= Потребление =================
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

// ================= Апгрейд =================
            int targetProdLevel = prod.CurrentStage + 1;
            bool prodUpgradeUnlocked = prod.IsUpgradeUnlocked(targetProdLevel);

// 👉 профицит считаем ОДИН РАЗ
            var surplus = AllBuildingsManager.Instance != null
                ? AllBuildingsManager.Instance.CalculateSurplus()
                : new Dictionary<string, float>();

            if (prodUpgradeUnlocked)
            {
                // -------- 1 -> 2 --------
                if (prod.CurrentStage == 1 &&
                    (
                        (prod.addConsumptionLevel2 != null && prod.addConsumptionLevel2.Count > 0) ||
                        (prod.upgradeProductionBonusLevel2 != null && prod.upgradeProductionBonusLevel2.Count > 0)
                    ))
                {
                    sb.Append("\n\n<b>Для улучшения до 2 уровня:</b>");

                    if (prod.addConsumptionLevel2 != null)
                    {
                        foreach (var kvp in prod.addConsumptionLevel2)
                        {
                            surplus.TryGetValue(kvp.Key, out float extra);
                            string color = extra >= kvp.Value ? "white" : "red";

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

                // -------- 2 -> 3 --------
                if (prod.CurrentStage == 2 &&
                    (
                        (prod.addConsumptionLevel3 != null && prod.addConsumptionLevel3.Count > 0) ||
                        (prod.upgradeProductionBonusLevel3 != null && prod.upgradeProductionBonusLevel3.Count > 0)
                    ))
                {
                    sb.Append("\n\n<b>Для улучшения до 3 уровня:</b>");

                    if (prod.addConsumptionLevel3 != null)
                    {
                        foreach (var kvp in prod.addConsumptionLevel3)
                        {
                            surplus.TryGetValue(kvp.Key, out float extra);
                            string color = extra >= kvp.Value ? "white" : "red";

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
        }

        infoText.text = sb.ToString();
    }

    // ===== кнопка из префаба, ровно на месте root =====

    private void EnsurePauseButtonCreated()
    {
        if (pauseButton != null) return;

        if (root == null)
        {
            Debug.LogError("[InfoUI] root is NULL. Assign root RectTransform in inspector.");
            return;
        }

        if (pauseButtonPrefab == null)
        {
            Debug.LogError("[InfoUI] pauseButtonPrefab is NULL. Assign button prefab in inspector.");
            return;
        }

        GameObject go = Instantiate(pauseButtonPrefab, root, false);

        // РОВНО на месте root (локально 0,0,0 относительно root)
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = root.anchorMin;
            rt.anchorMax = root.anchorMax;
            rt.pivot = root.pivot;

            rt.anchoredPosition = Vector2.zero;
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        pauseButton = go.GetComponent<Button>();
        if (pauseButton == null)
        {
            Debug.LogError("[InfoUI] pauseButtonPrefab must have a Button component.");
            Destroy(go);
            return;
        }

        pauseButtonLabel = go.GetComponentInChildren<TMP_Text>(true);
        if (pauseButtonLabel == null)
        {
            Debug.LogError("[InfoUI] pauseButtonPrefab must have TMP_Text somewhere in children.");
            // не уничтожаю — кнопка может быть иконкой без текста
        }

        pauseButton.onClick.RemoveAllListeners();
        pauseButton.onClick.AddListener(OnPauseButtonClicked);

        go.SetActive(false);
    }

    private void SetPauseButtonVisible(bool visible)
    {
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(visible);
    }

    private void RefreshPauseButtonVisuals()
    {
        if (pauseButtonLabel == null) return;
        if (currentProduction == null)
        {
            pauseButtonLabel.text = "Pause";
            return;
        }

        // Требует, чтобы в ProductionBuilding были:
        // public bool IsPaused { get; }
        // public void TogglePause()
        pauseButtonLabel.text = currentProduction.IsPaused ? "Resume" : "Pause";
    }

    private void OnPauseButtonClicked()
    {
        if (currentProduction == null) return;

        currentProduction.TogglePause();
        RefreshPauseButtonVisuals();

        // чтобы сразу обновить "Активно" и т.п.
        UpdateText(currentProduction);
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

        SetPauseButtonVisible(false);
    }

    // ====== шум вокруг дома ======

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

    private bool IsInEffectSquare(Vector2Int center, Vector2Int pos, int radius)
    {
        return Mathf.Abs(pos.x - center.x) <= radius &&
               Mathf.Abs(pos.y - center.y) <= radius;
    }
}