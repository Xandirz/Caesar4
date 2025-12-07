using System;
using System.Collections.Generic;
using UnityEngine;

public class ResearchManager : MonoBehaviour
{
    [Serializable]
    public class ResearchDef
    {
        public string id;                 // "Clay", "Pottery"
        public string displayName;        // "Глина", "Гончарное дело"
        public Sprite icon;               // иконка
        public Vector2 gridPosition;      // позиция в "ячейках" (0,0), (1,0) и т.п.
        public string[] prerequisites;    // id исследований, которые должны быть завершены
    }

    public static ResearchManager Instance;

    [Header("Unknown / Fog of war")]
    [SerializeField] private Sprite unknownIcon; // иконка с вопросом

    [Header("Иконки исследований")]
    [SerializeField] private Sprite clayIcon;
    [SerializeField] private Sprite berryIcon;
    [SerializeField] private Sprite lumberIcon;
    [SerializeField] private Sprite potteryIcon;
    [SerializeField] private Sprite toolsIcon;
    [SerializeField] private Sprite hunterIcon;
    [SerializeField] private Sprite craftsIcon;
    [SerializeField] private Sprite warehouseIcon;
    [SerializeField] private Sprite stage2Icon;
    [SerializeField] private Sprite stage3Icon;
    [SerializeField] private Sprite berry2Icon;
    [SerializeField] private Sprite lumber2Icon;
    [SerializeField] private Sprite hunter2Icon;

    [SerializeField] private Sprite wheatIcon;
    [SerializeField] private Sprite flourIcon;
    [SerializeField] private Sprite bakeryIcon;

    [SerializeField] private Sprite sheepIcon;
    [SerializeField] private Sprite dairyIcon;
    [SerializeField] private Sprite weaverIcon;
    [SerializeField] private Sprite clothesIcon;
    [SerializeField] private Sprite marketIcon;
    [SerializeField] private Sprite furnitureIcon;

    [SerializeField] private Sprite breweryIcon;
    [SerializeField] private Sprite coalIcon;
    [SerializeField] private Sprite beansIcon;

    [Header("Prefabs / UI")]
    [SerializeField] private ResearchNode nodePrefab;
    [SerializeField] private ResearchLine linePrefab;
    [SerializeField] private RectTransform nodesRoot;  // контейнер в Canvas
    [SerializeField] private RectTransform linesRoot;  // контейнер в Canvas
    [SerializeField] private float cellSize = 200f;    // расстояние между нодами по сетке

    private ResearchDef[] definitions;

    // Отключить требования исследований (для теста)
    [SerializeField] private bool disableResearchRequirements = false;

    // mood (0..100)
    private int lastKnownMood = 0;

    // Снапшоты производства на момент "открытия" ноды
    private readonly Dictionary<string, Dictionary<string, int>> producedAtReveal =
        new Dictionary<string, Dictionary<string, int>>();

    // Кумулятивное произведённое количество ресурсов
    private readonly Dictionary<string, int> producedTotals =
        new Dictionary<string, int>();

    // Все созданные ноды
    private readonly Dictionary<string, ResearchNode> nodes =
        new Dictionary<string, ResearchNode>();

