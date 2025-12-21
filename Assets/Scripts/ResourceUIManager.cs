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
        public bool hasBeenVisible; 
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

        if (isDeficit)
            return $"<color=red>{name}</color>";
        if (isBalanced)
            return $"<color=yellow>{name}</color>";
        return $"<color=white>{name}</color>";
    }

    /// <summary>
    /// Обновляет текстовое отображение всех ресурсов.
    /// </summary>
    private void UpdateUI()
    {
        float t0 = Time.realtimeSinceStartup;

        if (resourceText == null) return;

        string text = "";

        // 🔹 Mood — всегда в начале
        if (resources.ContainsKey("Mood"))
        {
            var mood = resources["Mood"];
            text += $"<b>Mood {mood.amount}%</b>\n";
        }

        // 🔹 People (Workers / Idle)
        int workers = ResourceManager.Instance.AssignedWorkers;
        int idle = ResourceManager.Instance.FreeWorkers;

        text += $"Workers: <color=white>{workers}</color>  ";
        text += $"Idle: <color={(idle > 0 ? "green" : "red")}>{idle}</color>\n";

     

        foreach (var kvp in resources)
        {
            if (kvp.Key == "Mood" || kvp.Key == "Research")
                continue;



            var data = kvp.Value;

            // скрываем ресурсы, которые ещё ни разу не были >0
            if (data.amount <= 0 && !data.hasBeenVisible)
                continue;

            string prodText = data.production > 0 ? $"; <color=green>+{data.production:F0}</color>" : "";
            string consText = data.consumption > 0 ? $"; <color=red>-{data.consumption:F0}</color>" : "";

            string resourceNameColored = ColorizeNameByBalance(kvp.Key, data.production, data.consumption);

            text += $"{resourceNameColored} {data.amount}{prodText}{consText}\n";
        }

        resourceText.text = text;

        float dt = (Time.realtimeSinceStartup - t0) * 1000f;
        if (dt > 5f)
            Debug.Log($"[PERF] updateUI занял {dt:F2} ms");
    }
}
