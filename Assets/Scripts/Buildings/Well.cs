using UnityEngine;
using System.Collections.Generic;

public class Well : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Well;
    public override int buildEffectRadius => 3;


    private new Dictionary<string,int> cost = new()
    {
        { "Rock", 1 },
    };

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

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

    public override void OnClicked()
    {
        base.OnClicked();
        MouseHighlighter.Instance.ShowEffectRadius(gridPos, buildEffectRadius);
    }

    

    
}
