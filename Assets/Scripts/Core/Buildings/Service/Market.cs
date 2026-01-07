using System.Collections.Generic;
using UnityEngine;

public class Market : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Market;
    public override int buildEffectRadius => 10;

    public override bool RequiresRoadAccess => true;

    private new Dictionary<string, int> cost = new()
    {
        { "Wood", 1 },
        { "Cloth", 1 },
    };


    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterOther(this);
        AllBuildingsManager.Instance.MarkEffectsDirtyAround(gridPos, buildEffectRadius);

    CreateStopSign();

        base.OnPlaced();
    }

    public override void OnRemoved()
    {
        AllBuildingsManager.Instance.UnregisterOther(this);
        AllBuildingsManager.Instance.MarkEffectsDirtyAround(gridPos, buildEffectRadius);

        if (stopSignInstance != null)
            Destroy(stopSignInstance);

        base.OnRemoved();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        MouseHighlighter.Instance.ShowEffectRadius(gridPos, buildEffectRadius);
    }

    public override void OnRoadAccessChanged(bool hasAccess)
    {
        if (stopSignInstance != null)
            stopSignInstance.SetActive(!hasAccess);

        if (BuildManager.Instance != null)
            BuildManager.Instance.CheckEffectsForHousesInRadius(gridPos, buildEffectRadius);
    }
    
    
}