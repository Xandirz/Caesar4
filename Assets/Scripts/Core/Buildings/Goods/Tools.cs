using System.Collections.Generic;
using UnityEngine;

public class Tools : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Tools;

    public Tools()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 5 },
            { "Rock", 2 },
        };

        workersRequired = 16;

        consumptionCost = new Dictionary<string, int>
        {
            { "Wood", 2 },
            { "Rock", 2 },
        };

        production = new Dictionary<string, int>
        {
            { "Tools", 40 }
        };

        // === Level 2 ===
        addConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Copper", 10 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Tools", 70 }
        };

        // === Level 3 ===
        // ✅ Удаляем Copper полностью на 3 уровне
        deleteFromConsumptionLevel3 = new List<string>
        {
            "Copper"
        };

        // ✅ Добавляем Bronze на 3 уровне
        addConsumptionLevel3 = new Dictionary<string, int>
        {
            { "Bronze", 10 }
        };

        upgradeProductionBonusLevel3 = new Dictionary<string, int>
        {
            { "Tools", 100 }
        };
    }

    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Tools2");
        level3Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl3/Tools3");
    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Tools2";
        if (level == 3) return "Tools3";
        return base.GetResearchIdForLevel(level);
    }

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
}