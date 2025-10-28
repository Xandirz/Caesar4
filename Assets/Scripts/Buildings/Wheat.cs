using System;
using System.Collections.Generic;
using UnityEngine;

public class Wheat : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Wheat; //!! ЗАПОЛНИТЬ
    public override int SizeX => 2;
    public override int SizeY => 2;
    public Wheat()
    {
        cost = new Dictionary<string,int>
        {
           
        };
        
        workersRequired = 2;
        consumptionCost = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        production = new Dictionary<string, int>
        {
            { "Wheat", 20 }
        };
        
    }

    private void Awake()
    {
        requiresRoadAccess = false;
    }


    public override Dictionary<string, int> GetCostDict() => cost;
}