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
        TutorialEvents.OnInfoUIOpened();

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
                sb.Append("\nRoad: <color=white>No need</color>");
            }
            else
            {
                string roadColor = po.hasRoadAccess ? "white" : "red";
                sb.Append("\nRoad: <color=")
                    .Append(roadColor)
                    .Append(">")
                    .Append(po.hasRoadAccess ? "Yes" : "No")
                    .Append("</color>");
            }
        }

        // 🏠 Дом
        if (po is House house)
        {
            currentHouse = house;

            sb.Append("\nLevel: ").Append(house.CurrentStage);
            sb.Append("\nPopulation: ").Append(house.currentPopulation);

            if (house.CurrentStage >= 2)
            {
                string waterColor = house.HasWater ? "white" : "red";
                sb.Append("\nWater: <color=")
                    .Append(waterColor)
                    .Append(">")
                    .Append(house.HasWater ? "Yes" : "No")
                    .Append("</color>");
            }

            if (house.CurrentStage >= 3)
            {
                string marketColor = house.HasMarket ? "white" : "red";
                sb.Append("\nMarket: <color=")
                    .Append(marketColor)
                    .Append(">")
                    .Append(house.HasMarket ?"Yes" : "No")
                    .Append("</color>");
            }

            if (house.CurrentStage >= 4)
            {
                string templeColor = house.HasTemple ? "white" : "red";
                sb.Append("\nTemple: <color=")
                    .Append(templeColor)
                    .Append(">")
                    .Append(house.HasTemple ? "Yes" : "No")
                    .Append("</color>");
            }

            bool inNoise = IsHouseInNoise(house);
            sb.Append("\nNoise: <color=")
                .Append(inNoise ? "red" : "white")
                .Append(">")
                .Append(inNoise ? "Noise" : "No")
                .Append("</color>");

            // ================= Consumption (столбцом) =================
            AppendResourceList(
                sb,
                "Consumption",
                house.consumption,
                missing: house.lastMissingResources,
                missingOnlyWhenInactive: false,   // для дома просто подсвечиваем missing всегда
                isActive: true,
                showPlus: false,
                suffix: "");


            var surplus = AllBuildingsManager.Instance != null
                ? AllBuildingsManager.Instance.CalculateSurplus()
                : new Dictionary<string, float>();

            Dictionary<string, int> nextCons = null;
            string nextLevelLabel = "";

            int targetHouseLevel = house.CurrentStage + 1;
            bool upgradeUnlocked = (targetHouseLevel <= 5) && house.IsUpgradeUnlocked(targetHouseLevel);

            if (upgradeUnlocked)
            {
                if (house.CurrentStage == 1 && house.consumptionLvl2 != null && house.consumptionLvl2.Count > 0)
                {
                    nextCons = house.consumptionLvl2;
                    nextLevelLabel = "2 level";
                }
                else if (house.CurrentStage == 2 && house.consumptionLvl3 != null && house.consumptionLvl3.Count > 0)
                {
                    nextCons = house.consumptionLvl3;
                    nextLevelLabel = "3 level";
                }
                else if (house.CurrentStage == 3 && house.consumptionLvl4 != null && house.consumptionLvl4.Count > 0)
                {
                    nextCons = house.consumptionLvl4;
                    nextLevelLabel = "4 level";
                }
                else if (house.CurrentStage == 4 && house.consumptionLvl5 != null && house.consumptionLvl5.Count > 0)
                {
                    nextCons = house.consumptionLvl5;
                    nextLevelLabel = "5 level";
                }
            }

            if (nextCons != null)
            {
                sb.Append("\n\n<b>Needs for next upgrade ")
                    .Append(nextLevelLabel)
                    .Append(":</b>");

                if (house.CurrentStage == 1)
                {
                    if (house.RequiresRoadAccess && !house.hasRoadAccess)
                        sb.Append("\n- Road: <color=red>No</color>");

                    sb.Append("\n- Water: <color=")
                        .Append(house.HasWater ? "white" : "red")
                        .Append(">")
                        .Append(house.HasWater ? "Yes" : "No")
                        .Append("</color>");
                }
                else if (house.CurrentStage == 2)
                {
                    sb.Append("\n- Market: <color=")
                        .Append(house.HasMarket ? "white" : "red")
                        .Append(">")
                        .Append(house.HasMarket ? "Yes" : "No")
                        .Append("</color>");
                }
                else if (house.CurrentStage == 3)
                {
                    sb.Append("\n- Temple: <color=")
                        .Append(house.HasTemple ? "white" : "red")
                        .Append(">")
                        .Append(house.HasTemple ? "Yes" : "No")
                        .Append("</color>");
                }
                else if (house.CurrentStage == 4)
                {
                    sb.Append("\n- Bathhouse: <color=")
                        .Append(house.HasBathhouse ? "white" : "red")
                        .Append(">")
                        .Append(house.HasBathhouse ? "Yes" : "No")
                        .Append("</color>");
                    
                    sb.Append("\n- Doctor: <color=")
                        .Append(house.HasDoctor ? "white" : "red")
                        .Append(">")
                        .Append(house.HasDoctor ?"Yes" : "No")
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

            sb.Append("\nActive: <color=")
                .Append(prod.isActive ? "white" : "red")
                .Append(">")
                .Append(prod.isActive ? "Yes" : "No")
                .Append("</color>");

            sb.Append("\nLevel: ").Append(prod.CurrentStage);

            if (prod.isNoisy)
            {
                sb.Append("\n<color=red>Makes noise</color> (radius: ")
                    .Append(prod.noiseRadius)
                    .Append(")");
            }

            if (SettingsManager.Instance.settingNeedHouse && prod.NeedHouseNearby)
            {
                string houseColor = prod.hasHouseNearby ? "white" : "red";
                sb.Append("\nHouse nearby: <color=")
                    .Append(houseColor)
                    .Append(">")
                    .Append(prod.hasHouseNearby ? "Yes" : "No")
                    .Append("</color>");
            }


            int freeWorkers = rm.FreeWorkers;
            int requiredWorkers = prod.WorkersRequired;

            if (requiredWorkers > 0)
            {
                if (freeWorkers >= requiredWorkers || prod.isActive)
                {
                    sb.Append("\nWorkers: <color=white>")
                        .Append(requiredWorkers)
                        .Append("</color> (Avaliable: ")
                        .Append(freeWorkers)
                        .Append(")");
                }
                else
                {
                    sb.Append("\nWorkers: <color=red>Not enough ")
                        .Append(requiredWorkers - freeWorkers)
                        .Append(" чел.</color> (Needs: ")
                        .Append(requiredWorkers)
                        .Append(")");
                }
            }

            // ================= Consumption (столбцом) =================
            HashSet<string> missing = prod.lastMissingResources; // может быть null

            AppendResourceList(
                sb,
                "Consumption",
                prod.consumptionCost,
                missing: missing,
                missingOnlyWhenInactive: true,
                isActive: prod.isActive,
                showPlus: false,
                suffix: "");

// ================= Production (столбцом) =================
            AppendResourceList(
                sb,
                "Production",
                prod.production,
                missing: null,
                missingOnlyWhenInactive: false,
                isActive: true,
                showPlus: false,   // по твоему формату: "Tools 1", без "+"
                suffix: "");

      

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
                    sb.Append("\n\n<b>Needs for level 2:</b>");

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
                    sb.Append("\n\n<b>Needs for level 3:</b>");

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
                // -------- 3 -> 4 --------

                if (prod.CurrentStage == 3 &&
                    (
                        (prod.addConsumptionLevel4 != null && prod.addConsumptionLevel4.Count > 0) ||
                        (prod.upgradeProductionBonusLevel4 != null && prod.upgradeProductionBonusLevel4.Count > 0)
                    ))
                {
                    sb.Append("\n\n<b>Needs for level 4:</b>");

                    if (prod.addConsumptionLevel4 != null)
                    {
                        foreach (var kvp in prod.addConsumptionLevel4)
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
    private static void AppendResourceList(
        StringBuilder sb,
        string header,
        Dictionary<string, int> dict,
        ICollection<string> missing = null,
        bool missingOnlyWhenInactive = false,
        bool isActive = true,
        bool showPlus = false,
        string suffix = "")
    {
        sb.Append("\n").Append(header).Append(":");

        if (dict == null || dict.Count == 0)
        {
            sb.Append(" No");
            return;
        }

        foreach (var kvp in dict)
        {
            string resName = kvp.Key;
            int amount = kvp.Value;

            bool isMissing = missing != null && missing.Contains(resName);
            if (missingOnlyWhenInactive && isActive) isMissing = false;

            string color = isMissing ? "red" : "white";

            sb.Append("\n<color=")
                .Append(color)
                .Append(">")
                .Append(resName)
                .Append(" ")
                .Append(showPlus ? "+" : "")
                .Append(amount)
                .Append(suffix)
                .Append("</color>");
        }
    }

    private bool IsInEffectSquare(Vector2Int center, Vector2Int pos, int radius)
    {
        return Mathf.Abs(pos.x - center.x) <= radius &&
               Mathf.Abs(pos.y - center.y) <= radius;
    }
}