using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Сюда BuildUIManager кладёт стоимость (та же структура, что у тебя в зданиях)
    public Dictionary<string, int> costDict;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null) return;

        string text = BuildCostText(costDict);
        TooltipUI.Instance.Show(text, eventData.position); // позиция мыши
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    private static string BuildCostText(Dictionary<string, int> costDict)
    {
        if (costDict == null || costDict.Count == 0)
            return "Free";

        const string GREEN = "#35C759";
        const string RED = "#FF3B30";

        var sb = new StringBuilder(128);

        foreach (var kvp in costDict)
        {
            string resName = kvp.Key;
            if (string.IsNullOrEmpty(resName))
                continue;

            resName = resName.Trim();
            int need = kvp.Value;

            int have = 0;
            if (ResourceManager.Instance != null)
                have = ResourceManager.Instance.GetResource(resName); // то же, что в Resource UI

            string color = (have >= need) ? GREEN : RED;
            sb.AppendLine($"<color={color}>{resName}: {need} (you have {have})</color>");
        }

        return sb.ToString().TrimEnd();
    }
}