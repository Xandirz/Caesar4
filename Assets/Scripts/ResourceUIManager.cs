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
        public bool hasBeenVisible; // оставляем, но для FoodLvl1-логики не используем
    }

    private readonly Dictionary<string, ResourceData> resources = new();

    // ✅ Группа Food Level 1 (UI-агрегация)
    private static readonly string[] FoodLvl1Resources =
    {
        "Berry",
        "Fish",
        "Nuts",
        "Mushrooms"
    };

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

    private static bool IsFoodLvl1(string resName)
    {
        for (int i = 0; i < FoodLvl1Resources.Length; i++)
            if (FoodLvl1Resources[i] == resName)
                return true;
        return false;
    }

    private static bool ShouldShowFoodItem(ResourceData data)
    {
        if (data == null) return false;
        // ✅ только если реально участвует: производится или потребляется
        return data.production > 0f || data.consumption > 0f;
    }

    private void GetFoodLvl1Totals(
        out int totalAmount,
        out float totalProd,
        out float totalCons,
        out int visibleItemsCount)
    {
        totalAmount = 0;
        totalProd = 0f;
        totalCons = 0f;
        visibleItemsCount = 0;

        for (int i = 0; i < FoodLvl1Resources.Length; i++)
        {
            var name = FoodLvl1Resources[i];
            if (!resources.TryGetValue(name, out var data))
                continue;

            if (!ShouldShowFoodItem(data))
                continue;

            visibleItemsCount++;
            totalAmount += data.amount;
            totalProd += data.production;
            totalCons += data.consumption;
        }
    }

    private static string FormatRateText(float prod, float cons)
    {
        string prodText = prod > 0 ? $"; <color=green>+{prod:F0}</color>" : "";
        string consText = cons > 0 ? $"; <color=red>-{cons:F0}</color>" : "";
        return prodText + consText;
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

        // ─────────────────────────────────────────────────────────────
        // ✅ Food Level 1 (агрегированная строка + только активные подстроки)
        // ─────────────────────────────────────────────────────────────
        GetFoodLvl1Totals(out int foodSum, out float foodProdSum, out float foodConsSum, out int visibleFoodCount);

        if (visibleFoodCount > 0)
        {
            text += "\n<b>Food Level 1</b>\n";

            // строка-группа с суммой (amount / prod / cons) только активных ресурсов
            string groupNameColored = ColorizeNameByBalance("FoodLvl1", foodProdSum, foodConsSum);
            string groupRates = FormatRateText(foodProdSum, foodConsSum);
            text += $"{groupNameColored} {foodSum}{groupRates}\n";

            // подстроки только тех ресурсов, которые реально участвуют
            for (int i = 0; i < FoodLvl1Resources.Length; i++)
            {
                string resName = FoodLvl1Resources[i];
                resources.TryGetValue(resName, out var data);

                if (!ShouldShowFoodItem(data))
                    continue;

                string itemNameColored = ColorizeNameByBalance(resName, data.production, data.consumption);
                string itemRates = FormatRateText(data.production, data.consumption);

                text += $"   {itemNameColored} {data.amount}{itemRates}\n";
            }
        }

        // 🔹 Остальные ресурсы (кроме Mood/Research и кроме FoodLvl1-ресурсов)
        foreach (var kvp in resources)
        {
            if (kvp.Key == "Mood" || kvp.Key == "Research")
                continue;

            if (IsFoodLvl1(kvp.Key))
                continue; // уже обработали в группе (или скрыли)

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
