using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResourceUIManager : MonoBehaviour
{
    public static ResourceUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI resourceText;
    [SerializeField] private float updateInterval = 1f; // обновление раз в 1 сек
    private float timer = 0f;

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

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateUI();
        }
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

        // Если когда-то был > 0 — считаем "разблокированным"
        if (amount > 0)
            data.hasBeenVisible = true;
    }

    /// <summary>
    /// Обновляет текстовое отображение всех ресурсов
    /// </summary>
    private void UpdateUI()
    {
        if (resourceText == null) return;

        string text = "";

        // 🔹 Mood — всегда в начале
        if (resources.ContainsKey("Mood"))
        {
            var mood = resources["Mood"];
            text += $"<b>Mood {mood.amount}%</b>\n\n";
        }

        // 🔹 Люди, работники, свободные — особый блок
        int totalPeople = ResourceManager.Instance.GetResource("People");
        int assignedWorkers = ResourceManager.Instance.AssignedWorkers;
        int freeWorkers = ResourceManager.Instance.FreeWorkers;

        if (totalPeople > 0)
        {
            // Определяем, хватает ли свободных людей для всех нужд
            // (если свободных меньше, чем требуется хотя бы одному зданию)
            bool shortage = freeWorkers < 0 || ResourceManager.Instance.FreeWorkers < 0;

            string freeColor = shortage ? "red" : "green";

            text += $"<b>Люди:</b> {totalPeople}\n";
            text += $"— Работники: <color=yellow>{assignedWorkers}</color>\n";
            text += $"— Свободные: <color={freeColor}>{freeWorkers}</color>\n\n";
        }

        // 🔹 Остальные ресурсы
        foreach (var kvp in resources)
        {
            if (kvp.Key == "Mood" || kvp.Key == "People") continue; // Mood и People отдельно

            var data = kvp.Value;

            // показываем только если есть количество или ресурс уже был виден
            if (data.amount <= 0 && !data.hasBeenVisible)
                continue;

            // формируем текст прироста/потребления
            string prodText = data.production > 0 ? $"; <color=green>+{data.production:F0}</color>" : "";
            string consText = data.consumption > 0 ? $"; <color=red>-{data.consumption:F0}</color>" : "";

            // сравниваем баланс
            bool isDeficit = data.consumption > data.production;
            bool isBalanced = Mathf.Approximately(data.consumption, data.production) && data.consumption > 0;

            string resourceNameColored;

            if (isDeficit)
                resourceNameColored = $"<color=red>{kvp.Key}</color>";          // 🔴 дефицит
            else if (isBalanced)
                resourceNameColored = $"<color=yellow>{kvp.Key}</color>";       // 🟡 баланс
            else
                resourceNameColored = $"<color=white>{kvp.Key}</color>";        // ⚪ профицит или нет расхода

            text += $"{resourceNameColored} {data.amount}{prodText}{consText}\n";
        }

        resourceText.text = text;
    }
}
