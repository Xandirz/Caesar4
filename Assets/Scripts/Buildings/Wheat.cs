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
        
    }

    private void Awake()
    {
        requiresRoadAccess = false;
    }


    public override Dictionary<string, int> GetCostDict() => cost;
}