    // Какие здания открывает каждое исследование
    private readonly Dictionary<string, List<BuildManager.BuildMode>> researchUnlocks =
        new Dictionary<string, List<BuildManager.BuildMode>>
        {
            // базовая линия
            { "Clay",      new List<BuildManager.BuildMode> { BuildManager.BuildMode.Clay      } },
            { "Pottery",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Pottery   } },
            { "Tools",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Tools     } },
            { "Hunter",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Hunter    } },
            { "Crafts",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Crafts    } },

            // зерновая ветка
            { "Wheat",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Wheat     } },
            { "Flour",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Flour     } },
            { "Bakery",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Bakery    } },

            // овцы и одежда
            { "Sheep",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Sheep     } },
            { "Dairy",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Dairy     } },
            { "Weaver",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Weaver    } },
            { "Clothes",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Clothes   } },
            { "Market",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Market    } },
            { "Furniture", new List<BuildManager.BuildMode> { BuildManager.BuildMode.Furniture } },

            // отдельные веточки
            { "Beans",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Beans     } },
            { "Brewery",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Brewery   } },
            { "Coal",      new List<BuildManager.BuildMode> { BuildManager.BuildMode.Coal      } },

            // склад
            { "Warehouse", new List<BuildManager.BuildMode> { BuildManager.BuildMode.Warehouse } },
        };

    // ------------------------------------------------------------------
    // КОНСТАНТЫ ТРЕБОВАНИЙ (ОБЩИЕ ДЛЯ ЛОГИКИ И ТУЛТИПОВ)
    // ------------------------------------------------------------------

    private const int MoodRequiredAll = 81; // >80

    private const int Clay_HousesRequired        = 10;
    private const int Pottery_HousesRequired     = 15;
    private const int Tools_HousesRequired       = 20;
    private const int Hunter_HousesRequired      = 25;
    private const int Stage2_HousesRequired      = 30;
    private const int Crafts_HousesLvl2Required  = 10;
    private const int Stage3_HousesLvl2Required  = 30;
    private const int Furniture_HousesLvl2Required = 20;
    private const int Beans_HousesLvl2Required   = 25;

    private const int Pottery_ClayRequired       = 50;  // было 500
    private const int Tools_WoodRequired         = 50;
    private const int Tools_RockRequired         = 50;
    private const int Hunter_ToolsRequired       = 50;
    private const int Warehouse_PotteryRequired  = 100;
    private const int Stage2_MeatRequired        = 50;  // было 500
    private const int Stage2_ToolsRequired       = 50;  // было 500
    private const int Crafts_BoneRequired        = 50;

    private const int Stage3_ClothesRequired     = 50;
    private const int Stage3_BeerRequired        = 50;
    private const int Stage3_FurnitureRequired   = 50;
    private const int Stage3_MilkRequired        = 50;

    private const int BerryHut2_BerryRequired    = 100;
    private const int BerryHut2_ToolsRequired    = 100;

    private const int Lumber2_WoodRequired       = 100;  // было 500
    private const int Lumber2_ToolsRequired      = 100;

    private const int Hunter2_MeatRequired       = 100;
    private const int Hunter2_ToolsRequired      = 100;

    private const int Furniture_CraftsRequired   = 50;

    private const int Wheat_BerryRequired        = 100;
    private const int Flour_WheatRequired        = 50;
    private const int Bakery_FlourRequired       = 50;
    private const int Sheep_WheatRequired        = 50;
    private const int Dairy_MilkRequired         = 50;
    private const int Weaver_WoolRequired        = 50;
    private const int Clothes_ClothRequired      = 50;
    private const int Market_ClothesRequired     = 50;
    private const int Brewery_WheatRequired      = 50;
    private const int Coal_WoodRequired          = 50;

    // ------------------------------------------------------------------
    // ЖИЗНЕННЫЙ ЦИКЛ
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        BuildDefinitions();
        BuildTree();
        RefreshAvailability();
        RefreshFogOfWar();
    }

    // ------------------------------------------------------------------
    // ПУБЛИЧНЫЕ МЕТОДЫ
    // ------------------------------------------------------------------

    /// <summary>
    /// Вызывать из дневного тика. Обновляет настроение и пересчитывает доступность исследований.
    /// </summary>
    public void OnDayPassed(int moodPercent)
    {
        lastKnownMood = Mathf.Clamp(moodPercent, 0, 100);
        RefreshAvailability();
    }

    /// <summary>
    /// Вызывать в момент производства ресурса (после AddResource).
    /// kvpKey - id ресурса ("Clay"), kvpValue - сколько произведено (добавляется к сумме).
    /// </summary>
    public void ReportProduced(string kvpKey, int kvpValue)
    {
        if (string.IsNullOrEmpty(kvpKey) || kvpValue <= 0) return;

        if (producedTotals.TryGetValue(kvpKey, out var cur))
            producedTotals[kvpKey] = cur + kvpValue;
        else
            producedTotals[kvpKey] = kvpValue;

        RefreshAvailability();
    }

    public bool IsResearchCompleted(string id)
    {
        if (nodes == null) return false;
        return nodes.TryGetValue(id, out var node) && node.IsCompleted;
    }

    // ------------------------------------------------------------------
    // ОПИСАНИЕ ДЕРЕВА
    // ------------------------------------------------------------------

  private void BuildDefinitions()
{
    definitions = new ResearchDef[]
    {
        // ===== ГЛАВНАЯ ЛИНИЯ ПО НИЖНЕМУ РЯДУ: Clay → Pottery → Tools → Hunter → Stage2 =====
        new ResearchDef
        {
            id = "Clay",
            displayName = "Глина",
            icon = clayIcon,
            gridPosition = new Vector2(0, 0),
            prerequisites = Array.Empty<string>()
        },
        new ResearchDef
        {
            id = "Pottery",
            displayName = "Гончарное дело",
            icon = potteryIcon,
            gridPosition = new Vector2(1, 0),
            prerequisites = new [] { "Clay" }
        },
        new ResearchDef
        {
            id = "Tools",
            displayName = "Инструменты",
            icon = toolsIcon,
            gridPosition = new Vector2(2, 0),
            prerequisites = new [] { "Pottery" }
        },
        new ResearchDef
        {
            id = "Hunter",
            displayName = "Охота",
            icon = hunterIcon,
            gridPosition = new Vector2(3, 0),
            prerequisites = new [] { "Tools" }
        },
        new ResearchDef
        {
            id = "Stage2",
            displayName = "Вторая стадия",
            icon = stage2Icon,
            gridPosition = new Vector2(4, 0),
            prerequisites = new [] { "Hunter" }
        },

        // ===== ВЕТКА ВВЕРХ ОТ STAGE2: Brewery → Wheat → Flour → Bakery =====
        new ResearchDef
        {
            id = "Brewery",
            displayName = "Пивоварня",
            icon = breweryIcon,
            gridPosition = new Vector2(3, 1),
            prerequisites = new [] { "Wheat" }     // wheat  → brewery
        },
        new ResearchDef
        {
            id = "Wheat",
            displayName = "Пшеница",
            icon = wheatIcon,
            gridPosition = new Vector2(4, 1),
            prerequisites = new [] { "Stage2"}     // stage2 → wheat
        },
        new ResearchDef
        {
            id = "Flour",
            displayName = "Мука",
            icon = flourIcon,
            gridPosition = new Vector2(5, 1),
            prerequisites = new [] { "Wheat" }
        },
        new ResearchDef
        {
            id = "Bakery",
            displayName = "Пекарня",
            icon = bakeryIcon,
            gridPosition = new Vector2(6, 1),
            prerequisites = new [] { "Flour" }
        },

        // ===== ВЕРТИКАЛЬ ОТ WHEAT: Wheat → Sheep → Weaver → Clothes → Market → Stage3 =====
        new ResearchDef
        {
            id = "Sheep",
            displayName = "Овцы",
            icon = sheepIcon,
            gridPosition = new Vector2(4, 2),
            prerequisites = new [] { "Wheat" }      // wheat → sheep
        },
        // Sheep → Dairy (ветка вправо)
        new ResearchDef
        {
            id = "Dairy",
            displayName = "Молочная",
            icon = dairyIcon,
            gridPosition = new Vector2(5, 2),
            prerequisites = new [] { "Sheep" }      // sheep → dairy
        },
        new ResearchDef
        {
            id = "Weaver",
            displayName = "Ткачество",
            icon = weaverIcon,
            gridPosition = new Vector2(4, 3),
            prerequisites = new [] { "Sheep" }      // sheep → weaver
        },
        new ResearchDef
        {
            id = "Clothes",
            displayName = "Одежда",
            icon = clothesIcon,
            gridPosition = new Vector2(4, 4),
            prerequisites = new [] { "Weaver" }     // weaver → clothes
        },
        new ResearchDef
        {
            id = "Market",
            displayName = "Рынок",
            icon = marketIcon,
            gridPosition = new Vector2(4, 5),
            prerequisites = new [] { "Clothes" }    // clothes → market
        },
        new ResearchDef
        {
            id = "Stage3",
            displayName = "Третья стадия",
            icon = stage3Icon,
            gridPosition = new Vector2(5, 5),
            prerequisites = new [] { "Market" }     // market → stage3
        },

        // ===== БОКОВЫЕ ВЕТКИ ОТ TOOLS / HUNTER / STAGE2 =====

        // Pottery → Warehouse (вниз)
        new ResearchDef
        {
            id = "Warehouse",
            displayName = "Склад",
            icon = warehouseIcon,
            gridPosition = new Vector2(1, -1),
            prerequisites = new [] { "Pottery" }
        },

        // Tools → Berry2 (вверх)
        new ResearchDef
        {
            id = "BerryHut2",
            displayName = "Ягодник II",
            icon = berry2Icon,
            gridPosition = new Vector2(2, 1),
            prerequisites = new [] { "Tools" }
        },

        // Tools → Lumber2 (вниз)
        new ResearchDef
        {
            id = "LumberMill2",
            displayName = "Лесопилка II",
            icon = lumber2Icon,
            gridPosition = new Vector2(2, -1),
            prerequisites = new [] { "Tools" }
        },

        // Tools → Coal (ещё одна боковая ветка вниз)
        new ResearchDef
        {
            id = "Coal",
            displayName = "Уголь",
            icon = coalIcon,
            gridPosition = new Vector2(2, -2),
            prerequisites = new [] { "LumberMill2" }
        },

        // Hunter → Hunter2 (вниз)
        new ResearchDef
        {
            id = "Hunter2",
            displayName = "Охотник II",
            icon = hunter2Icon,
            gridPosition = new Vector2(3, -1),
            prerequisites = new [] { "Hunter" }
        },

        // Stage2 → Beans (вправо по тому же ряду)
        new ResearchDef
        {
            id = "Beans",
            displayName = "Бобы",
            icon = beansIcon,
            gridPosition = new Vector2(5, 0),
            prerequisites = new [] { "Stage2" }
        },

        // Stage2 → Crafts → Furniture (вниз отдельная линия)
        new ResearchDef
        {
            id = "Crafts",
            displayName = "Ремесло",
            icon = craftsIcon,
            gridPosition = new Vector2(4, -1),
            prerequisites = new [] { "Stage2" }
        },
        new ResearchDef
        {
            id = "Furniture",
            displayName = "Мебель",
            icon = furnitureIcon,
            gridPosition = new Vector2(5, -1),
            prerequisites = new [] { "Crafts" }
        },
    };
}


    // ------------------------------------------------------------------
    // СНАПШОТЫ ПРОИЗВОДСТВА
    // ------------------------------------------------------------------

    /// <summary>
    /// Если для этого исследования ещё нет снапшота, создаём его.
    /// Вызываем, когда нода стала "видимой" (пререквизиты выполнены).
    /// </summary>
    private void EnsureRevealSnapshot(string researchId)
    {
        if (producedAtReveal.ContainsKey(researchId))
            return;

        var snap = new Dictionary<string, int>();
        foreach (var kvp in producedTotals)
            snap[kvp.Key] = kvp.Value;

        producedAtReveal[researchId] = snap;
    }

    /// <summary>
    /// Сколько ресурса resourceId произведено С МОМЕНТА ОТКРЫТИЯ этого исследования.
    /// </summary>
    private int GetProducedSinceReveal(string researchId, string resourceId)
    {
        int total = producedTotals.TryGetValue(resourceId, out var t) ? t : 0;

        if (!producedAtReveal.TryGetValue(researchId, out var snap))
            return total; // снапшот не снимали — значит считаем с начала игры

        int atReveal = snap.TryGetValue(resourceId, out var v) ? v : 0;
        int diff = total - atReveal;
        return diff < 0 ? 0 : diff;
    }

    // ------------------------------------------------------------------
    // СОЗДАНИЕ НОД И ЛИНИЙ
    // ------------------------------------------------------------------

    private void BuildTree()
    {
        if (definitions == null || nodePrefab == null || nodesRoot == null)
        {
            Debug.LogError("ResearchManager: не настроены definitions / nodePrefab / nodesRoot");
            return;
        }

        nodes.Clear();

        // 1) Создаём ноды
        foreach (var def in definitions)
        {
            var nodeGO = Instantiate(nodePrefab, nodesRoot);
            nodeGO.name = $"Node_{def.id}";

            var rt = (RectTransform)nodeGO.transform;
            rt.anchoredPosition = new Vector2(
                def.gridPosition.x * cellSize,
                -def.gridPosition.y * cellSize
            );

            nodeGO.Init(def.id, def.displayName, def.icon, OnNodeClicked);
            nodes[def.id] = nodeGO;
        }

        // 2) Линии — между нодами
        if (linePrefab != null)
        {
            Transform parentForLines = linesRoot != null ? (Transform)linesRoot : nodesRoot;

            foreach (var def in definitions)
            {
                if (def.prerequisites == null) continue;

                foreach (var preId in def.prerequisites)
                {
                    if (string.IsNullOrEmpty(preId)) continue;
                    if (!nodes.TryGetValue(preId, out var fromNode)) continue;
                    if (!nodes.TryGetValue(def.id, out var toNode)) continue;

                    var line = Instantiate(linePrefab, parentForLines);
                    line.name = $"Line_{preId}_to_{def.id}";
                    line.Connect((RectTransform)fromNode.transform, (RectTransform)toNode.transform);
                }
            }
        }
    }

    public void RefreshLineThickness(float zoom)
    {
        if (linesRoot == null) return;

        float thicknessBase = 4f;
        float thickness = thicknessBase / Mathf.Max(zoom, 0.0001f);

        foreach (var line in linesRoot.GetComponentsInChildren<ResearchLine>())
        {
            var rt = line.RectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, thickness);
        }
    }

    // ------------------------------------------------------------------
    // КЛИК ПО НОДЕ
    // ------------------------------------------------------------------

    private void OnNodeClicked(ResearchNode node)
    {
        if (!node.IsAvailable || node.IsCompleted)
            return;

        // проверяем условия ещё раз (на всякий случай)
        if (!AreGameConditionsMet(node.Id))
            return;

        CompleteResearch(node.Id);
    }

    private void CompleteResearch(string id)
    {
        if (!nodes.TryGetValue(id, out var node)) return;

        node.SetState(available: false, completed: true);
        Debug.Log($"Research completed: {id}");

        UnlockBuildingsForResearch(id);
        ApplySpecialEffects(id);

        RefreshAvailability();
        RefreshFogOfWar();
    }

    private void UnlockBuildingsForResearch(string researchId)
    {
        if (BuildManager.Instance == null) return;
        if (!researchUnlocks.TryGetValue(researchId, out var list)) return;

        foreach (var mode in list)
            BuildManager.Instance.UnlockBuilding(mode);
    }

    private void ApplySpecialEffects(string researchId)
    {
        switch (researchId)
        {
            case "Stage2":
                if (BuildUIManager.Instance != null)
                    BuildUIManager.Instance.UnlockStageTab("Stage II");
                break;

            case "Stage3":
                if (BuildUIManager.Instance != null)
                    BuildUIManager.Instance.UnlockStageTab("Stage III");
                break;

            case "BerryHut2":
                Debug.Log("BerryHut2 researched – level 2 upgrades for Berry are now allowed.");
                break;
        }
    }

    // ------------------------------------------------------------------
    // ДОСТУПНОСТЬ ИССЛЕДОВАНИЙ
    // ------------------------------------------------------------------

    private void RefreshAvailability()
    {
        if (definitions == null) return;

        // 1) всем выключаем доступность (completed не трогаем)
        foreach (var kv in nodes)
        {
            var node = kv.Value;
            if (!node.IsCompleted)
                node.SetState(available: false, completed: false);
        }

        // 2) включаем доступность тем, у кого выполнены пререквизиты и условия
        foreach (var def in definitions)
        {
            if (!nodes.TryGetValue(def.id, out var node))
                continue;
            if (node.IsCompleted)
                continue;

            // пререквизиты
            bool prereqOk = true;
            if (def.prerequisites != null && def.prerequisites.Length > 0)
            {
                foreach (var preId in def.prerequisites)
                {
                    if (string.IsNullOrEmpty(preId))
                        continue;

                    if (!nodes.TryGetValue(preId, out var preNode) || !preNode.IsCompleted)
                    {
                        prereqOk = false;
                        break;
                    }
                }
            }

            if (!prereqOk)
                continue;

            // нода дозрела по пререквизитам — снимаем снапшот производства
            EnsureRevealSnapshot(def.id);

            if (disableResearchRequirements)
            {
                node.SetState(available: true, completed: false);
                continue;
            }

            if (AreGameConditionsMet(def.id))
                node.SetState(available: true, completed: false);
        }
    }

    /// <summary>
    /// Кол-во домов любой стадии.
    /// </summary>
    private int CountAllHouses()
    {
        if (AllBuildingsManager.Instance == null) return 0;

        int count = 0;
        foreach (var po in AllBuildingsManager.Instance.GetAllBuildings())
        {
            if (po is House h && h != null)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Кол-во домов c уровнем >= minStage.
    /// </summary>
    private int CountHousesWithStageAtLeast(int minStage)
    {
        if (AllBuildingsManager.Instance == null) return 0;

        int count = 0;
        foreach (var po in AllBuildingsManager.Instance.GetAllBuildings())
        {
            if (po is House h && h != null && h.CurrentStage >= minStage)
                count++;
        }
        return count;
    }

    private bool AreGameConditionsMet(string researchId)
    {
        if (disableResearchRequirements)
            return true;

        // общее требование для всех исследований
        if (lastKnownMood < MoodRequiredAll)
            return false;

        switch (researchId)
        {
            case "Clay":
                {
                    int houses = CountAllHouses();
                    return houses >= Clay_HousesRequired;
                }

            case "Pottery":
                {
                    int houses = CountAllHouses();
                    if (houses < Pottery_HousesRequired) return false;

                    int haveClay = GetProducedSinceReveal("Pottery", "Clay");
                    return haveClay >= Pottery_ClayRequired;
                }

            case "Tools":
                {
                    int houses = CountAllHouses();
                    if (houses < Tools_HousesRequired) return false;

                    int haveWood = GetProducedSinceReveal("Tools", "Wood");
                    int haveRock = GetProducedSinceReveal("Tools", "Rock");
                    return haveWood >= Tools_WoodRequired && haveRock >= Tools_RockRequired;
                }

            case "Hunter":
                {
                    int houses = CountAllHouses();
                    if (houses < Hunter_HousesRequired) return false;

                    int haveTools = GetProducedSinceReveal("Hunter", "Tools");
                    return haveTools >= Hunter_ToolsRequired;
                }

            case "Warehouse":
                {
                    int havePottery = GetProducedSinceReveal("Warehouse", "Pottery");
                    return havePottery >= Warehouse_PotteryRequired;
                }

            case "Stage2":
                {
                    int houses = CountAllHouses();
                    if (houses < Stage2_HousesRequired) return false;

                    int haveMeat = GetProducedSinceReveal("Stage2", "Meat");
                    int haveTools = GetProducedSinceReveal("Stage2", "Tools");
                    return haveMeat >= Stage2_MeatRequired && haveTools >= Stage2_ToolsRequired;
                }

            case "Crafts":
                {
                    int housesLvl2 = CountHousesWithStageAtLeast(2);
                    if (housesLvl2 < Crafts_HousesLvl2Required) return false;

                    int haveBone = GetProducedSinceReveal("Crafts", "Bone");
                    return haveBone >= Crafts_BoneRequired;
                }

            case "Stage3":
                {
                    int housesLvl2 = CountHousesWithStageAtLeast(2);
                    if (housesLvl2 < Stage3_HousesLvl2Required) return false;

                    int haveClothes   = GetProducedSinceReveal("Stage3", "Clothes");
                    int haveBeer      = GetProducedSinceReveal("Stage3", "Beer");
                    int haveFurniture = GetProducedSinceReveal("Stage3", "Furniture");
                    int haveMilk      = GetProducedSinceReveal("Stage3", "Milk");

                    return haveClothes   >= Stage3_ClothesRequired
                        && haveBeer      >= Stage3_BeerRequired
                        && haveFurniture >= Stage3_FurnitureRequired
                        && haveMilk      >= Stage3_MilkRequired;
                }

            case "BerryHut2":
                {
                    int haveBerry = GetProducedSinceReveal("BerryHut2", "Berry");
                    int haveTools = GetProducedSinceReveal("BerryHut2", "Tools");
                    return haveBerry >= BerryHut2_BerryRequired && haveTools >= BerryHut2_ToolsRequired;
                }

            case "LumberMill2":
                {
                    int haveWood  = GetProducedSinceReveal("LumberMill2", "Wood");
                    int haveTools = GetProducedSinceReveal("LumberMill2", "Tools");
                    return haveWood >= Lumber2_WoodRequired && haveTools >= Lumber2_ToolsRequired;
                }

            case "Hunter2":
                {
                    int haveMeat  = GetProducedSinceReveal("Hunter2", "Meat");
                    int haveTools = GetProducedSinceReveal("Hunter2", "Tools");
                    return haveMeat >= Hunter2_MeatRequired && haveTools >= Hunter2_ToolsRequired;
                }

            case "Furniture":
                {
                    int housesLvl2 = CountHousesWithStageAtLeast(2);
                    if (housesLvl2 < Furniture_HousesLvl2Required) return false;

                    int haveCrafts = GetProducedSinceReveal("Furniture", "Crafts");
                    return haveCrafts >= Furniture_CraftsRequired;
                }

            case "Wheat":
                {
                    int haveBerry = GetProducedSinceReveal("Wheat", "Berry");
                    return haveBerry >= Wheat_BerryRequired;
                }

            case "Flour":
                {
                    int haveWheat = GetProducedSinceReveal("Flour", "Wheat");
                    return haveWheat >= Flour_WheatRequired;
                }

            case "Bakery":
                {
                    int haveFlour = GetProducedSinceReveal("Bakery", "Flour");
                    return haveFlour >= Bakery_FlourRequired;
                }

            case "Sheep":
                {
                    int haveWheat = GetProducedSinceReveal("Sheep", "Wheat");
                    return haveWheat >= Sheep_WheatRequired;
                }

            case "Dairy":
                {
                    int haveMilk = GetProducedSinceReveal("Dairy", "Milk");
                    return haveMilk >= Dairy_MilkRequired;
                }

            case "Beans":
                {
                    int housesLvl2 = CountHousesWithStageAtLeast(2);
                    return housesLvl2 >= Beans_HousesLvl2Required;
                }

            case "Weaver":
                {
                    int haveWool = GetProducedSinceReveal("Weaver", "Wool");
                    return haveWool >= Weaver_WoolRequired;
                }

            case "Clothes":
                {
                    int haveCloth = GetProducedSinceReveal("Clothes", "Cloth");
                    return haveCloth >= Clothes_ClothRequired;
                }

            case "Market":
                {
                    int haveClothes = GetProducedSinceReveal("Market", "Clothes");
                    return haveClothes >= Market_ClothesRequired;
                }

            case "Brewery":
                {
                    int haveWheat = GetProducedSinceReveal("Brewery", "Wheat");
                    return haveWheat >= Brewery_WheatRequired;
                }

            case "Coal":
                {
                    int haveWood = GetProducedSinceReveal("Coal", "Wood");
                    return haveWood >= Coal_WoodRequired;
                }

            default:
                return true;
        }
    }

    // ------------------------------------------------------------------
    // ТУМАН ВОЙНЫ / ВИДИМОСТЬ НОД
    // ------------------------------------------------------------------

    private bool IsNodeHidden(ResearchDef def)
    {
        if (def.prerequisites == null || def.prerequisites.Length == 0)
            return false; // корневые ноды видны всегда

        foreach (var pre in def.prerequisites)
        {
            if (nodes.TryGetValue(pre, out var preNode) && preNode.IsCompleted)
                return false;
        }

        return true;
    }

    private bool IsResearchRevealed(string researchId)
    {
        if (!nodes.TryGetValue(researchId, out var node))
            return false;

        if (node.IsCompleted)
            return true;

        ResearchDef def = null;
        if (definitions != null)
        {
            foreach (var d in definitions)
            {
                if (d.id == researchId)
                {
                    def = d;
                    break;
                }
            }
        }

        if (def == null)
            return true;

        if (def.prerequisites == null || def.prerequisites.Length == 0)
            return true;

        foreach (var preId in def.prerequisites)
        {
            if (string.IsNullOrEmpty(preId)) continue;
            if (nodes.TryGetValue(preId, out var preNode) && preNode.IsCompleted)
                return true;
        }

        return false;
    }

    private void RefreshFogOfWar()
    {
        if (definitions == null || unknownIcon == null)
            return;

        foreach (var def in definitions)
        {
            if (!nodes.TryGetValue(def.id, out var node))
                continue;

            bool revealed = IsResearchRevealed(def.id);

            if (revealed)
                node.SetIcon(def.icon);
            else
                node.SetIcon(unknownIcon);
        }
    }

    // ------------------------------------------------------------------
    // ТУЛТИПЫ
    // ------------------------------------------------------------------

    public string BuildTooltipForNode(string researchId)
    {
        // Находим дефиницию
        ResearchDef def = null;
        if (definitions != null)
        {
            foreach (var d in definitions)
            {
                if (d.id == researchId)
                {
                    def = d;
                    break;
                }
            }
        }

        if (def == null)
            return "???";

        // Если нода скрыта — показываем "туман войны"
        if (IsNodeHidden(def))
            return "<b>???</b>\n???\n???\n???";

        string name = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
        var parts = new List<string>();

        // Заголовок
        parts.Add($"<b>{name}</b>");

        // Статус
        if (nodes.TryGetValue(researchId, out var node))
        {
            if (node.IsCompleted)
                parts.Add("<color=#00ff00ff>Исследовано</color>");
            else if (node.IsAvailable)
                parts.Add("<color=#ffff00ff>Готово к изучению (кликни)</color>");
            else
                parts.Add("<color=#ff8080ff>Недоступно</color>");
        }

        // Общее требование по настроению для всех исследований
        string moodCol = lastKnownMood >= MoodRequiredAll ? "white" : "red";
        parts.Add($"Настроение: <color={moodCol}>{lastKnownMood}/{MoodRequiredAll}</color>");

        // Конкретные требования по исследованиям
        switch (researchId)
        {
            case "Clay":
                {
                    int curHouses = CountAllHouses();
                    string housesCol = curHouses >= Clay_HousesRequired ? "white" : "red";
                    parts.Add($"Дома: <color={housesCol}>{curHouses}/{Clay_HousesRequired}</color>");
                    break;
                }

            case "Pottery":
                {
                    int curHouses = CountAllHouses();
                    string housesCol = curHouses >= Pottery_HousesRequired ? "white" : "red";
                    parts.Add($"Дома: <color={housesCol}>{curHouses}/{Pottery_HousesRequired}</color>");

                    int haveClay = GetProducedSinceReveal("Pottery", "Clay");
                    if (haveClay > Pottery_ClayRequired) haveClay = Pottery_ClayRequired;
                    string clayCol = haveClay >= Pottery_ClayRequired ? "white" : "red";
                    parts.Add($"Глина (произведено): <color={clayCol}>{haveClay}/{Pottery_ClayRequired}</color>");
                    break;
                }

            case "Tools":
                {
                    int curHouses = CountAllHouses();
                    string housesCol = curHouses >= Tools_HousesRequired ? "white" : "red";
                    parts.Add($"Дома: <color={housesCol}>{curHouses}/{Tools_HousesRequired}</color>");

                    int haveW = GetProducedSinceReveal("Tools", "Wood");
                    int haveR = GetProducedSinceReveal("Tools", "Rock");
                    if (haveW > Tools_WoodRequired) haveW = Tools_WoodRequired;
                    if (haveR > Tools_RockRequired) haveR = Tools_RockRequired;
                    string wCol = haveW >= Tools_WoodRequired ? "white" : "red";
                    string rCol = haveR >= Tools_RockRequired ? "white" : "red";
                    parts.Add($"Дерево (произведено): <color={wCol}>{haveW}/{Tools_WoodRequired}</color>");
                    parts.Add($"Камень (произведено): <color={rCol}>{haveR}/{Tools_RockRequired}</color>");
                    break;
                }

            case "Hunter":
                {
                    int curHouses = CountAllHouses();
                    string housesCol = curHouses >= Hunter_HousesRequired ? "white" : "red";
                    parts.Add($"Дома: <color={housesCol}>{curHouses}/{Hunter_HousesRequired}</color>");

                    int haveTools = GetProducedSinceReveal("Hunter", "Tools");
                    if (haveTools > Hunter_ToolsRequired) haveTools = Hunter_ToolsRequired;
                    string tCol = haveTools >= Hunter_ToolsRequired ? "white" : "red";
                    parts.Add($"Инструменты (произведено): <color={tCol}>{haveTools}/{Hunter_ToolsRequired}</color>");
                    break;
                }

            case "Warehouse":
                {
                    int have = GetProducedSinceReveal("Warehouse", "Pottery");
                    if (have > Warehouse_PotteryRequired) have = Warehouse_PotteryRequired;
                    string col = have >= Warehouse_PotteryRequired ? "white" : "red";
                    parts.Add($"Гончарка (Pottery, произведено): <color={col}>{have}/{Warehouse_PotteryRequired}</color>");
                    break;
                }

            case "Stage2":
                {
                    int curHouses = CountAllHouses();
                    string housesCol = curHouses >= Stage2_HousesRequired ? "white" : "red";
                    parts.Add($"Дома: <color={housesCol}>{curHouses}/{Stage2_HousesRequired}</color>");

                    int haveMeat = GetProducedSinceReveal("Stage2", "Meat");
                    int haveTools = GetProducedSinceReveal("Stage2", "Tools");
                    if (haveMeat > Stage2_MeatRequired) haveMeat = Stage2_MeatRequired;
                    if (haveTools > Stage2_ToolsRequired) haveTools = Stage2_ToolsRequired;
                    string mCol = haveMeat >= Stage2_MeatRequired ? "white" : "red";
                    string tCol = haveTools >= Stage2_ToolsRequired ? "white" : "red";
                    parts.Add($"Мясо (произведено): <color={mCol}>{haveMeat}/{Stage2_MeatRequired}</color>");
                    parts.Add($"Инструменты (произведено): <color={tCol}>{haveTools}/{Stage2_ToolsRequired}</color>");
                    break;
                }

            case "Crafts":
                {
                    int curHouses = CountHousesWithStageAtLeast(2);
                    string housesCol = curHouses >= Crafts_HousesLvl2Required ? "white" : "red";
                    parts.Add($"Дома 2 уровня+: <color={housesCol}>{curHouses}/{Crafts_HousesLvl2Required}</color>");

                    int haveBones = GetProducedSinceReveal("Crafts", "Bone");
                    if (haveBones > Crafts_BoneRequired) haveBones = Crafts_BoneRequired;
                    string bCol = haveBones >= Crafts_BoneRequired ? "white" : "red";
                    parts.Add($"Кости (произведено): <color={bCol}>{haveBones}/{Crafts_BoneRequired}</color>");
                    break;
                }

            case "Stage3":
                {
                    int curHouses = CountHousesWithStageAtLeast(2);
                    string housesCol = curHouses >= Stage3_HousesLvl2Required ? "white" : "red";
                    parts.Add($"Дома 2 уровня+: <color={housesCol}>{curHouses}/{Stage3_HousesLvl2Required}</color>");

                    void AddRes(string label, string researchKey, string resId, int need)
                    {
                        int have = GetProducedSinceReveal(researchKey, resId);
                        if (have > need) have = need;
                        string col = have >= need ? "white" : "red";
                        parts.Add($"{label}: <color={col}>{have}/{need}</color>");
                    }

                    AddRes("Одежда",   "Stage3", "Clothes",   Stage3_ClothesRequired);
                    AddRes("Пиво",     "Stage3", "Beer",      Stage3_BeerRequired);
                    AddRes("Мебель",   "Stage3", "Furniture", Stage3_FurnitureRequired);
                    AddRes("Молоко",   "Stage3", "Milk",      Stage3_MilkRequired);
                    break;
                }

            case "BerryHut2":
                {
                    int haveBerry = GetProducedSinceReveal("BerryHut2", "Berry");
                    int haveTools = GetProducedSinceReveal("BerryHut2", "Tools");
                    if (haveBerry > BerryHut2_BerryRequired) haveBerry = BerryHut2_BerryRequired;
                    if (haveTools > BerryHut2_ToolsRequired) haveTools = BerryHut2_ToolsRequired;
                    string bCol = haveBerry >= BerryHut2_BerryRequired ? "white" : "red";
                    string tCol = haveTools >= BerryHut2_ToolsRequired ? "white" : "red";
                    parts.Add($"Ягоды (произведено): <color={bCol}>{haveBerry}/{BerryHut2_BerryRequired}</color>");
                    parts.Add($"Инструменты (произведено): <color={tCol}>{haveTools}/{BerryHut2_ToolsRequired}</color>");
                    break;
                }

            case "LumberMill2":
                {
                    int haveWood = GetProducedSinceReveal("LumberMill2", "Wood");
                    int haveTools = GetProducedSinceReveal("LumberMill2", "Tools");
                    if (haveWood > Lumber2_WoodRequired) haveWood = Lumber2_WoodRequired;
                    if (haveTools > Lumber2_ToolsRequired) haveTools = Lumber2_ToolsRequired;
                    string wCol = haveWood >= Lumber2_WoodRequired ? "white" : "red";
                    string tCol = haveTools >= Lumber2_ToolsRequired ? "white" : "red";
                    parts.Add($"Дерево (произведено): <color={wCol}>{haveWood}/{Lumber2_WoodRequired}</color>");
                    parts.Add($"Инструменты (произведено): <color={tCol}>{haveTools}/{Lumber2_ToolsRequired}</color>");
                    break;
                }

            case "Hunter2":
                {
                    int haveMeat = GetProducedSinceReveal("Hunter2", "Meat");
                    int haveTools = GetProducedSinceReveal("Hunter2", "Tools");
                    if (haveMeat > Hunter2_MeatRequired) haveMeat = Hunter2_MeatRequired;
                    if (haveTools > Hunter2_ToolsRequired) haveTools = Hunter2_ToolsRequired;
                    string mCol = haveMeat >= Hunter2_MeatRequired ? "white" : "red";
                    string tCol = haveTools >= Hunter2_ToolsRequired ? "white" : "red";
                    parts.Add($"Мясо (произведено): <color={mCol}>{haveMeat}/{Hunter2_MeatRequired}</color>");
                    parts.Add($"Инструменты (произведено): <color={tCol}>{haveTools}/{Hunter2_ToolsRequired}</color>");
                    break;
                }

            case "Furniture":
                {
                    int curHouses = CountHousesWithStageAtLeast(2);
                    string housesCol = curHouses >= Furniture_HousesLvl2Required ? "white" : "red";
                    parts.Add($"Дома 2 уровня+: <color={housesCol}>{curHouses}/{Furniture_HousesLvl2Required}</color>");

                    int have = GetProducedSinceReveal("Furniture", "Crafts");
                    if (have > Furniture_CraftsRequired) have = Furniture_CraftsRequired;
                    string col = have >= Furniture_CraftsRequired ? "white" : "red";
                    parts.Add($"Ремесло (Crafts, произведено): <color={col}>{have}/{Furniture_CraftsRequired}</color>");
                    break;
                }

            case "Wheat":
                {
                    int have = GetProducedSinceReveal("Wheat", "Berry");
                    if (have > Wheat_BerryRequired) have = Wheat_BerryRequired;
                    string col = have >= Wheat_BerryRequired ? "white" : "red";
                    parts.Add($"Ягоды (произведено): <color={col}>{have}/{Wheat_BerryRequired}</color>");
                    break;
                }

            case "Flour":
                {
                    int have = GetProducedSinceReveal("Flour", "Wheat");
                    if (have > Flour_WheatRequired) have = Flour_WheatRequired;
                    string col = have >= Flour_WheatRequired ? "white" : "red";
                    parts.Add($"Пшеница (произведено): <color={col}>{have}/{Flour_WheatRequired}</color>");
                    break;
                }

            case "Bakery":
                {
                    int have = GetProducedSinceReveal("Bakery", "Flour");
                    if (have > Bakery_FlourRequired) have = Bakery_FlourRequired;
                    string col = have >= Bakery_FlourRequired ? "white" : "red";
                    parts.Add($"Мука (произведено): <color={col}>{have}/{Bakery_FlourRequired}</color>");
                    break;
                }

            case "Sheep":
                {
                    int have = GetProducedSinceReveal("Sheep", "Wheat");
                    if (have > Sheep_WheatRequired) have = Sheep_WheatRequired;
                    string col = have >= Sheep_WheatRequired ? "white" : "red";
                    parts.Add($"Пшеница (произведено): <color={col}>{have}/{Sheep_WheatRequired}</color>");
                    break;
                }

            case "Dairy":
                {
                    int have = GetProducedSinceReveal("Dairy", "Milk");
                    if (have > Dairy_MilkRequired) have = Dairy_MilkRequired;
                    string col = have >= Dairy_MilkRequired ? "white" : "red";
                    parts.Add($"Молоко (произведено): <color={col}>{have}/{Dairy_MilkRequired}</color>");
                    break;
                }

            case "Beans":
                {
                    int curHouses = CountHousesWithStageAtLeast(2);
                    string housesCol = curHouses >= Beans_HousesLvl2Required ? "white" : "red";
                    parts.Add($"Дома 2 уровня+: <color={housesCol}>{curHouses}/{Beans_HousesLvl2Required}</color>");
                    break;
                }

            case "Weaver":
                {
                    int have = GetProducedSinceReveal("Weaver", "Wool");
                    if (have > Weaver_WoolRequired) have = Weaver_WoolRequired;
                    string col = have >= Weaver_WoolRequired ? "white" : "red";
                    parts.Add($"Шерсть (произведено): <color={col}>{have}/{Weaver_WoolRequired}</color>");
                    break;
                }

            case "Clothes":
                {
                    int have = GetProducedSinceReveal("Clothes", "Cloth");
                    if (have > Clothes_ClothRequired) have = Clothes_ClothRequired;
                    string col = have >= Clothes_ClothRequired ? "white" : "red";
                    parts.Add($"Ткань (произведено): <color={col}>{have}/{Clothes_ClothRequired}</color>");
                    break;
                }

            case "Market":
                {
                    int have = GetProducedSinceReveal("Market", "Clothes");
                    if (have > Market_ClothesRequired) have = Market_ClothesRequired;
                    string col = have >= Market_ClothesRequired ? "white" : "red";
                    parts.Add($"Одежда (произведено): <color={col}>{have}/{Market_ClothesRequired}</color>");
                    break;
                }

            case "Brewery":
                {
                    int have = GetProducedSinceReveal("Brewery", "Wheat");
                    if (have > Brewery_WheatRequired) have = Brewery_WheatRequired;
                    string col = have >= Brewery_WheatRequired ? "white" : "red";
                    parts.Add($"Пшеница (произведено): <color={col}>{have}/{Brewery_WheatRequired}</color>");
                    break;
                }

            case "Coal":
                {
                    int have = GetProducedSinceReveal("Coal", "Wood");
                    if (have > Coal_WoodRequired) have = Coal_WoodRequired;
                    string col = have >= Coal_WoodRequired ? "white" : "red";
                    parts.Add($"Дерево (произведено): <color={col}>{have}/{Coal_WoodRequired}</color>");
                    break;
                }

            default:
                parts.Add("Нет специальных требований.");
                break;
        }

        return string.Join("\n", parts);
    }
}
