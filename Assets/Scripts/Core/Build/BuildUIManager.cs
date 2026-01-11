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

    // tabs
    private readonly Dictionary<string, Button> stageTabs = new();
    private readonly Dictionary<string, GameObject> tabObjects = new(); // GO таба

    // stageName -> (mode -> buttonGO)
    private readonly Dictionary<string, Dictionary<BuildManager.BuildMode, GameObject>> tabButtons = new();

    // mode -> Button (для interactable/состояния)
    private readonly Dictionary<BuildManager.BuildMode, Button> buildingButtons = new();

    // stageName -> list modes
    private readonly Dictionary<string, List<BuildManager.BuildMode>> stages = new();

    // mode -> stageName (для быстрого show/reload текущего таба)
    private readonly Dictionary<BuildManager.BuildMode, string> modeToStage = new();

    private Button currentTabButton;
    private string currentStageName = null;

    
    [Header("Unlock Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

    private readonly HashSet<BuildManager.BuildMode> pendingUnlockHighlight = new();

    private readonly Dictionary<Button, Color> defaultTabColors = new();
    private readonly Dictionary<Button, Color> defaultBuildBtnColors = new();

    public static BuildUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
    }

    private void Start()
    {
        BuildStages();

        // 1) Создаём все табы (по умолчанию скрыты)
        foreach (var kvp in stages)
            CreateTab(kvp.Key);

        // 2) Создаём ВСЕ кнопки (по умолчанию скрыты)
        CreateAllButtonsHidden();

        // 3) Применяем локи/анлоки и показываем табы, где есть хотя бы одна доступная кнопка
        RefreshAllLocksAndTabs();

        // 4) Выбираем таб по умолчанию
        AutoSelectDefaultTab();
        

    }

    private void BuildStages()
    {
        stages.Clear();
        modeToStage.Clear();

        stages["Main"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Demolish,
            BuildManager.BuildMode.Road,
            BuildManager.BuildMode.House,
        };

        stages["Food"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Berry,
            BuildManager.BuildMode.Fish,
            BuildManager.BuildMode.Hunter,
            BuildManager.BuildMode.Flour,
            BuildManager.BuildMode.Bakery,
            BuildManager.BuildMode.Dairy,
            BuildManager.BuildMode.Brewery,
            BuildManager.BuildMode.OliveOil,
            BuildManager.BuildMode.Wine,
        };

        stages["Raw"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.LumberMill,
            BuildManager.BuildMode.Rock,
            BuildManager.BuildMode.Clay,
            BuildManager.BuildMode.CopperOre,
            BuildManager.BuildMode.TinOre,
            BuildManager.BuildMode.Sand,
            BuildManager.BuildMode.GoldOre,
        };

        stages["Craft"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Pottery,
            BuildManager.BuildMode.Tools,
            BuildManager.BuildMode.Crafts,
            BuildManager.BuildMode.Weaver,
            BuildManager.BuildMode.Clothes,
            BuildManager.BuildMode.Furniture,
            BuildManager.BuildMode.Candle,
            BuildManager.BuildMode.Soap,
            BuildManager.BuildMode.Smithy,
            BuildManager.BuildMode.Ash,
            BuildManager.BuildMode.Glass,
            BuildManager.BuildMode.Salt,
            BuildManager.BuildMode.Jewelry,
        };

        stages["Service"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Well,
            BuildManager.BuildMode.Market,
            BuildManager.BuildMode.Temple,
            BuildManager.BuildMode.Bathhouse,
            BuildManager.BuildMode.Doctor,
        };

        stages["Farm"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Wheat,
            BuildManager.BuildMode.Beans,
            BuildManager.BuildMode.Flax,
            BuildManager.BuildMode.Olive,
            BuildManager.BuildMode.Bee,
            BuildManager.BuildMode.Grape,
            BuildManager.BuildMode.Herbs,
            BuildManager.BuildMode.Fruit,
            BuildManager.BuildMode.Vegetables,
        };

        stages["Animals"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Sheep,
            BuildManager.BuildMode.Goat,
            BuildManager.BuildMode.Pig,
            BuildManager.BuildMode.Cattle,
            BuildManager.BuildMode.Chicken,
        };

        stages["Materials"] = new List<BuildManager.BuildMode>
        {
            BuildManager.BuildMode.Charcoal,
            BuildManager.BuildMode.Brick,
            BuildManager.BuildMode.Leather,
            BuildManager.BuildMode.Copper,
            BuildManager.BuildMode.Bronze,
            BuildManager.BuildMode.Gold,
        };

        // mode -> stageName
        foreach (var st in stages)
        {
            foreach (var mode in st.Value)
            {
                if (!modeToStage.ContainsKey(mode))
                    modeToStage.Add(mode, st.Key);
            }
        }
    }

    // ---------------- Tabs ----------------

    private void CreateTab(string stageName)
    {
        GameObject tabObj = Instantiate(tabButtonPrefab, tabParent);
        tabObjects[stageName] = tabObj;

        TMP_Text txt = tabObj.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = stageName;

        Button tabButton = tabObj.GetComponent<Button>();
        if (tabButton != null)
        {
            tabButton.onClick.RemoveAllListeners();
            tabButton.onClick.AddListener(() =>
            {
                ShowTab(stageName);
                HighlightTab(tabButton);
            });

            stageTabs[stageName] = tabButton;
            GetDefaultColor(defaultTabColors, tabButton);

        }

        // по умолчанию скрыто, пока не найдём unlocked-кнопки
        tabObj.SetActive(false);

        if (!tabButtons.ContainsKey(stageName))
            tabButtons[stageName] = new Dictionary<BuildManager.BuildMode, GameObject>();
    }

    private void HighlightTab(Button tabButton)
    {
        if (currentTabButton != null)
            currentTabButton.interactable = true;

        currentTabButton = tabButton;
        if (currentTabButton != null)
            currentTabButton.interactable = false;
    }

    // ---------------- Buttons creation (one-time) ----------------

    private void CreateAllButtonsHidden()
    {
        // подчистим старые (на всякий случай)
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        buildingButtons.Clear();

        foreach (var st in stages)
        {
            string stageName = st.Key;
            foreach (var mode in st.Value)
            {
                GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
                btnObj.SetActive(false); // ВСЕ скрыты до Refresh/ShowTab

                Button btn = btnObj.GetComponent<Button>();
                if (btn == null)
                {
                    Debug.LogWarning($"[BuildUI] Button component missing on buttonPrefab!");
                    Destroy(btnObj);
                    continue;
                }

                // Демолиш — особый режим без префаба
                if (mode == BuildManager.BuildMode.Demolish)
                {
                    SetupBuildButtonLabel(btnObj, "Demolish");
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => buildManager.SetBuildMode(BuildManager.BuildMode.Demolish));

                    // считаем “всегда доступен”
                    btn.interactable = true;

                    tabButtons[stageName][mode] = btnObj;
                    buildingButtons[mode] = btn;
                    continue;
                }

                // Обычные здания: нужны prefab + PlacedObject
                if (!TryGetPrefabByMode(mode, out GameObject prefab))
                {
                    Debug.LogWarning($"[BuildUI] Prefab not found for mode={mode}. " +
                                     $"Проверь buildManager.buildingPrefabs и PlacedObject.BuildMode на префабе.");
                    Destroy(btnObj);
                    continue;
                }

                if (!TryGetPlacedObject(prefab, out PlacedObject po))
                {
                    Debug.LogWarning($"[BuildUI] PlacedObject missing on prefab '{prefab.name}' for mode={mode}.");
                    Destroy(btnObj);
                    continue;
                }

                // Label
                SetupBuildButtonLabel(btnObj, prefab.name);

                // Tooltip
                var costDict = po.GetCostDict();
                SetupBuildButtonTooltip(btnObj, btn, costDict, po);

                // Action + initial interactable
                var modeLocal = po.BuildMode;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    buildManager.SetBuildMode(modeLocal);
                    ClearUnlockHighlight(modeLocal); // сброс подсветки после первого нажатия
                });

                GetDefaultColor(defaultBuildBtnColors, btn);


                // кэш
                tabButtons[stageName][mode] = btnObj;
                buildingButtons[mode] = btn;
            }
        }
    }

    private void SetupBuildButtonAction(Button btn, BuildManager.BuildMode mode)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => buildManager.SetBuildMode(mode));
    }

    private void SetupBuildButtonLabel(GameObject btnObj, string displayName)
    {
        TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.text = displayName;
            txt.raycastTarget = false;
        }
    }

    private void SetupBuildButtonTooltip(
        GameObject btnObj,
        Button btn,
        Dictionary<string, int> costDict,
        PlacedObject po)
    {
        GameObject hoverTarget = (btn != null && btn.targetGraphic != null)
            ? btn.targetGraphic.gameObject
            : btnObj;

        var tooltip = hoverTarget.GetComponent<BuildButtonTooltip>();
        if (tooltip == null)
            tooltip = hoverTarget.AddComponent<BuildButtonTooltip>();

        tooltip.costDict = costDict;
        tooltip.needWaterNearby = po.needWaterNearby;
        tooltip.requiresRoadAccess = po.RequiresRoadAccess;
        tooltip.needHouseNearby = po.NeedHouseNearby;
        tooltip.needMountainsNearby = po.needMountainsNearby;
    }

    private bool TryGetPlacedObject(GameObject prefab, out PlacedObject po)
    {
        po = prefab != null ? prefab.GetComponent<PlacedObject>() : null;
        return po != null;
    }

    private bool TryGetPrefabByMode(BuildManager.BuildMode mode, out GameObject prefab)
    {
        prefab = BuildManager.Instance.GetPrefabByMode(mode);
        return prefab != null;
    }


    // ---------------- Visibility / Locks ----------------

    public void RefreshAllLocksAndTabs()
    {
        // 1) кнопки: interactable по unlock
        foreach (var kv in buildingButtons)
        {
            var mode = kv.Key;
            var btn = kv.Value;

            bool unlocked = (mode == BuildManager.BuildMode.Demolish) || buildManager.IsBuildingUnlocked(mode);
            btn.interactable = unlocked;
        }

        // 2) табы: активен, если в табе есть хотя бы одна unlocked-кнопка
        foreach (var st in stages)
        {
            string stageName = st.Key;
            bool anyUnlocked = false;

            foreach (var mode in st.Value)
            {
                bool unlocked = (mode == BuildManager.BuildMode.Demolish) || buildManager.IsBuildingUnlocked(mode);
                if (unlocked)
                {
                    anyUnlocked = true;
                    break;
                }
            }

            if (tabObjects.TryGetValue(stageName, out var tabGO))
                tabGO.SetActive(anyUnlocked);
        }

        // 3) если текущий таб исчез — выбрать другой
        if (currentStageName != null && tabObjects.TryGetValue(currentStageName, out var curTabGO) && !curTabGO.activeSelf)
            AutoSelectDefaultTab();
        else if (currentStageName != null)
            ShowTab(currentStageName);
    }

    private void ShowTab(string stageName)
    {
        currentStageName = stageName;

        // скрыть все кнопки
        foreach (var tab in tabButtons)
            foreach (var kv in tab.Value)
                if (kv.Value != null) kv.Value.SetActive(false);

        // показать кнопки выбранного таба только если unlocked
        if (!tabButtons.TryGetValue(stageName, out var dict))
            return;

        foreach (var kv in dict)
        {
            var mode = kv.Key;
            var go = kv.Value;
            if (go == null) continue;

            bool unlocked = (mode == BuildManager.BuildMode.Demolish) || buildManager.IsBuildingUnlocked(mode);
            go.SetActive(unlocked);
        }
    }

    private void AutoSelectDefaultTab()
    {
        // Prefer Main если видим
        if (tabObjects.TryGetValue("Main", out var mainGO) && mainGO.activeSelf)
        {
            ShowTab("Main");
            if (stageTabs.TryGetValue("Main", out var mainBtn))
                HighlightTab(mainBtn);
            return;
        }

        // иначе первый видимый
        foreach (var st in stages.Keys)
        {
            if (tabObjects.TryGetValue(st, out var tabGO) && tabGO.activeSelf)
            {
                ShowTab(st);
                if (stageTabs.TryGetValue(st, out var btn))
                    HighlightTab(btn);
                return;
            }
        }

        // если ничего не открыто — всё остаётся скрытым
        currentStageName = null;
        currentTabButton = null;
    }

    // Вызывай это после анлока здания (из ResearchManager/BuildManager)
