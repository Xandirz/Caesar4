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

    public override void OnClicked()
    {
        // передаём в MouseHighlighter позицию колодца и радиус
        MouseHighlighter.Instance.ShowEffectRadius(gridPos, buildEffectRadius);
    }

    
    public override void OnPlaced()
    {
        base.OnPlaced();
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
    }
    
}
