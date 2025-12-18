using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

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

    // Main — базовые действия
    stages["Main"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Demolish,
        BuildManager.BuildMode.Road,
        BuildManager.BuildMode.House,
    };
    
    // Food — базовая еда
    stages["Food1"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Berry,
        BuildManager.BuildMode.Fish,
    };
    
    // Raw — добыча сырья
    stages["Raw"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.LumberMill,
        BuildManager.BuildMode.Rock,
        BuildManager.BuildMode.Clay,
        BuildManager.BuildMode.CopperOre,
    };
    
    // Service — городские сервисы
    stages["Service"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Well,
        BuildManager.BuildMode.Market,
        BuildManager.BuildMode.Warehouse,
        BuildManager.BuildMode.Temple,
    };

    // Farm — земледелие
    stages["Farm"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Wheat,
        BuildManager.BuildMode.Beans,
        BuildManager.BuildMode.Flax,
        BuildManager.BuildMode.Olive,
        BuildManager.BuildMode.Bee,
    };

    // Animals — животноводство
    stages["Animals"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Sheep,
        BuildManager.BuildMode.Goat,
        BuildManager.BuildMode.Pig,
        BuildManager.BuildMode.Cattle,
        BuildManager.BuildMode.Chicken,
    };



    // Process — пищевая переработка
    stages["Food2"] = new List<BuildManager.BuildMode>
    {        
        BuildManager.BuildMode.Hunter,
        BuildManager.BuildMode.Dairy,
        BuildManager.BuildMode.Flour,
        BuildManager.BuildMode.Bakery,
        BuildManager.BuildMode.Brewery,
        BuildManager.BuildMode.OliveOil,
    };

    // Materials — переработка материалов
    stages["Materials"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Charcoal,
        BuildManager.BuildMode.Brick,
        BuildManager.BuildMode.Pottery,
        BuildManager.BuildMode.Copper,
        BuildManager.BuildMode.Leather, // Tannery
    };

    // Craft — ремесло и товары
    stages["Craft"] = new List<BuildManager.BuildMode>
    {
        BuildManager.BuildMode.Tools,
        BuildManager.BuildMode.Crafts,
        BuildManager.BuildMode.Weaver,
        BuildManager.BuildMode.Clothes,
        BuildManager.BuildMode.Furniture,
        BuildManager.BuildMode.Candle,
        BuildManager.BuildMode.Soap,
    };

    // --- Создаем ВСЕ табы ---
    foreach (var kvp in stages)
    {
        CreateTab(kvp.Key, kvp.Value);
    }

    // --- По умолчанию показываем Main ---
    if (stages.TryGetValue("Main", out var mainStage))
    {
        RebuildBuildButtons(mainStage);

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
                // было ShowStage(stageBuildings);
                RebuildBuildButtons(stageBuildings);
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

    // ============================================================
    // РЕНДЕР ПАНЕЛИ СТРОИТЕЛЬСТВА (бывший ShowStage)
    // ============================================================

    void RebuildBuildButtons(List<BuildManager.BuildMode> buildModes)
    {
        ClearBuildButtonPanel();
        buildingButtons.Clear(); // очищаем старые ссылки

        foreach (var mode in buildModes)
        {
            if (mode == BuildManager.BuildMode.Demolish)
            {
                CreatDefaultButtons();
                continue;
            }

            if (!TryGetPrefabByMode(mode, out GameObject prefab))
                continue;

            if (!TryGetPlacedObject(prefab, out PlacedObject po))
                continue;

            var costDict = po.GetCostDict();

            GameObject btnObj = CreateBuildButtonObject();
            Button btn = btnObj.GetComponent<Button>();

            SetupBuildButtonLabel(btnObj, prefab.name);
            SetupBuildButtonTooltip(btnObj, btn, costDict);
            SetupBuildButtonActionAndState(btn, po.BuildMode);
        }
    }

    private void ClearBuildButtonPanel()
    {
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);
    }

    private GameObject CreateBuildButtonObject()
    {
        return Instantiate(buttonPrefab, buttonParent);
    }

    private void SetupBuildButtonLabel(GameObject btnObj, string displayName)
    {
        TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.text = displayName;       // больше НЕ пишем стоимость на кнопке
            txt.raycastTarget = false;    // важно: чтобы hover ловился кнопкой, а не текстом
        }
    }

    private void SetupBuildButtonTooltip(GameObject btnObj, Button btn, Dictionary<string, int> costDict)
    {
        // target для hover — лучше графика кнопки, а не весь объект
        GameObject hoverTarget = (btn != null && btn.targetGraphic != null)
            ? btn.targetGraphic.gameObject
            : btnObj;

        var tooltip = hoverTarget.GetComponent<BuildButtonTooltip>();
        if (tooltip == null)
            tooltip = hoverTarget.AddComponent<BuildButtonTooltip>();

        // передаём ДАННЫЕ, а не готовую строку (tooltip строится при наведении)
        tooltip.costDict = costDict; // если costDict пустой/null — tooltip покажет "Free"
    }

    private void SetupBuildButtonActionAndState(Button btn, BuildManager.BuildMode mode)
    {
        if (btn == null) return;

        BuildManager.BuildMode localMode = mode;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => buildManager.SetBuildMode(localMode));

        // проверяем, разблокировано ли здание
        bool isUnlocked = buildManager.IsBuildingUnlocked(localMode);

        // кнопка кликабельна только если здание открыто
        btn.interactable = isUnlocked;

        // сохраняем ссылку в словарь
        if (!buildingButtons.ContainsKey(localMode))
            buildingButtons.Add(localMode, btn);
    }

    private bool TryGetPlacedObject(GameObject prefab, out PlacedObject po)
    {
        po = prefab != null ? prefab.GetComponent<PlacedObject>() : null;
        return po != null;
    }

    private bool TryGetPrefabByMode(BuildManager.BuildMode mode, out GameObject prefab)
    {
        prefab = buildManager.buildingPrefabs.Find(p =>
        {
            var po = p != null ? p.GetComponent<PlacedObject>() : null;
            return po != null && po.BuildMode == mode;
        });

        return prefab != null;
    }

    // ============================================================
    // СНОС / ДЕФОЛТНЫЕ КНОПКИ
    // ============================================================

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

    // ============================================================
    // СТОИМОСТЬ (если ещё где-то используешь)
    // ============================================================

    string GetCostText(Dictionary<string, int> costDict)
    {
        if (costDict == null || costDict.Count == 0)
            return "Free";

        const string GREEN = "#35C759";
        const string RED = "#FF3B30";

        var sb = new System.Text.StringBuilder(128);

        foreach (var kvp in costDict)
        {
            string resName = kvp.Key;
            if (string.IsNullOrEmpty(resName))
                continue;

            resName = resName.Trim();
            int need = kvp.Value;

            int have = 0;

            if (ResourceManager.Instance != null)
            {
                // 1️⃣ если есть снапшот — берём его
                if (ResourceManager.Instance.resourceBuffer != null &&
                    ResourceManager.Instance.resourceBuffer.TryGetValue(resName, out float bufVal))
                {
                    have = Mathf.FloorToInt(bufVal);
                }
                // 2️⃣ иначе берём реальное значение (то, что видит UI)
                else
                {
                    have = ResourceManager.Instance.GetResource(resName);
                }
            }

            bool enough = have >= need;
            string color = enough ? GREEN : RED;

            sb.AppendLine(
                $"<color={color}>{resName}: {need} (you have {have})</color>"
            );
        }

        return sb.ToString().TrimEnd();
    }

    // ============================================================
    // ВКЛЮЧЕНИЕ КНОПКИ ПОСЛЕ РАЗБЛОКИРОВКИ
    // ============================================================

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
