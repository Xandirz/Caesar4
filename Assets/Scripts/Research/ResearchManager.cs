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

    // все созданные ноды
    private readonly Dictionary<string, ResearchNode> nodes = new();

    // mood (0..100)
    private int lastKnownMood = 0;

    // кумулятивное произведённое количество ресурсов
    private readonly Dictionary<string, int> producedTotals = new();

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

            // склад по ресерчу (если хочешь, чтобы Warehouse тоже был закрыт в начале)
            { "Warehouse", new List<BuildManager.BuildMode> { BuildManager.BuildMode.Warehouse } },
        };

    
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
        RefreshFogOfWar();   // ← добавили

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
            prerequisites = new [] { "Wheat" }     // wheat  ─► brewery
        },
        new ResearchDef
        {
            id = "Wheat",
            displayName = "Пшеница",
            icon = wheatIcon,
            gridPosition = new Vector2(4, 1),
            prerequisites = new [] { "Stage2"} // stage2 ─► wheat
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



  /// <summary>
  /// Видно ли игроку "настоящую" ноду (иконка и описание),
  /// или она должна быть скрыта под вопросами.
  /// </summary>
  private bool IsResearchRevealed(string researchId)
  {
      if (!nodes.TryGetValue(researchId, out var node))
          return false;

      // Если уже изучено — всегда видно
      if (node.IsCompleted)
          return true;

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
          return true; // на всякий случай не скрываем, если что-то пошло не так

      // Ноды без пререквизитов — видны сразу (корни дерева)
      if (def.prerequisites == null || def.prerequisites.Length == 0)
          return true;

      // Видна, если ХОТЯ БЫ ОДИН её пререквизит уже изучен
      foreach (var preId in def.prerequisites)
      {
          if (string.IsNullOrEmpty(preId)) continue;
          if (nodes.TryGetValue(preId, out var preNode) && preNode.IsCompleted)
              return true;
      }

      // Иначе — это дальше, чем "один шаг вперёд"
      return false;
  }
  
  /// <summary>
  /// Обновляет иконки нод в зависимости от того, раскрыты они или нет.
  /// </summary>
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
              // показываем настоящую иконку
              node.SetIcon(def.icon);
          }
          else
          {
              // скрыта: ставим иконку "?"
              node.SetIcon(unknownIcon);
          }
      }
  }

  
  

    private void UnlockBuildingsForResearch(string researchId)
    {
        if (BuildManager.Instance == null) return;
        if (!researchUnlocks.TryGetValue(researchId, out var list)) return;

        foreach (var mode in list)
        {
            BuildManager.Instance.UnlockBuilding(mode);
        }
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

            var rt = (RectTransform) nodeGO.transform;


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
            // если вдруг linesRoot не задан, по старинке спавним под nodesRoot
            Transform parentForLines = linesRoot != null ? (Transform) linesRoot : nodesRoot;

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
                    line.Connect((RectTransform) fromNode.transform, (RectTransform) toNode.transform);
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

    private bool IsNodeHidden(ResearchDef def)
    {
        // Нода скрыта, если ни один её пререквизит не завершён
        if (def.prerequisites == null || def.prerequisites.Length == 0)
            return false; // корневые ноды никогда не скрыты

        foreach (var pre in def.prerequisites)
        {
            if (nodes.TryGetValue(pre, out var preNode) && preNode.IsCompleted)
                return false;
        }

        return true;
    }

    
    private void CompleteResearch(string id)
    {
        if (!nodes.TryGetValue(id, out var node)) return;

        // помечаем исследование завершённым
        node.SetState(available: false, completed: true);
        Debug.Log($"Research completed: {id}");

        // 👉 здесь разблокируем здания, привязанные к этому исследованию
        UnlockBuildingsForResearch(id);

        // пересчитываем доступность остальных нод
        RefreshAvailability();
        RefreshFogOfWar(); 
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

    if (def == null)
        return "???";

    // ====== ПРОВЕРКА "ТУМАНА ВОЙНЫ" ======
    if (IsNodeHidden(def))
    {
        return "<b>???</b>\n???\n???\n???";
    }

    // ====== ЕСЛИ НОДА ВИДИМА — ПОКАЗЫВАЕМ НАСТОЯЩИЕ ДАННЫЕ ======
    string name = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;

    var parts = new System.Collections.Generic.List<string>();

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

    // Условия
    switch (researchId)
    {
        case "Clay":
            {
                int requiredHouses = 10;
                int curHouses = CountAllHouses();
                string col = curHouses >= requiredHouses ? "white" : "red";
                parts.Add($"Дома: <color={col}>{curHouses}/{requiredHouses}</color>");

                int requiredMood = 81;
                string moodCol = lastKnownMood >= requiredMood ? "white" : "red";
                parts.Add($"Настроение: <color={moodCol}>{lastKnownMood}/{requiredMood}</color>");
                break;
            }

        case "Pottery":
            {
                int requiredClay = 10;
                int haveClay = producedTotals.TryGetValue("Clay", out var v) ? v : 0;
                if (haveClay > requiredClay) haveClay = requiredClay;
                string clayCol = haveClay >= requiredClay ? "white" : "red";
                parts.Add($"Глина (произведено): <color={clayCol}>{haveClay}/{requiredClay}</color>");

                int requiredMood = 81;
                string moodCol = lastKnownMood >= requiredMood ? "white" : "red";
                parts.Add($"Настроение: <color={moodCol}>{lastKnownMood}/{requiredMood}</color>");

                break;
            }

        default:
            parts.Add("Нет специальных требований.");
            break;
    }

    return string.Join("\n", parts);
}


}
