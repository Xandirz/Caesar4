using System;
using System.Collections.Generic;
using System.Linq;
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
    private HashSet<string> completedResearchIds = new HashSet<string>();
    public List<string> ExportCompletedResearch() => completedResearchIds.ToList();

    public void ImportCompletedResearch(List<string> completed)
    {
        completedResearchIds = new HashSet<string>(completed ?? new List<string>());
        // тут же обновить UI/разблокировки, если нужно
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
        MoodAtLeast,
        HousesTotalAtLeast,
        HousesWithStageAtLeast,        // a=stageAtLeast, b=needCount
        HousesWithStageAtLeastPercent, // a=stageAtLeast, b=percent (0..100)   <-- ДОБАВЬ
        ProducedSinceRevealAtLeast
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

    
    [SerializeField] private ResearchIconDatabase iconDb;

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
            
            
            
            
            { "Herbs",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Herbs   } },
            { "Doctor",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Doctor   } },
            { "Vegetables",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Vegetables   } },
            { "Grape",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Grape   } },
            { "Wine",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Wine   } },
            { "Gold",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.GoldOre ,BuildManager.BuildMode.Gold   } },
            { "Bathhouse",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Bathhouse   } },
            

            { "Salt",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Salt   } },
            { "Fruit",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Fruit   } },
            { "Jewelry",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Jewelry   } },
            { "Sand",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Sand   } },
            { "Ash",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Ash   } },
            { "Glass",   new List<BuildManager.BuildMode> { BuildManager.BuildMode.Glass   } },
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
        if (iconDb != null) iconDb.WarmUp();

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
    private RequirementDef2 HousesStagePercentReq(int stageAtLeast, int percent) =>
        new RequirementDef2($"Дома {stageAtLeast} уровня+: {percent}%", RequirementType2.HousesWithStageAtLeastPercent, a: stageAtLeast, b: percent);

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
            // toolsIcon,
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
            // hunterIcon,
            gridPosition = new Vector2(4, 9),
            prerequisites = new[] { "Tools" },
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(25),
                ProducedReq("Инструменты (произведено)", "Tools", 150),
            }
        },
        new ResearchDef
        {
            id = "Stage2",
            displayName = "Вторая стадия",
            // stage2Icon,
            gridPosition = new Vector2(5, 9),
            prerequisites = new[] { "Hunter" },
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(30),
                ProducedReq("Мясо (произведено)", "Meat", 150),
                ProducedReq("Инструменты (произведено)", "Tools", 150),
            }
        },

        // -------------------------
        // АПГРЕЙДЫ ОТ TOOLS
        // -------------------------
        new ResearchDef
        {
            id = "BerryHut2",
            displayName = "Ягодник II",
            // berry2Icon,
            gridPosition = new Vector2(3, 8),
            prerequisites = new[] { "Tools" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Ягоды (произведено)", "Berry", 250),
                ProducedReq("Инструменты (произведено)", "Tools", 150),
            }
        },
        new ResearchDef
        {
            id = "LumberMill2",
            displayName = "Лесопилка II",
            // lumber2Icon,
            gridPosition = new Vector2(3, 10),
            prerequisites = new[] { "Tools" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Дерево (произведено)", "Wood", 300),
                ProducedReq("Инструменты (произведено)", "Tools", 200),
            }
        },
        new ResearchDef
        {
            id = "Charcoal",
            displayName = "Уголь",
            // charcoalIcon,
            gridPosition = new Vector2(3, 11),
            prerequisites = new[] { "LumberMill2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Дерево (произведено)", "Wood", 600),
            }
        },
        new ResearchDef
        {
            id = "Hunter2",
            displayName = "Bow and Arrow",
            // hunter2Icon,
            gridPosition = new Vector2(4, 10),
            prerequisites = new[] { "Hunter" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Мясо (произведено)", "Meat", 300),
                ProducedReq("Инструменты (произведоно)", "Tools", 300),
            }
        },

        // -------------------------
        // WHEAT ВЕТКА
        // -------------------------
        new ResearchDef
        {
            id = "Wheat",
            displayName = "Пшеница",
            // wheatIcon,
            gridPosition = new Vector2(5, 8),
            prerequisites = new[] { "Stage2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 20),
                ProducedReq("Ягоды (произведено)", "Berry", 100),
            }
        },
        new ResearchDef
        {
            id = "Brewery",
            displayName = "Пивоварня",
            // breweryIcon,
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
            // flourIcon,
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
            // bakeryIcon,
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
            // sheepIcon,
            gridPosition = new Vector2(5, 7),
            prerequisites = new[] { "Wheat" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 30),
                ProducedReq("Пшеница (произведено)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Fertilization",
            displayName = "Удобрение",
            // fertilizationIcon,
            gridPosition = new Vector2(4, 7),
            prerequisites = new[] { "Sheep" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 35),
                ProducedReq("Manure (произведено)", "Manure", 100),
            }
        },
        new ResearchDef
        {
            id = "Farm2",
            displayName = "Farm II",
            // farm2Icon,
            gridPosition = new Vector2(3, 7),
            prerequisites = new[] { "Fertilization" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Пшеница (произведено)", "Wheat", 150),
            }
        },
        new ResearchDef
        {
            id = "Dairy",
            displayName = "Молочная",
            // dairyIcon,
            gridPosition = new Vector2(6, 7),
            prerequisites = new[] { "Sheep" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 40),
                ProducedReq("Молоко (произведено)", "Milk", 50),
            }
        },
        new ResearchDef
        {
            id = "Weaver",
            displayName = "Ткачество",
            // weaverIcon,
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
            // fish2Icon,
            gridPosition = new Vector2(6, 6),
            prerequisites = new[] { "Weaver" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Рыба (произведено)", "Fish", 150),
            }
        },
        new ResearchDef
        {
            id = "Clothes",
            displayName = "Одежда",
            // clothesIcon,
            gridPosition = new Vector2(5, 5),
            prerequisites = new[] { "Weaver" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 45),
                ProducedReq("Ткань (произведено)", "Cloth", 150),
            }
        },
        new ResearchDef
        {
            id = "Market",
            displayName = "Рынок",
            // marketIcon,
            gridPosition = new Vector2(5, 4),
            prerequisites = new[] { "Clothes" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 45),
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
            // stage3Icon,
            gridPosition = new Vector2(6, 4),
            prerequisites = new[] { "Market" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 50),
                ProducedReq("Одежда", "Clothes", 250),
                ProducedReq("Пиво", "Beer", 250),
                ProducedReq("Мебель", "Furniture", 250),
                ProducedReq("Молоко", "Milk", 250),
            }
        },

        // -------------------------
        // ПОСЛЕ STAGE 3 (Flax chain)
        // -------------------------
        new ResearchDef
        {
            id = "Flax",
            displayName = "Flax",
            // flaxIcon,
            gridPosition = new Vector2(7, 4),
            prerequisites = new[] { "Stage3" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 20),

                ProducedReq("Шерсть (произведено)", "Wool", 150),
            }
        },
        new ResearchDef
        {
            id = "Weaver2",
            displayName = "Advanced Weaving",
            // weaver2Icon,
            gridPosition = new Vector2(7, 3), // <-- не перекрывает Flax
            prerequisites = new[] { "Flax" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 25),

                ProducedReq("Лён (произведено)", "Flax", 150),
            }
        },
        new ResearchDef
        {
            id = "Leather",
            displayName = "Leatherworking",
            // leatherIcon,
            gridPosition = new Vector2(8, 3), // сдвинули вправо цепочку
            prerequisites = new[] { "Weaver2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Hide processed", "Hide", 150),
            }
        },
        new ResearchDef
        {
            id = "Clothes2",
            displayName = "Tailoring",
            // clothes2Icon,
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
            id = "Crafts",
            displayName = "Ремесло",
            // craftsIcon,
            gridPosition = new Vector2(5, 10),
            prerequisites = new[] { "Stage2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 10),
                ProducedReq("Кости (произведено)", "Bone", 250),
            }
        },
        new ResearchDef
        {
            id = "Furniture",
            displayName = "Мебель",
            // furnitureIcon,
            gridPosition = new Vector2(6, 10),
            prerequisites = new[] { "Crafts" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 20),
                ProducedReq("Ремесло (Crafts, произведено)", "Crafts", 150),
            }
        },
        new ResearchDef
        {
            id = "Beans",
            displayName = "Бобы",
            // beansIcon,
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
            // oliveIcon,
            gridPosition = new Vector2(6, 3),
            prerequisites = new[] { "Stage3" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 15),
                ProducedReq("Wheat (произведено)", "Wheat", 100),
            }
        },
        new ResearchDef
        {
            id = "OliveOil",
            displayName = "Оливковое масло",
            // oliveOilIcon,
            gridPosition = new Vector2(6, 2),
            prerequisites = new[] { "Olive" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 20),

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
            // miningIcon,
            gridPosition = new Vector2(4, 6),
            prerequisites = new[] { "Weaver" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 40),
                ProducedReq("Глина (произведено)", "Rock", 20),
            }
        },
        new ResearchDef
        {
            id = "CopperOre",
            displayName = "Copper Ore",
            // copperOreIcon,
            gridPosition = new Vector2(3, 6),
            prerequisites = new[] { "Mining" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Rock (произведено)", "Rock", 20),
            }
        },
        new ResearchDef
        {
            id = "Copper",
            displayName = "Copper",
            // copperIcon,
            gridPosition = new Vector2(2, 6),
            prerequisites = new[] { "CopperOre" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Copper ore (произведено)", "CopperOre", 150),
            }
        },
        new ResearchDef
        {
            id = "Tools2",
            displayName = "Tools 2",
            // tools2Icon,
            gridPosition = new Vector2(1, 6),
            prerequisites = new[] { "Copper" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 50),
                ProducedReq("Copper (произведено)", "Copper", 120),
            }
        },

        // -------------------------
        // STAGE4 + TIN/BRONZE
        // -------------------------
        new ResearchDef
        {
            id = "Brick",
            displayName = "Кирпич",
            // brickIcon,
            gridPosition = new Vector2(11, 4),
            prerequisites = new[] { "Cattle" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 45),
                ProducedReq("Глина (произведено)", "Clay", 150),
            }
        },
        new ResearchDef
        {
            id = "Temple",
            displayName = "Храм",
            // templeIcon,
            gridPosition = new Vector2(12, 4),
            prerequisites = new[] { "Brick" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),
                ProducedReq("Кирпич (произведено)", "Brick", 50),
            }
        },
        new ResearchDef
        {
            id = "Stage4",
            displayName = "Stage IV",
            // stage4Icon,
            gridPosition = new Vector2(13, 4),
            prerequisites = new[] { "Temple" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 60),
                ProducedReq("Кирпич (произведено)", "Brick", 100),
                ProducedReq("Soap (произведено)", "Soap", 100),
                ProducedReq("Candle (произведено)", "Candle", 100),
                ProducedReq("OliveOil (произведено)", "OliveOil", 500),
            }
        },
        new ResearchDef
        {
            id = "TinOre",
            displayName = "Tin Ore",
            // tinIcon,
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
            // bronzeIcon,
            gridPosition = new Vector2(12, 5),
            prerequisites = new[] { "TinOre" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),
                ProducedReq("Tin ore (произведено)", "TinOre", 400),
            }
        },
        new ResearchDef
        {
            id = "Tools3",
            displayName = "Tools 3",
            // tools3Icon,
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
            // mining2Icon,
            gridPosition = new Vector2(11, 6),
            prerequisites = new[] { "Tools3" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze (произведено)", "Bronze", 100),
            }
        },


        new ResearchDef
        {
            id = "Pig",
            displayName = "Свиньи",
            // pigIcon,
            gridPosition = new Vector2(8, 4),
            prerequisites = new[] { "Flax" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Мясо (произведено)", "Meat", 150),
            }
        },
        new ResearchDef
        {
            id = "Goat",
            displayName = "Козы",
            // goatIcon,
            gridPosition = new Vector2(9, 4),
            prerequisites = new[] { "Pig" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Молоко (произведено)", "Milk", 150),
            }
        },
        new ResearchDef
        {
            id = "Cattle",
            displayName = "Крупный скот",
            // cattleIcon,
            gridPosition = new Vector2(10, 4),
            prerequisites = new[] { "Goat" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),

                ProducedReq("Мясо (произведено)", "Meat", 150),
            }
        },
        new ResearchDef
        {
            id = "Bee",
            displayName = "Пчёлы",
            // beeIcon,
            gridPosition = new Vector2(7, 5),
            prerequisites = new[] { "Flax" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 25),
                ProducedReq("Wood (произведено)", "Wood", 150),
            }
        },
        new ResearchDef
        {
            id = "Candle",
            displayName = "Свечи",
            // candleIcon,
            gridPosition = new Vector2(7, 6),
            prerequisites = new[] { "Bee" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Воск (произведено)", "Wax", 150),
            }
        },
        new ResearchDef
        {
            id = "Soap",
            displayName = "Мыло",
            // soapIcon,
            gridPosition = new Vector2(8, 5),
            prerequisites = new[] { "Pig" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Жир (произведено)", "Fat", 150),
            }
        },
        new ResearchDef
        {
            id = "Chicken",
            displayName = "Куры",
            // chickenIcon,
            gridPosition = new Vector2(9, 5),
            prerequisites = new[] { "Goat" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 40),
                ProducedReq("Meat (произведено)", "Meat", 250),
            }
        },
        new ResearchDef
        {
            id = "Plough",
            displayName = "Плуг",
            // ploughIcon,
            gridPosition = new Vector2(10, 5),
            prerequisites = new[] { "Cattle" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 45),
                ProducedReq("Инструменты (произведено)", "Tools", 350),
            }
        },
        new ResearchDef
        {
            id = "Farm3",
            displayName = "Фермы III",
            // farm3Icon,
            gridPosition = new Vector2(10, 6),
            prerequisites = new[] { "Plough" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 50),
                ProducedReq("Пшеница (произведено)", "Wheat", 200),
            }
        },
        new ResearchDef
        {
            id = "PotteryWheel",
            displayName = "Pottery Wheel",
            // potteryWheelIcon,
            gridPosition = new Vector2(5, 3),
            prerequisites = new[] { "Market" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Pottery (произведено)", "Pottery", 120),
            }
        },
        new ResearchDef
        {
            id = "Pottery2",
            displayName = "Pottery 2",
            // pottery2Icon,
            gridPosition = new Vector2(5, 2),
            prerequisites = new[] { "PotteryWheel" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Pottery (произведено)", "Pottery", 120),
            }
        },
        new ResearchDef
        {
            id = "Clay2",
            displayName = "Clay 2",
            // clay2Icon,
            gridPosition = new Vector2(2, 8),
            prerequisites = new[] { "BerryHut2" },
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Глина (произведено)", "Clay", 350),
            }
        },
        
        new ResearchDef
        {
            id = "Furniture2",
            displayName = "Furniture2",
            // furniture2Icon,
            gridPosition = new Vector2(9, 2),
            prerequisites = new[] { "Clothes2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 35),
                ProducedReq("Furniture", "Furniture", 199),
            }
        },
        new ResearchDef
        {
            id = "Dairy2",
            displayName = "Dairy2",
            // dairy2Icon,
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
            id = "Quern",
            displayName = "Quern",
            // quernIcon,
            gridPosition = new Vector2(10, 7),
            prerequisites = new[] { "Farm3" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),

                ProducedReq("Flour", "Flour", 100),
            }
        },
        
        new ResearchDef
        {
            id = "Flour2",
            displayName = "Flour2",
            // flour2Icon,
            gridPosition = new Vector2(9, 7),
            prerequisites = new[] { "Quern" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),
                ProducedReq("Flour", "Flour", 100),
            }
        },
        new ResearchDef
        {
            id = "Bakery2",
            displayName = "Bakery2",
            // bakery2Icon,
            gridPosition = new Vector2(8, 7),
            prerequisites = new[] { "Flour2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),
                ProducedReq("Bread", "Bread", 100),
            }
        },
  
        
        new ResearchDef
        {
            id = "Brewery2",
            displayName = "Brewery2",
            // brewery2Icon,
            gridPosition = new Vector2(7, 7),
            prerequisites = new[] { "Bakery2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),

                ProducedReq("Bread", "Bread", 150),
            }
        },
        new ResearchDef
        {
            id = "Charcoal2",
            displayName = "Charcoal2",
            // charcoal2Icon,
            gridPosition = new Vector2(11, 7),
            prerequisites = new[] { "Mining2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),

                ProducedReq("Charcoal", "Charcoal", 100),
            }
        },
        new ResearchDef
        {
            id = "Smithy",
            displayName = "Smithy",
            // smithyIcon,
            gridPosition = new Vector2(11, 8),
            prerequisites = new[] { "Charcoal2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),

                ProducedReq("Bronze", "Bronze", 100),
            }
        },
        new ResearchDef
        {
            id = "LumberMill3",
            displayName = "LumberMill3",
            // lumber3Icon,
            gridPosition = new Vector2(10, 8),
            prerequisites = new[] { "Smithy" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),

                ProducedReq("Bronze", "Bronze", 100),
            }
        },
        
      
        new ResearchDef
        {
            id = "RotaryMill",
            displayName = "Animal Powered Rotary Mill",
            // rotaryMillIcon,
            gridPosition = new Vector2(14, 4),
            prerequisites = new[] { "Stage4" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 20),
                ProducedReq("Flour", "Flour", 350),
            }
        },
        new ResearchDef
        {
            id = "Flour3",
            displayName = "Flour3",
            // flour3Icon,
            gridPosition = new Vector2(15, 5),
            prerequisites = new[] { "RotaryMill" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 20),
                ProducedReq("Flour", "Flour", 300),
            }
        },
        new ResearchDef
        {
            id = "Herbs",
            displayName = "Herbs",
            // herbsIcon,
            gridPosition = new Vector2(14, 5),
            prerequisites = new[] { "RotaryMill" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 25),
                ProducedReq("Berry", "Berry", 300),
            }
        },
        new ResearchDef
        {
            id = "Doctor",
            displayName = "Doctor",
            // doctorIcon,
            gridPosition = new Vector2(14, 6),
            prerequisites = new[] { "Herbs" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 30),
                ProducedReq("Herbs", "Herbs", 100),
            }
        },
        
        new ResearchDef
        {
            id = "Salt",
            displayName = "Salt",
            // saltIcon,
            gridPosition = new Vector2(14, 7),
            prerequisites = new[] { "Doctor" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 30),
                ProducedReq("Clay", "Clay", 100),
            }
        },
        new ResearchDef
        {
            id = "Weaver3",
            displayName = "Weaver3",
            // weaver3Icon,
            gridPosition = new Vector2(13, 7),
            prerequisites = new[] { "Salt" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Salt", "Salt", 100),
            }
        },
        new ResearchDef
        {
            id = "Leather2",
            displayName = "Leather2",
            // leather2Icon,
            gridPosition = new Vector2(13, 6),
            prerequisites = new[] { "Weaver3" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 45),
                ProducedReq("Salt", "Salt", 100),
            }
        },
        new ResearchDef
        {
            id = "MeatPreservation",
            displayName = "MeatPreservation",
            // meatPreservationIcon,
            gridPosition = new Vector2(15, 7),
            prerequisites = new[] { "Salt" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Salt", "Salt", 100),
            }
        },
        new ResearchDef
        {
            id = "Dairy3",
            displayName = "Dairy3",
            // dairy3Icon,
            gridPosition = new Vector2(15, 6),
            prerequisites = new[] { "MeatPreservation" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Salt", "Salt", 100),
            }
        },

        new ResearchDef
        {
            id = "Hunter3",
            displayName = "Hunter3",
            // hunter3Icon,
            gridPosition = new Vector2(16, 6),
            prerequisites = new[] { "MeatPreservation" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Salt", "Salt", 100),
            }
        },
        new ResearchDef
        {
            id = "Animal2",
            displayName = "Animal2",
            // animal2Icon,
            gridPosition = new Vector2(17, 6),
            prerequisites = new[] { "Hunter3" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Salt", "Salt", 100),
            }
        },
        new ResearchDef
        {
            id = "Fruit",
            displayName = "Fruit",
            // fruitIcon,
            gridPosition = new Vector2(14, 8),
            prerequisites = new[] { "Salt" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Herbs", "Herbs", 200),
            }
        },

        new ResearchDef
        {
            id = "Sand",
            displayName = "Sand",
            // sandIcon,
            gridPosition = new Vector2(14, 9),
            prerequisites = new[] { "Fruit" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Clay", "Clay", 200),
            }
        },
        new ResearchDef
        {
            id = "Ash",
            displayName = "Ash",
            // ashIcon,
            gridPosition = new Vector2(15, 9),
            prerequisites = new[] { "Sand" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Sand", "Sand", 200),
            }
        },
        new ResearchDef
        {
            id = "Soap2",
            displayName = "Soap2",
            // soap2Icon,
            gridPosition = new Vector2(15, 8),
            prerequisites = new[] { "Ash" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Ash", "Ash", 200),
            }
        },
        new ResearchDef
        {
            id = "Glass",
            displayName = "Glass",
            // glassIcon,
            gridPosition = new Vector2(16, 9),
            prerequisites = new[] { "Ash" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Ash", "Ash", 200),
            }
        },
        new ResearchDef
        {
            id = "Pottery3",
            displayName = "Pottery3",
            // pottery3Icon,
            gridPosition = new Vector2(17, 9),
            prerequisites = new[] { "Glass" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Glass", "Glass", 400),
            }
        },
        new ResearchDef
        {
            id = "Farm4",
            displayName = "Farm4",
            // farm4Icon,
            gridPosition = new Vector2(15, 10),
            prerequisites = new[] { "Ash" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Ash", "Ash", 300),
            }
        },
        new ResearchDef
        {
            id = "Vegetables",
            displayName = "Vegetables",
            // vegetablesIcon,
            gridPosition = new Vector2(14, 10),
            prerequisites = new[] { "Sand" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 35),
                ProducedReq("Wheat", "Wheat", 100),
            }
        },
        new ResearchDef
        {
            id = "Grape",
            displayName = "Grape",
            // grapeIcon,
            gridPosition = new Vector2(13, 10),
            prerequisites = new[] { "Vegetables" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 40),
                ProducedReq("Vegetables", "Vegetables", 100),
            }
        },
        new ResearchDef
        {
            id = "Wine",
            displayName = "Wine",
            // wineIcon,
            gridPosition = new Vector2(13, 11),
            prerequisites = new[] { "Grape" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 40),
                ProducedReq("Grape", "Grape", 300),
            }
        },
        new ResearchDef
        {
            id = "Gold",
            displayName = "Gold",
            // goldIcon,
            gridPosition = new Vector2(12, 10),
            prerequisites = new[] { "Grape" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 50),
                ProducedReq("TinOre", "TinOre", 100),
            }
        },
        
        new ResearchDef
        {
            id = "Money",
            displayName = "Money",
            // moneyIcon,
            gridPosition = new Vector2(12, 9),
            prerequisites = new[] { "Gold" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 45),
                ProducedReq("Gold", "Gold", 300),
            }
        },
        new ResearchDef
        {
            id = "Smithy2",
            displayName = "Smithy2",
            // smithy2Icon,
            gridPosition = new Vector2(12, 8),
            prerequisites = new[] { "Money" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 50),

                ProducedReq("GoldOre", "GoldOre", 100),
            }
        },
        new ResearchDef
        {
            id = "Jewelry",
            displayName = "Jewelry",
            // jewelryIcon,
            gridPosition = new Vector2(13, 8),
            prerequisites = new[] { "Smithy2" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 50),

                ProducedReq("Gold", "Gold", 300),
            }
        },

        new ResearchDef
        {
            id = "Bathhouse",
            displayName = "Bathhouse",
            // bathIcon,
            gridPosition = new Vector2(11, 10),
            prerequisites = new[] { "Gold" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(4, 50),

                ProducedReq("Gold", "Gold", 100),
            }
        },

        new ResearchDef
        {
            id = "Stage5",
            displayName = "Stage5",
            // stage5Icon,
            gridPosition = new Vector2(10, 10),
            prerequisites = new[] { "Bathhouse" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStagePercentReq(4, 100),
                ProducedReq("Gold", "Gold", 100),
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
    private Sprite Icon(string id)
    {
        return iconDb != null ? iconDb.Get(id) : null;
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

            var icon = def.icon != null ? def.icon : Icon(def.id);
            def.icon = icon; // <-- ВАЖНО: запоминаем найденную иконку
            nodeGO.Init(def.id, def.displayName, icon, OnNodeClicked);

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
        completedResearchIds.Add(id);


        UnlockBuildingsForResearch(id);

        RefreshAvailability();
        RefreshFogOfWar();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayResearch();
        }


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
                case RequirementType2.HousesWithStageAtLeastPercent:
                {
                    int stageAtLeast = req.a;
                    int needPercent = Mathf.Clamp(req.b, 0, 100);

                    int total = CountAllHouses();
                    int ok = CountHousesWithStageAtLeast(stageAtLeast);

                    // если домов нет — 0% (и условие не выполнено для 100%)
                    int curPercent = (total <= 0) ? 0 : (ok * 100) / total;

                    // ВНИМАНИЕ: тут мы заполняем Cur/Need так, чтобы работал eval.IsMet (Cur >= Need)
                    eval.Lines.Add(new ReqLine(req.label, curPercent, needPercent));
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

            if (revealed)
            {
                var icon = def.icon != null ? def.icon : Icon(def.id);
                def.icon = icon; // кэшируем на будущее
                node.SetIcon(icon != null ? icon : unknownIcon);
            }
            else
            {
                node.SetIcon(unknownIcon);
            }
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
    
    public ResearchSaveData ExportState()
    {
        return new ResearchSaveData
        {
            completed = completedResearchIds.ToList()
        };
    }

    public void ImportState(ResearchSaveData data)
    {
        completedResearchIds = new HashSet<string>(data.completed ?? new List<string>());

        // важно: обновить UI/локи/эффекты, если есть
        // BuildUIManager.Instance.RefreshAllLocksAndTabs();
        
        foreach (var def in definitions)
        {
            if (!nodes.TryGetValue(def.id, out var node)) continue;

            bool done = completedResearchIds.Contains(def.id);
            node.SetState(available: false, completed: done);
        }

// пересчитать доступность/граф
        RefreshAvailability();
        RefreshFogOfWar();

    }

}
