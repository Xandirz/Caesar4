using UnityEngine;
using System.Collections.Generic;

public class Warehouse : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Warehouse;
    public override int buildEffectRadius => 3;
    public override int SizeX => 2;
    public override int SizeY => 2;


    private new Dictionary<string,int> cost = new()
    {
        { "Rock", 1 },
    };

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }


    
}