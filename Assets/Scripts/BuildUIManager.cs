using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildUIManager : MonoBehaviour
{
    public BuildManager buildManager;

    [Header("UI Prefabs")]
    public GameObject buttonPrefab;      // кнопка здания
    public GameObject tabButtonPrefab;   // кнопка вкладки

    [Header("Parents")]
    public Transform buttonParent;       // контейнер для кнопок зданий
    public Transform tabParent;          // контейнер для вкладок

    private Button demolishButton;
    private Button currentTabButton;

    // --- Новое ---
    private Dictionary<string, List<BuildManager.BuildMode>> stages = new();
    private Dictionary<BuildManager.BuildMode, Button> buildingButtons = new(); // хранит кнопки зданий

    public static BuildUIManager Instance { get; private set; }

    public void Awake()
    {
        if (Instance == null) Instance = this;

    }

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
            BuildManager.BuildMode.Fish,
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
            BuildManager.BuildMode.Market,
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

        buildingButtons.Clear(); // очищаем старые ссылки

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

                // 🚫 По умолчанию блокируем кнопку, если здание не разблокировано
                btn.interactable = buildManager.IsUnlocked(localMode);

                // 💾 Сохраняем ссылку
                if (!buildingButtons.ContainsKey(localMode))
                    buildingButtons.Add(localMode, btn);
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
    }

    string GetCostText(Dictionary<string, int> costDict)
    {
        if (costDict == null || costDict.Count == 0) return "Стоимость: 0";

        string text = "";
        foreach (var kvp in costDict)
            text += $"{kvp.Key}:{kvp.Value} ";
        return text.Trim();
    }

    // === Новый метод ===
    public void EnableBuildingButton(BuildManager.BuildMode mode)
    {
        if (buildingButtons.TryGetValue(mode, out var btn))
        {
            btn.interactable = true;

            // ✨ Эффект активации
            var colors = btn.colors;
            colors.normalColor = new Color(0.6f, 1f, 0.6f);
            btn.colors = colors;

            Debug.Log($"Кнопка для {mode} активирована!");
        }
        else
        {
            Debug.LogWarning($"Не удалось активировать кнопку: {mode} (не найдена)");
        }
    }
}
