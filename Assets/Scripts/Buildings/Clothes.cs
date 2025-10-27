﻿using System.Collections.Generic;
using UnityEngine;

public class Clothes : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clothes; //!! ЗАПОЛНИТЬ
    
    public Clothes()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Rock", 1 },
            
        };
        
        workersRequired = 5;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Cloth", 1 },
            { "Bone", 1 },
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Clothes", 10 }
        };
        
 
        
        upgradeConsumptionLevel1 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel1 = new Dictionary<string, int>
        {
            { "Clothes", 10 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Clothes2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}