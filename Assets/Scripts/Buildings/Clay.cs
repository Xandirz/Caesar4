using System.Collections.Generic;
using UnityEngine;

public class Clay : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Clay;
    
    public Clay()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
          
        };
        
        workersRequired = 5;
        
        production = new Dictionary<string, int>
        {
            { "Clay", 15 }
        };
        

        
        upgradeConsumptionLevel1 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel1 = new Dictionary<string, int>
        {
            { "Clay", 15 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Clay2");
    }
    
    public override Dictionary<string, int> GetCostDict() => cost;
}