using System.Collections.Generic; 
using UnityEngine;
using TMPro;

public class ResourceUIManager : MonoBehaviour
{
    public static ResourceUIManager Instance { get; private set; }
    public TextMeshProUGUI resourceText;
    public float updateInterval = 1f; // обновление раз в 1 сек
    private float timer = 0f;

    private class ResourceData
    {
        public int amount;
        public float production;
        public float consumption;
        public bool hasBeenVisible; // 🔹 показывать ли всегда (как только ресурс появился)
    }

    private Dictionary<string, ResourceData> resources = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateUI();
        }
    }

    public void SetResource(string name, int amount, float prod = 0, float cons = 0)
    {
        if (!resources.ContainsKey(name))
            resources[name] = new ResourceData();

        var data = resources[name];
        data.amount = amount;
        data.production = prod;
        data.consumption = cons;

        // 🔹 Если ресурс когда-то был > 0 → считаем его "разблокированным"
        if (amount > 0)
            data.hasBeenVisible = true;
    }

    private void UpdateUI()
    {
        string text = "";

        // 🔹 Сначала Mood
        if (resources.ContainsKey("Mood"))
        {
            var mood = resources["Mood"];
            text += $"<b>Mood {mood.amount}%</b>\n\n";
        }

        // 🔹 Потом все остальные ресурсы
        foreach (var kvp in resources)
        {
            if (kvp.Key == "Mood") continue; // уже вывели сверху

            var data = kvp.Value;

            // 🔹 показываем только если ресурс >0 или он уже "разблокирован"
            if (data.amount <= 0 && !data.hasBeenVisible)
                continue;

            string prodText = data.production > 0 ? $"; <color=green>+{data.production:F0}</color>" : "";
            string consText = data.consumption > 0 ? $"; <color=red>-{data.consumption:F0}</color>" : "";

            // ⚡ Если потребление больше, чем производство → выделяем имя ресурса красным
            bool isDeficit = data.consumption > data.production;

            string resourceNameColored = isDeficit
                ? $"<color=red>{kvp.Key}</color>"
                : kvp.Key;

            text += $"{resourceNameColored} {data.amount}{prodText}{consText}\n";
        }

        if (resourceText != null)
            resourceText.text = text;
    }
}
