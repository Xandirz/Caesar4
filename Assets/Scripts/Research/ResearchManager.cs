using UnityEngine;
using System;
using System.Collections.Generic;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance;

    [Header("UI")]
    public ResearchUI researchUI;

    // ====== Вся «библиотека» открытий: тексты после открытия ======
    private readonly Dictionary<string, string> discoveryTexts = new()
    {
        { "Clay",       "Пока копали землю у реки, вы нашли мягкий липкий материал, который твердеет на солнце." },
        { "Pottery",    "Из обожжённой глины получаются прочные сосуды. Теперь можно хранить еду и воду дольше." },
        { "Meat",       "Охота даёт мясо — оно питательнее ягод и кореньев." },
        { "Bone",       "Кости от добычи — отличный материал для игл и наконечников." },
        { "Hide",       "Научились выделывать шкуры — тёплый и прочный материал для одежды и укрытий." },
        { "Tools",      "Простые орудия труда ускоряют любую работу — эпоха инструментов началась." },
        { "Crafts",     "Люди создают не только полезное, но и красивое — ремёсла рождают культуру." },
        { "Needles",    "Иглы из кости — теперь можно сшивать шкуры и ткань." },
        { "Sheep",      "Приручены овцы — шерсть, мясо и молоко под рукой." },
        { "Wool",       "Научились прясть шерсть и ткать — рождается тёплая ткань." },
        { "Milk",       "Молоко — питательный продукт, спасает от голода." },
        { "Cheese",     "Свернув молоко, получили сыр — сытный и хранится дольше." },
        { "Yogurt",     "Кисломолочный напиток получается сам — дольше не портится." },
        { "Cloth",      "Из нитей соткали первую ткань — основа одежды." },
        { "Clothes",    "Появилась одежда — меньше мёрзнем, можно жить в холодных землях." },
        { "Beans",      "Дикие бобы — питательны и легко растут рядом с домом." },
        { "Beer",       "Случайное брожение зёрен — появился бодрящий напиток." },
        { "Wheat",      "Посеяное зерно даёт новые колосья — начало земледелия." },
        { "Flour",      "Размолов зерно, получили муку — основу лепёшек и хлеба." },
        { "Bread",      "Испекли хлеб — ароматный и сытный." },
        { "Furniture",  "Стулья и столы из дерева — жить стало удобнее." },
        { "Coal",       "В углях костра нашли топливо, что горит дольше дров." },
        { "CopperOre",  "Зелёный блеск в камне — это медная руда." },
        { "Copper",     "Расплавив руду, получили первый металл — мягкий, но прочный." }
    };

    // ====== Описание узла исследования (всё задаётся здесь, в одном месте) ======
    private class Node
    {
        public string id;
        public string requirementText;
        public Func<bool> condition;
        public string nextId;
        public bool isCompleted;
        public bool isAvailableShown; // уже показали кнопку OK в UI

        public Node(string id, string requirementText, Func<bool> condition, string nextId)
        {
            this.id = id;
            this.requirementText = requirementText;
            this.condition = condition;
            this.nextId = nextId;
        }
    }

    private readonly Dictionary<string, Node> nodes = new();
    private string firstId = "Clay";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        BuildChain();                // 1) задаём все исследования + условия
        researchUI.Initialize(this); // 2) даём UI ссылку на менеджер (чтобы передавать колбэки)
        researchUI.AddRow(firstId, nodes[firstId].requirementText); // 3) рисуем первую строку
    }

    void Update()
    {
        // Периодически проверяем выполнение условий и включаем кнопки OK в UI
        foreach (var kv in nodes)
        {
            var n = kv.Value;
            if (n.isCompleted || n.condition == null || n.isAvailableShown) continue;

            if (n.condition.Invoke())
            {
                n.isAvailableShown = true;
                researchUI.SetAvailable(n.id, true);
            }
        }
    }

    // ====== Единая точка задания всей цепочки исследований ======
 private void BuildChain()
{
    nodes.Clear();

    // локальные шорткаты
    int R(string res) => ResourceManager.Instance != null ? ResourceManager.Instance.GetResource(res) : 0;
    int C(BuildManager.BuildMode m) => AllBuildingsManager.Instance != null ? AllBuildingsManager.Instance.GetBuildingCount(m) : 0;

    // === ЦЕПОЧКА ИССЛЕДОВАНИЙ ===

    // 1. Clay — строим 10 домов
    Add("Clay", "Постройте 10 домов",
        () => C(BuildManager.BuildMode.House) >= 10,
        "Pottery");

    // 2. Pottery — накопить 20 глины
    Add("Pottery", "Накопите 20 глины (Clay)",
        () => R("Clay") >= 20,
        "Tools");

    // 3. Tools — накопить 10 камня
    Add("Tools", "Накопите 10 камня (Rock)",
        () => R("Rock") >= 10,
        "Hunter");

    // 4. Meat — накопить 10 ягод
    Add("Hunter", "Накопите 10 ягод (Berry)",
        () => R("Berry") >= 10,
        "Warehouse");

    // 5. Warehouse — построить 20 домов
    Add("Warehouse", "Постройте 30 домов",
        () => C(BuildManager.BuildMode.House) >= 30,
        "Crafts");

    // 6. Crafts — произвести 10 костей
    Add("Crafts", "Создайте 10 костей (Bone)",
        () => R("Bone") >= 10,
        "Wheat");

    // 7. Wheat — построить 30 домов
    Add("Wheat", "Постройте 30 домов",
        () => C(BuildManager.BuildMode.House) >= 30,
        "Flour");

    // 8. Flour — накопить 10 пшеницы
    Add("Flour", "Накопите 10 пшеницы (Wheat)",
        () => R("Wheat") >= 10,
        "Bakery");

    // 9. Bread — накопить 10 муки
    Add("Bakery", "Накопите 10 муки (Flour)",
        () => R("Flour") >= 10,
        "Sheep");

    // 10. Sheep — накопить 10 хлеба
    Add("Sheep", "Накопите 10 хлеба (Bread)",
        () => R("Bread") >= 10,
        "Dairy");

    // 11. Dairy — накопить 10 овец
    Add("Dairy", "Накопите 10 овечьих ресурсов (Sheep)",
        () => R("Sheep") >= 10,
        "Weaver");

    // 12. Weaver — накопить 10 шерсти
    Add("Weaver", "Накопите 10 шерсти (Wool)",
        () => R("Wool") >= 10,
        "Clothes");

    // 13. Clothes — накопить 10 ткани
    Add("Clothes", "Накопите 10 ткани (Cloth)",
        () => R("Cloth") >= 10,
        "Market");

    // 14. Market — накопить 10 одежды
    Add("Market", "Накопите 10 одежды (Clothes)",
        () => R("Clothes") >= 10,
        "Furniture");

    // 15. Furniture — построить 1 рынок
    Add("Furniture", "Постройте 1 рынок (Market)",
        () => C(BuildManager.BuildMode.Market) >= 1,
        "Beans");

    // 16. Beans — накопить 10 пшеницы
    Add("Beans", "Накопите 10 пшеницы (Wheat)",
        () => R("Wheat") >= 10,
        "Brewery");

    // 17. Brewery — накопить 10 хлеба
    Add("Brewery", "Накопите 10 хлеба (Bread)",
        () => R("Bread") >= 10,
        "Coal");

    // 18. Coal — накопить 10 дерева
    Add("Coal", "Накопите 10 дерева (Wood)",
        () => R("Wood") >= 10,
        null);
}


    private void Add(string id, string reqText, Func<bool> cond, string nextId)
    {
        nodes[id] = new Node(id, reqText, cond, nextId);
    }

    // ====== Вызывается UI при нажатии OK на строке ======
    public void CompleteResearch(string id)
    {
        if (!nodes.ContainsKey(id)) return;
        var node = nodes[id];
        if (node.isCompleted) return;

        node.isCompleted = true;

        // 1) Анлок здания с таким же именем, если есть соответствующий BuildMode
        if (System.Enum.TryParse(id, out BuildManager.BuildMode mode))
            BuildManager.Instance.UnlockBuilding(mode);

        // 2) Показать «описание открытия» в строке
        string discovery = discoveryTexts.TryGetValue(id, out var txt) ? txt : id + " discovered.";
        researchUI.SetCompleted(id, discovery);

        // 3) Добавить следующую строку (если есть)
        if (!string.IsNullOrEmpty(node.nextId) && nodes.ContainsKey(node.nextId))
        {
            var next = nodes[node.nextId];
            researchUI.AddRow(next.id, next.requirementText);

            // если условие уже выполнено — сразу подсветить OK
            if (next.condition != null && next.condition.Invoke())
            {
                next.isAvailableShown = true;
                researchUI.SetAvailable(next.id, true);
            }
        }
    }

    // === Минимальный API для UI ===
    public bool HasNode(string id) => nodes.ContainsKey(id);
    public string GetRequirement(string id) => nodes.TryGetValue(id, out var n) ? n.requirementText : "";
}
