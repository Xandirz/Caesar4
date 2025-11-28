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

    private const string ClayId = "Clay";
    private const string PotteryId = "Pottery";

    [Header("Иконки исследований")]
    [SerializeField] private Sprite clayIcon;
    [SerializeField] private Sprite potteryIcon;
    [SerializeField] private Sprite toolsIcon;
    [SerializeField] private Sprite hunterIcon;
    [SerializeField] private Sprite craftsIcon;

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
    [SerializeField] private float cellSize = 50f;    // расстояние между нодами по сетке

    private ResearchDef[] definitions;

    // все созданные ноды
    private readonly Dictionary<string, ResearchNode> nodes = new();

    // mood (0..100)
    private int lastKnownMood = 0;

    // кумулятивное произведённое количество ресурсов
    private readonly Dictionary<string, int> producedTotals = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        BuildDefinitions();   // описываем Clay -> Pottery
        BuildTree();          // создаём ноды и линии
        RefreshAvailability();// выставляем доступность
    }

    // ---------------------------------------------------------------------
    // ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ВЫЗОВА ИЗ ИГРЫ
    // ---------------------------------------------------------------------

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

    // ---------------------------------------------------------------------
    // ОПИСАНИЕ ДЕРЕВА (две ноды: Clay -> Pottery)
    // ---------------------------------------------------------------------

    private void BuildDefinitions()
{
    definitions = new ResearchDef[]
    {
        // ---------- CLAY BRANCH ----------
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

        // ---------- TOOLS → HUNTER → CRAFTS ----------
        new ResearchDef
        {
            id = "Tools",
            displayName = "Инструменты",
            icon = toolsIcon,
            gridPosition = new Vector2(0, -1),
            prerequisites = Array.Empty<string>()
        },
        new ResearchDef
        {
            id = "Hunter",
            displayName = "Охота",
            icon = hunterIcon,
            gridPosition = new Vector2(1, -1),
            prerequisites = new [] { "Tools" }
        },
        new ResearchDef
        {
            id = "Crafts",
            displayName = "Ремесло",
            icon = craftsIcon,
            gridPosition = new Vector2(2, -1),
            prerequisites = new [] { "Hunter" }
        },

        // ---------- WHEAT MAIN ----------
        new ResearchDef
        {
            id = "Wheat",
            displayName = "Пшеница",
            icon = wheatIcon,
            gridPosition = new Vector2(0, -2),
            prerequisites = Array.Empty<string>()
        },
        new ResearchDef
        {
            id = "Flour",
            displayName = "Мука",
            icon = flourIcon,
            gridPosition = new Vector2(1, -2),
            prerequisites = new [] { "Wheat" }
        },
        new ResearchDef
        {
            id = "Bakery",
            displayName = "Пекарня",
            icon = bakeryIcon,
            gridPosition = new Vector2(2, -2),
            prerequisites = new [] { "Flour" }
        },

        // ---------- WHEAT SECONDARY BRANCH (Sheep ...) ----------
        new ResearchDef
        {
            id = "Sheep",
            displayName = "Овцы",
            icon = sheepIcon,
            gridPosition = new Vector2(2, -3),
            prerequisites = new [] { "Wheat" }
        },
        new ResearchDef
        {
            id = "Dairy",
            displayName = "Молочная",
            icon = dairyIcon,
            gridPosition = new Vector2(3, -3),
            prerequisites = new [] { "Sheep" }
        },
        new ResearchDef
        {
            id = "Weaver",
            displayName = "Ткачество",
            icon = weaverIcon,
            gridPosition = new Vector2(4, -3),
            prerequisites = new [] { "Dairy" }
        },
        new ResearchDef
        {
            id = "Clothes",
            displayName = "Одежда",
            icon = clothesIcon,
            gridPosition = new Vector2(5, -3),
            prerequisites = new [] { "Weaver" }
        },
        new ResearchDef
        {
            id = "Market",
            displayName = "Рынок",
            icon = marketIcon,
            gridPosition = new Vector2(6, -3),
            prerequisites = new [] { "Clothes" }
        },
        new ResearchDef
        {
            id = "Furniture",
            displayName = "Мебель",
            icon = furnitureIcon,
            gridPosition = new Vector2(7, -3),
            prerequisites = new [] { "Market" }
        },

        // ---------- WHEAT: BREWERY ----------
        new ResearchDef
        {
            id = "Brewery",
            displayName = "Пивоварня",
            icon = breweryIcon,
            gridPosition = new Vector2(1, -4),
            prerequisites = new [] { "Wheat" }
        },

        // ---------- STANDALONE ----------
        new ResearchDef
        {
            id = "Coal",
            displayName = "Уголь",
            icon = coalIcon,
            gridPosition = new Vector2(0, -5),
            prerequisites = Array.Empty<string>()
        },
        new ResearchDef
        {
            id = "Beans",
            displayName = "Бобы",
            icon = beansIcon,
            gridPosition = new Vector2(0, -6),
            prerequisites = Array.Empty<string>()
        },
    };
}


    // ---------------------------------------------------------------------
    // СОЗДАНИЕ НОД И ЛИНИЙ
    // ---------------------------------------------------------------------

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

        // 2) Линии так же остаются, они используют anchoredPosition нод — им пофиг, где ноль
        if (linePrefab != null)
        {
            foreach (var def in definitions)
            {
                if (def.prerequisites == null) continue;

                foreach (var preId in def.prerequisites)
                {
                    if (string.IsNullOrEmpty(preId)) continue;
                    if (!nodes.TryGetValue(preId, out var fromNode)) continue;
                    if (!nodes.TryGetValue(def.id, out var toNode)) continue;

                    var line = Instantiate(linePrefab, nodesRoot);
                    line.name = $"Line_{preId}_to_{def.id}";
                    line.Connect((RectTransform)fromNode.transform, (RectTransform)toNode.transform);
                }
            }
        }
    }


    // ---------------------------------------------------------------------
    // КЛИК ПО НОДЕ
    // ---------------------------------------------------------------------

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

        // по желанию: анлок зданий, эффект ресерча и т.д.

        RefreshAvailability();
    }

    // ---------------------------------------------------------------------
    // ДОСТУПНОСТЬ НОД
    // ---------------------------------------------------------------------

    private void RefreshAvailability()
    {
        if (definitions == null) return;

        // сначала всем выключаем доступность (completed не трогаем)
        foreach (var kv in nodes)
        {
            var node = kv.Value;
            if (!node.IsCompleted)
                node.SetState(available: false, completed: false);
        }

        // теперь включаем доступность для тех, у кого выполнены пререквизиты и условия
        foreach (var def in definitions)
        {
            if (!nodes.TryGetValue(def.id, out var node)) continue;
            if (node.IsCompleted) continue;

            // 1) проверяем пререквизиты по другим исследованиям
            bool prereqOk = true;
            if (def.prerequisites != null && def.prerequisites.Length > 0)
            {
                foreach (var preId in def.prerequisites)
                {
                    if (string.IsNullOrEmpty(preId)) continue;
                    if (!nodes.TryGetValue(preId, out var preNode) || !preNode.IsCompleted)
                    {
                        prereqOk = false;
                        break;
                    }
                }
            }

            if (!prereqOk)
                continue;

            // 2) проверяем игровые условия (mood, дома, ресурсы)
            if (AreGameConditionsMet(def.id))
            {
                node.SetState(available: true, completed: false);
            }
        }
    }

    // ---------------------------------------------------------------------
    // УСЛОВИЯ ДЛЯ КАЖДОГО ИССЛЕДОВАНИЯ
    // ---------------------------------------------------------------------
    //
    // Clay:
    //   - 10 домов
    //   - mood > 80
    //
    // Pottery:
    //   - произведено 10 "Clay"
    //   - mood > 80
    //
    // при желании сюда же добавишь и остальные исследования позже

    private bool AreGameConditionsMet(string researchId)
    {
        switch (researchId)
        {
            case ClayId:
                {
                    // mood > 80
                    if (lastKnownMood <= 80) return false;

                    // 10 домов (любой стадии)
                    int housesCount = CountAllHouses();
                    return housesCount >= 10;
                }

            case PotteryId:
                {
                    // mood > 80
                    if (lastKnownMood <= 80) return false;

                    // произведено 10 "Clay"
                    int haveClay = producedTotals.TryGetValue(ClayId, out var v) ? v : 0;
                    return haveClay >= 10;
                }

            default:
                // для остальных (если появятся) по умолчанию — нет условий
                return true;
        }
    }

    // Подсчёт всех домов (House) на карте
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

    string name = def != null && !string.IsNullOrEmpty(def.displayName)
        ? def.displayName
        : researchId;

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

    // Условия / прогресс
    switch (researchId)
    {
        case ClayId:
            {
                // Дома
                int requiredHouses = 10;
                int curHouses = CountAllHouses();
                string col = curHouses >= requiredHouses ? "white" : "red";
                parts.Add($"Дома: <color={col}>{curHouses}/{requiredHouses}</color>");

                // Mood
                int requiredMood = 81; // >80
                string moodCol = lastKnownMood >= requiredMood ? "white" : "red";
                parts.Add($"Настроение: <color={moodCol}>{lastKnownMood}/{requiredMood}</color>");

                break;
            }

        case PotteryId:
            {
                // Глина
                int requiredClay = 10;
                int haveClay = producedTotals.TryGetValue(ClayId, out var v) ? v : 0;
                if (haveClay > requiredClay) haveClay = requiredClay;
                string clayCol = haveClay >= requiredClay ? "white" : "red";
                parts.Add($"Глина (произведено): <color={clayCol}>{haveClay}/{requiredClay}</color>");

                // Mood
                int requiredMood = 81;
                string moodCol = lastKnownMood >= requiredMood ? "white" : "red";
                parts.Add($"Настроение: <color={moodCol}>{lastKnownMood}/{requiredMood}</color>");

                // Пререквизит: Clay
                if (nodes.TryGetValue(ClayId, out var clayNode))
                {
                    string prereqCol = clayNode.IsCompleted ? "white" : "red";
                    string status = clayNode.IsCompleted ? "исследовано" : "не исследовано";
                    parts.Add($"Требует: <color={prereqCol}>Глина ({status})</color>");
                }

                break;
            }

        default:
            parts.Add("Нет специальных требований.");
            break;
    }

    return string.Join("\n", parts);
}

}
