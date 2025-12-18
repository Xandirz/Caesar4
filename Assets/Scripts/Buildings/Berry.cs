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
           
        };
        
        workersRequired = 6;
        
        consumptionCost = new Dictionary<string, int>();
        
        production = new Dictionary<string, int>
        {
            { "Berry", 20 }
        };  
        
 
        
        upgradeConsumptionLevel2 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel2 = new Dictionary<string, int>
        {
            { "Berry", 30 }  
        };
    }
    
    
    protected override string GetResearchIdForLevel(int level)
    {
        if (level == 2)
            return "BerryHut2";

        return base.GetResearchIdForLevel(level);
    }

    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Berry2");
    }


}