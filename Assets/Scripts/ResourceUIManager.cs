using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ResourceUIManager : MonoBehaviour
{
    public static ResourceUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI resourceText;
    [SerializeField] private TMP_InputField searchInput;

    private class ResourceData
    {
        public int amount;
        public float production;
        public float consumption;
        public bool hasBeenVisible;
    }

    private readonly Dictionary<string, ResourceData> resources = new();

    // кеш поиска, чтобы не читать input каждый кадр
    private string searchQuery = "";

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
            // если окно включили и там уже есть текст
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
            resources[name] = new ResourceData();

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

        // Вариант A (как ты описал): показываем ресурсы, НАЧИНАЮЩИЕСЯ с введённого текста
        // return resourceName.StartsWith(query, StringComparison.OrdinalIgnoreCase);

        // Вариант B (чуть удобнее для игрока): ищем по ВХОЖДЕНИЮ (whe найдёт wheat, heat тоже найдёт wheat)
        return resourceName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Обновляет текстовое отображение всех ресурсов.
    /// </summary>
    private void UpdateUI()
    {
        float t0 = Time.realtimeSinceStartup;
        if (resourceText == null) return;

        var sb = new StringBuilder(512);

        // Mood — всегда в начале (не фильтруем)
        if (resources.TryGetValue("Mood", out var mood))
            sb.AppendLine($"<b>Mood {mood.amount}%</b>");

        // People (Workers / Idle) — всегда показываем
        int workers = ResourceManager.Instance.AssignedWorkers;
        int idle = ResourceManager.Instance.FreeWorkers;

        sb.Append("Workers: <color=white>").Append(workers).Append("</color>  ");
        sb.Append("Idle: <color=").Append(idle > 0 ? "green" : "red").Append(">")
          .Append(idle).AppendLine("</color>");

        // Основной список с фильтром
        foreach (var kvp in resources)
        {
            var name = kvp.Key;
            if (name == "Mood" || name == "Research")
                continue;

            var data = kvp.Value;

            // скрываем ресурсы, которые ещё ни разу не были >0
            if (data.amount <= 0 && !data.hasBeenVisible)
                continue;

            // 🔎 фильтр поиска
            if (!MatchesSearch(name, searchQuery))
                continue;

            string prodText = data.production > 0 ? $"; <color=green>+{data.production:F0}</color>" : "";
            string consText = data.consumption > 0 ? $"; <color=red>-{data.consumption:F0}</color>" : "";

            string resourceNameColored = ColorizeNameByBalance(name, data.production, data.consumption);
            sb.Append(resourceNameColored).Append(' ').Append(data.amount).Append(prodText).Append(consText).AppendLine();
        }

        resourceText.text = sb.ToString();

        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dt > 5f)
            Debug.Log($"[PERF] updateUI занял {dt:F2} ms");
    }
}
