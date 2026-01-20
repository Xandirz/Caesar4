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
        
        [TextArea(2, 6)] public string descriptionBefore;
        [TextArea(2, 6)] public string descriptionAfter;
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
        new RequirementDef2("Mood", RequirementType2.MoodAtLeast, a: mood);

    private RequirementDef2 HousesReq(int houses) =>
        new RequirementDef2("House", RequirementType2.HousesTotalAtLeast, a: houses);

    private RequirementDef2 HousesStageReq(int stageAtLeast, int count) =>
        new RequirementDef2($"House {stageAtLeast} level+", RequirementType2.HousesWithStageAtLeast, a: stageAtLeast, b: count);
    private RequirementDef2 HousesStagePercentReq(int stageAtLeast, int percent) =>
        new RequirementDef2($"House {stageAtLeast} level+: {percent}%", RequirementType2.HousesWithStageAtLeastPercent, a: stageAtLeast, b: percent);

    private RequirementDef2 ProducedReq(string label, string resId, int need) =>
        new RequirementDef2(label, RequirementType2.ProducedSinceRevealAtLeast, a: need, resourceId: resId);

private void BuildDefinitions()
{
    definitions = new ResearchDef[]
    {
        // -------------------------
        // BASE BRANCH (Stage2)
        // -------------------------
        new ResearchDef
        {
            id = "Clay",
            displayName = "Clay",
            gridPosition = new Vector2(1, 9),
            prerequisites = Array.Empty<string>(),
            descriptionBefore = "By the river, we often noticed soft earth. It stuck to our feet and changed shape easily, but we paid no attention to it.",
            descriptionAfter  = "Over time we noticed: if you shape this earth and leave it in the sun, it hardens and keeps its form. We began using it for bowls, hearths, and simple walls. Ordinary mud became a useful material.",
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(10),
            }
        },
        new ResearchDef
        {
            id = "Pottery",
            displayName = "Pottery",
            gridPosition = new Vector2(2, 9),
            prerequisites = new[] { "Clay" },
            descriptionBefore = "We molded vessels from clay, but they were fragile. Water washed them away, and time turned them back into earth. We used them carefully, knowing they wouldn’t last long.",
            descriptionAfter  = "We noticed: vessels left near the fire become harder and no longer fear water. Fire changed the clay. Now we could store supplies longer and not rely on chance.",
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(15),
                ProducedReq("Clay (produced)", "Clay", 50),
            }
        },
        new ResearchDef
        {
            id = "Tools",
            displayName = "Tools",
            // toolsIcon,
            gridPosition = new Vector2(3, 9),
            prerequisites = new[] { "Pottery" },
            descriptionBefore = "We used our hands and whatever we found. Stones broke, sticks bent, and every action took effort and time.",
            descriptionAfter  = "We began choosing stones by shape and binding them to wood. Tools became an extension of our hands. Work, hunting, and defense became faster and more reliable.",
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(20),
                ProducedReq("Wood (produced)", "Wood", 50),
                ProducedReq("Rock (produced)", "Rock", 50),
            }
        },
        new ResearchDef
        {
            id = "Hunter",
            displayName = "Hunter",
            // hunterIcon,
            gridPosition = new Vector2(4, 9),
            prerequisites = new[] { "Tools" },
            descriptionBefore = "We chased beasts, relying on speed and luck. Often the prey escaped and our strength was wasted.",
            descriptionAfter  = "We began watching trails and habits and hunting together. Hunting became a plan, and food became a steady resource.",
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(25),
                ProducedReq("Tools (produced)", "Tools", 150),
            }
        },
        new ResearchDef
        {
            id = "Stage2",
            displayName = "Stage 2",
            // stage2Icon,
            gridPosition = new Vector2(5, 9),
            prerequisites = new[] { "Hunter" },
            descriptionBefore = "Our shelters protected us from wind and rain, but there was little space and little order. We ate what we could get right away and rarely stored food for long.",
            descriptionAfter  = "We reinforced our homes and made them more livable. Inside there was space for a hearth, pottery, and supplies. People began eating cooked food more often, consuming more meat, and using vessels. Comfort increased — and with it, our needs grew.",
            requirements = new[]
            {
                MoodReq(81),
                HousesReq(30),
                ProducedReq("Meat (produced)", "Meat", 150),
                ProducedReq("Tools (produced)", "Tools", 150),
            }
        },

        // -------------------------
        // UPGRADES FROM TOOLS
        // -------------------------
        new ResearchDef
        {
            id = "BerryHut2",
            displayName = "Berry Hut 2",
            // berry2Icon,
            gridPosition = new Vector2(3, 8),
            prerequisites = new[] { "Tools" },
            descriptionBefore = "We gathered berries by hand.",
            descriptionAfter  = "With tools, we can harvest berries faster.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Berries (produced)", "Berry", 250),
                ProducedReq("Tools (produced)", "Tools", 150),
            }
        },
        new ResearchDef
        {
            id = "LumberMill2",
            displayName = "Lumber Mill 2",
            // lumber2Icon,
            gridPosition = new Vector2(3, 10),
            prerequisites = new[] { "Tools" },
            descriptionBefore = "Wood resisted us. We snapped branches and spent strength on every trunk.",
            descriptionAfter  = "We improved the tools for cutting and shaping. Wood could now be gathered faster and in greater quantities.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wood (produced)", "Wood", 300),
                ProducedReq("Tools (produced)", "Tools", 200),
            }
        },
        new ResearchDef
        {
            id = "Charcoal",
            displayName = "Charcoal",
            // charcoalIcon,
            gridPosition = new Vector2(3, 11),
            prerequisites = new[] { "LumberMill2" },
            descriptionBefore = "Fire consumed firewood quickly and demanded constant attention. The heat was unstable, and it took a lot of fuel.",
            descriptionAfter  = "We noticed: charred wood burns longer and hotter. Charcoal gave us steady fire for cooking, crafts, and new technologies.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wood (produced)", "Wood", 600),
            }
        },
        new ResearchDef
        {
            id = "Hunter2",
            displayName = "Bow and Arrow",
            // hunter2Icon,
            gridPosition = new Vector2(4, 10),
            prerequisites = new[] { "Hunter" },
            descriptionBefore = "We hunted with spears and throwing sticks. We had to get close, taking risks and often missing.",
            descriptionAfter  = "We strung a bow and let the strength of wood live in its bend. Bow and arrows gave us range, accuracy, and a new role for the hunter.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Meat (produced)", "Meat", 300),
                ProducedReq("Tools (produced)", "Tools", 300),
            }
        },

        // -------------------------
        // WHEAT BRANCH
        // -------------------------
        new ResearchDef
        {
            id = "Wheat",
            displayName = "Wheat",
            // wheatIcon,
            gridPosition = new Vector2(5, 8),
            prerequisites = new[] { "Stage2" },
            descriptionBefore = "We collected grains wherever we happened to find them. The harvest was inconsistent and depended on chance.",
            descriptionAfter  = "We realized: grains can be sown and harvested again. Wheat gave predictable food, supplies, and the ability to plan time and labor.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 20),
                ProducedReq("Berries (produced)", "Berry", 100),
            }
        },
        new ResearchDef
        {
            id = "Brewery",
            displayName = "Brewery",
            // breweryIcon,
            gridPosition = new Vector2(4, 8),
            prerequisites = new[] { "Wheat" },
            descriptionBefore = "Grains kept poorly and spoiled quickly. Sometimes they changed taste, and we didn’t know why.",
            descriptionAfter  = "We noticed: if grains and water sit for a while, the drink becomes stronger and lasts longer. Beer became food, drink, and a reason to gather together.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wheat (produced)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Flour",
            displayName = "Flour",
            // flourIcon,
            gridPosition = new Vector2(6, 8),
            prerequisites = new[] { "Wheat" },
            descriptionBefore = "We ate grains whole or only lightly crushed them. Food was tough, took long to cook, and wasn’t always fully digested.",
            descriptionAfter  = "We noticed: grinding grains between stones turns them into soft powder. Flour changed food — cooking became faster, and grain served us better and longer.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wheat (produced)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Bakery",
            displayName = "Bakery",
            // bakeryIcon,
            gridPosition = new Vector2(7, 8),
            prerequisites = new[] { "Flour" },
            descriptionBefore = "We baked bread at different hearths, and each time it turned out differently. Baking took time and depended on each family’s experience.",
            descriptionAfter  = "We realized: with a shared fire and permanent ovens, bread can be made consistently and regularly. The bakery gave the settlement stable food and freed time for other work.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Flour (produced)", "Flour", 50),
            }
        },

        // -------------------------
        // SHEEP/WEAVER BRANCH
        // -------------------------
        new ResearchDef
        {
            id = "Sheep",
            displayName = "Sheep",
            // sheepIcon,
            gridPosition = new Vector2(5, 7),
            prerequisites = new[] { "Wheat" },
            descriptionBefore = "Our hunters met these animals on distant pastures. They stayed in herds and didn’t roam far, but we saw them only as prey.",
            descriptionAfter  = "We realized: if we keep them nearby, they provide not only meat, but also wool and milk again and again.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 30),
                ProducedReq("Wheat (produced)", "Wheat", 50),
            }
        },
        new ResearchDef
        {
            id = "Fertilization",
            displayName = "Fertilization",
            // fertilizationIcon,
            gridPosition = new Vector2(4, 7),
            prerequisites = new[] { "Sheep" },
            descriptionBefore = "Manure piled up near the pens and made living unpleasant. We tried to move it away, not seeing any value in it.",
            descriptionAfter  = "We noticed: where manure reached the soil, plants grew better.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 35),
                ProducedReq("Manure (produced)", "Manure", 100),
            }
        },
        new ResearchDef
        {
            id = "Farm2",
            displayName = "Farm II",
            // farm2Icon,
            gridPosition = new Vector2(3, 7),
            prerequisites = new[] { "Fertilization" },
            descriptionBefore = "Fields became exhausted after a few harvests, and yields weakened. We depended on finding new land.",
            descriptionAfter  = "We began adding manure to fields and caring for the soil. Farms produced more, but now required constant resources.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wheat (produced)", "Wheat", 150),
            }
        },
        new ResearchDef
        {
            id = "Dairy",
            displayName = "Dairy",
            descriptionBefore = "Milk spoiled quickly, so we could use it only right after milking. Much of it went to waste.",
            descriptionAfter  = "We realized: with time and warmth, milk changes. Cheese and yogurt turned milk into long-lasting food stores.",
            // dairyIcon,
            gridPosition = new Vector2(6, 7),
            prerequisites = new[] { "Sheep" },
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 40),
                ProducedReq("Milk (produced)", "Milk", 50),
            }
        },
        new ResearchDef
        {
            id = "Weaver",
            displayName = "Weaver",
            // weaverIcon,
            gridPosition = new Vector2(5, 6),
            prerequisites = new[] { "Sheep" },
            descriptionBefore = "Wool was rough and uncomfortable to use. We could only wrap ourselves in it as it was.",
            descriptionAfter  = "We learned to spin and weave fibers together.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Wool (produced)", "Wool", 50),
            }
        },
        new ResearchDef
        {
            id = "Fish2",
            displayName = "Fishing Net",
            // fish2Icon,
            gridPosition = new Vector2(6, 6),
            prerequisites = new[] { "Weaver" },
            descriptionBefore = "We caught fish by hand and with spears, and the catch depended on luck and skill. Many fish escaped without giving us a chance.",
            descriptionAfter  = "We wove fibers into nets and blocked the fish’s paths. Fishing became stable, and food more predictable.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Fish (produced)", "Fish", 150),
            }
        },
        new ResearchDef
        {
            id = "Clothes",
            displayName = "Clothes",
            // clothesIcon,
            gridPosition = new Vector2(5, 5),
            prerequisites = new[] { "Weaver" },
            descriptionBefore = "We covered ourselves with cloth, but it was uncomfortable.",
            descriptionAfter  = "We began cutting and sewing clothing from wool.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 45),
                ProducedReq("Cloth (produced)", "Cloth", 150),
            }
        },
        new ResearchDef
        {
            id = "Market",
            displayName = "Market",
            // marketIcon,
            gridPosition = new Vector2(5, 4),
            prerequisites = new[] { "Clothes" },
            descriptionBefore = "We traded goods randomly and directly whenever a need arose.",
            descriptionAfter  = "We set aside a place for regular exchange and agreed on rules.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 45),
                ProducedReq("Clothes (produced)", "Clothes", 50),
            }
        },

        // -------------------------
        // STAGE 3 (key)
        // -------------------------
        new ResearchDef
        {
            id = "Stage3",
            displayName = "Stage3",
            // stage3Icon,
            gridPosition = new Vector2(6, 4),
            prerequisites = new[] { "Market" },
            descriptionBefore = "Our homes provided shelter, but there was little comfort inside, and life remained simple and monotonous.",
            descriptionAfter  = "We improved our dwellings and filled them with bread, dairy products, and clothing. A market appeared, and life became more complex, richer, and more demanding of resources.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 50),
                ProducedReq("Clothes", "Clothes", 250),
                ProducedReq("Beer", "Beer", 250),
                ProducedReq("Furniture", "Furniture", 250),
                ProducedReq("Milk", "Milk", 250),
            }
        },

        // -------------------------
        // AFTER STAGE 3 (Flax chain)
        // -------------------------
        new ResearchDef
        {
            id = "Flax",
            displayName = "Flax",
            // flaxIcon,
            gridPosition = new Vector2(7, 4),
            prerequisites = new[] { "Stage3" },
            descriptionBefore = "We knew this plant, but saw no value in it and did not grow it on purpose. Land was used only for food.",
            descriptionAfter  = "We realized: flax can be grown for its fibers. Flax farms gave a new resource for crafts and trade.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 20),
                ProducedReq("Wool (produced)", "Wool", 150),
            }
        },
        new ResearchDef
        {
            id = "Weaver2",
            displayName = "Advanced Weaving",
            // weaver2Icon,
            gridPosition = new Vector2(7, 3), // <-- does not overlap Flax
            prerequisites = new[] { "Flax" },
            descriptionBefore = "We could make cloth from wool, but it was rough and heavy. It wasn’t enough for finer work.",
            descriptionAfter  = "We learned to process flax and weave cloth from it.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 25),
                ProducedReq("Flax (produced)", "Flax", 150),
            }
        },
        new ResearchDef
        {
            id = "Leather",
            displayName = "Leatherworking",
            // leatherIcon,
            gridPosition = new Vector2(8, 3), // moved the chain to the right
            prerequisites = new[] { "Weaver2" },
            descriptionBefore = "We used animal hides as they were, and they wore out quickly and protected poorly.",
            descriptionAfter  = "We learned to process hides and produce durable leather for many needs.",
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

            descriptionBefore = "Clothing was simple and made from a single material, not always suited for work and weather.",
            descriptionAfter  = "We began combining wool, flax, and leather, creating stronger and more comfortable clothing.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Linen (produced)", "Linen", 50),
            }
        },

        // -------------------------
        // Other branches not directly dependent on Stage3
        // -------------------------
        new ResearchDef
        {
            id = "Crafts",
            displayName = "Crafts",
            // craftsIcon,
            gridPosition = new Vector2(5, 10),
            prerequisites = new[] { "Stage2" },
            descriptionBefore = "Most things were made hastily and didn’t last long. Everyday life lacked simple, reliable items.",
            descriptionAfter  = "We began crafting small household goods that made daily life easier.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 10),
                ProducedReq("Bones (produced)", "Bone", 250),
            }
        },
        new ResearchDef
        {
            id = "Furniture",
            displayName = "Furniture",
            // furnitureIcon,
            gridPosition = new Vector2(6, 10),
            prerequisites = new[] { "Crafts" },

            descriptionBefore = "There was almost no comfort in homes; people sat and slept on the ground.",
            descriptionAfter  = "We started making furniture, and homes became more comfortable for living and work.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 20),
                ProducedReq("Crafts (produced)", "Crafts", 150),
            }
        },
        new ResearchDef
        {
            id = "Beans",
            displayName = "Beans",
            // beansIcon,
            gridPosition = new Vector2(6, 9),
            prerequisites = new[] { "Stage2" },
            descriptionBefore = "We gathered beans where they grew on their own, and the harvest was inconsistent.",
            descriptionAfter  = "We began growing beans on farms and gained another reliable source of food.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 25),
            }
        },
        new ResearchDef
        {
            id = "Olive",
            displayName = "Olive",
            // oliveIcon,
            gridPosition = new Vector2(6, 3),
            prerequisites = new[] { "Stage3" },
            descriptionBefore = "We knew these trees, but harvested the fruit rarely and without any special purpose.",
            descriptionAfter  = "We began cultivating olives and getting a steady fruit harvest.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 15),
                ProducedReq("Wheat (produced)", "Wheat", 100),
            }
        },
        new ResearchDef
        {
            id = "OliveOil",
            displayName = "Olive Oil",
            // oliveOilIcon,
            gridPosition = new Vector2(6, 2),
            prerequisites = new[] { "Olive" },
            descriptionBefore = "Olives were used whole and spoiled quickly.",
            descriptionAfter  = "We learned to press oil from the fruit.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 20),
                ProducedReq("Olives (produced)", "Olive", 100),
            }
        },

        // -------------------------
        // MINING/BRONZE BRANCH
        // -------------------------
        new ResearchDef
        {
            id = "Mining",
            displayName = "Mining",
            // miningIcon,
            gridPosition = new Vector2(4, 6),
            prerequisites = new[] { "Weaver" },
            descriptionBefore = "We collected stone only from the surface and did not dig into the ground.",
            descriptionAfter  = "We began digging and extracting resources from the depths, gaining access to new materials.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 40),
                ProducedReq("Clay (produced)", "Rock", 20),
            }
        },
        new ResearchDef
        {
            id = "CopperOre",
            displayName = "Copper Ore",
            // copperOreIcon,
            gridPosition = new Vector2(3, 6),
            prerequisites = new[] { "Mining" },
            descriptionBefore = "We found strange green stones but didn’t know what they were for.",
            descriptionAfter  = "We realized: copper is hidden in these stones, and it can be extracted and used.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Rock (produced)", "Rock", 20),
            }
        },
        new ResearchDef
        {
            id = "Copper",
            displayName = "Copper",
            // copperIcon,
            gridPosition = new Vector2(2, 6),
            prerequisites = new[] { "CopperOre" },
            descriptionBefore = "Metal was beyond our reach, and we relied on stone and wood.",
            descriptionAfter  = "We learned to smelt copper and gained a durable material for tools and goods.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Copper ore (produced)", "CopperOre", 150),
            }
        },
        new ResearchDef
        {
            id = "Tools2",
            displayName = "Tools 2",
            // tools2Icon,
            gridPosition = new Vector2(1, 6),
            prerequisites = new[] { "Copper" },
            descriptionBefore = "Stone and wooden tools were unreliable.",
            descriptionAfter  = "We began making tools from copper, and work became faster.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(2, 50),
                ProducedReq("Copper (produced)", "Copper", 120),
            }
        },

        // -------------------------
        // STAGE4 + TIN/BRONZE
        // -------------------------
        new ResearchDef
        {
            id = "Brick",
            displayName = "Brick",
            // brickIcon,
            gridPosition = new Vector2(11, 4),
            prerequisites = new[] { "Cattle" },
            descriptionBefore = "Clay was used raw and quickly broke down in the rain.",
            descriptionAfter  = "We began firing clay and making strong bricks for construction.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 45),
                ProducedReq("Clay (produced)", "Clay", 150),
            }
        },
        new ResearchDef
        {
            id = "Temple",
            displayName = "Temple",
            // templeIcon,
            gridPosition = new Vector2(12, 4),
            prerequisites = new[] { "Brick" },
            descriptionBefore = "Spiritual life existed only in traditions and oral rites.",
            descriptionAfter  = "We built a temple, giving the community a place for rituals and shared meaning.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),
                ProducedReq("Brick (produced)", "Brick", 50),
            }
        },
        new ResearchDef
        {
            id = "Stage4",
            displayName = "Stage IV",
            // stage4Icon,
            gridPosition = new Vector2(13, 4),
            prerequisites = new[] { "Temple" },
            descriptionBefore = "Our homes remained simple, spiritual life was fragmented, and daily life depended on daylight.",
            descriptionAfter  = "We moved to brick houses, strengthened spiritual life, and began using soap and lighting, making life cleaner and brighter.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 60),
                ProducedReq("Brick (produced)", "Brick", 100),
                ProducedReq("Soap (produced)", "Soap", 100),
                ProducedReq("Candle (produced)", "Candle", 100),
                ProducedReq("OliveOil (produced)", "OliveOil", 500),
            }
        },
        new ResearchDef
        {
            id = "TinOre",
            displayName = "Tin Ore",
            // tinIcon,
            gridPosition = new Vector2(13, 5),
            prerequisites = new[] { "Stage4" },
            descriptionBefore = "We found rare pale stones, but didn’t understand their value and ignored them.",
            descriptionAfter  = "We learned about tin ore and began mining it as an important metal for alloys.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Copper ore (produced)", "CopperOre", 100),
            }
        },
        new ResearchDef
        {
            id = "Bronze",
            displayName = "Bronze",
            // bronzeIcon,
            gridPosition = new Vector2(12, 5),
            prerequisites = new[] { "TinOre" },
            descriptionBefore = "Copper was useful, but its strength was often not enough.",
            descriptionAfter  = "We learned to mix metals and got bronze — a harder, more reliable material.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 55),
                ProducedReq("Tin ore (produced)", "TinOre", 400),
            }
        },
        new ResearchDef
        {
            id = "Tools3",
            displayName = "Tools 3",
            // tools3Icon,
            gridPosition = new Vector2(11, 5),
            prerequisites = new[] { "Bronze" },
            descriptionBefore = "Copper tools improved labor, but wore out quickly.",
            descriptionAfter  = "Bronze tools became stronger and lasted longer, increasing work efficiency.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze (produced)", "Bronze", 100),
            }
        },
        new ResearchDef
        {
            id = "Mining2",
            displayName = "Mining 2",
            // mining2Icon,
            gridPosition = new Vector2(11, 6),
            prerequisites = new[] { "Tools3" },
            descriptionBefore = "Resource extraction was slow and depended on surface veins.",
            descriptionAfter  = "We improved mining and began extracting more ore from deeper underground.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Bronze (produced)", "Bronze", 100),
            }
        },

        new ResearchDef
        {
            id = "Pig",
            displayName = "Pigs",
            // pigIcon,
            gridPosition = new Vector2(8, 4),
            prerequisites = new[] { "Flax" },
            descriptionBefore = "We hunted these animals and didn’t keep them near the settlement.",
            descriptionAfter  = "We began breeding pigs, gaining a stable source of meat.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Meat (produced)", "Meat", 150),
            }
        },
        new ResearchDef
        {
            id = "Goat",
            displayName = "Goats",
            // goatIcon,
            gridPosition = new Vector2(9, 4),
            prerequisites = new[] { "Pig" },
            descriptionBefore = "We met goats in the wild and they were only occasional prey.",
            descriptionAfter  = "We domesticated goats and began getting milk, meat, and hides from them.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Milk (produced)", "Milk", 150),
            }
        },
        new ResearchDef
        {
            id = "Cattle",
            displayName = "Cattle",
            // cattleIcon,
            gridPosition = new Vector2(10, 4),
            prerequisites = new[] { "Goat" },
            descriptionBefore = "Large animals were prey.",
            descriptionAfter  = "We began raising cattle, gaining meat and milk.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Meat (produced)", "Meat", 150),
            }
        },
        new ResearchDef
        {
            id = "Bee",
            displayName = "Bees",
            // beeIcon,
            gridPosition = new Vector2(7, 5),
            prerequisites = new[] { "Flax" },
            descriptionBefore = "We found hives in the wild and tried not to disturb them without need.",
            descriptionAfter  = "We learned to keep bees and obtain honey and wax as useful resources.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 25),
                ProducedReq("Wood (produced)", "Wood", 150),
            }
        },
        new ResearchDef
        {
            id = "Candle",
            displayName = "Candles",
            // candleIcon,
            gridPosition = new Vector2(7, 6),
            prerequisites = new[] { "Bee" },
            descriptionBefore = "After sunset, darkness limited work and daily life.",
            descriptionAfter  = "We began making candles from wax and gained a reliable source of light.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Wax (produced)", "Wax", 150),
            }
        },
        new ResearchDef
        {
            id = "Soap",
            displayName = "Soap",
            // soapIcon,
            gridPosition = new Vector2(8, 5),
            prerequisites = new[] { "Pig" },
            descriptionBefore = "Cleanliness was maintained with water, but that was often not enough.",
            descriptionAfter  = "We learned to make soap and improved hygiene and people’s health.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 30),
                ProducedReq("Fat (produced)", "Fat", 150),
            }
        },
        new ResearchDef
        {
            id = "Chicken",
            displayName = "Chickens",
            // chickenIcon,
            gridPosition = new Vector2(9, 5),
            prerequisites = new[] { "Goat" },
            descriptionBefore = "We caught these birds in the wild when we could.",
            descriptionAfter  = "We began raising chickens and gained a constant source of meat and eggs.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 40),
                ProducedReq("Meat (produced)", "Meat", 250),
            }
        },
        new ResearchDef
        {
            id = "Plough",
            displayName = "Plough",
            // ploughIcon,
            gridPosition = new Vector2(10, 5),
            prerequisites = new[] { "Cattle" },
            descriptionBefore = "Fields were worked by hand, and preparing the soil took a lot of time and effort.",
            descriptionAfter  = "We created the plough, and the soil yielded faster and deeper, increasing harvests.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 45),
                ProducedReq("Tools (produced)", "Tools", 350),
            }
        },
        new ResearchDef
        {
            id = "Farm3",
            displayName = "Farms III",
            // farm3Icon,
            gridPosition = new Vector2(10, 6),
            prerequisites = new[] { "Plough" },
            descriptionBefore = "Farms relied on manual labor and produced limited yields.",
            descriptionAfter  = "Using the plough made farms more productive, but required more resources and maintenance.",
            requirements = new[]
            {
                MoodReq(81),
                HousesStageReq(3, 50),
                ProducedReq("Wheat (produced)", "Wheat", 200),
            }
        },
        new ResearchDef
        {
            id = "PotteryWheel",
            displayName = "Pottery Wheel",
            // potteryWheelIcon,
            gridPosition = new Vector2(5, 3),
            prerequisites = new[] { "Market" },
            descriptionBefore = "Vessels were shaped by hand, and every form was different.",
            descriptionAfter  = "We invented the potter’s wheel and learned to give clay smooth, precise shapes.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Pottery (produced)", "Pottery", 120),
            }
        },
        new ResearchDef
        {
            id = "Pottery2",
            displayName = "Pottery 2",
            // pottery2Icon,
            gridPosition = new Vector2(5, 2),
            prerequisites = new[] { "PotteryWheel" },
            descriptionBefore = "Pottery was made slowly and in small quantities.",
            descriptionAfter  = "The potter’s wheel sped up production and allowed us to make more durable pottery.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Pottery (produced)", "Pottery", 120),
            }
        },
        new ResearchDef
        {
            id = "Clay2",
            displayName = "Clay 2",
            // clay2Icon,
            gridPosition = new Vector2(2, 8),
            prerequisites = new[] { "BerryHut2" },
            descriptionBefore = "Working with clay took a lot of time and effort.",
            descriptionAfter  = "We began using tools to добывать clay and made production faster.",
            requirements = new[]
            {
                MoodReq(81),
                ProducedReq("Clay (produced)", "Clay", 350),
            }
        },

        new ResearchDef
        {
            id = "Furniture2",
            displayName = "Furniture2",
            // furniture2Icon,
            gridPosition = new Vector2(9, 2),
            prerequisites = new[] { "Clothes2" },
            descriptionBefore = "Furniture was simple and uncomfortable, made only of wood.",
            descriptionAfter  = "We began using leather and wool, making furniture softer, more comfortable, and longer-lasting.",
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
            descriptionBefore = "Producing dairy products required a lot of manual labor and was slow.",
            descriptionAfter  = "We began using tools, speeding up milk processing and increasing production volumes.",
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
            descriptionBefore = "Grains were ground by hand between stones, and it took a lot of time.",
            descriptionAfter  = "We created querns and made grain grinding faster and more even.",
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
            descriptionBefore = "Flour was produced in small amounts and with great effort.",
            descriptionAfter  = "Querns allowed producing flour faster and in greater volume.",
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
            descriptionBefore = "Baking was slow and limited by simple ovens and small volumes.",
            descriptionAfter  = "We improved bakeries and began baking more bread faster and more consistently.",
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
            descriptionBefore = "Beer production was small and depended on manual labor.",
            descriptionAfter  = "We improved breweries and established regular, larger-scale production.",
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
            descriptionBefore = "Charcoal was produced slowly and with significant wood losses.",
            descriptionAfter  = "We improved the firing process and began producing more charcoal with less cost.",
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
            descriptionBefore = "Metal was used rarely and mostly for tools.",
            descriptionAfter  = "We built a smithy and began making metal goods for daily life, making life more comfortable and durable.",
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
            descriptionBefore = "Wood cutting still required a lot of manual labor and time.",
            descriptionAfter  = "We improved lumber mills and significantly increased the speed and volume of wood processing.",
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
            descriptionBefore = "Grinding grain depended on people’s strength and was slow.",
            descriptionAfter  = "We used animal power to rotate the mill and drastically sped up flour production.", //todo rewrite
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
            descriptionBefore = "Even with mills, flour production was limited by available power and time.",
            descriptionAfter  = "We used animal power to rotate the mill and drastically sped up flour production.",
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
            descriptionBefore = "Herbs were gathered randomly and used without any system.",
            descriptionAfter  = "We began growing and using herbs as seasonings and beneficial food additives.",
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
            descriptionBefore = "Illnesses were treated by guesswork, relying on experience and luck.",
            descriptionAfter  = "We set aside healers and doctors, improving health and survival.",
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
            descriptionBefore = "We did not know what it was and did not drink seawater.",
            descriptionAfter  = "We began evaporating salt from seawater. It makes food taste better.",
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
            descriptionBefore = "Fabrics wore out faster, and dyes and fibers did not keep well over time.",
            descriptionAfter  = "We began using salt while processing fibers, making fabrics stronger and longer-lasting.",
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
            descriptionBefore = "Leather spoiled and lost quality during storage and processing.",
            descriptionAfter  = "We began using salt for tanning, greatly improving leather’s strength and lifespan.",
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
            descriptionBefore = "Meat spoiled quickly and had to be eaten immediately.",
            descriptionAfter  = "We began using salt to preserve meat and could store it longer.",
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
            descriptionBefore = "Dairy products did not keep long and often went bad.",
            descriptionAfter  = "Salt made it possible to preserve cheese and other dairy products longer, increasing food stores.",
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
            descriptionBefore = "Hunting provided a lot of meat, but excess spoiled quickly and was wasted.",
            descriptionAfter  = "We began using salt to preserve meat, making hunting more profitable and stable.",
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
            descriptionBefore = "Animal husbandry provided meat that had to be used right away.",
            descriptionAfter  = "Salt made it possible to preserve meat longer and use animal products more efficiently.",
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
            descriptionBefore = "Fruits were gathered in the wild.",
            descriptionAfter  = "We began cultivating fruit plants and gained another stable source of food.",
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
            descriptionBefore = "Sand held no value for us and lay unused.",
            descriptionAfter  = "We began using sand as a material for construction, crafts, and new mixtures.",
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
            descriptionBefore = "Ash remained after campfires and was considered a useless residue.",
            descriptionAfter  = "We found a use for it in daily life and production, turning fire’s waste into a useful resource.",
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
            descriptionBefore = "Soap was made from limited ingredients, and there wasn’t always enough of it.",
            descriptionAfter  = "We began using ash, improving soap production and making it more accessible.",
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
            descriptionBefore = "Sand and ash were used separately and gave no new possibilities.",
            descriptionAfter  = "We learned to melt sand with ash and obtained glass.",
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
            descriptionBefore = "Pottery was useful, but remained porous and limited in form and use.",
            descriptionAfter  = "We began using glass together with ceramics, making vessels stronger, more beautiful, and more varied.",
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
            descriptionBefore = "Ash was not used on fields.",
            descriptionAfter  = "We began fertilizing the soil with ash, improving fertility and farm yields.",
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
            descriptionBefore = "Vegetables were gathered rarely and were not cultivated intentionally.",
            descriptionAfter  = "We began growing vegetables in fields and gained varied, sustainable food.",
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
            descriptionBefore = "Wild grapes were found by chance and used immediately.",
            descriptionAfter  = "We began cultivating grapes and obtaining a steady fruit harvest.",
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
            descriptionBefore = "Grapes spoiled quickly and did not keep long.",
            descriptionAfter  = "We learned to make wine, turning fruit into a drink that stores well and is valued.",
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
            descriptionBefore = "Gold was rare and had no practical use.",
            descriptionAfter  = "We began valuing gold for its rarity and using it as a sign of wealth.",
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
            descriptionBefore = "Trade was direct, and the value of goods often caused disputes.",
            descriptionAfter  = "We introduced money as a measure of value, simplifying trade and settlements.",
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
            descriptionBefore = "Gold was used only as decoration and a reserve.",
            descriptionAfter  = "We built smithies for minting coins and made money part of the economy.",
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
            descriptionBefore = "Jewelry was made from simple materials and had little value.",
            descriptionAfter  = "We began creating jewelry from precious metals and stones.",
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
            descriptionBefore = "People bathed rarely, and cleanliness depended on rivers and weather.",
            descriptionAfter  = "We built bathhouses, improving hygiene, health, and social life.",
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
            descriptionBefore = "Homes were comfortable, but people’s possibilities remained limited.",
            descriptionAfter  = "We built comfortable homes, increasing comfort.",
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
        
        TutorialEvents.RaiseResearchCompleted();

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

                int curPercent = (total <= 0) ? 0 : (ok * 100) / total;
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
                parts.Add("<color=#00ff00ff>Researched</color>");
            else if (node.IsAvailable)
                parts.Add("<color=#ffff00ff>Ready to research</color>");
            else
                parts.Add("<color=#ff8080ff>Unavaliable</color>");
        }

   

        var eval = EvaluateResearch(researchId);

        if (eval.Lines == null || eval.Lines.Count == 0)
        {
            parts.Add("No requirements.");
            return string.Join("\n", parts);
        }

        foreach (var line in eval.Lines)
        {
            bool met = line.Cur >= line.Need;

            // если debug-режим — всегда белым (или можно серым)
            string col = disableResearchRequirements ? "white" : (met ? "white" : "red");

            parts.Add($"{line.Label}: <color={col}>{line.Cur}/{line.Need}</color>");
        }

        bool completed = nodes.TryGetValue(researchId, out var n) && n.IsCompleted;

        string desc = BuildResearchDescription(researchId, completed);
        if (!string.IsNullOrEmpty(desc))
        {
            parts.Add(desc);
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
    
    private string BuildResearchDescription(string researchId, bool isCompleted)
    {
        var def = FindDef(researchId);
        if (def == null) return "";

        // Если описания нет — просто ничего не добавляем
        string text = isCompleted ? def.descriptionAfter : def.descriptionBefore;
        if (string.IsNullOrWhiteSpace(text)) return "";

        // Небольшой заголовок секции
        return $"<color=#a0a0a0ff>{text}</color>";
    }


}
