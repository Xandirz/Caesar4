using UnityEngine;

public class BuildUI : MonoBehaviour
{
    public BuildManager buildManager;

    public void SelectRoad()
    {
        buildManager.SetBuildMode(BuildManager.BuildMode.Road);
    }

    public void SelectHouse()
    {
        buildManager.SetBuildMode(BuildManager.BuildMode.House);
    }

    public void SelectLumberMill()
    {
        buildManager.SetBuildMode(BuildManager.BuildMode.LumberMill);
    }

    public void SelectDemolish()
    {
        buildManager.SetBuildMode(BuildManager.BuildMode.Demolish);
    }
}