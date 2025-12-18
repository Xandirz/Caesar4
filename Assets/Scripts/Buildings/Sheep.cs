using System.Collections.Generic;
using UnityEngine;

public class Sheep : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Sheep; //!! ЗАПОЛНИТЬ
    
    public Sheep()
    {
        cost = new Dictionary<string,int>
        {
            { "Wood", 1 },
            { "Tools", 1 },
           
        };
        
        workersRequired = 2;
        consumptionCost = new Dictionary<string, int>
        {
            { "Wheat", 1 },
            { "Tools", 1 },
        };
        
        production = new Dictionary<string, int>
        {
            { "Meat", 1 },
            { "Wool", 1 },
            { "Hide", 1 },
            { "Milk", 2 },
            { "Bone", 2 },
            { "Manure", 1 },

            
        };
    }
    private void Awake()
    {
        requiresRoadAccess = false;
    }
    

}