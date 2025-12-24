using System;
using System.Collections.Generic;
using UnityEngine;

public class ResearchManager : MonoBehaviour
{
    [Serializable]
    public class RequirementDef
    {
        // Понятное имя строки в тултипе
        public string label;

        // Тип требования
        public RequirementType type;

        // Параметры (используются в зависимости от type)
        public int intParam;          // напр. housesNeeded / stageAtLeast / moodNeeded / amountNeeded
        public string resourceId;     // напр. "Clay", "Wood" и т.п.

        public RequirementDef(string label, RequirementType type, int intParam = 0, string resourceId = null)
        {
            this.label = label;
            this.type = type;
            this.intParam = intParam;
            this.resourceId = resourceId;
        }
    }

    public enum RequirementType
    {
        MoodAtLeast,          // intParam = минимальное настроение
        HousesTotalAtLeast,   // intParam = минимум домов (любой стадии)
        HousesStageAtLeast,   // intParam = stageAtLeast, а amountNeeded берём из intParam2 (см ниже) -> но чтобы без лишних полей: используем две записи:
                              //   1) HousesStageAtLeastCount: stageAtLeast + need
        ProducedSinceRevealAtLeast // resourceId + intParam (need)
    }

    // Вариант без "лишних" полей: для домов >= stage используем отдельный тип со stage в intParam, а need в intParam2.
    // Но ты попросил "задавать в блоках", а не плодить константы — поэтому добавлю второй intParam2.
    [Serializable]
    public class RequirementDef2
    {
        public string label;
        public RequirementType2 type;
        public int a;            // moodNeed / housesNeed / stageAtLeast / producedNeed
        public int b;            // для HousesStageCount: needCount
        public string resourceId;

        public RequirementDef2(string label, RequirementType2 type, int a = 0, int b = 0, string resourceId = null)
        {
            this.label = label;
            this.type = type;
            this.a = a;
            this.b = b;
            this.resourceId = resourceId;
        }
    }

    public enum RequirementType2
    {
        MoodAtLeast,              // a = moodNeed
        HousesTotalAtLeast,       // a = housesNeed
        HousesWithStageAtLeast,   // a = stageAtLeast, b = needCount
        ProducedSinceRevealAtLeast // resourceId + a = need
    }

    [Serializable]
    public class ResearchDef
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public Vector2 gridPosition;
        public string[] prerequisites;

