using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceUIManager : MonoBehaviour
{
    public static ResourceUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI resourceText;

    private class ResourceData
    {
        public int amount;
        public float production;
        public float consumption;
        public bool hasBeenVisible; // показывать ли всегда (как только ресурс появился)
    }

    private readonly Dictionary<string, ResourceData> resources = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Старый таймер можно оставить закомментированным, если хочешь легко вернуть его обратно
    /*
    private float timer = 0f;
    [SerializeField] private float updateInterval = 1f;
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateUI();
        }
    }
    */

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

        // Если когда-то был > 0 — считаем "разблокированным"
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

    /// <summary>
    /// Обновляет текстовое отображение всех ресурсов.
    /// </summary>
    private void UpdateUI()
    {
        if (resourceText == null) return;

        string text = "";

        // 🔹 Mood — всегда в начале
        if (resources.ContainsKey("Mood"))
        {
            var mood = resources["Mood"];
            text += $"<b>Mood {mood.amount}%</b>\n";
        }

        // 🔹 Очки исследований — если есть
        if (resources.ContainsKey("Research"))
        {
            var rp = resources["Research"];
            text += $"Research: <b>{rp.amount}</b>\n\n";
        }

        // 🔹 Остальные ресурсы
        foreach (var kvp in resources)
        {
            if (kvp.Key == "Mood" || kvp.Key == "Research")
                continue; // уже показаны выше

            var data = kvp.Value;

            // скрываем ресурсы, которые ещё ни разу не были >0
            if (data.amount <= 0 && !data.hasBeenVisible)
                continue;

            string prodText = data.production > 0 ? $"; <color=green>+{data.production:F0}</color>" : "";
            string consText = data.consumption > 0 ? $"; <color=red>-{data.consumption:F0}</color>" : "";

            bool isDeficit = data.consumption > data.production;
            bool isBalanced = Mathf.Approximately(data.consumption, data.production) && data.consumption > 0;

            string resourceNameColored;
            if (isDeficit)
                resourceNameColored = $"<color=red>{kvp.Key}</color>";
            else if (isBalanced)
                resourceNameColored = $"<color=yellow>{kvp.Key}</color>";
            else
                resourceNameColored = $"<color=white>{kvp.Key}</color>";

            text += $"{resourceNameColored} {data.amount}{prodText}{consText}\n";
        }

        resourceText.text = text;
    }
}
