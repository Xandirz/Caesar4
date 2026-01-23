using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ResourceUIManager : MonoBehaviour
{
    public static ResourceUIManager Instance { get; private set; }

    [Header("Optional header text (Workers/Idle). Can be null.")]
    [SerializeField] private TextMeshProUGUI resourceText;

    [Header("List UI (required for prefab mode)")]
    [SerializeField] private Transform listParent;     // Content с VerticalLayoutGroup
    [SerializeField] private ResourceRowUI rowPrefab;  // Префаб строки ресурса
    [SerializeField] private PeopleRowUI peopleRowPrefab;
    
    private PeopleRowUI peopleRow;  
    [Header("Search")]
    [SerializeField] private TMP_InputField searchInput;
// порядок появления ресурсов
    private readonly List<string> resourceOrder = new();

    private class ResourceData
    {
        public int amount;
        public float production;
        public float consumption;
        public bool hasBeenVisible;
    }

    private readonly Dictionary<string, ResourceData> resources = new();

    // кэш поиска, чтобы не читать input каждый кадр
    private string searchQuery = "";

    // пул UI-строк: resourceName -> row
    private readonly Dictionary<string, ResourceRowUI> rowsByName = new();
    private readonly HashSet<string> shownThisUpdate = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchChanged);
            searchQuery = searchInput.text ?? "";
        }
    }

    private void OnDisable()
    {
        if (searchInput != null)
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);
    }

    private void OnSearchChanged(string value)
    {
        searchQuery = value ?? "";
        UpdateUI(); // обновляем сразу при вводе
    }

    /// <summary>
    /// Обновляет данные ресурса (кол-во, производство, потребление)
    /// </summary>
    public void SetResource(string name, int amount, float prod = 0, float cons = 0)
    {
        if (!resources.ContainsKey(name))
        {
            resources[name] = new ResourceData();
            resourceOrder.Add(name); // ✅ фиксируем порядок добавления
        }

        var data = resources[name];
        data.amount = amount;
        data.production = prod;
        data.consumption = cons;

        if (amount > 0)
            data.hasBeenVisible = true;
    }


    /// <summary>
    /// Вызывать после тика экономики (из AllBuildingsManager) вместо собственного таймера.
    /// </summary>
    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    // Оставляю старую функцию (полезно как reference / на случай fallback-а).
    private static string ColorizeNameByBalance(string name, float prod, float cons)
    {
        bool isDeficit = cons > prod;
        bool isBalanced = Mathf.Approximately(cons, prod) && cons > 0;

        if (isDeficit) return $"<color=red>{name}</color>";
        if (isBalanced) return $"<color=yellow>{name}</color>";
        return $"<color=white>{name}</color>";
    }

    private static bool MatchesSearch(string resourceName, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        query = query.Trim();

        // Вариант B (как сейчас): поиск по вхождению
        return resourceName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void UpdateUI()
    {
        float t0 = Time.realtimeSinceStartup;
   

        // 1) Header (Workers/Idle) — не ресурсы, можно оставить текстом

        // 2) Строим список строк ресурсов
        shownThisUpdate.Clear();

        UpdatePeopleUI();
        int siblingIndex = (peopleRow != null) ? 1 : 0;



        foreach (var name in resourceOrder)
        {
            if (!resources.TryGetValue(name, out var data))
                continue;

            if (name == "Mood" || name == "Research")
                continue;
            if (name == "People")
                continue;

            if (data.amount <= 0 && !data.hasBeenVisible)
                continue;

            if (!MatchesSearch(name, searchQuery))
                continue;

            var row = GetOrCreateRow(name);
            row.Bind(name, data.amount, data.production, data.consumption, amountIsPercent: false);

            row.gameObject.SetActive(true);
            row.transform.SetSiblingIndex(siblingIndex++);
            shownThisUpdate.Add(name);
        }


        // 3) Прячем неиспользуемые строки
        foreach (var kv in rowsByName)
        {
            if (!shownThisUpdate.Contains(kv.Key) && kv.Value != null)
                kv.Value.gameObject.SetActive(false);
        }

        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dt > 5f)
            Debug.Log($"[PERF] updateUI занял {dt:F2} ms");
    }
    private void UpdatePeopleUI()
    {
        if (ResourceManager.Instance == null)
            return;

        EnsurePeopleRowFirst();
        if (peopleRow == null)
            return;

        int workers = ResourceManager.Instance.AssignedWorkers;
        int idle = ResourceManager.Instance.FreeWorkers;
        int total = workers + idle;

        int moodPercent = 0;
        if (resources.TryGetValue("Mood", out var moodData))
            moodPercent = moodData.amount;

        peopleRow.SetAmounts(total, workers, idle, moodPercent);

        peopleRow.transform.SetSiblingIndex(0);
        peopleRow.gameObject.SetActive(true);
    }

    

    private void UpdateWorkersHeader()
    {
        if (resourceText == null) return;

        int workers = 0;
        int idle = 0;

        if (ResourceManager.Instance != null)
        {
            workers = ResourceManager.Instance.AssignedWorkers;
            idle = ResourceManager.Instance.FreeWorkers;
        }

        string idleCol = idle > 0 ? "green" : "red";
        resourceText.text = $"Workers: {workers}  Idle: <color={idleCol}>{idle}</color>";
    }

    private ResourceRowUI GetOrCreateRow(string resourceName)
    {
        if (rowsByName.TryGetValue(resourceName, out var existing) && existing != null)
            return existing;

        var row = Instantiate(rowPrefab, listParent);
        row.name = $"ResourceRow_{resourceName}";
        rowsByName[resourceName] = row;
        return row;
    }

    /// <summary>
    /// Старый режим (одним текстом) — оставлен как fallback, чтобы миграция UI не ломала сцену.
    /// </summary>
   
    private void EnsurePeopleRowFirst()
    {
        if (peopleRow != null) return;
        if (listParent == null || peopleRowPrefab == null) return;

        peopleRow = Instantiate(peopleRowPrefab, listParent);
        peopleRow.name = "PeopleRow";
        peopleRow.transform.SetSiblingIndex(0); // всегда первым
    }

    public void ClearSearch()
    {
        // 1) сбрасываем фокус
        if (EventSystem.current != null && searchInput != null &&
            EventSystem.current.currentSelectedGameObject == searchInput.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 2) сбрасываем и поле, и кеш
        searchQuery = "";

        if (searchInput != null)
        {
            searchInput.SetTextWithoutNotify(string.Empty);
            OnSearchChanged(string.Empty);
        }
        else
        {
            UpdateUI();
        }
    }
}
