using UnityEngine;
using System;
using System.Collections.Generic;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance;

    [Header("UI")]
    public ResearchUI researchUI;

    [Header("Mood Requirement")]
    [SerializeField] private int moodThreshold = 80; // минимальный mood для открытия

    // последняя известная величина настроения (0..100)
    private int lastKnownMood = 0;

    // --- Тексты открытий (показываются после нажатия ОК) ---
    private readonly Dictionary<string, string> discoveryTexts = new()
    {
        { "Clay",       "Пока копали землю у реки, вы нашли мягкий липкий материал, который твердеет на солнце." },
        { "Pottery",    "Из обожжённой глины получаются прочные сосуды. Теперь можно хранить еду и воду дольше." },
        { "Tools",      "Простые орудия труда ускоряют любую работу — эпоха инструментов началась." },
        { "Hunter",     "Охота — вы добываете мясо, которое питательнее ягод и кореньев." },
        { "Warehouse",  "Склад — теперь можно хранить больше припасов и навести порядок в поставках." },
        { "Crafts",     "Ремесло — люди начали создавать не только нужное, но и красивое." },
        { "Wheat",      "Посеянное зерно даёт новые колосья — начало земледелия." },
        { "Flour",      "Размолов зерно, вы получили муку — основу лепёшек и хлеба." },
        { "Bakery",     "Пекарня — мука превращается в ароматный хлеб." },
        { "Sheep",      "Вы приручили овец — шерсть, мясо и молоко под рукой." },
        { "Dairy",      "Молочная — молоко, сыр и йогурт становятся доступнее." },
        { "Weaver",     "Ткачество — из шерсти получаем ткань." },
        { "Clothes",    "Одежда — люди меньше мёрзнут и могут жить в холодных землях." },
        { "Market",     "Рынок — обмен товарами стал проще, город ожил." },
        { "Furniture",  "Мебель — стулья и столы делают жизнь удобнее." },
        { "Beans",      "Бобы — питательны и хорошо растут рядом с домом." },
        { "Brewery",    "Пивоварня — из хлеба и зерна рождается бодрящий напиток." },
        { "Coal",       "Уголь — топливо, которое горит дольше обычных дров." }
    };

    // ——— Узел исследования ———
    private class Node
    {
        public string id;                 // должен совпадать с BuildManager.BuildMode (для анлока)
        public string producedResId;      // какой ресурс нужно ПРОИЗВЕСТИ (кумулятивно)
        public int    producedRequired;   // сколько произвести (счетчик суммарный)
        public int    populationRequired; // People ≥ X
        public int    housesRequired;     // кол-во домов
        public int    minHouseStage;      // минимальная стадия этих домов
        public string nextId;             // линейная зависимость
        public string prevId;             // заполняется после сборки
        public bool   completed;          // уже открыто
        public bool   rowCreated;         // строка создана

        public Node(string id, string producedResId, int producedRequired, int populationRequired, int housesRequired, int minHouseStage, string nextId)
        {
            this.id                 = id;
            this.producedResId      = producedResId;
            this.producedRequired   = producedRequired;
            this.populationRequired = populationRequired;
            this.housesRequired     = housesRequired;
            this.minHouseStage      = minHouseStage;
            this.nextId             = nextId;
        }
    }

    private readonly Dictionary<string, Node> nodes = new();
    private readonly Dictionary<string, int> producedTotals = new(); // кумулятивное производство по ресурсам
    private string firstId = "Clay";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        BuildChain();
        LinkPrevPointers();

        if (researchUI != null)
        {
            researchUI.Initialize(this);
            EnsureRowCreated(firstId); // стартовая строка
        }

        RefreshAllRows();
    }

    void Update()
    {
        // Можно обновлять реже таймером — здесь простая периодическая проверка
        RefreshAllRows();
    }

    // === вызывать из вашего дневного тика ===
    public void OnDayPassed(int moodPercent)
    {
        SetCurrentMood(moodPercent);
    }

    public void SetCurrentMood(int moodPercent)
    {
        lastKnownMood = Mathf.Clamp(moodPercent, 0, 100);
        RefreshAllRows();
    }

    // === вызывать из места ПРОИЗВОДСТВА (после AddResource) ===
    public void ReportProduced(string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId) || amount <= 0) return;

        if (producedTotals.TryGetValue(resourceId, out var cur))
            producedTotals[resourceId] = cur + amount;
        else
            producedTotals[resourceId] = amount;

        RefreshAllRows();
    }

    // ——— Цепочка исследований и требования (примерная кривая) ———
    private void BuildChain()
    {
        nodes.Clear();

        // До Warehouse — считаем дома стадии ≥1, далее — ≥2

        Add("Clay",      producedResId: null,     producedReq:   0, populationReq:  50, housesReq: 10, minStage: 1, nextId: "Pottery");
        Add("Pottery",   producedResId: "Clay",   producedReq:  20, populationReq:  60, housesReq: 10, minStage: 1, nextId: "Tools");
        Add("Tools",     producedResId: "Rock",   producedReq:  10, populationReq:  70, housesReq: 12, minStage: 1, nextId: "Hunter");
        Add("Hunter",    producedResId: "Berry",  producedReq:  10, populationReq:  80, housesReq: 14, minStage: 1, nextId: "Warehouse");
        Add("Warehouse", producedResId: null,     producedReq:   0, populationReq: 100, housesReq: 20, minStage: 1, nextId: "Crafts");

        Add("Crafts",    producedResId: "Bone",   producedReq:  10, populationReq: 110, housesReq:  5, minStage: 2, nextId: "Wheat");
        Add("Wheat",     producedResId: null,     producedReq:   0, populationReq: 120, housesReq:  8, minStage: 2, nextId: "Flour");
        Add("Flour",     producedResId: "Wheat",  producedReq:  10, populationReq: 130, housesReq: 10, minStage: 2, nextId: "Bakery");
        Add("Bakery",    producedResId: "Flour",  producedReq:  10, populationReq: 140, housesReq: 12, minStage: 2, nextId: "Sheep");

        // Пример «овцы»: 100 пшеницы произведено, 100+ людей, ≥5 домов стадии 2
        Add("Sheep",     producedResId: "Wheat",  producedReq: 100, populationReq: 100, housesReq:  5, minStage: 2, nextId: "Dairy");

        Add("Dairy",     producedResId: "Wool",   producedReq:  20, populationReq: 150, housesReq:  8, minStage: 2, nextId: "Weaver");
        Add("Weaver",    producedResId: "Wool",   producedReq:  20, populationReq: 160, housesReq: 10, minStage: 2, nextId: "Clothes");
        Add("Clothes",   producedResId: "Cloth",  producedReq:  10, populationReq: 170, housesReq: 12, minStage: 2, nextId: "Market");
        Add("Market",    producedResId: "Clothes",producedReq:  10, populationReq: 180, housesReq: 12, minStage: 2, nextId: "Furniture");
        Add("Furniture", producedResId: "Wood",   producedReq:  30, populationReq: 190, housesReq: 14, minStage: 2, nextId: "Beans");
        Add("Beans",     producedResId: "Wheat",  producedReq:  10, populationReq: 150, housesReq: 14, minStage: 2, nextId: "Brewery");
        Add("Brewery",   producedResId: "Bread",  producedReq:  10, populationReq: 160, housesReq: 14, minStage: 2, nextId: "Coal");
        Add("Coal",      producedResId: "Wood",   producedReq:  20, populationReq: 170, housesReq: 16, minStage: 2, nextId: null);
    }

    private void Add(string id, string producedResId, int producedReq, int populationReq, int housesReq, int minStage, string nextId)
    {
        nodes[id] = new Node(id, producedResId, producedReq, populationReq, housesReq, minStage, nextId);
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

    // ——— Завершение исследования (нажатие ОК в UI) ———
    public void CompleteResearch(string id)
    {
        if (!nodes.TryGetValue(id, out var node) || node.completed) return;

        // зависимость
        if (!string.IsNullOrEmpty(node.prevId) &&
            (!nodes.TryGetValue(node.prevId, out var prev) || !prev.completed))
            return;

        // финальная проверка требований
        if (!AreRequirementsMet(node)) return;

        node.completed = true;

        // анлок здания
        if (Enum.TryParse(id, out BuildManager.BuildMode mode))
            BuildManager.Instance.UnlockBuilding(mode);

        // UI: описание открытия
        string text = discoveryTexts.TryGetValue(id, out var desc) ? desc : $"{id} discovered.";
        researchUI.SetCompleted(id, text);
        ClearAllProducedProgress();

        // следующая строка
        if (!string.IsNullOrEmpty(node.nextId) && nodes.ContainsKey(node.nextId))
            EnsureRowCreated(node.nextId);

        RefreshAllRows();
    }
    
    // ——— Сброс всех накопленных произведённых ресурсов ———
    private void ClearAllProducedProgress()
    {
        producedTotals.Clear();
    }

    // ——— Создание строки в UI ———
    private void EnsureRowCreated(string id)
    {
        if (!nodes.TryGetValue(id, out var n) || n.rowCreated) return;

        string text = BuildRequirementText(n);
        researchUI.AddRow(id, text);
        n.rowCreated = true;

        UpdateRowProgress(n);
    }

    // ——— Обновление всех строк (тексты + доступность) ———
    private void RefreshAllRows()
    {
        foreach (var n in nodes.Values)
        {
            if (!n.rowCreated || n.completed) continue;

            UpdateRowProgress(n);

            bool prereqOk  = string.IsNullOrEmpty(n.prevId) || (nodes.TryGetValue(n.prevId, out var p) && p.completed);
            bool available = prereqOk && AreRequirementsMet(n);

            researchUI.SetAvailable(n.id, available);
        }
    }

    // ——— Проверка всех требований, включая mood ———
    private bool AreRequirementsMet(Node n)
    {
        // mood
        if (lastKnownMood < moodThreshold) return false;

        // население
        if (n.populationRequired > 0)
        {
            int people = ResourceManager.Instance != null ? ResourceManager.Instance.GetResource("People") : 0;
            if (people < n.populationRequired) return false;
        }

        // дома
        if (n.housesRequired > 0 && !HasEnoughHouses(n.housesRequired, n.minHouseStage))
            return false;

        // произведённый ресурс
        if (!string.IsNullOrEmpty(n.producedResId) && n.producedRequired > 0)
        {
            int have = producedTotals.TryGetValue(n.producedResId, out var v) ? v : 0;
            if (have < n.producedRequired) return false;
        }

        return true;
    }

    // ——— Базовый текст требований ———
    private string BuildRequirementText(Node n)
    {
        List<string> pieces = new();

        if (!string.IsNullOrEmpty(n.producedResId) && n.producedRequired > 0)
            pieces.Add($"{n.producedResId}: 0/{n.producedRequired}");

        if (n.populationRequired > 0)
            pieces.Add($"Население ≥ {n.populationRequired}");

        if (n.housesRequired > 0)
            pieces.Add($"Дома ≥ {n.housesRequired} (стадия ≥ {n.minHouseStage})");

        pieces.Add($"Mood ≥ {moodThreshold}%");

        string body = pieces.Count > 0 ? string.Join("  •  ", pieces) : "Без требований";
        return $"Исследование: <b>{n.id}</b> — {body}";
    }

    // ——— Обновление прогресса строки (значения/цвета) ———
    private void UpdateRowProgress(Node n)
    {
        List<string> parts = new();

        if (!string.IsNullOrEmpty(n.producedResId) && n.producedRequired > 0)
        {
            int have = producedTotals.TryGetValue(n.producedResId, out var v) ? v : 0;
            if (have > n.producedRequired) have = n.producedRequired;
            parts.Add($"{n.producedResId}: <b>{have}/{n.producedRequired}</b>");
        }

        if (n.populationRequired > 0)
        {
            int people = ResourceManager.Instance != null ? ResourceManager.Instance.GetResource("People") : 0;
            string col = people >= n.populationRequired ? "white" : "red";
            parts.Add($"Население: <color={col}>{people}/{n.populationRequired}</color>");
        }

        if (n.housesRequired > 0)
        {
            int cur = CountHousesWithMinStage(n.minHouseStage);
            string col = cur >= n.housesRequired ? "white" : "red";
            parts.Add($"Дома (ст.≥{n.minHouseStage}): <color={col}>{cur}/{n.housesRequired}</color>");
        }

        string moodCol = lastKnownMood >= moodThreshold ? "white" : "red";
        parts.Add($"Mood: <color={moodCol}>{lastKnownMood}/{moodThreshold}</color>");

        if (parts.Count == 0) parts.Add("Без требований");

        string text = $"Исследование: <b>{n.id}</b> — " + string.Join("  •  ", parts);
        researchUI.UpdateRowText(n.id, text);
    }

    // ——— Подсчёт домов нужной стадии ———
    private int CountHousesWithMinStage(int minStage)
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

    private bool HasEnoughHouses(int required, int minStage)
    {
        if (required <= 0) return true;
        return CountHousesWithMinStage(minStage) >= required;
    }
}
