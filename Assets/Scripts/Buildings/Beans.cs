using System;
using System.Collections.Generic;
using UnityEngine;

public class Beans : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Beans; //!! ЗАПОЛНИТЬ
    
    public Beans()
    {
        cost = new Dictionary<string,int>
        {
            { "Tools", 1 },
        };
        
        workersRequired = 2;
        
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Beans", 20 }
        };
        
    }

    private void Awake()
    {
        requiresRoadAccess = false;
    }


    public override Dictionary<string, int> GetCostDict() => cost;
}