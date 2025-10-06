using System;
using System.Collections.Generic;
using UnityEngine;

public class Berry : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Berry;


    public Berry()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "People", 3 }
        };
        
        consumptionCost = new Dictionary<string, int>();
        
        production = new Dictionary<string, int>
        {
            { "Berry", 12 }
        };
        
        upgradeCost = new Dictionary<string, int>
        {
            { "Wood", 10 },
            { "Rock", 5 },
            { "Tools", 2 }
        };
        
        upgradeConsumption = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonus = new Dictionary<string, int>
        {
            { "Berry", 8 }  
        };
    }

    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Berry2");
    }

    public override Dictionary<string, int> GetCostDict() => cost;
}