public void EnableBuildingButton(BuildManager.BuildMode mode)
{
    RefreshAllLocksAndTabs();

    if (currentStageName != null && modeToStage.TryGetValue(mode, out var st) && st == currentStageName)
        ShowTab(currentStageName);

    ApplyUnlockHighlight(mode);
}

    
    
    private static Graphic GetGraphic(Button b) => b != null ? b.targetGraphic : null;

    private Color GetDefaultColor(Dictionary<Button, Color> cache, Button b)
    {
        if (b == null) return Color.white;
        if (cache.TryGetValue(b, out var c)) return c;

        var g = GetGraphic(b);
        var def = g != null ? g.color : Color.white;
        cache[b] = def;
        return def;
    }

    private void SetButtonColor(Button b, Color c)
    {
        var g = GetGraphic(b);
        if (g != null) g.color = c;
    }

    private void ApplyUnlockHighlight(BuildManager.BuildMode mode)
    {
        if (!modeToStage.TryGetValue(mode, out var stageName)) return;

        // подсветить таб
        if (stageTabs.TryGetValue(stageName, out var tabBtn))
        {
            GetDefaultColor(defaultTabColors, tabBtn);
            SetButtonColor(tabBtn, highlightColor);
        }

        // подсветить кнопку здания (если уже создана)
        if (buildingButtons.TryGetValue(mode, out var buildBtn))
        {
            GetDefaultColor(defaultBuildBtnColors, buildBtn);
            SetButtonColor(buildBtn, highlightColor);
        }

        pendingUnlockHighlight.Add(mode);
    }

    private void ClearUnlockHighlight(BuildManager.BuildMode mode)
    {
        if (!pendingUnlockHighlight.Contains(mode)) return;

        if (modeToStage.TryGetValue(mode, out var stageName) &&
            stageTabs.TryGetValue(stageName, out var tabBtn))
        {
            SetButtonColor(tabBtn, GetDefaultColor(defaultTabColors, tabBtn));
        }

        if (buildingButtons.TryGetValue(mode, out var buildBtn))
        {
            SetButtonColor(buildBtn, GetDefaultColor(defaultBuildBtnColors, buildBtn));
        }

        pendingUnlockHighlight.Remove(mode);
    }

}
