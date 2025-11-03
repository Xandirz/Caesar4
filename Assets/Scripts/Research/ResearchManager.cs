using UnityEngine;
using System;
using System.Collections.Generic;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance;

    [Header("UI")]
    public ResearchUI researchUI;

    [Header("Research Points")]
    [SerializeField] private int moodThreshold = 80;
    [SerializeField] private int pointsPerGoodDay = 1;

    // --- тексты открытий ---
    private readonly Dictionary<string, string> discoveryTexts = new()
    {
        { "Clay", "Пока копали землю у реки, вы нашли мягкий липкий материал, который твердеет на солнце." },
        { "Pottery", "Из обожжённой глины получаются прочные сосуды. Теперь можно хранить еду и воду дольше." },
        { "Tools", "Простые орудия труда ускоряют любую работу — эпоха инструментов началась." },
        { "Hunter", "Охота — вы добываете мясо, которое питательнее ягод и кореньев." },
        { "Warehouse", "Склад — теперь можно хранить больше припасов и навести порядок в поставках." },
        { "Crafts", "Ремесло — люди начали создавать не только нужное, но и красивое." },
        { "Wheat", "Посеянное зерно даёт новые колосья — начало земледелия." },
        { "Flour", "Размолов зерно, получили муку — основу лепёшек и хлеба." },
        { "Bakery", "Пекарня — мука превращается в ароматный хлеб." },
        { "Sheep", "Приручены овцы — шерсть, мясо и молоко под рукой." },
        { "Dairy", "Молочная — молоко, сыр и йогурт становятся доступнее." },
        { "Weaver", "Ткачество — из шерсти получаем ткань." },
        { "Clothes", "Одежда — люди меньше мёрзнут и могут жить в холодных землях." },
        { "Market", "Рынок — обмен товарами стал проще, город ожил." },
        { "Furniture", "Мебель — стулья и столы делают жизнь удобнее." },
        { "Beans", "Бобы — питательны и хорошо растут рядом с домом." },
        { "Brewery", "Пивоварня — из хлеба и зерна рождается бодрящий напиток." },
        { "Coal", "Уголь — топливо, которое горит дольше обычных дров." }
    };

    // --- структура одного исследования ---
    private class Node
    {
        public string id;
        public int cost;
        public string nextId;
        public string prevId;
        public bool completed;
        public bool rowCreated;

        public Node(string id, int cost, string nextId)
        {
            this.id = id;
            this.cost = cost;
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
        BuildChain();
        LinkPrevPointers();

        if (researchUI != null)
        {
            researchUI.Initialize(this);
            EnsureRowCreated(firstId);
        }

        RefreshAvailability();
    }

    // === начисляем Research Points за хороший день ===
    public void OnDayPassed(int mood)
    {
        // mood — это проценты 0..100
        if (mood >= moodThreshold)
        {
            AddRP(pointsPerGoodDay);
            // опционально: обновить UI/строку очков исследования, если выводите
            // researchUI?.SetResearchPoints(GetRP());
        }

        RefreshAvailability(); // пересчитать доступность кнопок исследований
    }


    // === работа с Research как с обычным ресурсом ===
    private int GetRP() =>
        ResourceManager.Instance?.GetResource("Research") ?? 0;

    private void AddRP(int amount)
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddResource("Research", amount);
    }

    private bool TrySpendRP(int amount)
    {
        if (ResourceManager.Instance == null) return false;
        if (GetRP() < amount) return false;
        ResourceManager.Instance.SpendResource("Research", amount);
        return true;
    }

    // === завершение исследования ===
    public void CompleteResearch(string id)
    {
        if (!nodes.TryGetValue(id, out var node) || node.completed) return;

        // проверяем зависимость
        if (!string.IsNullOrEmpty(node.prevId) && (!nodes.TryGetValue(node.prevId, out var prev) || !prev.completed))
            return;

        // тратим очки
        if (!TrySpendRP(node.cost))
            return;

        node.completed = true;

        // открываем здание
        if (Enum.TryParse(id, out BuildManager.BuildMode mode))
            BuildManager.Instance.UnlockBuilding(mode);

        // меняем UI на описание открытия
        string text = discoveryTexts.TryGetValue(id, out var desc) ? desc : $"{id} discovered.";
        researchUI.SetCompleted(id, text);

        // создаём следующее исследование
        if (!string.IsNullOrEmpty(node.nextId) && nodes.ContainsKey(node.nextId))
            EnsureRowCreated(node.nextId);

        RefreshAvailability();
    }

    // === инициализация цепочки ===
    private void BuildChain()
    {
        nodes.Clear();

        Add("Clay", 3, "Pottery");
        Add("Pottery", 4, "Tools");
        Add("Tools", 4, "Hunter");
        Add("Hunter", 3, "Warehouse");
        Add("Warehouse", 4, "Crafts");
        Add("Crafts", 4, "Wheat");
        Add("Wheat", 5, "Flour");
        Add("Flour", 3, "Bakery");
        Add("Bakery", 3, "Sheep");
        Add("Sheep", 4, "Dairy");
        Add("Dairy", 4, "Weaver");
        Add("Weaver", 4, "Clothes");
        Add("Clothes", 4, "Market");
        Add("Market", 4, "Furniture");
        Add("Furniture", 5, "Beans");
        Add("Beans", 3, "Brewery");
        Add("Brewery", 4, "Coal");
        Add("Coal", 4, null);
    }

    private void Add(string id, int cost, string nextId)
    {
        nodes[id] = new Node(id, cost, nextId);
    }

    private void LinkPrevPointers()
    {
        foreach (var kv in nodes)
        {
            var n = kv.Value;
            if (!string.IsNullOrEmpty(n.nextId) && nodes.TryGetValue(n.nextId, out var next))
                next.prevId = n.id;
        }
    }

    private void EnsureRowCreated(string id)
    {
        if (!nodes.TryGetValue(id, out var n) || n.rowCreated) return;

        string text = $"Исследование: <b>{n.id}</b> — стоимость: <b>{n.cost}</b> очков";
        researchUI.AddRow(id, text);
        n.rowCreated = true;
    }

    private void RefreshAvailability()
    {
        int points = GetRP();
        foreach (var n in nodes.Values)
        {
            if (!n.rowCreated || n.completed) continue;

            bool prereqOk = string.IsNullOrEmpty(n.prevId) || (nodes.TryGetValue(n.prevId, out var p) && p.completed);
            bool enoughPoints = points >= n.cost;

            researchUI.SetAvailable(n.id, prereqOk && enoughPoints);
        }
    }
}
