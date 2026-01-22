using TMPro;
using UnityEngine;

public class ResourceRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text resourceNameText;
    [SerializeField] private TMP_Text resourceAmountText;
    [SerializeField] private TMP_Text resourceProductionText;
    [SerializeField] private TMP_Text resourceConsumptionText;

    /// <summary>
    /// Обновляет отображение строки ресурса.
    /// amountIsPercent - для Mood (например 75%).
    /// </summary>
    public void Bind(string resourceName, int amount, float production, float consumption, bool amountIsPercent = false)
    {
        if (resourceNameText != null)
        {
            resourceNameText.text = resourceName;
            resourceNameText.color = GetNameColorByBalance(production, consumption);
        }

        if (resourceAmountText != null)
        {
            resourceAmountText.text = amountIsPercent ? $"{amount}%" : amount.ToString();
        }

        if (resourceProductionText != null)
        {
            resourceProductionText.color = Color.green;
            resourceProductionText.text = (production > 0f) ? $"+{production:F0}" : "";
        }

        if (resourceConsumptionText != null)
        {
            resourceConsumptionText.color = Color.red;
            resourceConsumptionText.text = (consumption > 0f) ? $"-{consumption:F0}" : "";
        }
    }

    // Логика 1-в-1 как в ColorizeNameByBalance (только цветом, а не richtext).
    private static Color GetNameColorByBalance(float prod, float cons)
    {
        bool isDeficit = cons > prod;
        bool isBalanced = Mathf.Approximately(cons, prod) && cons > 0;

        if (isDeficit) return Color.red;
        if (isBalanced) return Color.yellow;
        return Color.white;
    }
}