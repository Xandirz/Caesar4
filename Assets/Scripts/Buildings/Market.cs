using UnityEngine;
using System.Collections.Generic;

public class Market : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Market;
    public override int buildEffectRadius => 6;


    private new Dictionary<string,int> cost = new()
    {
        { "Wood", 1 },
        { "Cloth", 1 },
    };
    
    public override void OnPlaced()
    {

        AllBuildingsManager.Instance.RegisterOther(this);
        base.OnPlaced();

    }

    public override void OnRemoved()
    {
        
        AllBuildingsManager.Instance.UnregisterOther(this);
        base.OnRemoved();

    }

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

    public override void OnClicked()
    {
        base.OnClicked();
        MouseHighlighter.Instance.ShowEffectRadius(gridPos, buildEffectRadius);
    }

    

    
}