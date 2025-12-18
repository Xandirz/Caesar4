using System;
using System.Collections.Generic;
using UnityEngine;

public class Wheat : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Wheat; //!! ЗАПОЛНИТЬ

    public Wheat()
    {
        cost = new Dictionary<string,int>
        {
           
        };
        
        workersRequired = 3;
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Wheat", 1 }
        };
        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Manure", 1 }
        };

        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Wheat", 1 } // итого 2
        };
        
    }

    private void Awake()
    {
        requiresRoadAccess = false;
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl1/Wheat");

    }

    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2) return "Farm2";
        return base.GetResearchIdForLevel(level);
    }
}