using System.Collections.Generic;
using UnityEngine;

public class Doctor : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Doctor;
    public override int buildEffectRadius => 14;

    public override bool RequiresRoadAccess => true;

    private new Dictionary<string, int> cost = new()
    {
        { "Brick", 5 },
        { "Wood", 1 },
    };

    private GameObject stopSignInstance;

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }

    public override void OnPlaced()
    {
        AllBuildingsManager.Instance.RegisterOther(this);

        GameObject stopSignPrefab = Resources.Load<GameObject>("stop");
        if (stopSignPrefab != null)
        {
            stopSignInstance = Instantiate(stopSignPrefab, transform);
            stopSignInstance.transform.localPosition = Vector3.up * 0f;
            stopSignInstance.SetActive(true);
        }

        base.OnPlaced();
    }

    public override void OnRemoved()
    {
        AllBuildingsManager.Instance.UnregisterOther(this);

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