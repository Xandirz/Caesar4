using UnityEngine.EventSystems;
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
    private Dictionary<string, Button> stageTabs = new();

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
    // --- Группы по категориям ---

    // Main - destroy, road, house
    stages["Main"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Demolish,
        BuildManager.BuildMode.Road,
        BuildManager.BuildMode.House,
    };

    // Service - Well, Market
    stages["Service"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Well,
        BuildManager.BuildMode.Market,
        BuildManager.BuildMode.Warehouse,
    };

    // Resources - все что добывает ресурсы
    stages["Resources"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.LumberMill,
        BuildManager.BuildMode.Rock,
        BuildManager.BuildMode.Clay,
        BuildManager.BuildMode.Coal,
        BuildManager.BuildMode.CopperOre,
    };

    // Food - все что производит еду
    stages["Food"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Berry,
        BuildManager.BuildMode.Fish,
        BuildManager.BuildMode.Hunter,
        BuildManager.BuildMode.Wheat,
        BuildManager.BuildMode.Sheep,
        BuildManager.BuildMode.Beans,
        BuildManager.BuildMode.Dairy,
        BuildManager.BuildMode.Flour,
        BuildManager.BuildMode.Bakery,
        BuildManager.BuildMode.Brewery,
    };

    // Production - все остальное
    stages["Production"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Pottery,
        BuildManager.BuildMode.Tools,
        BuildManager.BuildMode.Crafts,
        BuildManager.BuildMode.Weaver,
        BuildManager.BuildMode.Clothes,
        BuildManager.BuildMode.Furniture,
        BuildManager.BuildMode.Copper,
    };

    // --- Создаем ВСЕ табы ---
    foreach (var kvp in stages)
    {
        CreateTab(kvp.Key, kvp.Value);
    }

    // --- По умолчанию показываем Main ---
    if (stages.TryGetValue("Main", out var mainStage))
    {
        ShowStage(mainStage);

        if (stageTabs.TryGetValue("Main", out var mainTabButton))
        {
            HighlightTab(mainTabButton);
        }
    }
}



    void CreateTab(string name, List<BuildManager.BuildMode> stageBuildings)
    {
        GameObject tabObj = Instantiate(tabButtonPrefab, tabParent);
        TMP_Text txt = tabObj.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = name;

        Button tabButton = tabObj.GetComponent<Button>();
        if (tabButton != null)
        {
            tabButton.onClick.AddListener(() =>
            {
                ShowStage(stageBuildings);
                HighlightTab(tabButton);
            });

            if (!stageTabs.ContainsKey(name))
                stageTabs.Add(name, tabButton);
        }
    }
    public void UnlockStageTab(string stageName)
    {
        if (!stages.ContainsKey(stageName))
        {
            Debug.LogWarning($"Stage '{stageName}' not found in stages dictionary.");
            return;
        }

        // Если таб уже создан – ничего не делаем
        if (stageTabs.ContainsKey(stageName))
            return;

        CreateTab(stageName, stages[stageName]);
        Debug.Log($"Stage tab '{stageName}' unlocked.");
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
                txt.text = name; // больше НЕ пишем стоимость на кнопке

// === Tooltip по стоимости ===
            if (costDict != null && costDict.Count > 0 && !string.IsNullOrEmpty(costText))
            {
                var tooltip = btnObj.AddComponent<BuildButtonTooltip>();
                tooltip.tooltipText = $"{costText}";
            }


            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                BuildManager.BuildMode localMode = po.BuildMode;
                btn.onClick.AddListener(() => buildManager.SetBuildMode(localMode));

                // 👇 проверяем, разблокировано ли здание
                bool isUnlocked = buildManager.IsBuildingUnlocked(localMode);

                // 🔹 Кнопка как объект включена только если здание уже открыто
               // btnObj.SetActive(isUnlocked);

                // на всякий случай: если его активировали — сделать кликабельной
                btn.interactable = isUnlocked;

                // 💾 Сохраняем ссылку в словарь, даже если объект не активен
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
            // включаем сам объект кнопки
            btn.gameObject.SetActive(true);

            // и делаем её кликабельной
            btn.interactable = true;

            Debug.Log($"Кнопка для {mode} активирована!");
        }
        else
        {
            Debug.LogWarning($"Не удалось активировать кнопку для {mode}: не найдена в buildingButtons");
        }
    }

}
