using System.Collections.Generic;
using UnityEngine;

public class Fish : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Fish; //!! ЗАПОЛНИТЬ
    

    public Fish()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 3 },
        };

        needWaterNearby = true;
        
        
        workersRequired = 2;
        
        consumptionCost = new Dictionary<string, int>
        {
            
        };
        
        production = new Dictionary<string, int>
        {
            { "Fish", 10 }
        };


    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl1/Fish");
    }

    
    public override Dictionary<string, int> GetCostDict() => cost;
}