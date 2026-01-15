using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Стоимость
    public Dictionary<string, int> costDict;
    public Dictionary<string, int> consumptionDict;

    public Dictionary<string, int> productionDict;

    // Требования к размещению
    public bool needWaterNearby;
    public bool requiresRoadAccess;
    public bool needHouseNearby; // 👈 уже есть :contentReference[oaicite:1]{index=1}
    public bool needMountainsNearby; // 👈 NEW


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null) return;

        string text = BuildTooltipText(
            costDict,
            consumptionDict,
            productionDict,
            needWaterNearby,
            requiresRoadAccess,
            needHouseNearby,
            needMountainsNearby
        );

        TooltipUI.Instance.Show(text, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    private static string BuildTooltipText(
        Dictionary<string, int> costDict,
        Dictionary<string, int> consumptionDict,
        Dictionary<string, int> productionDict,
        bool needWater,
        bool needRoad,
        bool needHouse,
        bool needMountains)
    {
        var sb = new StringBuilder(256);

        // === Требования ===
        if (needWater)
            sb.AppendLine("<b>Needs water nearby</b>");

        if (needRoad)
            sb.AppendLine("<b>Needs road access</b>");

        if (SettingsManager.Instance.settingNeedHouse)
        {
            if (needHouse)
                sb.AppendLine("<b>Needs house nearby</b>");
        }


        // NEW
        if (needMountains)
            sb.AppendLine("<b>Needs mountains nearby</b>");

        if (needWater || needRoad || needHouse || needMountains)
            sb.AppendLine();

        // === Стоимость строительства ===
        sb.AppendLine("<b>Build cost</b>");
        sb.AppendLine(BuildCostText(costDict));

        // === Production info (только если есть данные) ===
        if ((consumptionDict != null && consumptionDict.Count > 0) ||
            (productionDict != null && productionDict.Count > 0))
        {
            sb.AppendLine();
        }

        if (consumptionDict != null && consumptionDict.Count > 0)
        {
            sb.AppendLine("<b>Consumption</b>");
            sb.AppendLine(BuildFlowText(consumptionDict, prefix: "- "));
        }

        if (productionDict != null && productionDict.Count > 0)
        {
            sb.AppendLine("<b>Production</b>");
            sb.AppendLine(BuildFlowText(productionDict, prefix: "+ "));
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildFlowText(Dictionary<string, int> dict, string prefix)
    {
        if (dict == null || dict.Count == 0)
            return string.Empty;

        var sb = new StringBuilder(128);
        foreach (var kvp in dict)
        {
            var resName = kvp.Key;
            if (string.IsNullOrEmpty(resName)) continue;
            resName = resName.Trim();
            sb.AppendLine($"{prefix}{resName}");
        }

        return sb.ToString().TrimEnd();
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
                have = ResourceManager.Instance.GetResource(resName);

            string color = (have >= need) ? GREEN : RED;
            sb.AppendLine(
                $"<color={color}>{resName}: {need} (you have {have})</color>"
            );
        }

        return sb.ToString().TrimEnd();
    }
}