        // ✅ Требования задаём прямо тут (1 источник правды)
        public RequirementDef2[] requirements;
    }

    public static ResearchManager Instance;

    [Header("Unknown / Fog of war")]
    [SerializeField] private Sprite unknownIcon;

    [Header("Иконки исследований")]
    [SerializeField] private Sprite clayIcon;

    [SerializeField] private Sprite potteryIcon;
    [SerializeField] private Sprite toolsIcon;
    [SerializeField] private Sprite hunterIcon;
    [SerializeField] private Sprite craftsIcon;
    [SerializeField] private Sprite warehouseIcon;
    [SerializeField] private Sprite stage2Icon;
    [SerializeField] private Sprite stage3Icon;
    [SerializeField] private Sprite stage4Icon;
    [SerializeField] private Sprite berry2Icon;
    [SerializeField] private Sprite lumber2Icon;
    [SerializeField] private Sprite hunter2Icon;

    [SerializeField] private Sprite wheatIcon;
    [SerializeField] private Sprite flourIcon;
    [SerializeField] private Sprite bakeryIcon;

    [SerializeField] private Sprite sheepIcon;
    [SerializeField] private Sprite dairyIcon;
    [SerializeField] private Sprite weaverIcon;
    [SerializeField] private Sprite weaver2Icon;
    [SerializeField] private Sprite fish2Icon;
    [SerializeField] private Sprite clothesIcon;
    [SerializeField] private Sprite clothes2Icon;
    [SerializeField] private Sprite flaxIcon;
    [SerializeField] private Sprite marketIcon;
    [SerializeField] private Sprite furnitureIcon;

    [SerializeField] private Sprite breweryIcon;
    [SerializeField] private Sprite charcoalIcon;
    [SerializeField] private Sprite beansIcon;

    [SerializeField] private Sprite oliveIcon;
    [SerializeField] private Sprite oliveOilIcon;

    [SerializeField] private Sprite pigIcon;
    [SerializeField] private Sprite goatIcon;
    [SerializeField] private Sprite cattleIcon;
    [SerializeField] private Sprite brickIcon;

    [SerializeField] private Sprite beeIcon;
    [SerializeField] private Sprite candleIcon;

    [SerializeField] private Sprite soapIcon;
    [SerializeField] private Sprite chickenIcon;
    [SerializeField] private Sprite ploughIcon;
    [SerializeField] private Sprite templeIcon;

    [SerializeField] private Sprite fertilizationIcon;
    [SerializeField] private Sprite farm2Icon;
    [SerializeField] private Sprite farm3Icon;
    [SerializeField] private Sprite leatherIcon;

    [SerializeField] private Sprite potteryWheelIcon;
    [SerializeField] private Sprite pottery2Icon;
    [SerializeField] private Sprite clay2Icon;
    
    
    [SerializeField] private Sprite copperOreIcon;
    [SerializeField] private Sprite copperIcon;
    [SerializeField] private Sprite tinIcon;
    [SerializeField] private Sprite bronzeIcon;
    [SerializeField] private Sprite tools2Icon;
    [SerializeField] private Sprite tools3Icon;
    [SerializeField] private Sprite miningIcon;
    [SerializeField] private Sprite mining2Icon;
    
    [SerializeField] private Sprite lumber3Icon;
    [SerializeField] private Sprite charcoal2Icon;
    [SerializeField] private Sprite smithy2Icon;
    [SerializeField] private Sprite flour2Icon;
    [SerializeField] private Sprite bakery2Icon;
    [SerializeField] private Sprite dairy2Icon;
    [SerializeField] private Sprite brewery2Icon;
    [SerializeField] private Sprite furniture2Icon;

    
    

    [Header("Prefabs / UI")]
    [SerializeField] private ResearchNode nodePrefab;
    [SerializeField] private ResearchLine linePrefab;
    [SerializeField] private RectTransform nodesRoot;
    [SerializeField] private RectTransform linesRoot;
    [SerializeField] private float cellSize = 200f;

    private ResearchDef[] definitions;

    [SerializeField] private bool disableResearchRequirements = false;

    private int lastKnownMood = 0;

    private readonly Dictionary<string, Dictionary<string, int>> producedAtReveal =
        new Dictionary<string, Dictionary<string, int>>();

    private readonly Dictionary<string, int> producedTotals =
        new Dictionary<string, int>();

    private readonly Dictionary<string, ResearchNode> nodes =
        new Dictionary<string, ResearchNode>();

    private readonly Dictionary<string, List<BuildManager.BuildMode>> researchUnlocks =
        new Dictionary<string, List<BuildManager.BuildMode>>
        {
            { "Clay",      new List<BuildManager.BuildMode> { BuildManager.BuildMode.Clay      } },
            { "Pottery",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Pottery   } },
            { "Tools",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Tools     } },
            { "Hunter",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Hunter    } },
            { "Crafts",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Crafts    } },

            { "Wheat",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Wheat     } },
            { "Flour",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Flour     } },
            { "Bakery",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Bakery    } },

            { "Sheep",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Sheep     } },
            { "Dairy",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Dairy     } },
            { "Weaver",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Weaver    } },
            { "Flax",      new List<BuildManager.BuildMode> { BuildManager.BuildMode.Flax      } },
            { "Clothes",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Clothes   } },
            { "Market",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Market    } },
            { "Furniture", new List<BuildManager.BuildMode> { BuildManager.BuildMode.Furniture } },

            { "Beans",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Beans     } },
            { "Brewery",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Brewery   } },
            { "Charcoal",  new List<BuildManager.BuildMode> { BuildManager.BuildMode.Charcoal  } },

            { "Warehouse", new List<BuildManager.BuildMode> { BuildManager.BuildMode.Warehouse } },

            { "Bee",       new List<BuildManager.BuildMode> { BuildManager.BuildMode.Bee       } },
            { "Candle",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Candle    } },
            { "Pig",       new List<BuildManager.BuildMode> { BuildManager.BuildMode.Pig       } },
            { "Goat",      new List<BuildManager.BuildMode> { BuildManager.BuildMode.Goat      } },
            { "Soap",      new List<BuildManager.BuildMode> { BuildManager.BuildMode.Soap      } },
            { "Brick",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Brick     } },
            { "Olive",     new List<BuildManager.BuildMode> { BuildManager.BuildMode.Olive     } },
            { "OliveOil",  new List<BuildManager.BuildMode> { BuildManager.BuildMode.OliveOil  } },
            { "Chicken",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Chicken   } },
            { "Cattle",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Cattle    } },
            { "Temple",    new List<BuildManager.BuildMode> { BuildManager.BuildMode.Temple    } },
            { "Leather",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Leather   } },
            
            { "CopperOre",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.CopperOre   } },
            { "TinOre",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.TinOre   } },
            { "Copper",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Copper   } },
            { "Bronze",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Bronze   } },
            
            { "Smithy",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Smithy   } },
        };

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

    public void OnDayPassed(int moodPercent)
    {
        lastKnownMood = Mathf.Clamp(moodPercent, 0, 100);
        RefreshAvailability();
    }

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
    // ОПИСАНИЕ ДЕРЕВА + ТРЕБОВАНИЯ В БЛОКАХ
    // ------------------------------------------------------------------

    private RequirementDef2 MoodReq(int mood) =>
        new RequirementDef2("Настроение", RequirementType2.MoodAtLeast, a: mood);

    private RequirementDef2 HousesReq(int houses) =>
        new RequirementDef2("Дома", RequirementType2.HousesTotalAtLeast, a: houses);

    private RequirementDef2 HousesStageReq(int stageAtLeast, int count) =>
        new RequirementDef2($"Дома {stageAtLeast} уровня+", RequirementType2.HousesWithStageAtLeast, a: stageAtLeast, b: count);

    private RequirementDef2 ProducedReq(string label, string resId, int need) =>
        new RequirementDef2(label, RequirementType2.ProducedSinceRevealAtLeast, a: need, resourceId: resId);

   private void BuildDefinitions()
{
    definitions = new ResearchDef[]
    {
        // -------------------------
        // БАЗОВАЯ ВЕТКА (Stage2)
        // -------------------------
        new ResearchDef
        {
            id = "Clay",
            displayName = "Глина",
            icon = clayIcon,
            gridPosition = new Vector2(1, 9),
            prerequisites = Array.Empty<string>(),
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(10),
            }
        },
        new ResearchDef
        {
            id = "Pottery",
            displayName = "Гончарное дело",
            icon = potteryIcon,
            gridPosition = new Vector2(2, 9),
            prerequisites = new[] { "Clay" },
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(15),
                ProducedReq("Глина (произведено)", "Clay", 50),
            }
        },
        new ResearchDef
        {
            id = "Tools",
            displayName = "Инструменты",
            icon = toolsIcon,
            gridPosition = new Vector2(3, 9),
            prerequisites = new[] { "Pottery" },
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(20),
                ProducedReq("Дерево (произведено)", "Wood", 50),
                ProducedReq("Камень (произведено)", "Rock", 50),
            }
        },
        new ResearchDef
        {
            id = "Hunter",
            displayName = "Охота",
            icon = hunterIcon,
            gridPosition = new Vector2(4, 9),
            prerequisites = new[] { "Tools" },
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(25),
                ProducedReq("Инструменты (произведено)", "Tools", 50),
            }
        },
        new ResearchDef
        {
            id = "Stage2",
            displayName = "Вторая стадия",
            icon = stage2Icon,
            gridPosition = new Vector2(5, 9),
            prerequisites = new[] { "Hunter" },
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(30),
                ProducedReq("Мясо (произведено)", "Meat", 50),
                ProducedReq("Инструменты (произведено)", "Tools", 50),
            }
        },

        // -------------------------
        // АПГРЕЙДЫ ОТ TOOLS
        // -------------------------
        new ResearchDef
        {
            id = "BerryHut2",
            displayName = "Ягодник II",
            icon = berry2Icon,
            gridPosition = new Vector2(3, 8),
            prerequisites = new[] { "Tools" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Ягоды (произведено)", "Berry", 100),
                ProducedReq("Инструменты (произведено)", "Tools", 100),
            }
        },
        new ResearchDef
        {
            id = "LumberMill2",
            displayName = "Лесопилка II",
            icon = lumber2Icon,
            gridPosition = new Vector2(3, 10),
            prerequisites = new[] { "Tools" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Дерево (произведено)", "Wood", 100),
                ProducedReq("Инструменты (произведено)", "Tools", 100),
            }
        },
        new ResearchDef
        {
            id = "Charcoal",
            displayName = "Уголь",
            icon = charcoalIcon,
            gridPosition = new Vector2(3, 11),
            prerequisites = new[] { "LumberMill2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Дерево (произведено)", "Wood", 50),
            }
        },
        new ResearchDef
        {
            id = "Hunter2",
            displayName = "Bow and Arrow",
            icon = hunter2Icon,
            gridPosition = new Vector2(4, 10),
            prerequisites = new[] { "Hunter" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Мясо (произведено)", "Meat", 100),
                ProducedReq("Инструменты (произведоно)", "Tools", 100),
            }
        },

        // -------------------------
        // WHEAT ВЕТКА
        // -------------------------
        new ResearchDef
        {
            id = "Wheat",
            displayName = "Пшеница",
            icon = wheatIcon,
            gridPosition = new Vector2(5, 8),
            prerequisites = new[] { "Stage2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Ягоды (произведено)", "Berry", 100),
            }
        },
        new ResearchDef
        {
            id = "Brewery",
            displayName = "Пивоварня",
            icon = breweryIcon,
            gridPosition = new Vector2(4, 8),
            prerequisites = new[] { "Wheat" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Пшеница (произведено)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Flour",
            displayName = "Мука",
            icon = flourIcon,
            gridPosition = new Vector2(6, 8),
            prerequisites = new[] { "Wheat" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Пшеница (произведено)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Bakery",
            displayName = "Пекарня",
            icon = bakeryIcon,
            gridPosition = new Vector2(7, 8),
            prerequisites = new[] { "Flour" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Мука (произведено)", "Flour", 50),
            }
        },

        // -------------------------
        // SHEEP/WEAVER ВЕТКА
        // -------------------------
        new ResearchDef
        {
            id = "Sheep",
            displayName = "Овцы",
            icon = sheepIcon,
            gridPosition = new Vector2(5, 7),
            prerequisites = new[] { "Wheat" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Пшеница (произведено)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Fertilization",
            displayName = "Удобрение",
            icon = fertilizationIcon,
            gridPosition = new Vector2(4, 7),
            prerequisites = new[] { "Sheep" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Manure (произведено)", "Manure", 100),
            }
        },
        new ResearchDef
        {
            id = "Farm2",
            displayName = "Farm II",
            icon = farm2Icon,
            gridPosition = new Vector2(3, 7),
            prerequisites = new[] { "Fertilization" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Пшеница (произведено)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Dairy",
            displayName = "Молочная",
            icon = dairyIcon,
            gridPosition = new Vector2(6, 7),
            prerequisites = new[] { "Sheep" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Молоко (произведено)", "Milk", 50),
            }
        },
        new ResearchDef
        {
            id = "Weaver",
            displayName = "Ткачество",
            icon = weaverIcon,
            gridPosition = new Vector2(5, 6),
            prerequisites = new[] { "Sheep" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Шерсть (произведено)", "Wool", 50),
            }
        },
        new ResearchDef
        {
            id = "Fish2",
            displayName = "Fishing Net",
            icon = fish2Icon,
            gridPosition = new Vector2(6, 6),
            prerequisites = new[] { "Weaver" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Рыба (произведено)", "Fish", 100),
            }
        },
        new ResearchDef
        {
            id = "Clothes",
            displayName = "Одежда",
            icon = clothesIcon,
            gridPosition = new Vector2(5, 5),
            prerequisites = new[] { "Weaver" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Ткань (произведено)", "Cloth", 50),
            }
        },
        new ResearchDef
        {
            id = "Market",
            displayName = "Рынок",
            icon = marketIcon,
            gridPosition = new Vector2(5, 4),
            prerequisites = new[] { "Clothes" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Одежда (произведено)", "Clothes", 50),
            }
        },

        // -------------------------
        // STAGE 3 (ключевое)
        // -------------------------
        new ResearchDef
        {
            id = "Stage3",
            displayName = "Третья стадия",
            icon = stage3Icon,
            gridPosition = new Vector2(6, 4),
            prerequisites = new[] { "Market" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 30),
                ProducedReq("Одежда", "Clothes", 50),
                ProducedReq("Пиво", "Beer", 50),
                ProducedReq("Мебель", "Furniture", 50),
                ProducedReq("Молоко", "Milk", 50),
            }
        },

        // -------------------------
        // ПОСЛЕ STAGE 3 (Flax chain)
        // -------------------------
        new ResearchDef
        {
            id = "Flax",
            displayName = "Flax",
            icon = flaxIcon,
            gridPosition = new Vector2(7, 4),
            prerequisites = new[] { "Stage3" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Шерсть (произведено)", "Wool", 50),
            }
        },
        new ResearchDef
        {
            id = "Weaver2",
            displayName = "Advanced Weaving",
            icon = weaver2Icon,
            gridPosition = new Vector2(7, 3), // <-- не перекрывает Flax
            prerequisites = new[] { "Flax" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Лён (произведено)", "Flax", 50),
            }
        },
        new ResearchDef
        {
            id = "Leather",
            displayName = "Leatherworking",
            icon = leatherIcon,
            gridPosition = new Vector2(8, 3), // сдвинули вправо цепочку
            prerequisites = new[] { "Weaver2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Hide processed", "Hide", 50),
            }
        },
        new ResearchDef
        {
            id = "Clothes2",
            displayName = "Tailoring",
            icon = clothes2Icon,
            gridPosition = new Vector2(9, 3),
            prerequisites = new[] { "Leather" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Linen (произведено)", "Linen", 50),
            }
        },

        // -------------------------
        // Прочие ветки, которые не зависят от Stage3 напрямую
        // -------------------------
        new ResearchDef
        {
            id = "Warehouse",
            displayName = "Склад",
            icon = warehouseIcon,
            gridPosition = new Vector2(2, 10),
            prerequisites = new[] { "Pottery" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Гончарка (Pottery, произведено)", "Pottery", 100),
            }
        },
        new ResearchDef
        {
            id = "Crafts",
            displayName = "Ремесло",
            icon = craftsIcon,
            gridPosition = new Vector2(5, 10),
            prerequisites = new[] { "Stage2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 10),
                ProducedReq("Кости (произведено)", "Bone", 50),
            }
        },
        new ResearchDef
        {
            id = "Furniture",
            displayName = "Мебель",
            icon = furnitureIcon,
            gridPosition = new Vector2(6, 10),
            prerequisites = new[] { "Crafts" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 20),
                ProducedReq("Ремесло (Crafts, произведено)", "Crafts", 50),
            }
        },
        new ResearchDef
        {
            id = "Beans",
            displayName = "Бобы",
            icon = beansIcon,
            gridPosition = new Vector2(6, 9),
            prerequisites = new[] { "Stage2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 25),
            }
        },
        new ResearchDef
        {
            id = "Olive",
            displayName = "Оливки",
            icon = oliveIcon,
            gridPosition = new Vector2(6, 3),
            prerequisites = new[] { "Stage3" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wheat (произведено)", "Wheat", 100),
            }
        },
        new ResearchDef
        {
            id = "OliveOil",
            displayName = "Оливковое масло",
            icon = oliveOilIcon,
            gridPosition = new Vector2(6, 2),
            prerequisites = new[] { "Olive" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Оливки (произведено)", "Olive", 100),
            }
        },

        // -------------------------
        // MINING/BRONZE ВЕТКА
        // -------------------------
        new ResearchDef
        {
            id = "Mining",
            displayName = "Mining",
            icon = miningIcon,
            gridPosition = new Vector2(4, 6),
            prerequisites = new[] { "Weaver" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Глина (произведено)", "Clay", 50),
            }
        },
        new ResearchDef
        {
            id = "CopperOre",
            displayName = "Copper Ore",
            icon = copperOreIcon,
            gridPosition = new Vector2(3, 6),
            prerequisites = new[] { "Mining" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Глина (произведено)", "Clay", 50),
            }
        },
        new ResearchDef
        {
            id = "Copper",
            displayName = "Copper",
            icon = copperIcon,
            gridPosition = new Vector2(2, 6),
            prerequisites = new[] { "CopperOre" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Copper ore (произведено)", "CopperOre", 50),
            }
        },
        new ResearchDef
        {
            id = "Tools2",
            displayName = "Tools 2",
            icon = tools2Icon,
            gridPosition = new Vector2(1, 6),
            prerequisites = new[] { "Copper" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Copper (произведено)", "Copper", 100),
            }
        },

        // -------------------------
        // STAGE4 + TIN/BRONZE
        // -------------------------
        new ResearchDef
        {
            id = "Brick",
            displayName = "Кирпич",
            icon = brickIcon,
            gridPosition = new Vector2(11, 4),
            prerequisites = new[] { "Cattle" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Глина (произведено)", "Clay", 100),
            }
        },
        new ResearchDef
        {
            id = "Temple",
            displayName = "Храм",
            icon = templeIcon,
            gridPosition = new Vector2(12, 4),
            prerequisites = new[] { "Brick" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Кирпич (произведено)", "Brick", 50),
            }
        },
        new ResearchDef
        {
            id = "Stage4",
            displayName = "Stage IV",
            icon = stage4Icon,
            gridPosition = new Vector2(13, 4),
            prerequisites = new[] { "Temple" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Кирпич (произведено)", "Brick", 100),
            }
        },
        new ResearchDef
        {
            id = "TinOre",
            displayName = "Tin Ore",
            icon = tinIcon,
            gridPosition = new Vector2(13, 5),
            prerequisites = new[] { "Stage4" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Copper ore (произведено)", "CopperOre", 100),
            }
        },
        new ResearchDef
        {
            id = "Bronze",
            displayName = "Bronze",
            icon = bronzeIcon,
            gridPosition = new Vector2(12, 5),
            prerequisites = new[] { "TinOre" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Tin ore (произведено)", "TinOre", 100),
            }
        },
        new ResearchDef
        {
            id = "Tools3",
            displayName = "Tools 3",
            icon = tools3Icon,
            gridPosition = new Vector2(11, 5),
            prerequisites = new[] { "Bronze" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze (произведено)", "Bronze", 100),
            }
        },
        new ResearchDef
        {
            id = "Mining2",
            displayName = "Mining 2",
            icon = mining2Icon,
            gridPosition = new Vector2(11, 6),
            prerequisites = new[] { "Tools3" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze (произведено)", "Bronze", 100),
            }
        },

        // -------------------------
        // Остальные (животные, мыло, свечи, фермы, колесо гончара, Clay2)
        // (оставил как у тебя, порядок не критичен для Stage3->Flax проблемы)
        // -------------------------
        new ResearchDef
        {
            id = "Pig",
            displayName = "Свиньи",
            icon = pigIcon,
            gridPosition = new Vector2(8, 4),
            prerequisites = new[] { "Flax" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Мясо (произведено)", "Meat", 50),
            }
        },
        new ResearchDef
        {
            id = "Goat",
            displayName = "Козы",
            icon = goatIcon,
            gridPosition = new Vector2(9, 4),
            prerequisites = new[] { "Pig" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Молоко (произведено)", "Milk", 50),
            }
        },
        new ResearchDef
        {
            id = "Cattle",
            displayName = "Крупный скот",
            icon = cattleIcon,
            gridPosition = new Vector2(10, 4),
            prerequisites = new[] { "Goat" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Мясо (произведено)", "Meat", 100),
            }
        },
        new ResearchDef
        {
            id = "Bee",
            displayName = "Пчёлы",
            icon = beeIcon,
            gridPosition = new Vector2(7, 5),
            prerequisites = new[] { "Flax" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wood (произведено)", "Wood", 50),
            }
        },
        new ResearchDef
        {
            id = "Candle",
            displayName = "Свечи",
            icon = candleIcon,
            gridPosition = new Vector2(7, 6),
            prerequisites = new[] { "Bee" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Воск (произведено)", "Wax", 50),
            }
        },
        new ResearchDef
        {
            id = "Soap",
            displayName = "Мыло",
            icon = soapIcon,
            gridPosition = new Vector2(8, 5),
            prerequisites = new[] { "Pig" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Жир (произведено)", "Fat", 50),
            }
        },
        new ResearchDef
        {
            id = "Chicken",
            displayName = "Куры",
            icon = chickenIcon,
            gridPosition = new Vector2(9, 5),
            prerequisites = new[] { "Goat" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Meat (произведено)", "Meat", 50),
            }
        },
        new ResearchDef
        {
            id = "Plough",
            displayName = "Плуг",
            icon = ploughIcon,
            gridPosition = new Vector2(10, 5),
            prerequisites = new[] { "Cattle" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Инструменты (произведено)", "Tools", 50),
            }
        },
        new ResearchDef
        {
            id = "Farm3",
            displayName = "Фермы III",
            icon = farm3Icon,
            gridPosition = new Vector2(10, 6),
            prerequisites = new[] { "Plough" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Пшеница (произведено)", "Wheat", 200),
            }
        },
        new ResearchDef
        {
            id = "PotteryWheel",
            displayName = "Pottery Wheel",
            icon = potteryWheelIcon,
            gridPosition = new Vector2(5, 3),
            prerequisites = new[] { "Market" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Pottery (произведено)", "Pottery", 100),
            }
        },
        new ResearchDef
        {
            id = "Pottery2",
            displayName = "Pottery 2",
            icon = pottery2Icon,
            gridPosition = new Vector2(5, 2),
            prerequisites = new[] { "PotteryWheel" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Pottery (произведено)", "Pottery", 100),
            }
        },
        new ResearchDef
        {
            id = "Clay2",
            displayName = "Clay 2",
            icon = clay2Icon,
            gridPosition = new Vector2(2, 8),
            prerequisites = new[] { "BerryHut2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Глина (произведено)", "Clay", 50),
            }
        },
        
        new ResearchDef
        {
            id = "Furniture2",
            displayName = "Furniture2",
            icon = furniture2Icon,
            gridPosition = new Vector2(9, 2),
            prerequisites = new[] { "Clothes2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Furniture", "Furniture", 199),
            }
        },
        new ResearchDef
        {
            id = "Dairy2",
            displayName = "Dairy2",
            icon = dairy2Icon,
            gridPosition = new Vector2(9, 6),
            prerequisites = new[] { "Farm3" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Cheese", "Cheese", 100),
            }
        },
        
        new ResearchDef
        {
            id = "Flour2",
            displayName = "Flour2",
            icon = flour2Icon,
            gridPosition = new Vector2(10, 7),
            prerequisites = new[] { "Farm3" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Flour", "Flour", 100),
            }
        },
        new ResearchDef
        {
            id = "Bakery2",
            displayName = "Flour2",
            icon = bakery2Icon,
            gridPosition = new Vector2(9, 7),
            prerequisites = new[] { "Flour2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bread", "Bread", 100),
            }
        },
  
        
        new ResearchDef
        {
            id = "Brewery2",
            displayName = "Brewery2",
            icon = brewery2Icon,
            gridPosition = new Vector2(8, 7),
            prerequisites = new[] { "Bakery2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bread", "Bread", 100),
            }
        },
        new ResearchDef
        {
            id = "Charcoal2",
            displayName = "Charcoal2",
            icon = charcoal2Icon,
            gridPosition = new Vector2(11, 7),
            prerequisites = new[] { "Mining2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Charcoal", "Charcoal", 100),
            }
        },
        new ResearchDef
        {
            id = "Smithy",
            displayName = "Smithy",
            icon = charcoal2Icon,
            gridPosition = new Vector2(11, 8),
            prerequisites = new[] { "Charcoal2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze", "Bronze", 100),
            }
        },
        new ResearchDef
        {
            id = "Lumber3",
            displayName = "Lumber3",
            icon = lumber2Icon,
            gridPosition = new Vector2(10, 8),
            prerequisites = new[] { "Smithy" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze", "Bronze", 100),
            }
        },
        
        
    };
}

    // ------------------------------------------------------------------
    // СНАПШОТЫ ПРОИЗВОДСТВА
    // ------------------------------------------------------------------

    private void EnsureRevealSnapshot(string researchId)
    {
        if (producedAtReveal.ContainsKey(researchId))
            return;

        var snap = new Dictionary<string, int>();
        foreach (var kvp in producedTotals)
            snap[kvp.Key] = kvp.Value;

        producedAtReveal[researchId] = snap;
    }

    private int GetProducedSinceReveal(string researchId, string resourceId)
    {
        int total = producedTotals.TryGetValue(resourceId, out var t) ? t : 0;

        if (!producedAtReveal.TryGetValue(researchId, out var snap))
            return total;

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

    // ------------------------------------------------------------------
    // ДОСТУПНОСТЬ ИССЛЕДОВАНИЙ
    // ------------------------------------------------------------------

    private void RefreshAvailability()
    {
        if (definitions == null) return;

        foreach (var kv in nodes)
        {
            var node = kv.Value;
            if (!node.IsCompleted)
                node.SetState(available: false, completed: false);
        }

        foreach (var def in definitions)
        {
            if (!nodes.TryGetValue(def.id, out var node))
                continue;
            if (node.IsCompleted)
                continue;

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

    // ------------------------------------------------------------------
    // ЕДИНАЯ ОЦЕНКА ТРЕБОВАНИЙ (и для логики, и для тултипа)
    // ------------------------------------------------------------------

    private struct ReqLine
    {
        public string Label;
        public int Cur;
        public int Need;

        public ReqLine(string label, int cur, int need)
        {
            Label = label;
            Cur = cur;
            Need = need;
        }
    }

    private struct ResearchEval
    {
        public List<ReqLine> Lines;

        public bool IsMet
        {
            get
            {
                if (Lines == null) return true;
                for (int i = 0; i < Lines.Count; i++)
                    if (Lines[i].Cur < Lines[i].Need)
                        return false;
                return true;
            }
        }
    }

    private ResearchDef FindDef(string researchId)
    {
        if (definitions == null) return null;
        for (int i = 0; i < definitions.Length; i++)
            if (definitions[i].id == researchId)
                return definitions[i];
        return null;
    }

    private ResearchEval EvaluateResearch(string researchId)
    {
        var def = FindDef(researchId);
        var eval = new ResearchEval { Lines = new List<ReqLine>(4) };

        if (disableResearchRequirements)
            return eval;

        if (def == null || def.requirements == null || def.requirements.Length == 0)
            return eval;

        for (int i = 0; i < def.requirements.Length; i++)
        {
            var req = def.requirements[i];
            switch (req.type)
            {
                case RequirementType2.MoodAtLeast:
                {
                    int cur = lastKnownMood;
                    int need = req.a;
                    eval.Lines.Add(new ReqLine(req.label, cur, need));
                    break;
                }

                case RequirementType2.HousesTotalAtLeast:
                {
                    int cur = CountAllHouses();
                    int need = req.a;
                    eval.Lines.Add(new ReqLine(req.label, cur, need));
                    break;
                }

                case RequirementType2.HousesWithStageAtLeast:
                {
                    int stageAtLeast = req.a;
                    int need = req.b;
                    int cur = CountHousesWithStageAtLeast(stageAtLeast);
                    eval.Lines.Add(new ReqLine(req.label, cur, need));
                    break;
                }

                case RequirementType2.ProducedSinceRevealAtLeast:
                {
                    int need = req.a;
                    int cur = GetProducedSinceReveal(researchId, req.resourceId);
                    if (cur > need) cur = need;
                    eval.Lines.Add(new ReqLine(req.label, cur, need));
                    break;
                }

                default:
                    break;
            }
        }

        return eval;
    }

    private bool AreGameConditionsMet(string researchId)
    {
        if (disableResearchRequirements)
            return true;

        var eval = EvaluateResearch(researchId);
        return eval.IsMet;
    }

    // ------------------------------------------------------------------
    // ТУМАН ВОЙНЫ / ВИДИМОСТЬ НОД
    // ------------------------------------------------------------------

    private bool IsNodeHidden(ResearchDef def)
    {
        if (def.prerequisites == null || def.prerequisites.Length == 0)
            return false;

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

        var def = FindDef(researchId);
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

            node.SetIcon(revealed ? def.icon : unknownIcon);
        }
    }

    // ------------------------------------------------------------------
    // ТУЛТИПЫ
    // ------------------------------------------------------------------

    public string BuildTooltipForNode(string researchId)
    {
        var def = FindDef(researchId);
        if (def == null)
            return "???";

        if (IsNodeHidden(def))
            return "<b>???</b>\n???\n???\n???";

        string name = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
        var parts = new List<string>();

        parts.Add($"<b>{name}</b>");

        if (nodes.TryGetValue(researchId, out var node))
        {
            if (node.IsCompleted)
                parts.Add("<color=#00ff00ff>Исследовано</color>");
            else if (node.IsAvailable)
                parts.Add("<color=#ffff00ff>Готово к изучению (кликни)</color>");
            else
                parts.Add("<color=#ff8080ff>Недоступно</color>");
        }

        if (disableResearchRequirements)
        {
            parts.Add("<color=#aaaaaaff>Требования отключены (debug)</color>");
            return string.Join("\n", parts);
        }

        var eval = EvaluateResearch(researchId);

        if (eval.Lines == null || eval.Lines.Count == 0)
        {
            parts.Add("Нет специальных требований.");
            return string.Join("\n", parts);
        }

        foreach (var line in eval.Lines)
        {
            string col = line.Cur >= line.Need ? "white" : "red";
            parts.Add($"{line.Label}: <color={col}>{line.Cur}/{line.Need}</color>");
        }

        return string.Join("\n", parts);
    }
}
