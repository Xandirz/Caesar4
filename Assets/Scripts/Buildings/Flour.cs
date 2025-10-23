using System.Collections.Generic;
using UnityEngine;

public class Flour : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Flour; //!! ЗАПОЛНИТЬ
    
    public Flour()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
            { "Rock", 3 },
            { "Tools", 1 },
            { "People", 5 }
        };
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Flour", 10 }
        };

        upgradeConsumptionLevel1 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel1 = new Dictionary<string, int>
        {
            { "Flour", 20 }  
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Flour2");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}