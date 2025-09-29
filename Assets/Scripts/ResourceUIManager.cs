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

        resources[name].amount = amount;
        resources[name].production = prod;
        resources[name].consumption = cons;
    }

    private void UpdateUI()
    {
        string text = "";

        // 🔹 Сначала всегда Mood
        if (resources.ContainsKey("Mood"))
        {
            var mood = resources["Mood"];
            text += $"<b>Mood {mood.amount}%</b>\n\n";
        }

        // 🔹 Потом все остальные ресурсы
        foreach (var kvp in resources)
        {
            if (kvp.Key == "Mood") continue; // уже вывели сверху

            string prodText = kvp.Value.production > 0 ? $"; <color=green>+{kvp.Value.production:F0}</color>" : "";
            string consText = kvp.Value.consumption > 0 ? $"; <color=red>-{kvp.Value.consumption:F0}</color>" : "";

            text += $"{kvp.Key} {kvp.Value.amount}{prodText}{consText}\n";
        }

        if (resourceText != null)
            resourceText.text = text;
    }
}
