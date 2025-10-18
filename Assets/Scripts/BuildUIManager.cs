using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildUIManager : MonoBehaviour
{
    public BuildManager buildManager;

    [Header("UI Prefabs")]
    public GameObject buttonPrefab;      // кнопка здания (то что было)
    public GameObject tabButtonPrefab;   // кнопка вкладки

    [Header("Parents")]
    public Transform buttonParent;       // контейнер для кнопок зданий
    public Transform tabParent;          // контейнер для вкладок

    private Button demolishButton;
    private Button currentTabButton;

    private Dictionary<string, List<BuildManager.BuildMode>> stages = new();

    void Start()
    {
        // --- Определяем стадии ---
        stages["Stage I"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Demolish,
            BuildManager.BuildMode.Road,
            BuildManager.BuildMode.House,
            BuildManager.BuildMode.Well,
            BuildManager.BuildMode.Berry,
            BuildManager.BuildMode.LumberMill,
            BuildManager.BuildMode.Rock,
            BuildManager.BuildMode.Clay,
            BuildManager.BuildMode.Pottery,
            BuildManager.BuildMode.Tools,
            BuildManager.BuildMode.Hunter,
            BuildManager.BuildMode.Warehouse,
        };

        stages["Stage II"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Crafts,
            BuildManager.BuildMode.Wheat,
            BuildManager.BuildMode.Flour,
            BuildManager.BuildMode.Bakery,
            BuildManager.BuildMode.Sheep,
            BuildManager.BuildMode.Dairy,
            BuildManager.BuildMode.Weaver,
            BuildManager.BuildMode.Clothes,
            BuildManager.BuildMode.Furniture,
            BuildManager.BuildMode.Beans,
            BuildManager.BuildMode.Brewery,
            BuildManager.BuildMode.Coal,
            BuildManager.BuildMode.CopperOre,
            BuildManager.BuildMode.Copper,
        }; 

        // --- Создаем вкладки ---
        foreach (var kvp in stages)
        {
            CreateTab(kvp.Key, kvp.Value);
        }

        // --- Сразу загружаем первую вкладку ---
        if (stages.ContainsKey("Stage I"))
        {
            ShowStage(stages["Stage I"]);
        }
    }

    void CreateTab(string name, List<BuildManager.BuildMode> stageBuildings)
    {
        GameObject tabObj = Instantiate(tabButtonPrefab, tabParent);
        TMP_Text txt = tabObj.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = name;

        Button tabButton = tabObj.GetComponent<Button>();
        tabButton.onClick.AddListener(() =>
        {
            ShowStage(stageBuildings);
            HighlightTab(tabButton);
        });
    }

    void HighlightTab(Button tabButton)
    {
        if (currentTabButton != null)
            currentTabButton.interactable = true; // вернуть активность прошлой

        currentTabButton = tabButton;
        currentTabButton.interactable = false; // подсветка текущей
    }

    void ShowStage(List<BuildManager.BuildMode> stageBuildings)
    {
        // очищаем панель
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        foreach (var mode in stageBuildings)
        {
            if (mode == BuildManager.BuildMode.Demolish)
            {
                CreatDefaultButtons();
                continue;
            }

            // ищем префаб по BuildMode
            GameObject prefab = buildManager.buildingPrefabs.Find(p =>
            {
                var po = p?.GetComponent<PlacedObject>();
                return po != null && po.BuildMode == mode;
            });

            if (prefab == null) continue;

            PlacedObject po = prefab.GetComponent<PlacedObject>();
            if (po == null) continue;

            var costDict = po.GetCostDict();
            string costText = GetCostText(costDict);
            string name = prefab.name;

            // Создаём кнопку
            GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{name}\n{costText}";

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                BuildManager.BuildMode localMode = po.BuildMode;
                btn.onClick.AddListener(() => buildManager.SetBuildMode(localMode));
            }
        }
    }

    void CreatDefaultButtons()
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
        TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = "Снос";

        demolishButton = btnObj.GetComponent<Button>();
        demolishButton.onClick.AddListener(() =>
        {
            buildManager.SetBuildMode(BuildManager.BuildMode.Demolish);
            Debug.Log("Режим сноса активирован");
        });
        
        GameObject upgradeObj = Instantiate(buttonPrefab, buttonParent);
        TMP_Text upgradeTxt = upgradeObj.GetComponentInChildren<TMP_Text>();
        if (upgradeTxt != null) upgradeTxt.text = "Улучшить";

        Button upgradeButton = upgradeObj.GetComponent<Button>();
        upgradeButton.onClick.AddListener(() =>
        {
            buildManager.SetBuildMode(BuildManager.BuildMode.Upgrade);
            Debug.Log("Режим улучшения активирован");
        });
        
    }

    string GetCostText(Dictionary<string, int> costDict)
    {
        if (costDict == null || costDict.Count == 0) return "Стоимость: 0";

        string text = "";
        foreach (var kvp in costDict)
            text += $"{kvp.Key}:{kvp.Value} ";
        return text.Trim();
    }
}
